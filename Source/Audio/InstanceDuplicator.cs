using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.AudioSplitter.Extensions;
using Celeste.Mod.AudioSplitter.Module;
using FMOD;
using FMOD.Studio;

using CelesteAudio = global::Celeste.Audio;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public class InstanceDuplicator
    {
        public static List<InstanceDuplicator> Instances { get; private set; } = new();
        public static List<InstanceDuplicator> InitializedInstances { get => Instances.Where(x => x.Initialized).ToList(); }

        private FMOD.Studio.System system;
        public bool Initialized { get; private set; } = false;

        private Dictionary<EventInstance, EventInstance> duplicateInstances = new();

        public InstanceDuplicator(FMOD.Studio.System system)
        {
            this.system = system;
            Instances.Add(this);
        }

        ~InstanceDuplicator()
        {
            Instances.Remove(this);
        }

        public void Initialize()
        {
            DuplicateExistingInstances();
            Initialized = true;
        }

        public void Terminate()
        {
            Initialized = false;
        }

        public void Clear() => duplicateInstances.Clear();

        public EventInstance GetDuplicate(EventInstance origInst)
        {
            return duplicateInstances.TryGetValue(origInst, out EventInstance inst) ? inst : null;
        }

        public void DestroyDuplicate(EventInstance origInst)
        {
            duplicateInstances[origInst].release();
            duplicateInstances.Remove(origInst);
            Logger.Verbose(nameof(AudioSplitterModule), $"Destroyed duplicate of {origInst.getRaw()}");
        }

        public RESULT DuplicateInstance(Guid origDescGuid, EventInstance origInst)
        {
            RESULT result;

            result = CreateInstance(origDescGuid, out EventInstance duplicateInst);
            if (result != RESULT.OK)
            {
                Logger.Error(nameof(AudioSplitterModule),
                    $"Failed to get create a duplicate instance {AudioExtensions.GetEventPath(origDescGuid)}, orig={origInst.getRaw()}, result: {result}");
                return result;
            }

#if DEBUG
            //system.getEventByID(origDescGuid, out var evt);
            //evt.getPath(out string path);
            //if (path == "event:/env/amb/worldmap" || path == "event:/music/menu/level_select")
            //{
            //    System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            //    Console.WriteLine(t.ToString());
            //}
#endif

            duplicateInstances[origInst] = duplicateInst;
            Logger.Verbose(nameof(AudioSplitterModule),
                $"Created duplicate {AudioExtensions.GetEventPath(origDescGuid)}, orig={origInst.getRaw()}, duplicate={duplicateInst.getRaw()}");
            CopyInstanceState(origInst, duplicateInst);

            return result;
        }

        private void CopyInstanceState(EventInstance original, EventInstance duplicate)
        {
            original.getPitch(out float pitch, out _);
            duplicate.setPitch(pitch);

            original.getTimelinePosition(out int position);
            duplicate.setTimelinePosition(position);

            original.getVolume(out float volume, out _);
            duplicate.setVolume(volume);

            original.get3DAttributes(out var attributes);
            duplicate.set3DAttributes(attributes);

            original.getListenerMask(out uint mask);
            duplicate.setListenerMask(mask);

            original.getUserData(out nint userdata);
            duplicate.setUserData(userdata);

            duplicate.getDescription(out var description);
            description.getParameterCount(out var parameterCount);

            float[] parameterValues = new float[parameterCount];
            for (int index = 0; index < parameterCount; index++)
            {
                original.getParameterValueByIndex(index, out float value, out _);
                parameterValues[index] = value;
            }

            int[] parameterIndices = Enumerable.Range(0, parameterCount).ToArray();
            duplicate.setParameterValuesByIndices(
                parameterIndices,
                parameterValues,
                parameterCount
            );

            foreach (EVENT_PROPERTY property in Enum.GetValues<EVENT_PROPERTY>())
            {
                original.getProperty(property, out float value);
                duplicate.setProperty(property, value);
            }

            original.getPlaybackState(out var state);
            if (state == PLAYBACK_STATE.PLAYING ||
                state == PLAYBACK_STATE.SUSTAINING ||
                state == PLAYBACK_STATE.STARTING)
            {
                duplicate.start();
            }
            else
            {
                duplicate.stop(STOP_MODE.IMMEDIATE);
            }
        }

        private RESULT CreateInstance(Guid id, out EventInstance duplicate)
        {
            duplicate = null;
            RESULT result = system.getEventByID(id, out EventDescription duplicateDescription);
            if (result != RESULT.OK)
                return result;

            duplicateDescription.loadSampleData();

            result = duplicateDescription.createInstance(out duplicate);
            return result;
        }

        private RESULT CreateInstance(EventDescription celesteDescription, out EventInstance duplicate)
        {
            duplicate = null;
            RESULT result = celesteDescription.getID(out Guid id);
            if (result != RESULT.OK)
                return result;

            return CreateInstance(id, out duplicate);
        }

        private void DuplicateExistingInstances()
        {
            HashSet<Bank> loadedBanks = new HashSet<Bank>();
            loadedBanks.UnionWith(CelesteAudio.Banks.Banks.Values);
            loadedBanks.UnionWith(CelesteAudio.Banks.ModCache.Values);

            // Banks may have duplicate event descriptions
            HashSet<EventDescription> descriptions = new();

            foreach (Bank bank in loadedBanks)
            {
                bank.getEventList(out EventDescription[] bankDescriptions);
                foreach (EventDescription backDesc in bankDescriptions)
                    descriptions.Add(backDesc);
            }

            foreach (EventDescription desc in descriptions)
            {
                desc.getID(out Guid id);
                desc.getInstanceList(out EventInstance[] instances);
                foreach (EventInstance inst in instances)
                {
                    RESULT result = DuplicateInstance(id, inst);
                    if (result != RESULT.OK)
                        result.CheckFMOD();
                    //CopyInstanceState(inst, duplicateInstances[inst]);
                }
            }
        }
    }
}

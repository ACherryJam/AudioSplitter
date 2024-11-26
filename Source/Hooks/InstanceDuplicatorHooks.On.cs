using System;
using Celeste.Mod.AudioSplitter.Extensions;
using Celeste.Mod.AudioSplitter.Module;
using FMOD;
using FMOD.Studio;

using CelesteAudio = global::Celeste.Audio;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public static partial class InstanceDuplicatorHooks
    {
        public static void ApplyOn()
        {
            On.FMOD.Studio.EventDescription.createInstance += OnInstanceCreate;

            // TODO: replace these with native hooks
            On.FMOD.Studio.EventInstance.setParameterValue += OnInstanceSetParameterValue;
            On.FMOD.Studio.EventInstance.setParameterValueByIndex += OnInstanceSetParameterValueByIndex;
            On.FMOD.Studio.EventInstance.setParameterValuesByIndices += OnInstanceSetParameterValuesByIndices;

            On.Celeste.Audio.Banks.Load += SetCallbacksToLoadedVanillaBank;
            On.Celeste.Audio.IngestBank += SetCallbacksToLoadedModdedBank;
        }

        public static void RemoveOn()
        {
            On.FMOD.Studio.EventDescription.createInstance -= OnInstanceCreate;

            On.FMOD.Studio.EventInstance.setParameterValue -= OnInstanceSetParameterValue;
            On.FMOD.Studio.EventInstance.setParameterValueByIndex -= OnInstanceSetParameterValueByIndex;
            On.FMOD.Studio.EventInstance.setParameterValuesByIndices -= OnInstanceSetParameterValuesByIndices;

            On.Celeste.Audio.Banks.Load -= SetCallbacksToLoadedVanillaBank;
            On.Celeste.Audio.IngestBank -= SetCallbacksToLoadedModdedBank;
        }

        private static RESULT OnInstanceCreate(On.FMOD.Studio.EventDescription.orig_createInstance orig, EventDescription origDesc, out EventInstance origInst)
        {
            origDesc.getID(out Guid guid);

            RESULT result;
            result = orig(origDesc, out origInst);
            if (result != RESULT.OK)
            {
                Logger.Error(nameof(AudioSplitterModule),
                    $"Failed to create an instance {AudioExtensions.GetEventPath(guid)}, result: {result}");
                return result;
            }

            if (!locker.TryEnter(nameof(OnInstanceCreate), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    result = instanceDuplicater.DuplicateInstance(guid, origInst);
                    result.CheckFMOD();
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceStart(On.FMOD.Studio.EventInstance.orig_start orig, EventInstance inst)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Starting instance {inst.getRaw()}");
            RESULT result = orig(inst);

            if (!locker.TryEnter(nameof(OnInstanceStart), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.start();
                    result.CheckFMOD();
                    Logger.Verbose(nameof(AudioSplitterModule), $"Starting duplicate {duplicate.getRaw()}");
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceStop(On.FMOD.Studio.EventInstance.orig_stop orig, EventInstance inst, STOP_MODE mode)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Stopping instance {inst.getRaw()}");

            RESULT result = orig(inst, mode);

            if (!locker.TryEnter(nameof(OnInstanceStop), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.stop(mode);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceRelease(On.FMOD.Studio.EventInstance.orig_release orig, EventInstance inst)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Releasing instance {inst.getRaw()}");

            RESULT result = orig(inst);

            if (!locker.TryEnter(nameof(OnInstanceRelease), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.release();
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceTriggerCue(On.FMOD.Studio.EventInstance.orig_triggerCue orig, EventInstance inst)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Trigger cue instance {inst.getRaw()}");

            RESULT result = orig(inst);

            if (!locker.TryEnter(nameof(OnInstanceTriggerCue), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.triggerCue();
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSet3DAttributes(
            On.FMOD.Studio.EventInstance.orig_set3DAttributes orig,
            EventInstance inst, FMOD.Studio._3D_ATTRIBUTES attributes
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set 3d instance {inst.getRaw()}");
            RESULT result = orig(inst, attributes);

            if (!locker.TryEnter(nameof(OnInstanceSet3DAttributes), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.set3DAttributes(attributes);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetCallback(
            On.FMOD.Studio.EventInstance.orig_setCallback orig,
            EventInstance inst, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE type
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set callback instance {inst.getRaw()}");

            RESULT result = orig(inst, callback, type);

            if (!locker.TryEnter(nameof(OnInstanceSetCallback), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setCallback(callback, type);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetListenerMask(On.FMOD.Studio.EventInstance.orig_setListenerMask orig, EventInstance inst, uint mask)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set listener mask instance {inst.getRaw()}");
            RESULT result = orig(inst, mask);

            if (!locker.TryEnter(nameof(OnInstanceSetListenerMask), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setListenerMask(mask);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetParameterValue(
            On.FMOD.Studio.EventInstance.orig_setParameterValue orig,
            EventInstance inst, string name, float value
        )
        {
            //Logger.Log(LogLevel.Verbose, nameof(AudioSplitterModule), $"Setting parameter value for instance {inst.getRaw()}, {name}={value}");

            RESULT result = orig(inst, name, value);

            if (!locker.TryEnter(nameof(OnInstanceSetParameterValue), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setParameterValue(name, value);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetParameterValueByIndex(
            On.FMOD.Studio.EventInstance.orig_setParameterValueByIndex orig,
            EventInstance inst, int index, float value
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set parameter by index instance {inst.getRaw()}");
            RESULT result = orig(inst, index, value);

            if (!locker.TryEnter(nameof(OnInstanceSetParameterValueByIndex), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setParameterValueByIndex(index, value);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetParameterValuesByIndices(
            On.FMOD.Studio.EventInstance.orig_setParameterValuesByIndices orig,
            EventInstance inst, int[] indices, float[] values, int count
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set parameters by indices instance {inst.getRaw()}");
            RESULT result = orig(inst, indices, values, count);

            if (!locker.TryEnter(nameof(OnInstanceSetParameterValuesByIndices), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setParameterValuesByIndices(indices, values, count);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetPaused(On.FMOD.Studio.EventInstance.orig_setPaused orig, EventInstance inst, bool paused)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set paused instance {inst.getRaw()}");
            RESULT result = orig(inst, paused);

            if (!locker.TryEnter(nameof(OnInstanceSetPaused), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setPaused(paused);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetPitch(On.FMOD.Studio.EventInstance.orig_setPitch orig, EventInstance inst, float pitch)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set pitch instance {inst.getRaw()}");
            RESULT result = orig(inst, pitch);

            if (!locker.TryEnter(nameof(OnInstanceSetPitch), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setPitch(pitch);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetProperty(
            On.FMOD.Studio.EventInstance.orig_setProperty orig,
            EventInstance inst, EVENT_PROPERTY index, float value
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set property instance {inst.getRaw()}");
            RESULT result = orig(inst, index, value);

            if (!locker.TryEnter(nameof(OnInstanceSetProperty), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setProperty(index, value);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetReverbLevel(
            On.FMOD.Studio.EventInstance.orig_setReverbLevel orig,
            EventInstance inst, int index, float level
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set reverb level instance {inst.getRaw()}");
            RESULT result = orig(inst, index, level);

            if (!locker.TryEnter(nameof(OnInstanceSetReverbLevel), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setReverbLevel(index, level);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetTimelinePosition(
            On.FMOD.Studio.EventInstance.orig_setTimelinePosition orig,
            EventInstance inst, int position
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set timeline instance {inst.getRaw()}");
            RESULT result = orig(inst, position);

            if (!locker.TryEnter(nameof(OnInstanceSetTimelinePosition), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setTimelinePosition(position);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetUserData(On.FMOD.Studio.EventInstance.orig_setUserData orig, EventInstance inst, nint data)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set userdata instance {inst.getRaw()}");
            RESULT result = orig(inst, data);

            if (!locker.TryEnter(nameof(OnInstanceSetUserData), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setUserData(data);
                }

            return RESULT.OK;
        }

        private static RESULT OnInstanceSetVolume(On.FMOD.Studio.EventInstance.orig_setVolume orig, EventInstance inst, float volume)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set volume instance {inst.getRaw()}");
            RESULT result = orig(inst, volume);

            if (!locker.TryEnter(nameof(OnInstanceSetVolume), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(inst);
                    if (duplicate == null) continue;

                    result = duplicate.setVolume(volume);
                }

            return RESULT.OK;
        }

        private static void SetCallbacksToBank(Bank bank)
        {
            // Apply an empty callback to all EventDescriptions in the bank
            // setCallback hooks will wrap these into EVENT_DESTROYED callbacks
            bank.getEventList(out var descriptions);
            foreach (EventDescription description in descriptions)
            {
                description.setCallback(emptyCallback, 0).CheckFMOD();
            }
        }

        private static Bank SetCallbacksToLoadedModdedBank(On.Celeste.Audio.orig_IngestBank orig, ModAsset asset)
        {
            bool needToSetCallbacks = !CelesteAudio.Banks.ModCache.TryGetValue(asset, out _);

            Bank bank = orig(asset);
            if (needToSetCallbacks)
                SetCallbacksToBank(bank);

            return bank;
        }

        private static Bank SetCallbacksToLoadedVanillaBank(On.Celeste.Audio.Banks.orig_Load orig, string name, bool loadStrings)
        {
            bool needToSetCallbacks = !CelesteAudio.Banks.Banks.TryGetValue(name, out _);

            Bank bank = orig(name, loadStrings);
            if (needToSetCallbacks)
                SetCallbacksToBank(bank);

            return bank;
        }
    }
}

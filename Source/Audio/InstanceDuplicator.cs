using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static List<InstanceDuplicator> ActiveDuplicaters { get => Instances.Where(x => x.IsActive).ToList(); }

        private FMOD.Studio.System system;

        public bool IsActive { get; private set; } = false;

        private Dictionary<IntPtr, EventInstance> duplicateInstances = new();

        public InstanceDuplicator(FMOD.Studio.System system) 
        {
            this.system = system;
            Instances.Add(this);
        }

        ~InstanceDuplicator()
        {
            Instances.Remove(this);
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Clear() => duplicateInstances.Clear();
        
        public EventInstance GetDuplicate(EventInstance origInst)
        {
            return GetDuplicate(origInst.getRaw());
        }

        public EventInstance GetDuplicate(IntPtr origInstPtr)
        {
            return duplicateInstances.TryGetValue(origInstPtr, out EventInstance inst) ? inst : null;
        }

        public RESULT DuplicateInstance(Guid origDescGuid, IntPtr origInstPtr)
        {
            RESULT result;

            result = CreateInstance(origDescGuid, out EventInstance duplicateInst);
            if (result != RESULT.OK)
            {
                Logger.Error(nameof(AudioSplitterModule),
                    $"Failed to get create a duplicate instance {AudioExtensions.GetEventPath(origDescGuid)}, orig={origInstPtr}, result: {result}");
                return result;
            }

            duplicateInstances[origInstPtr] = duplicateInst;
            Logger.Verbose(nameof(AudioSplitterModule),
                $"Created instance {AudioExtensions.GetEventPath(origDescGuid)}, orig={origInstPtr}, duplicate={duplicateInst.getRaw()}");

            return result;
        }

        private RESULT CreateInstance(Guid id, out EventInstance duplicate)
        {
            duplicate = null;
            RESULT result = system.getEventByID(id, out EventDescription duplicateDescription);
            if (result != RESULT.OK)
                return result;

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

        /// <summary>
        /// Adds a DESTROYED check to an user-defined event callback
        /// Callbacks are the only way to know when instance is destroyed and we don't want to fully override user callbacks
        /// </summary>
        /// <param name="callback">Original event callback to be set</param>
        /// <param name="callbackmask">Original callback bitmask</param>
        /// <returns>Wrapped event callback, modified callback bitmask with added callback types</returns>
        public (EVENT_CALLBACK, EVENT_CALLBACK_TYPE) WrapEventCallback(EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask)
        {
            bool expectesDestroyed = (callbackmask & EVENT_CALLBACK_TYPE.DESTROYED) == EVENT_CALLBACK_TYPE.DESTROYED;

            EVENT_CALLBACK wrappedCallback = (type, eventInstance, parameters) =>
            {
                if (IsActive)
                {
                    if (type == EVENT_CALLBACK_TYPE.DESTROYED)
                        duplicateInstances.Remove(eventInstance);
                }

                if (!expectesDestroyed)
                    return RESULT.OK;

                return callback(type, eventInstance, parameters);
            };

            return (wrappedCallback, callbackmask | EVENT_CALLBACK_TYPE.DESTROYED);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Celeste.Mod.AudioSplitter.Extensions;
using Celeste.Mod.AudioSplitter.Module;
using Celeste.Mod.AudioSplitter.Utility;
using FMOD;
using FMOD.Studio;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AudioSplitter.Audio
{
    /// <summary>
    /// It is recommended to make hooks static to increase performance
    /// since instance hooks are much complicated behind the curtains of MonoMod
    /// <see href="https://discord.com/channels/403698615446536203/1305446781076770958/1305448143818719242"/>
    /// </summary>
    public static class InstanceDuplicatorHooks
    {
        private static RecursionLocker locker = new();

        public static void Apply()
        {
            On.FMOD.Studio.EventDescription.createInstance += OnInstanceCreate;

            On.FMOD.Studio.EventInstance.start += OnInstanceStart;
            On.FMOD.Studio.EventInstance.stop += OnInstanceStop;
            On.FMOD.Studio.EventInstance.release += OnInstanceRelease;
            On.FMOD.Studio.EventInstance.triggerCue += OnInstanceTriggerCue;
            On.FMOD.Studio.EventInstance.set3DAttributes += OnInstanceSet3DAttributes;
            On.FMOD.Studio.EventInstance.setListenerMask += OnInstanceSetListenerMask;
            On.FMOD.Studio.EventInstance.setParameterValue += OnInstanceSetParameterValue;
            On.FMOD.Studio.EventInstance.setParameterValueByIndex += OnInstanceSetParameterValueByIndex;
            On.FMOD.Studio.EventInstance.setParameterValuesByIndices += OnInstanceSetParameterValuesByIndices;
            On.FMOD.Studio.EventInstance.setPaused += OnInstanceSetPaused;
            On.FMOD.Studio.EventInstance.setPitch += OnInstanceSetPitch;
            On.FMOD.Studio.EventInstance.setProperty += OnInstanceSetProperty;
            On.FMOD.Studio.EventInstance.setReverbLevel += OnInstanceSetReverbLevel;
            On.FMOD.Studio.EventInstance.setTimelinePosition += OnInstanceSetTimelinePosition;
            On.FMOD.Studio.EventInstance.setUserData += OnInstanceSetUserData;
            On.FMOD.Studio.EventInstance.setVolume += OnInstanceSetVolume;

            On.FMOD.Studio.EventInstance.setCallback += OnInstanceSetCallback;

            // We want to run the wrapper last, otherwise we'll set the wrapped callback to the duplicate instances
            // setCallback call -> duplicate setCallback -> Wrapper -(wrapped callback)-> original call
            var config = new DetourConfig(nameof(InstanceDuplicatorHooks), after: new List<string> { "*" });
            var context = new DetourConfigContext(config);
            using (var scope = context.Use())
            {
                On.FMOD.Studio.EventInstance.setCallback += WrapEventInstanceCallback;
                On.FMOD.Studio.EventDescription.setCallback += WrapEventDescriptionCallback;
            }
        }

        public static void Remove()
        {
            On.FMOD.Studio.EventDescription.createInstance -= OnInstanceCreate;

            On.FMOD.Studio.EventInstance.start -= OnInstanceStart;
            On.FMOD.Studio.EventInstance.stop -= OnInstanceStop;
            On.FMOD.Studio.EventInstance.release -= OnInstanceRelease;
            On.FMOD.Studio.EventInstance.triggerCue -= OnInstanceTriggerCue;
            On.FMOD.Studio.EventInstance.set3DAttributes -= OnInstanceSet3DAttributes;
            On.FMOD.Studio.EventInstance.setListenerMask -= OnInstanceSetListenerMask;
            On.FMOD.Studio.EventInstance.setParameterValue -= OnInstanceSetParameterValue;
            On.FMOD.Studio.EventInstance.setParameterValueByIndex -= OnInstanceSetParameterValueByIndex;
            On.FMOD.Studio.EventInstance.setParameterValuesByIndices -= OnInstanceSetParameterValuesByIndices;
            On.FMOD.Studio.EventInstance.setPaused -= OnInstanceSetPaused;
            On.FMOD.Studio.EventInstance.setPitch -= OnInstanceSetPitch;
            On.FMOD.Studio.EventInstance.setProperty -= OnInstanceSetProperty;
            On.FMOD.Studio.EventInstance.setReverbLevel -= OnInstanceSetReverbLevel;
            On.FMOD.Studio.EventInstance.setTimelinePosition -= OnInstanceSetTimelinePosition;
            On.FMOD.Studio.EventInstance.setUserData -= OnInstanceSetUserData;
            On.FMOD.Studio.EventInstance.setVolume -= OnInstanceSetVolume;

            On.FMOD.Studio.EventInstance.setCallback -= OnInstanceSetCallback;

            On.FMOD.Studio.EventInstance.setCallback -= WrapEventInstanceCallback;
            On.FMOD.Studio.EventDescription.setCallback -= WrapEventDescriptionCallback;
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

        private static List<GCHandle> wrapperHandles = new();

        /// <summary>
        /// Adds a DESTROYED check to an user-defined event callback
        /// Callbacks are the only way to know when instance is destroyed and we don't want to fully override user callbacks
        /// </summary>
        /// <param name="callback">Original event callback to be set</param>
        /// <param name="callbackmask">Original callback bitmask</param>
        /// <returns>Wrapped event callback, modified callback bitmask with added callback types</returns>
        public static Tuple<EVENT_CALLBACK, EVENT_CALLBACK_TYPE> WrapEventCallback(EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask)
        {
            Logger.Verbose(nameof(AudioSplitterModule), "Wrapping event callback");
            bool expectesDestroyed = (callbackmask & EVENT_CALLBACK_TYPE.DESTROYED) == EVENT_CALLBACK_TYPE.DESTROYED;

            EVENT_CALLBACK wrappedCallback = (type, instancePtr, parameters) =>
            {
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    if (type == EVENT_CALLBACK_TYPE.DESTROYED)
                        instanceDuplicater.DestroyDuplicate(new EventInstance(instancePtr));
                }

                if (!expectesDestroyed)
                    return RESULT.OK;

                return callback(type, instancePtr, parameters);
            };
            wrapperHandles.Add(GCHandle.Alloc(wrappedCallback));

            return new Tuple<EVENT_CALLBACK, EVENT_CALLBACK_TYPE>(wrappedCallback, callbackmask | EVENT_CALLBACK_TYPE.DESTROYED);
        }

        /// <see cref="InstanceDuplicator.WrapEventCallback">
        private static RESULT WrapEventInstanceCallback(
            On.FMOD.Studio.EventInstance.orig_setCallback setCallback,
            EventInstance eventInstance, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask
        )
        {
            var wrapped = WrapEventCallback(callback, callbackmask);
            return setCallback(eventInstance, wrapped.Item1, wrapped.Item2);
        }

        private static RESULT WrapEventDescriptionCallback(
            On.FMOD.Studio.EventDescription.orig_setCallback setCallback,
            EventDescription eventDescription, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask
        )
        {
            var wrapped = WrapEventCallback(callback, callbackmask);
            return setCallback(eventDescription, wrapped.Item1, wrapped.Item2);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Celeste.Mod.AudioSplitter.Module;
using Celeste.Mod.AudioSplitter.Utility;
using FMOD;
using FMOD.Studio;

namespace Celeste.Mod.AudioSplitter.Audio
{
    /// <summary>
    /// It is recommended to make hooks static to increase performance
    /// since instance hooks are much complicated behind the curtains of MonoMod
    /// <see href="https://discord.com/channels/403698615446536203/1305446781076770958/1305448143818719242"/>
    /// </summary>
    public static partial class InstanceDuplicatorHooks
    {
        private static RecursionLocker locker = new();

        public static void Apply()
        {
            ApplyOn();
            ApplyNative();
        }

        public static void Remove()
        {
            RemoveOn();
            RemoveNative();
        }

        private static EVENT_CALLBACK emptyCallback = (type, eventInstance, parameters) => RESULT.OK;
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
    }
}

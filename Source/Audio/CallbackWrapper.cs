using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Celeste.Mod.AudioSplitter.Module;
using FMOD;
using FMOD.Studio;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public static class CallbackWrapper
    {
        private static EVENT_CALLBACK emptyCallback = (type, eventInstance, parameters) => RESULT.OK;

        private static Dictionary<IntPtr, GCHandle> instanceWrapperHandles = new();
        private static Dictionary<IntPtr, GCHandle> descriptionWrapperHandles = new();

        public static void RemoveInstanceWrapperHandle(IntPtr eventInstance)
        {
            if (instanceWrapperHandles.TryGetValue(eventInstance, out var handle))
            {
                handle.Free();
                instanceWrapperHandles.Remove(eventInstance);
            }
        }

        public static void RemoveDescriptionWrapperHandle(IntPtr eventDescription) 
        {
            if (descriptionWrapperHandles.TryGetValue(eventDescription, out var handle)) 
            {
                handle.Free();
                descriptionWrapperHandles.Remove(eventDescription);
            }
        }

        public static void FreeCallbackWrapperGCHandles()
        {
            foreach (var handle in instanceWrapperHandles.Values)
                handle.Free();
            instanceWrapperHandles.Clear();

            foreach (var handle in descriptionWrapperHandles.Values)
                handle.Free();
            descriptionWrapperHandles.Clear();
        }

        /// <summary>
        /// Adds a DESTROYED check to an user-defined event callback
        /// Callbacks are the only way to know when instance is destroyed and we don't want to fully override user callbacks
        /// </summary>
        /// <param name="callback">Original event callback to be set</param>
        /// <param name="callbackmask">Original callback bitmask</param>
        /// <returns>Wrapped event callback, modified callback bitmask with added callback types</returns>
        private static Tuple<EVENT_CALLBACK, EVENT_CALLBACK_TYPE> WrapEventCallback(EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask)
        {
            Logger.Verbose(nameof(AudioSplitterModule), "Wrapping event callback");
            bool expectesDestroyed = (callbackmask & EVENT_CALLBACK_TYPE.DESTROYED) == EVENT_CALLBACK_TYPE.DESTROYED;

            EVENT_CALLBACK wrappedCallback = (type, instancePtr, parameters) =>
            {
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    if (type == EVENT_CALLBACK_TYPE.DESTROYED)
                    {
                        instanceDuplicater.DestroyDuplicate(new EventInstance(instancePtr));
                        RemoveInstanceWrapperHandle(instancePtr);
                    }
                }

                if (!expectesDestroyed)
                    return RESULT.OK;

                return callback(type, instancePtr, parameters);
            };

            return new Tuple<EVENT_CALLBACK, EVENT_CALLBACK_TYPE>(wrappedCallback, callbackmask | EVENT_CALLBACK_TYPE.DESTROYED);
        }

        public static Tuple<EVENT_CALLBACK, EVENT_CALLBACK_TYPE> WrapInstanceEventCallback(
            IntPtr eventInstance, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask
        )
        {
            var wrapped = WrapEventCallback(callback, callbackmask);
            RemoveInstanceWrapperHandle(eventInstance);
            instanceWrapperHandles[eventInstance] = GCHandle.Alloc(wrapped.Item1);

            return wrapped;
        }

        public static Tuple<EVENT_CALLBACK, EVENT_CALLBACK_TYPE> WrapDescriptionEventCallback(
            IntPtr eventDescription, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask
        )
        {
            var wrapped = WrapEventCallback(callback, callbackmask);
            RemoveDescriptionWrapperHandle(eventDescription);
            descriptionWrapperHandles[eventDescription] = GCHandle.Alloc(wrapped.Item1);

            return wrapped;
        }

        public static void SetCallbacksToBank(Bank bank)
        {
            // Apply an empty callback to all EventDescriptions in the bank
            // setCallback hooks will wrap these into EVENT_DESTROYED callbacks
            bank.getEventList(out var descriptions);
            foreach (EventDescription description in descriptions)
            {
                description.setCallback(emptyCallback, 0).CheckFMOD();
            }
        }
    }
}

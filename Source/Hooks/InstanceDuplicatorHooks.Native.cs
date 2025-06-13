using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Celeste.Mod.AudioSplitter.Module;
using FMOD;
using FMOD.Studio;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public static partial class InstanceDuplicatorHooks
    {
        internal static IntPtr fmodLib;
        private static List<NativeHook> refs = new();

        /// <summary>
        /// Native hooks of EventInstance extern functions. Needed because inlining.
        /// </summary>
        private static void ApplyNative()
        {
            fmodLib = DynDll.OpenLibrary("fmodstudio");

            // TODO: Hook other potentially inlinable functions
            List<Tuple<IntPtr, Delegate>> methods = new()
            {
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_Start"), new Sigs.Inst.HookedStart(NativeInstanceStart)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_Stop"), new Sigs.Inst.HookedStop(NativeInstanceStop)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_Release"), new Sigs.Inst.HookedRelease(NativeInstanceRelease)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_Set3DAttributes"), new Sigs.Inst.HookedSet3DAttributes(NativeInstanceSet3DAttributes)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetCallback"), new Sigs.Inst.HookedSetCallback(NativeInstanceSetCallback)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetListenerMask"), new Sigs.Inst.HookedSetListenerMask(NativeInstanceSetListenerMask)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetParameterValue"), new Sigs.Inst.HookedSetParameterValue(NativeInstanceSetParameterValue)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetParameterValueByIndex"), new Sigs.Inst.HookedSetParameterValueByIndex(NativeInstanceSetParameterValueByIndex)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetParameterValuesByIndices"), new Sigs.Inst.HookedSetParameterValuesByIndices(NativeInstanceSetParameterValuesByIndices)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetPaused"), new Sigs.Inst.HookedSetPaused(NativeInstanceSetPaused)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetPitch"), new Sigs.Inst.HookedSetPitch(NativeInstanceSetPitch)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetProperty"), new Sigs.Inst.HookedSetProperty(NativeInstanceSetProperty)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetReverbLevel"), new Sigs.Inst.HookedSetReverbLevel(NativeInstanceSetReverbLevel)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetTimelinePosition"), new Sigs.Inst.HookedSetTimelinePosition(NativeInstanceSetTimelinePosition)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetUserData"), new Sigs.Inst.HookedSetUserData(NativeInstanceSetUserData)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetVolume"), new Sigs.Inst.HookedSetVolume(NativeInstanceSetVolume)),
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_TriggerCue"), new Sigs.Inst.HookedTriggerCue(NativeInstanceTriggerCue)),
            };

            // We want to run the wrapper last, otherwise we'll set the wrapped callback to the duplicate instances
            // setCallback call -> duplicate setCallback -> Wrapper -(wrapped callback)-> original call
            var afterConfig = new DetourConfig(nameof(InstanceDuplicatorHooks), after: new List<string> { "*" });
            List<Tuple<IntPtr, Delegate>> afterMethods = new()
            {
                new(fmodLib.GetExport("FMOD_Studio_EventInstance_SetCallback"), new Sigs.Inst.HookedSetCallback(WrapEventInstanceCallback)),
                new(fmodLib.GetExport("FMOD_Studio_EventDescription_SetCallback"), new Sigs.Desc.HookedSetCallback(WrapEventDescriptionCallback))
            };

            foreach (var method in methods)
                refs.Add(new(method.Item1, method.Item2, true));
            foreach (var method in afterMethods)
                refs.Add(new(method.Item1, method.Item2, afterConfig, true));
        }

        private static void RemoveNative()
        {
            foreach (var hook in refs)
                hook.Dispose();
            DynDll.CloseLibrary(fmodLib);
        }

        private static RESULT NativeInstanceStart(Sigs.Inst.Start orig, IntPtr inst)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Starting instance {inst}");
            RESULT result = orig(inst);

            if (!locker.TryEnter(nameof(NativeInstanceStart), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.start();
                    result.CheckFMOD();
                    Logger.Verbose(nameof(AudioSplitterModule), $"Starting duplicate {duplicate.getRaw()}");
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceStop(Sigs.Inst.Stop orig, IntPtr inst, STOP_MODE stop_mode)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Stopping instance {inst}");

            RESULT result = orig(inst, stop_mode);

            if (!locker.TryEnter(nameof(NativeInstanceStop), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.stop(stop_mode);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceRelease(Sigs.Inst.Release orig, IntPtr inst)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Releasing instance {inst}");

            RESULT result = orig(inst);

            if (!locker.TryEnter(nameof(NativeInstanceRelease), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.release();
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceTriggerCue(Sigs.Inst.TriggerCue orig, IntPtr inst)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Trigger cue instance {inst}");

            RESULT result = orig(inst);

            if (!locker.TryEnter(nameof(NativeInstanceTriggerCue), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.triggerCue();
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSet3DAttributes(
            Sigs.Inst.Set3DAttributes orig,
            IntPtr inst, ref FMOD.Studio._3D_ATTRIBUTES attributes
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set 3d instance {inst}");
            RESULT result = orig(inst, ref attributes);

            if (!locker.TryEnter(nameof(NativeInstanceSet3DAttributes), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.set3DAttributes(attributes);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetCallback(
            Sigs.Inst.SetCallback orig,
            IntPtr inst, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE type
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set callback instance {inst}");

            RESULT result = orig(inst, callback, type);

            if (!locker.TryEnter(nameof(NativeInstanceSetCallback), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setCallback(callback, type);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetListenerMask(Sigs.Inst.SetListenerMask orig, IntPtr inst, uint mask)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set listener mask instance {inst}");
            RESULT result = orig(inst, mask);

            if (!locker.TryEnter(nameof(NativeInstanceSetListenerMask), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setListenerMask(mask);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetParameterValue(
            Sigs.Inst.SetParameterValue orig,
            IntPtr inst, IntPtr name, float value
        )
        {
            string nameValue = Marshal.PtrToStringUTF8(name);
            Logger.Verbose(nameof(AudioSplitterModule), $"Setting parameter value for instance {inst}, {nameValue}={value}");
            
            RESULT result = orig(inst, name, value);
            if (result != RESULT.OK)
            {
                Logger.Error(nameof(AudioSplitterModule), $"Set parameter error {Enum.GetName(typeof(RESULT), result)}");
                return result;
            }

            if (!locker.TryEnter(nameof(NativeInstanceSetParameterValue), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setParameterValue(nameValue, value);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetParameterValueByIndex(
            Sigs.Inst.SetParameterValueByIndex orig,
            IntPtr inst, int index, float value
        )
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set parameter by index instance {inst}");
            RESULT result = orig(inst, index, value);
            if (result != RESULT.OK)
            {
                Logger.Error(nameof(AudioSplitterModule), $"Set parameter by index error {Enum.GetName(typeof(RESULT), result)}");
                return result;
            }

            if (!locker.TryEnter(nameof(NativeInstanceSetParameterValueByIndex), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setParameterValueByIndex(index, value);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetParameterValuesByIndices(
            Sigs.Inst.SetParameterValuesByIndices orig,
            IntPtr inst, IntPtr indicesPtr, IntPtr valuesPtr, int count
        )
        {
            int[] indices = new int[count];
            float[] values = new float[count];

            if (indicesPtr != IntPtr.Zero)
                Marshal.Copy(indicesPtr, indices, 0, count);
            if (valuesPtr != IntPtr.Zero)
                Marshal.Copy(valuesPtr, values, 0, count);
            
            Logger.Verbose(nameof(AudioSplitterModule), $"Set parameters by indices instance {inst}");
            RESULT result = orig(inst, indicesPtr, valuesPtr, count);
            if (result != RESULT.OK)
            {
                Logger.Error(nameof(AudioSplitterModule), $"Set parameter by indices error {Enum.GetName(typeof(RESULT), result)}");
                return result;
            }

            if (!locker.TryEnter(nameof(NativeInstanceSetParameterValuesByIndices), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setParameterValuesByIndices(indices, values, count);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetPaused(Sigs.Inst.SetPaused orig, IntPtr inst, bool paused)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set paused instance {inst}");
            RESULT result = orig(inst, paused);

            if (!locker.TryEnter(nameof(NativeInstanceSetPaused), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setPaused(paused);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetPitch(Sigs.Inst.SetPitch orig, IntPtr inst, float pitch)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set pitch instance {inst}");
            RESULT result = orig(inst, pitch);

            if (!locker.TryEnter(nameof(NativeInstanceSetPitch), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setPitch(pitch);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetProperty(Sigs.Inst.SetProperty orig, IntPtr inst, EVENT_PROPERTY index, float value)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set property instance {inst}");
            RESULT result = orig(inst, index, value);

            if (!locker.TryEnter(nameof(NativeInstanceSetProperty), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setProperty(index, value);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetReverbLevel(Sigs.Inst.SetReverbLevel orig, IntPtr inst, int index, float level)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set reverb level instance {inst}");
            RESULT result = orig(inst, index, level);

            if (!locker.TryEnter(nameof(NativeInstanceSetReverbLevel), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setReverbLevel(index, level);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetTimelinePosition(Sigs.Inst.SetTimelinePosition orig, IntPtr inst, int position)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set timeline instance {inst}");
            RESULT result = orig(inst, position);

            if (!locker.TryEnter(nameof(NativeInstanceSetTimelinePosition), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setTimelinePosition(position);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetUserData(Sigs.Inst.SetUserData orig, IntPtr inst, nint data)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set userdata instance {inst}");
            RESULT result = orig(inst, data);

            if (!locker.TryEnter(nameof(NativeInstanceSetUserData), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setUserData(data);
                }

            return RESULT.OK;
        }

        private static RESULT NativeInstanceSetVolume(Sigs.Inst.SetVolume orig, IntPtr inst, float volume)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Set volume instance {inst}");
            RESULT result = orig(inst, volume);

            if (!locker.TryEnter(nameof(NativeInstanceSetVolume), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    var duplicate = instanceDuplicater.GetDuplicate(new EventInstance(inst));
                    if (duplicate == null) continue;

                    result = duplicate.setVolume(volume);
                }

            return RESULT.OK;
        }

        /// <see cref="InstanceDuplicator.WrapEventCallback">
        private static RESULT WrapEventInstanceCallback(
            Sigs.Inst.SetCallback setCallback,
            IntPtr eventInstance, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask
        )
        {
            var wrapped = CallbackWrapper.WrapInstanceEventCallback(eventInstance, callback, callbackmask);
            return setCallback(eventInstance, wrapped.Item1, wrapped.Item2);
        }

        private static RESULT WrapEventDescriptionCallback(
            Sigs.Desc.SetCallback setCallback,
            IntPtr eventDescription, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask
        )
        {
            var wrapped = CallbackWrapper.WrapDescriptionEventCallback(eventDescription, callback, callbackmask);
            return setCallback(eventDescription, wrapped.Item1, wrapped.Item2);
        }

        internal static class Sigs
        {
            public static class Inst
            {
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT Release(IntPtr inst);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedRelease(Release orig, IntPtr inst);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT Set3DAttributes(IntPtr inst, ref FMOD.Studio._3D_ATTRIBUTES attributes);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSet3DAttributes(Set3DAttributes orig, IntPtr inst, ref FMOD.Studio._3D_ATTRIBUTES attributes);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetCallback(IntPtr inst, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetCallback(SetCallback orig, IntPtr inst, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetListenerMask(IntPtr inst, uint mask);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetListenerMask(SetListenerMask orig, IntPtr inst, uint mask);

                // Have to make `name` IntPtr instead of byte[] because the delegate gets only the first character
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetParameterValue(IntPtr inst, IntPtr name, float value);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetParameterValue(SetParameterValue orig, IntPtr inst, IntPtr name, float value);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetParameterValueByIndex(IntPtr inst, int index, float value);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetParameterValueByIndex(SetParameterValueByIndex orig, IntPtr inst, int index, float value);

                // Have to change arrays to IntPtr because the delegate gets only the first value
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetParameterValuesByIndices(IntPtr inst, IntPtr indicesPtr, IntPtr valuesPtr, int count);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetParameterValuesByIndices(SetParameterValuesByIndices orig, IntPtr inst, IntPtr indicesPtr, IntPtr valuesPtr, int count);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetPaused(IntPtr inst, bool paused);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetPaused(SetPaused orig, IntPtr inst, bool paused);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetPitch(IntPtr inst, float pitch);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetPitch(SetPitch orig, IntPtr inst, float pitch);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetProperty(IntPtr inst, EVENT_PROPERTY index, float value);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetProperty(SetProperty orig, IntPtr inst, EVENT_PROPERTY index, float value);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetReverbLevel(IntPtr inst, int index, float level);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetReverbLevel(SetReverbLevel orig, IntPtr inst, int index, float level);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetTimelinePosition(IntPtr inst, int position);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetTimelinePosition(SetTimelinePosition orig, IntPtr inst, int position);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetUserData(IntPtr inst, IntPtr userdata);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetUserData(SetUserData orig, IntPtr inst, IntPtr userdata);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetVolume(IntPtr inst, float volume);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetVolume(SetVolume orig, IntPtr inst, float volume);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT Start(IntPtr inst);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedStart(Start orig, IntPtr inst);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT Stop(IntPtr inst, STOP_MODE mode);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedStop(Stop orig, IntPtr inst, STOP_MODE mode);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT TriggerCue(IntPtr inst);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedTriggerCue(TriggerCue orig, IntPtr inst);
            }

            public static class Desc
            {
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT SetCallback(IntPtr desc, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask);
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate RESULT HookedSetCallback(SetCallback orig, IntPtr desc, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask);
            }
        }
    }
}

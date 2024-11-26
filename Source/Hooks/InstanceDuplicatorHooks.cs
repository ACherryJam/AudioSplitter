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

        [ApplyOnLoad]
        public static void Apply()
        {
            ApplyOn();
            ApplyNative();
        }

        [RemoveOnUnload]
        public static void Remove()
        {
            RemoveOn();
            RemoveNative();
        }
    }
}

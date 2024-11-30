using System.Collections.Generic;
using Celeste.Mod.AudioSplitter.Module;
using Celeste.Mod.AudioSplitter.Utility;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.AudioSplitter.Volume
{
    public static class VolumeChangeInputListener
    {
        public static readonly ChannelInputListener SFX = new(
            AudioSplitterModule.Settings.DecreaseSFXVolumeBinding,    
            AudioSplitterModule.Settings.IncreaseSFXVolumeBinding
        );

        public static readonly ChannelInputListener Music = new(
            AudioSplitterModule.Settings.DecreaseMusicVolumeBinding,
            AudioSplitterModule.Settings.IncreaseMusicVolumeBinding
        );

        public static readonly List<ChannelInputListener> Listeners = new() { SFX, Music };

        private static void ConsumeBindingPresses()
        {
            foreach (var channel in Listeners)
            {
                channel.DecreaseBinding.ConsumePress();
                channel.IncreaseBinding.ConsumePress();
            }
        }

        internal static class Hooks
        {
            [ApplyOnLoad]
            public static void Apply()
            {
                On.Celeste.KeyboardConfigUI.AddRemap_Keys += ConsumeKeyboardBindingPress;
                On.Celeste.ButtonConfigUI.AddRemap += ConsumeControllerBindingPress;
            }

            [RemoveOnUnload]
            public static void Remove()
            {
                On.Celeste.KeyboardConfigUI.AddRemap_Keys -= ConsumeKeyboardBindingPress;
                On.Celeste.ButtonConfigUI.AddRemap -= ConsumeControllerBindingPress;
            }

            private static void ConsumeKeyboardBindingPress(On.Celeste.KeyboardConfigUI.orig_AddRemap_Keys orig, KeyboardConfigUI self, Microsoft.Xna.Framework.Input.Keys key)
            {
                orig(self, key);
                ConsumeBindingPresses();
            }

            private static void ConsumeControllerBindingPress(On.Celeste.ButtonConfigUI.orig_AddRemap orig, ButtonConfigUI self, Buttons btn)
            {
                orig(self, btn);
                ConsumeBindingPresses();
            }
        }
    }
}

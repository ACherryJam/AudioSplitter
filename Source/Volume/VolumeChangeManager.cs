using System;
using System.Collections.Generic;
using Celeste.Mod.AudioSplitter.Utility;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.AudioSplitter.Module;
using System.Linq;

namespace Celeste.Mod.AudioSplitter.Volume
{
    public static class VolumeChangeManager
    {
        private static AudioSplitterModuleSettings ModuleSettings => AudioSplitterModule.Settings;

        public static bool Initialized { get; private set; } = false;

        public static void Initialize()
        {
            VolumeChangeInputListener.Music.OnDecrease += () => MenuOptions.SetMusic(ClampVolume(Settings.Instance.MusicVolume - 1));
            VolumeChangeInputListener.Music.OnIncrease += () => MenuOptions.SetMusic(ClampVolume(Settings.Instance.MusicVolume + 1));

            VolumeChangeInputListener.SFX.OnDecrease += () => MenuOptions.SetSfx(ClampVolume(Settings.Instance.SFXVolume - 1));
            VolumeChangeInputListener.SFX.OnIncrease += () => MenuOptions.SetSfx(ClampVolume(Settings.Instance.SFXVolume + 1));

            Initialized = true;
        }

        public static void Update()
        {
            if (Initialized)
            {
                foreach (var channel in VolumeChangeInputListener.Listeners)
                    channel.Update();
            }
        }

        public static int ClampVolume(int volume) => Math.Clamp(volume, 0, 10);
        
        internal static class Hooks
        {
            [ApplyOnLoad]
            public static void Apply()
            {
                On.Celeste.Celeste.Update += CallControllerUpdate;

                On.Celeste.MenuOptions.SetSfx += UpdateSFXSlider;
                On.Celeste.MenuOptions.SetMusic += UpdateMusicSlider;
            }

            [RemoveOnUnload]
            public static void Remove()
            {
                On.Celeste.Celeste.Update -= CallControllerUpdate;

                On.Celeste.MenuOptions.SetSfx -= UpdateSFXSlider;
                On.Celeste.MenuOptions.SetMusic -= UpdateMusicSlider;
            }

            private static void CallControllerUpdate(On.Celeste.Celeste.orig_Update orig, Celeste self, GameTime gametime)
            {
                orig(self, gametime);
                Update();
            }

            private static void UpdateMusicSlider(On.Celeste.MenuOptions.orig_SetMusic orig, int volume)
            {
                if (MenuOptions.menu != null)
                {
                    var sliders = MenuOptions.menu.Items.Where(i => i is TextMenu.Slider).ToList().ConvertAll(i => (TextMenu.Slider)i);
                    var slider = sliders.FirstOrDefault(i => i.Label == Dialog.Clean("options_music", null));
                    if (slider != default)
                    {
                        slider.Index = volume;
                    }
                }
                orig(volume);
            }

            private static void UpdateSFXSlider(On.Celeste.MenuOptions.orig_SetSfx orig, int volume)
            {
                if (MenuOptions.menu != null)
                {
                    var sliders = MenuOptions.menu.Items.Where(i => i is TextMenu.Slider).ToList().ConvertAll(i => (TextMenu.Slider)i);
                    var slider = sliders.FirstOrDefault(i => i.Label == Dialog.Clean("options_sounds", null));
                    if (slider != default)
                    {
                        slider.Index = volume;
                    }
                }
                orig(volume);
            }
        }
    }
}

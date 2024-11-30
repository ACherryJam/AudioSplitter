using System;
using Celeste.Mod.AudioSplitter.Audio;

namespace Celeste.Mod.AudioSplitter.Module
{
    public class AudioSplitterModuleSettings : EverestModuleSettings
    {
        // ===== Audio Device Selection ===== //
        public OutputDeviceInfo AudioOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;

        public OutputDeviceInfo SFXOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;
        public OutputDeviceInfo MusicOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;

        // ===== Audio Splitting ===== //
        public bool EnableOnStartup { get; set; } = false;

        // ===== Volume Changing ===== //
        public ButtonBinding DecreaseSFXVolumeBinding { get; set; }
        public ButtonBinding IncreaseSFXVolumeBinding { get; set; }

        public ButtonBinding DecreaseMusicVolumeBinding { get; set; }
        public ButtonBinding IncreaseMusicVolumeBinding { get; set; }
    }
}
using Celeste.Mod.AudioSplitter.Audio;

namespace Celeste.Mod.AudioSplitter.Module
{
    public class AudioSplitterModuleSettings : EverestModuleSettings
    {
        public enum Version : uint
        {
            Initial = 0,
            NullDeviceNameFix = 1
        }

        // ===== Audio Device Selection ===== //
        public OutputDeviceInfo AudioOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;

        public OutputDeviceInfo SFXOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;
        public OutputDeviceInfo MusicOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;

        // ===== Audio Splitting ===== //
        public bool EnableOnStartup { get; set; } = false;

        // ===== Migration ===== //
        public Version SettingsVersion { get; set; } = Version.Initial;
    }
}
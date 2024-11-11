using Celeste.Mod.AudioSplitter.Audio;

namespace Celeste.Mod.AudioSplitter.Module
{
    public class AudioSplitterModuleSettings : EverestModuleSettings
    {
        // ===== Audio Device Selection ===== //
        public OutputDeviceInfo CelesteAudioOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;
        public OutputDeviceInfo DuplicateAudioOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;

        // ===== Audio Splitting ===== //
        public bool EnableOnStartup { get; set; } = false;
        public bool PlayElevatorMusicWhileLoading { get; set; } = true;
    }
}
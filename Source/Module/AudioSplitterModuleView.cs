using Celeste.Mod.AudioSplitter.Audio;
using Celeste.Mod.AudioSplitter.Extensions;
using Celeste.Mod.AudioSplitter.UI;

namespace Celeste.Mod.AudioSplitter.Module
{
    public class AudioSplitterModuleView
    {
        public DropdownMenu<OutputDeviceInfo> AudioDeviceDropdown, SFXDeviceDropdown, MusicDeviceDropdown;
        public TextMenu.Button ReloadDevicesButton;
        public ConfirmButton ToggleDuplicatorButton;
        public TextMenu.OnOff EnableOnStartupOnOff;

        public AudioSplitterModuleView() => CreateElements();

        public void CreateElements()
        {
            AudioDeviceDropdown = new DropdownMenu<OutputDeviceInfo>(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_AUDIO_DEVICE"));
            SFXDeviceDropdown = new DropdownMenu<OutputDeviceInfo>(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_SFX_DEVICE"));
            MusicDeviceDropdown = new DropdownMenu<OutputDeviceInfo>(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_MUSIC_DEVICE"));

            ToggleDuplicatorButton = new ConfirmButton(default(string));
            ReloadDevicesButton = new TextMenu.Button(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_RELOAD_DEVICE_LIST"));

            EnableOnStartupOnOff = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_ENABLE_ON_STARTUP"), default(bool));
        }

        public void AddTo(TextMenu menu, bool inGame)
        {
            menu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_HEADER_DEVICESELECTION"), false));
            menu.Add(AudioDeviceDropdown);
            menu.Add(SFXDeviceDropdown);
            menu.Add(MusicDeviceDropdown);
            menu.Add(ReloadDevicesButton);
            menu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_HEADER_AUDIOSPLITTING"), false));
            menu.Add(ToggleDuplicatorButton);
            menu.Add(EnableOnStartupOnOff);

            ReloadDevicesButton.AddDescription(menu, Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_RELOAD_DEVICE_LIST_DESCRIPTION"));
            var desc = ReloadDevicesButton.GetDescriptionText();
            if (desc != null)
                desc.IncludeWidthInMeasurement = false;
        }
    }
}

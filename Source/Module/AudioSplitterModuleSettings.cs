using System.Collections.Generic;
using System.Reflection.Emit;
using Celeste.Mod.AudioSplitter.Audio;
using Celeste.Mod.AudioSplitter.UI;

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

        // ===== Menu ===== //
        private bool DuplicatorInitialized => AudioSplitterModule.Instance.Duplicator.Initialized;

        private static void FillDropdownMenu(DropdownMenu<OutputDeviceInfo> dropdown, List<OutputDeviceInfo> devices)
        {
            dropdown.Clear();
            dropdown.Add(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_DEFAULT_DEVICE"), OutputDeviceInfo.DefaultDevice);
            foreach (var info in devices)
                dropdown.Add(info.Name, info);
        }

        private static void UpdateSystemDevice(DropdownMenu<OutputDeviceInfo> dropdown, List<OutputDeviceInfo> devices, FMOD.Studio.System system)
        {
            // It's kind of a mess...

            OutputDeviceInfo device = dropdown.CurrentOption.Value;
            var index = devices.IndexOf(device);
            if (index == -1)
            {
                OutputDeviceInfo.DefaultDevice.Apply(system);
            }
            else
            {
                dropdown.OptionIndex = index + (device != OutputDeviceInfo.DefaultDevice ? 1 : 0);
                dropdown.CurrentOption.Value.Apply(system);
            }
        }

        public void CreateMenu(TextMenu menu, bool inGame)
        {
            // 1. Create menu items
            var audioDevice = new DropdownMenu<OutputDeviceInfo>(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_AUDIO_DEVICE"))
                .Change((device) => {
                    AudioOutputDevice = device;
                    AudioOutputDevice.Apply(global::Celeste.Audio.System);
                });

            var sfxDevice = new DropdownMenu<OutputDeviceInfo>(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_SFX_DEVICE"))
                .Change((device) => {
                    SFXOutputDevice = device;
                    SFXOutputDevice.Apply(global::Celeste.Audio.System); 
                });

            var musicDevice = new DropdownMenu<OutputDeviceInfo>(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_MUSIC_DEVICE"))
                .Change((device) => {
                    MusicOutputDevice = device;
                    MusicOutputDevice.Apply(AudioSplitterModule.Instance.Duplicator.System);
                });

            void ToggleDropdownwVisibility()
            {
                audioDevice.Visible = !DuplicatorInitialized;
                sfxDevice.Visible = DuplicatorInitialized;
                musicDevice.Visible = DuplicatorInitialized;
            }

            var toggleDuplicate = new ConfirmButton(
                Dialog.Clean($"MODOPTIONS_AUDIOSPLITTER_{(DuplicatorInitialized ? "DISABLE_DUPLICATE" : "ENABLE_DUPLICATE")}")
            );
            toggleDuplicate.Pressed(() =>
            {
                AudioSplitterModule.Instance.ToggleAudioDuplicator();
                ToggleDropdownwVisibility();

                var label = DuplicatorInitialized ? "DISABLE_DUPLICATE" : "ENABLE_DUPLICATE";
                toggleDuplicate.Label = Dialog.Clean($"MODOPTIONS_AUDIOSPLITTER_{label}");
            });

            var reloadDevices = new TextMenu.Button(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_RELOAD_DEVICE_LIST"))
                .Pressed(() => { AudioSplitterModule.Instance.DeviceManager.ReloadDeviceList(); });

            // 2. Add items
            menu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_HEADER_DEVICESELECTION"), false));
            menu.Add(audioDevice);
            menu.Add(sfxDevice);
            menu.Add(musicDevice);
            menu.Add(reloadDevices);
            menu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_HEADER_AUDIOSPLITTING"), false));
            menu.Add(toggleDuplicate);
            menu.Add(
                new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_ENABLE_ON_STARTUP"), EnableOnStartup)
                .Change((value) => { EnableOnStartup = value; })
            );

            // 3. Configure items
            // TODO: This description looks ugly as fuck beacuse it renders in left parts
            // so you either do a multiline or a huge one-liner breaks the menu. Write custom description ig?
            reloadDevices.AddDescription(menu, Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_RELOAD_DEVICE_LIST_DESCRIPTION"));
            
            // TODO: Fix dropdowns so that you can fill them without being contained in parent container
            var devices = AudioSplitterModule.Instance.DeviceManager.Devices;
            FillDropdownMenu(audioDevice, devices);
            FillDropdownMenu(musicDevice, devices);
            FillDropdownMenu(sfxDevice, devices);

            audioDevice.OptionIndex = AudioOutputDevice.Index + (AudioOutputDevice != OutputDeviceInfo.DefaultDevice ? 1 : 0);
            musicDevice.OptionIndex = MusicOutputDevice.Index + (MusicOutputDevice != OutputDeviceInfo.DefaultDevice ? 1 : 0);
            sfxDevice.OptionIndex = SFXOutputDevice.Index + (SFXOutputDevice != OutputDeviceInfo.DefaultDevice ? 1 : 0);

            AudioSplitterModule.Instance.DeviceManager.OnListUpdate += (devices) => {
                FillDropdownMenu(audioDevice, devices);
                FillDropdownMenu(musicDevice, devices);
                FillDropdownMenu(sfxDevice, devices);

                // TODO: Move this to a better place??
                if (!DuplicatorInitialized)
                {
                    UpdateSystemDevice(audioDevice, devices, global::Celeste.Audio.System);
                } else
                {
                    UpdateSystemDevice(musicDevice, devices, global::Celeste.Audio.System);
                    UpdateSystemDevice(sfxDevice, devices, AudioSplitterModule.Instance.Duplicator.System);
                }
            };

            ToggleDropdownwVisibility();
        }
    }
}
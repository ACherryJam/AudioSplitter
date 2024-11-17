using System.Reflection.Emit;
using Celeste.Mod.AudioSplitter.Audio;
using Celeste.Mod.AudioSplitter.UI;

namespace Celeste.Mod.AudioSplitter.Module
{
    public class AudioSplitterModuleSettings : EverestModuleSettings
    {
        // ===== Audio Device Selection ===== //
        public OutputDeviceInfo CelesteAudioOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;
        public OutputDeviceInfo DuplicateAudioOutputDevice { get; set; } = OutputDeviceInfo.DefaultDevice;

        // ===== Audio Splitting ===== //
        public bool EnableOnStartup { get; set; } = false;

        // ===== Menu ===== //
        private bool DuplicatorInitialized => AudioSplitterModule.Instance.Duplicator.Initialized;

        private static void FillDropdownMenu(DropdownMenu<OutputDeviceInfo> dropdown)
        {
            dropdown.Clear();

            dropdown.Add(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_DEFAULT_DEVICE"), OutputDeviceInfo.DefaultDevice);
            foreach (var info in AudioSplitterModule.Instance.DeviceManager.Devices)
                dropdown.Add(info.Name, info);
        }

        public void CreateMenu(TextMenu menu, bool inGame)
        {
            // 1. Create menu items
            var celesteDevice = new DropdownMenu<OutputDeviceInfo>(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_CELESTE_DEVICE"))
                .Change((device) => {
                    CelesteAudioOutputDevice = device;
                    CelesteAudioOutputDevice.Apply(global::Celeste.Audio.System);
                });
            
            var duplicateDevice = new DropdownMenu<OutputDeviceInfo>(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_DUPLICATE_DEVICE"))
                .Change((device) => {
                    DuplicateAudioOutputDevice = device;
                    DuplicateAudioOutputDevice.Apply(AudioSplitterModule.Instance.Duplicator.System); 
                });

            var toggleDuplicate = new ConfirmButton(
                Dialog.Clean($"MODOPTIONS_AUDIOSPLITTER_{(DuplicatorInitialized ? "DISABLE_DUPLICATE" : "ENABLE_DUPLICATE")}")
            );
            toggleDuplicate.Pressed(() =>
            {
                AudioSplitterModule.Instance.ToggleAudioDuplicator();
                duplicateDevice.Disabled = !DuplicatorInitialized;

                var label = DuplicatorInitialized ? "DISABLE_DUPLICATE" : "ENABLE_DUPLICATE";
                toggleDuplicate.Label = Dialog.Clean($"MODOPTIONS_AUDIOSPLITTER_{label}");
            });

            // 2. Add items
            menu.Add(new TextMenu.SubHeader("Device Selection", false));
            menu.Add(celesteDevice);
            menu.Add(duplicateDevice);
            menu.Add(
                new TextMenu.Button(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_RELOAD_DEVICE_LIST"))
                .Pressed(() => { AudioSplitterModule.Instance.DeviceManager.ReloadDeviceList(); })
                //.AddDescription(menu, "MODOPTIONS_AUDIOSPLITTER_RELOAD_DEVICE_LIST_DESCRIPTION")
            );
            menu.Add(new TextMenu.SubHeader("Audio Splitting", false));
            menu.Add(toggleDuplicate);
            menu.Add(
                new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_ENABLE_ON_STARTUP"), EnableOnStartup)
                .Change((value) => { EnableOnStartup = value; })
            );

            // 3. Configure items
            // TODO: Fix dropdowns so that you can fill them without being contained in parent container
            FillDropdownMenu(celesteDevice);
            AudioSplitterModule.Instance.DeviceManager.OnListUpdate += () => { FillDropdownMenu(celesteDevice); };
            celesteDevice.OptionIndex = CelesteAudioOutputDevice.Index + (CelesteAudioOutputDevice != OutputDeviceInfo.DefaultDevice ? 1 : 0);

            FillDropdownMenu(duplicateDevice);
            AudioSplitterModule.Instance.DeviceManager.OnListUpdate += () => { FillDropdownMenu(duplicateDevice); };
            duplicateDevice.OptionIndex = DuplicateAudioOutputDevice.Index + (DuplicateAudioOutputDevice != OutputDeviceInfo.DefaultDevice ? 1 : 0);
            duplicateDevice.Disabled = !DuplicatorInitialized;
        }
    }
}
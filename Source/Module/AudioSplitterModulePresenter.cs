using System;
using System.Collections.Generic;
using Celeste.Mod.AudioSplitter.Audio;
using Celeste.Mod.AudioSplitter.UI;

namespace Celeste.Mod.AudioSplitter.Module
{
    public class AudioSplitterModulePresenter
    {
        internal static AudioSplitterModule Module => AudioSplitterModule.Instance;
        internal static AudioSplitterModuleSettings Settings => AudioSplitterModule.Settings;

        private AudioSplitterModuleView view;

        public AudioSplitterModulePresenter(AudioSplitterModuleView view) {
            this.view = view;
            Attach();
        }

        public void Attach()
        {
            InitializeElementValues();
            AddEventsToElements();

            Module.DeviceManager.OnListUpdate += OnDeviceListUpdate;
            Module.LoadingAudioDuplication.Observe(OnLoadingAudioDuplicatorUpdate);
        }

        public void Detach()
        {
            Module.DeviceManager.OnListUpdate -= OnDeviceListUpdate;
            Module.LoadingAudioDuplication.StopObserving(OnLoadingAudioDuplicatorUpdate);
        }

        private void InitializeElementValues()
        {
            UpdateToggleDuplicatorLabel();
            ToggleDropdownVisibility();

            // Important: Set option first, update devices after!
            view.AudioDeviceDropdown.CurrentOption = Settings.AudioOutputDevice.ToOption();
            view.SFXDeviceDropdown.CurrentOption = Settings.SFXOutputDevice.ToOption();
            view.MusicDeviceDropdown.CurrentOption = Settings.MusicOutputDevice.ToOption();

            UpdateDropdownDevices(view.AudioDeviceDropdown, Module.DeviceManager.Devices);
            UpdateDropdownDevices(view.SFXDeviceDropdown, Module.DeviceManager.Devices);
            UpdateDropdownDevices(view.MusicDeviceDropdown, Module.DeviceManager.Devices);

            view.EnableOnStartupOnOff.Index = (Settings.EnableOnStartup ? 1 : 0);
        }

        private void AddEventsToElements()
        {
            view.AudioDeviceDropdown.Change((device) =>
            {
                if (device == null)
                    return;
                Settings.AudioOutputDevice = (OutputDeviceInfo)device;
                global::Celeste.Audio.System?.SetDevice(Settings.AudioOutputDevice);
            });
            view.MusicDeviceDropdown.Change((device) =>
            {
                if (device == null)
                    return;
                Settings.MusicOutputDevice = (OutputDeviceInfo)device;
                Module.Duplicator.System?.SetDevice(Settings.MusicOutputDevice);
            });
            view.SFXDeviceDropdown.Change((device) =>
            {
                if (device == null)
                    return;
                Settings.SFXOutputDevice = (OutputDeviceInfo)device;
                global::Celeste.Audio.System?.SetDevice(Settings.SFXOutputDevice);
            });

            view.ToggleDuplicatorButton.Pressed(() =>
            {
                Module.ToggleAudioDuplicatorInThread();
            });

            view.ReloadDevicesButton.Pressed(() => { Module.DeviceManager.ReloadDeviceList(); });
            view.EnableOnStartupOnOff.Change((value) => { Settings.EnableOnStartup = value; });
        }

        private void UpdateToggleDuplicatorLabel()
        {
            var toggleDialog = Module.Enabled ? "DISABLE_DUPLICATE" : "ENABLE_DUPLICATE";
            view.ToggleDuplicatorButton.Label = Dialog.Clean($"MODOPTIONS_AUDIOSPLITTER_{toggleDialog}");
        }

        private void ToggleDropdownVisibility()
        {
            view.AudioDeviceDropdown.Visible = !Module.Enabled;
            view.SFXDeviceDropdown.Visible = Module.Enabled;
            view.MusicDeviceDropdown.Visible = Module.Enabled;
        }

        private void UpdateDropdownDevices(DropdownMenu<OutputDeviceInfo> dropdownMenu, List<OutputDeviceInfo> devices)
        {
            // Add devices
            dropdownMenu.Clear();
            dropdownMenu.Add(Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_DEFAULT_DEVICE"), OutputDeviceInfo.DefaultDevice);
            foreach (var info in devices)
                dropdownMenu.Add(info.Name, info);

            // Update option index
            OutputDeviceInfo? device = dropdownMenu.CurrentOption?.Value;
            if (device == null)
                return;
            var index = devices.IndexOf((OutputDeviceInfo)device);
            if (index != -1)
                dropdownMenu.OptionIndex = GetDeviceIndex((OutputDeviceInfo)device);
        }

        private int GetDeviceIndex(OutputDeviceInfo deviceInfo)
        {
            return deviceInfo.Index + (deviceInfo != OutputDeviceInfo.DefaultDevice ? 1 : 0); 
        }

        private void OnDeviceListUpdate(List<OutputDeviceInfo> devices)
        {
            UpdateDropdownDevices(view.AudioDeviceDropdown, devices);
            UpdateDropdownDevices(view.SFXDeviceDropdown, devices);
            UpdateDropdownDevices(view.MusicDeviceDropdown, devices);
        }

        private void OnLoadingAudioDuplicatorUpdate(bool loading)
        {
            if (view != null)
            {
                if (loading)
                {
                    view.ToggleDuplicatorButton.Disabled = true;
                }
                else
                {
                    ToggleDropdownVisibility();
                    UpdateToggleDuplicatorLabel();
                    view.ToggleDuplicatorButton.Disabled = false;
                }
            }
        }
    }

    internal static class OutputDeviceInfoExtensions
    {
        public static DropdownMenu<OutputDeviceInfo>.Option ToOption(this OutputDeviceInfo info)
        {
            var name = info.Name;
            if (info == OutputDeviceInfo.DefaultDevice)
                name = Dialog.Clean("MODOPTIONS_AUDIOSPLITTER_DEFAULT_DEVICE");

            return new(name, info);
        }
    }
}

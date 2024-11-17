using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Celeste.Mod.AudioSplitter.Module;
using FMOD;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public class OutputDeviceManager
    {
        /// <summary>
        /// System to set DEVICE_LIST_CHANGED callbacks to, won't play any audio
        /// </summary>
        FMOD.System system;

        private Dictionary<Guid, OutputDeviceInfo> devices = new();

        public List<OutputDeviceInfo> Devices
        {
            get { return devices.Values.ToList(); }
        }

        public OutputDeviceInfo GetDevice(Guid id)
        {
            return devices.GetValueOrDefault(id);
        }

        public bool Initialized { get; private set; } = false;

        public Action<List<OutputDeviceInfo>> OnListUpdate;

        // ref to not get freed by GC (GC is such a pain)
        private SYSTEM_CALLBACK callback;

        public void Initialize()
        {
            if (Initialized)
                return;

            RESULT result;
            result = Factory.System_Create(out system);
            result.CheckFMOD();

            // We don't want to play any audio AT ALL
            // Definitely an overkill but it'll do the job
            ADVANCEDSETTINGS settings = new ADVANCEDSETTINGS
            {
                cbSize = Marshal.SizeOf<ADVANCEDSETTINGS>(),
                maxMPEGCodecs = 0,
                maxADPCMCodecs = 0,
                maxXMACodecs = 0,
                maxVorbisCodecs = 0,
                maxAT9Codecs = 0,
                maxFADPCMCodecs = 0,
                maxPCMCodecs = 0,
                ASIONumChannels = 0,
                defaultDecodeBufferSize = 0,
                DSPBufferPoolSize = 0,
            };

            system.setSoftwareChannels(0);
            system.setSoftwareFormat(8000, SPEAKERMODE.DEFAULT, 0);
            system.setAdvancedSettings(ref settings);
            result = system.init(0, INITFLAGS.NORMAL, IntPtr.Zero);
            result.CheckFMOD();

            callback = new SYSTEM_CALLBACK(DeviceListCallback);
            result = system.setCallback(callback, SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED | SYSTEM_CALLBACK_TYPE.DEVICELOST);
            result.CheckFMOD();

            On.Celeste.Audio.Update += OnAudioUpdate;
            FetchDevices();

            Logger.Verbose(nameof(OutputDeviceManager), "Initialized system");
            Initialized = true;
        }

        public void Terminate()
        {
            if (!Initialized)
                return;

            On.Celeste.Audio.Update -= OnAudioUpdate;

            system.release();
            system = null;

            Initialized = false;
        }

        public void OnAudioUpdate(On.Celeste.Audio.orig_Update origUpdate)
        {
            origUpdate();
            Update();
        }

        public void Update()
        {
            if (system != null && Initialized)
            {
                system.update().CheckFMOD();
            }
        }

        public RESULT DeviceListCallback(nint systemraw, SYSTEM_CALLBACK_TYPE type, nint commanddata1, nint commanddata2, nint userdata)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Got callback {Enum.GetName(typeof(SYSTEM_CALLBACK_TYPE), type)}");

            try
            {
                OnListUpdate(FetchDevices());
                return RESULT.OK;
            }
            catch (Exception e)
            {
                Logger.Error(nameof(AudioSplitterModule), $"Failed to fetch the output device list, e: {e.Message}, stacktrace:\n{e.StackTrace.ToString()}");
                return RESULT.OK;
            }
        }

        public void ReloadDeviceList()
        {
            system.getOutput(out OUTPUTTYPE outputtype);
            system.setOutput(OUTPUTTYPE.NOSOUND);
            system.setOutput(outputtype);
            
            OnListUpdate(FetchDevices());
        }

        public List<OutputDeviceInfo> FetchDevices()
        {
            Dictionary<Guid, OutputDeviceInfo> newDevices = new();

            RESULT result;
            result = system.getNumDrivers(out int numdrivers);
            result.CheckFMOD();

            for (int index = 0; index < numdrivers; index++)
            {
                StringBuilder stringBuilder = new(256);

                result = system.getDriverInfo(index, stringBuilder, 256, out Guid id, out _, out _, out _);
                result.CheckFMOD();

                OutputDeviceInfo info = new OutputDeviceInfo
                {
                    Index = index,
                    Id = id,
                    Name = stringBuilder.ToString(),
                };
                newDevices[id] = info;
            }

            devices = newDevices;
            return Devices;
        }
    }
}

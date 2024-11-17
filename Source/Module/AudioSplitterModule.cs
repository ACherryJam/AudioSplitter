using System;
using System.Diagnostics;
using System.Reflection;
using Celeste.Mod.AudioSplitter.Audio;
using Celeste.Mod.AudioSplitter.UI;
using FMOD;
using FMOD.Studio;
using MonoMod.RuntimeDetour;

using CelesteAudio = global::Celeste.Audio;

namespace Celeste.Mod.AudioSplitter.Module
{
    public class AudioSplitterModule : EverestModule
    {
        public static AudioSplitterModule Instance { get; private set; }

        public override Type SettingsType => typeof(AudioSplitterModuleSettings);
        public static AudioSplitterModuleSettings Settings => (AudioSplitterModuleSettings)Instance._Settings;

        public override Type SessionType => typeof(AudioSplitterModuleSession);
        public static AudioSplitterModuleSession Session => (AudioSplitterModuleSession)Instance._Session;

        public AudioDuplicator Duplicator { get; private set; } = new AudioDuplicator();
        public OutputDeviceManager DeviceManager { get; private set; } = new OutputDeviceManager();

        public AudioSplitterModule()
        {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(AudioSplitterModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(AudioSplitterModule), LogLevel.Info);
#endif
        }

        public override void Load()
        {
            MultiLanguageFontHooks.Apply();
            BankCache.ApplyHooks();
            InstanceDuplicatorHooks.Apply();

            On.Celeste.Audio.Init += OnAudioInit;
            On.Celeste.Audio.Unload += OnAudioUnload;
            On.Celeste.Audio.VCAVolume += OnAudioVCAVolume;

#if DEBUG
            bool waitForDebugger = false;
            while (waitForDebugger && !Debugger.IsAttached)
                continue;
#endif
        }

    public override void Unload()
        {
            InstanceDuplicatorHooks.Remove();
            DeviceManager.Terminate();
            Duplicator.Terminate();

            MultiLanguageFontHooks.Remove();
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);

            if (!firstLoad)
                return;
        }

        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
        {
            CreateModMenuSectionHeader(menu, inGame, snapshot);
            Settings.CreateMenu(menu, inGame);
        }

        private void OnAudioInit(On.Celeste.Audio.orig_Init orig)
        {
            DeviceManager.Initialize();
            
            orig();

            if (Settings.EnableOnStartup)
            {
                ToggleAudioDuplicator();
            }
        }

        private void OnAudioUnload(On.Celeste.Audio.orig_Unload orig)
        {
            orig();
            Duplicator.Terminate();
        }

        private float OnAudioVCAVolume(On.Celeste.Audio.orig_VCAVolume orig, string path, float? volume = null)
        {
            // If duplicator is not active, just control the original VCA
            if (!Duplicator.Initialized)
                return orig(path, volume);
            
            // Forward sounds to original and music to duplicator
            if (path == "vca:/gameplay_sfx" || path == "vca:/ui_sfx")
            {
                Duplicator.VCAVolume(path, 0);
                return orig(path, volume);
            }
            else if (path == "vca:/music")
            {
                orig(path, 0);
                return Duplicator.VCAVolume(path, volume);
            }
            else
            {
                // The rest of VCAs should go to both
                Duplicator.VCAVolume(path, volume);
                return orig(path, volume);
            }
        }

        public void ToggleAudioDuplicator()
        {
            if (!Duplicator.Initialized)
            {
                Duplicator.Initialize();
                Settings.CelesteAudioOutputDevice.Apply(CelesteAudio.System).CheckFMOD();
                Settings.DuplicateAudioOutputDevice.Apply(Duplicator.System).CheckFMOD();
            }
            else
            {
                Duplicator.Terminate();
            }
            
            global::Celeste.Settings.Instance.ApplyVolumes();
        }
    }
}
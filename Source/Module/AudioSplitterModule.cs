using System;
using System.Diagnostics;
using System.Threading;
using Celeste.Mod.AudioSplitter.Audio;
using Celeste.Mod.AudioSplitter.Extensions;
using Celeste.Mod.AudioSplitter.UI;
using Celeste.Mod.AudioSplitter.Utility;
using FMOD.Studio;
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

        public bool Enabled => Duplicator.Initialized;

        private AudioSplitterModulePresenter presenter = new();
        private LoadingMessage loadingMessage = null;

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
#if DEBUG
            bool waitForDebugger = false;
            while (waitForDebugger && !Debugger.IsAttached)
                continue;
#endif

            HookAttribute.Invoke(typeof(ApplyOnLoadAttribute));
        }

        public override void Unload()
        {
            
            HookAttribute.Invoke(typeof(RemoveOnUnloadAttribute));

            DeviceManager.Terminate();
            Duplicator.Terminate();
        }

        public override void Initialize()
        {
            DeviceManager.Initialize();
            DeviceManager.OnListUpdate += (_) => { ConfigureSystemDevices(); };
        }

        public override void LoadContent(bool firstLoad)
        {
            if (firstLoad)
            {
                loadingMessage = new(Celeste.Instance, default, new(20f, LoadingMessage.UI_HEIGHT - 20f));
            }
        }

        private void ShowLoadingMessageOnLoading(bool loading)
        {
            if (loading)
            {
                var dialog = !Enabled ? "LOADING_MESSAGE" : "UNLOADING_MESSAGE";
                loadingMessage.Label = Dialog.Clean($"AUDIOSPLITTER_{dialog}");
                loadingMessage.Add();
            }
            else
            {
                loadingMessage.Remove();
            }
        }

        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
        {
            CreateModMenuSectionHeader(menu, inGame, snapshot);

            var view = new AudioSplitterModuleView();
            presenter.Attach(view);
            view.AddTo(menu, inGame);

            menu.OnClose += () => { presenter.Detach(); };
        }

        public LiveData<bool> LoadingAudioDuplication = new(false);

        public void ToggleAudioDuplicator()
        {
            LoadingAudioDuplication.Value = true;
            if (!Duplicator.Initialized)
                Duplicator.Initialize();
            else
                Duplicator.Terminate();
            LoadingAudioDuplication.Value = false;

            ConfigureSystemDevices();
            global::Celeste.Settings.Instance.ApplyVolumes();
        }

        public void ToggleAudioDuplicatorInThread()
        {
            Thread thread = new(
                () => {
                    ToggleAudioDuplicator();
                }
            );
            thread.Start();
        }
        

        public void ConfigureSystemDevices()
        {
            if (!Enabled)
            {
                DeviceManager.SetDevice(Settings.AudioOutputDevice, CelesteAudio.System);
            }
            else
            {
                DeviceManager.SetDevice(Settings.SFXOutputDevice, CelesteAudio.System);
                DeviceManager.SetDevice(Settings.MusicOutputDevice, Duplicator.System);
            }
        }

        internal static class AudioSplitterModuleHooks
        {
            [ApplyOnLoad]
            public static void Apply()
            {
                On.Celeste.Audio.Init += OnAudioInit;
                On.Celeste.Audio.Unload += OnAudioUnload;
                On.Celeste.Audio.VCAVolume += OnAudioVCAVolume;

                On.Celeste.OuiMainMenu.Update += DisableExitWhileLoading;

                On.Celeste.Overworld.ctor += Overworld_ctor;
            }

            [RemoveOnUnload]
            public static void Remove()
            {
                On.Celeste.Audio.Init -= OnAudioInit;
                On.Celeste.Audio.Unload -= OnAudioUnload;
                On.Celeste.Audio.VCAVolume -= OnAudioVCAVolume;

                On.Celeste.OuiMainMenu.Update -= DisableExitWhileLoading;
                
                On.Celeste.Overworld.ctor -= Overworld_ctor;
            }

            public static void OnAudioInit(On.Celeste.Audio.orig_Init orig)
            {
                orig();

                if (Settings.EnableOnStartup)
                    Instance.ToggleAudioDuplicatorInThread();
            }

            public static void OnAudioUnload(On.Celeste.Audio.orig_Unload orig)
            {
                orig();
                Instance.Duplicator.Terminate();
            }

            public static float OnAudioVCAVolume(On.Celeste.Audio.orig_VCAVolume orig, string path, float? volume = null)
            {
                // If duplicator is not active, just control the original VCA
                if (!Instance.Enabled)
                    return orig(path, volume);

                // Forward sounds to original and music to duplicator
                if (path == "vca:/gameplay_sfx" || path == "vca:/ui_sfx")
                {
                    Instance.Duplicator.VCAVolume(path, 0);
                    return orig(path, volume);
                }
                else if (path == "vca:/music")
                {
                    orig(path, 0);
                    return Instance.Duplicator.VCAVolume(path, volume);
                }
                else
                {
                    // The rest of VCAs should go to both
                    Instance.Duplicator.VCAVolume(path, volume);
                    return orig(path, volume);
                }
            }

            public static void DisableExitWhileLoading(On.Celeste.OuiMainMenu.orig_Update orig, OuiMainMenu self)
            {
                var exitButton = self.Buttons.Find(
                    b => b.GetType() == typeof(MainMenuSmallButton)
                    && ((MainMenuSmallButton)b).LabelName == "menu_exit"
                );
                if (exitButton != default(MenuButton))
                {
                    if (Instance.LoadingAudioDuplication.Value)
                    {
                        if (!exitButton.IsDisabled())
                            exitButton.Disable();
                    } else
                    {
                        if (exitButton.IsDisabled())
                            exitButton.Enable();
                    }
                }

                orig(self);
            }

            public static void Overworld_ctor(On.Celeste.Overworld.orig_ctor orig, Overworld self, OverworldLoader loader)
            {
                Instance.LoadingAudioDuplication.Observe(Instance.ShowLoadingMessageOnLoading);
                orig(self, loader);
            }
        }
    }
}
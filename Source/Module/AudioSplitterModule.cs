using System;
using Celeste.Mod.AudioSplitter.Audio;
using FMOD;
using FMOD.Studio;

namespace Celeste.Mod.AudioSplitter.Module
{
    public class AudioSplitterModule : EverestModule
    {
        public static AudioSplitterModule Instance { get; private set; }

        public override Type SettingsType => typeof(AudioSplitterModuleSettings);
        public static AudioSplitterModuleSettings Settings => (AudioSplitterModuleSettings)Instance._Settings;

        public override Type SessionType => typeof(AudioSplitterModuleSession);
        public static AudioSplitterModuleSession Session => (AudioSplitterModuleSession)Instance._Session;

        public AudioDuplicator audioDuplicate { get; private set; } = new AudioDuplicator();
        public OutputDeviceManager outputDeviceManager { get; private set; } = new OutputDeviceManager();

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

        ~AudioSplitterModule()
        {
            outputDeviceManager.Terminate();
        }

        public override void Load()
        {
            On.Celeste.Audio.Unload += OnAudioUnload;
            InstanceDuplicatorHooks.Apply();

            On.Celeste.Audio.Init += static (On.Celeste.Audio.orig_Init orig) =>
            {
                orig();

                // TODO: THIS ISN'T RIGHT, ADD CALLBACK IMMEDIATELY AFTER LOADING BANKS

                // Apply an empty callback to all loaded EventDescriptions
                // setCallback hooks will wrap these into EVENT_DESTROYED callbacks
                EVENT_CALLBACK emptyCallback = static (type, eventInstance, parameters) => RESULT.OK;

                foreach (Guid guid in global::Celeste.Audio.cachedPaths.Keys)
                {
                    global::Celeste.Audio.System.getEventByID(guid, out EventDescription description);
                    description.setCallback(emptyCallback, 0);
                }
            };
        }

        public override void Unload()
        {
            InstanceDuplicatorHooks.Remove();
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);

            if (!firstLoad)
                return;

            if (Settings.EnableOnStartup)
            {
                audioDuplicate.Initialize();
            }
        }

        private void OnAudioUnload(On.Celeste.Audio.orig_Unload orig)
        {
            orig();
            audioDuplicate.Terminate();
        }

        public void EnableAudioDuplicate()
        {
            audioDuplicate.Initialize();
        }
    }
}
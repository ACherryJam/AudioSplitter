using System;
using FMOD;
using FMOD.Studio;
using Celeste.Mod.AudioSplitter.Audio;

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
            // Wrap EventInstance/EventDescription callbacks to know when instances
            // from original Audio are destroyed
            //
            // Note that we want to hook this before AudioDuplicate makes its EventInstance.setCallback hook
            // because otherwise we'll set the wrapped callback to the duplicate instances
            // setCallback call -> AudioDuplicate hook -> ASM Hook -(wrapped callback)-> original
            On.FMOD.Studio.EventInstance.setCallback += WrapEventInstanceCallback;
            On.FMOD.Studio.EventDescription.setCallback += WrapEventDescriptionCallback;

            On.Celeste.Audio.Init += (On.Celeste.Audio.orig_Init orig) =>
            {
                orig();

                // Apply an empty callback to all loaded EventDescriptions
                // setCallback hooks will wrap these into EVENT_DESTROYED callbacks
                EVENT_CALLBACK emptyCallback = (type, eventInstance, parameters) =>
                {
                    return RESULT.OK;
                };

                foreach (Guid guid in global::Celeste.Audio.cachedPaths.Keys)
                {
                    global::Celeste.Audio.System.getEventByID(guid, out EventDescription description);
                    description.setCallback(emptyCallback, 0);
                }

                if (Settings.EnableOnStartup)
                {
                    audioDuplicate.Initialize();
                }
            }

            On.Celeste.Audio.Unload += OnAudioUnload;
        }

        public override void Unload()
        {
            On.FMOD.Studio.EventInstance.setCallback -= WrapEventInstanceCallback;
            On.FMOD.Studio.EventDescription.setCallback -= WrapEventDescriptionCallback;
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);

            if (!firstLoad)
                return;
        }

        /// <see cref="AudioDuplicator.WrapEventCallback">
        private RESULT WrapEventInstanceCallback(
            On.FMOD.Studio.EventInstance.orig_setCallback setCallback,
            EventInstance eventInstance, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask
        )
        {
            (callback, callbackmask) = audioDuplicate.WrapEventCallback(callback, callbackmask);
            return setCallback(eventInstance, callback, callbackmask);
        }

        private RESULT WrapEventDescriptionCallback(
            On.FMOD.Studio.EventDescription.orig_setCallback setCallback,
            EventDescription eventDescription, EVENT_CALLBACK callback, EVENT_CALLBACK_TYPE callbackmask
        )
        {
            (callback, callbackmask) = audioDuplicate.WrapEventCallback(callback, callbackmask);
            return setCallback(eventDescription, callback, callbackmask);
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
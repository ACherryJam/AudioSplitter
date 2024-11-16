using System;
using Celeste.Mod.AudioSplitter.Module;
using Celeste.Mod.AudioSplitter.Utility;
using Celeste.Mod.Core;
using FMOD;
using FMOD.Studio;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.AudioSplitter.Audio
{
    /// <summary>
    /// Fully duplicates all audio from original <see cref="CelesteNamespace.Audio">Celeste.Audio<see/> FMOD system
    /// into a AudioDuplicate's FMOD system
    /// </summary>
    /// 
    /// <remarks>
    /// One FMOD system can only play to a single audio device so we need more systems to play to more devices  
    /// https://qa.fmod.com/t/playing-different-audios-simultaneously-through-different-output-devices/18461/2
    /// 
    /// Each instance of AudioDuplicate is responsible for hooking the necessary methods for full audio replication
    /// </remarks>
    public class AudioDuplicator
    {
        private FMOD.Studio.System system = null;

        private BankCache bankCache = null;
        private EventCache eventCache = null;
        private InstanceDuplicator instanceDuplicator = null;

        private RecursionLocker locker = new();

        public bool Initialized { get; private set; } = false;
        public FMOD.Studio.System System => system;

        /// <summary>
        /// Load the audio data and apply the necessary hooks
        /// </summary>
        public void Initialize()
        {
            if (Initialized)
                return;

            FMOD.Studio.System.create(out system).CheckFMOD();
            system.initialize(1024, FMOD.Studio.INITFLAGS.NORMAL, FMOD.INITFLAGS.NORMAL, IntPtr.Zero).CheckFMOD();

            var attributes = new FMOD.Studio._3D_ATTRIBUTES
            {
                position = new VECTOR { x = 0f, y = 0f, z = 1f },
                forward = new VECTOR { x = 0f, y = 1f, z = 0f },
                up = new VECTOR { x = 0f, y = 0f, z = -345f },
            };
            system.setListenerAttributes(0, attributes).CheckFMOD();

            // TODO: Load banks in a different thread and add a cool loading animation
            bankCache = new(system);
            eventCache = new(system);
            instanceDuplicator = new(system);

            bankCache.LoadUnloadedBanks();
            eventCache.LoadUsedDescriptions();
            instanceDuplicator.Initialize();

            On.Celeste.Audio.GetEventDescription += OnAudioGetEventDescription;
            On.Celeste.Audio.ReleaseUnusedDescriptions += OnAudioReleaseUnusedDescriptions;

            ApplyHooks();

            Initialized = true;
        }

        /// <summary>
        /// Unload the audio data and remove the hooks
        /// </summary>
        public void Terminate()
        {
            if (!Initialized)
                return;

            bankCache.UnloadBanks();

            On.Celeste.Audio.GetEventDescription -= OnAudioGetEventDescription;
            On.Celeste.Audio.ReleaseUnusedDescriptions -= OnAudioReleaseUnusedDescriptions;
            eventCache.Clear();

            instanceDuplicator.Terminate();
            instanceDuplicator.Clear();

            RemoveHooks();

            system.release();
            system = null;

            Initialized = false;
        }

        private void Update()
        {
            if (system != null && Initialized)
            {
                system.update().CheckFMOD();
            }
        }

        public void ApplyHooks()
        {
            On.Celeste.Audio.Update += OnAudioUpdate;
            On.Celeste.Audio.BusMuted += OnAudioBusMuted;
            On.Celeste.Audio.BusPaused += OnAudioBusPaused;
            On.Celeste.Audio.BusStopAll += OnAudioBusStopAll;
            On.Celeste.Audio.SetListenerPosition += OnAudioSetListenerPosition;
        }

        public void RemoveHooks()
        {
            On.Celeste.Audio.Update -= OnAudioUpdate;
            On.Celeste.Audio.BusMuted -= OnAudioBusMuted;
            On.Celeste.Audio.BusPaused -= OnAudioBusPaused;
            On.Celeste.Audio.BusStopAll -= OnAudioBusStopAll;
            On.Celeste.Audio.SetListenerPosition -= OnAudioSetListenerPosition;
        }

        public float VCAVolume(string path, float? newVolume = null)
        {
            float volume = 1f;
            if (system.getVCA(path, out VCA vca) == RESULT.OK)
            {
                if (newVolume != null)
                    vca.setVolume(newVolume.Value);
                vca.getVolume(out volume, out _);
            }
            else 
            {
                Logger.Error(nameof(AudioSplitterModule), "Failed to get VCA for VCAVolume");
            }
            return volume;
        }

        private void OnAudioUpdate(On.Celeste.Audio.orig_Update origUpdate)
        {
            origUpdate();
            Update();
        }

        private bool OnAudioBusMuted(On.Celeste.Audio.orig_BusMuted orig, string path, bool? mute = null)
        {
            if (mute != null && system != null && system.getBus(path, out Bus bus) == RESULT.OK)
            {
                bus.setMute(mute.Value);
            }

            return orig(path, mute);
        }

        private bool OnAudioBusPaused(On.Celeste.Audio.orig_BusPaused orig, string path, bool? pause = null)
        {
            if (pause != null && system != null && system.getBus(path, out Bus bus) == RESULT.OK)
            {
                bus.setPaused(pause.Value);
            }

            return orig(path, pause);
        }

        private void OnAudioBusStopAll(On.Celeste.Audio.orig_BusStopAll orig, string path, bool immediate = false)
        {
            if (system != null && system.getBus(path, out Bus bus) == RESULT.OK)
            {
                bus.stopAllEvents(immediate ? STOP_MODE.IMMEDIATE : STOP_MODE.ALLOWFADEOUT);
            }

            orig(path, immediate);
        }

        private void OnAudioSetListenerPosition(On.Celeste.Audio.orig_SetListenerPosition orig, Vector3 forward, Vector3 up, Vector3 position)
        {
            FMOD.Studio._3D_ATTRIBUTES attributes = new FMOD.Studio._3D_ATTRIBUTES
            {
                position = new VECTOR { x = position.X, y = position.Y, z = position.Z },
                forward = new VECTOR { x = forward.X, y = forward.Y, z = forward.Z },
                up = new VECTOR { x = up.X, y = up.Y, z = up.Z },
            };
            system.setListenerAttributes(0, attributes);

            orig(forward, up, position);
        }

        private EventDescription OnAudioGetEventDescription(On.Celeste.Audio.orig_GetEventDescription orig, string path)
        {
            eventCache.LoadEventDescription(path);
            return orig(path);
        }

        private void OnAudioReleaseUnusedDescriptions(On.Celeste.Audio.orig_ReleaseUnusedDescriptions orig)
        {
            if (CoreModule.Settings.UnloadUnusedAudio)
                eventCache.ReleaseUnusedDescriptions();
            orig();
        }
    }
}

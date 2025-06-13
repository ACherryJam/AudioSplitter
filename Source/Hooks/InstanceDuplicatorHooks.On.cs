using System;
using Celeste.Mod.AudioSplitter.Extensions;
using Celeste.Mod.AudioSplitter.Module;
using FMOD;
using FMOD.Studio;

using CelesteAudio = global::Celeste.Audio;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public static partial class InstanceDuplicatorHooks
    {
        public static void ApplyOn()
        {
            On.FMOD.Studio.EventDescription.createInstance += OnInstanceCreate;

            On.Celeste.Audio.Banks.Load += SetCallbacksToLoadedVanillaBank;
            On.Celeste.Audio.IngestBank += SetCallbacksToLoadedModdedBank;

            On.Celeste.Audio.Unload += OnAudioUnload;
            Everest.Events.Celeste.OnShutdown += OnCelesteShutdown;
        }

        public static void RemoveOn()
        {
            On.FMOD.Studio.EventDescription.createInstance -= OnInstanceCreate;

            On.Celeste.Audio.Banks.Load -= SetCallbacksToLoadedVanillaBank;
            On.Celeste.Audio.IngestBank -= SetCallbacksToLoadedModdedBank;

            On.Celeste.Audio.Unload -= OnAudioUnload;
            Everest.Events.Celeste.OnShutdown -= OnCelesteShutdown;
        }

        private static void OnCelesteShutdown()
        {
            CallbackWrapper.FreeCallbackWrapperGCHandles();
        }

        private static void OnAudioUnload(On.Celeste.Audio.orig_Unload orig)
        {
            orig();
            CallbackWrapper.FreeCallbackWrapperGCHandles();
        }

        private static RESULT OnInstanceCreate(On.FMOD.Studio.EventDescription.orig_createInstance orig, EventDescription origDesc, out EventInstance origInst)
        {
            origDesc.getID(out Guid guid);

            RESULT result;
            result = orig(origDesc, out origInst);
            if (result != RESULT.OK)
            {
                Logger.Error(nameof(AudioSplitterModule),
                    $"Failed to create an instance {AudioExtensions.GetEventPath(guid)}, result: {result}");
                return result;
            }

            if (!locker.TryEnter(nameof(OnInstanceCreate), out IDisposable scope))
                return result;

            using (scope)
                foreach (var instanceDuplicater in InstanceDuplicator.InitializedInstances)
                {
                    result = instanceDuplicater.DuplicateInstance(guid, origInst);
                    result.CheckFMOD();
                }

            return RESULT.OK;
        }

        private static Bank SetCallbacksToLoadedModdedBank(On.Celeste.Audio.orig_IngestBank orig, ModAsset asset)
        {
            bool needToSetCallbacks = !CelesteAudio.Banks.ModCache.TryGetValue(asset, out _);

            Bank bank = orig(asset);
            if (bank == null)
            {
                Logger.Warn(nameof(AudioSplitterModule), $"Failed to set callbacks to modded bank {asset.PathVirtual}, IngestBank returned null");
                return bank;
            }

            if (needToSetCallbacks)
                CallbackWrapper.SetCallbacksToBank(bank);

            return bank;
        }

        private static Bank SetCallbacksToLoadedVanillaBank(On.Celeste.Audio.Banks.orig_Load orig, string name, bool loadStrings)
        {
            bool needToSetCallbacks = !CelesteAudio.Banks.Banks.TryGetValue(name, out _);

            Bank bank = orig(name, loadStrings);
            if (bank == null)
            {
                Logger.Warn(nameof(AudioSplitterModule), $"Failed to set callbacks to vanilla bank {name}, Load returned null");
                return bank;
            }

            if (needToSetCallbacks)
                CallbackWrapper.SetCallbacksToBank(bank);

            return bank;
        }
    }
}

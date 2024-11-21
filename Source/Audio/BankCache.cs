using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;

using CelesteAudio = global::Celeste.Audio;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public class BankCache
    {
        private static HashSet<string> banksNeedingStringLoading = new();
        public static void ApplyHooks()
        {
            On.Celeste.Audio.Banks.Load += OnAudioBanksLoad;
        }

        static Bank OnAudioBanksLoad(On.Celeste.Audio.Banks.orig_Load orig, string name, bool loadStrings)
        {
            if (loadStrings)
                banksNeedingStringLoading.Add(name);
            return orig(name, loadStrings);
        }

        private Dictionary<string, Bank> bankCache = new();

        private FMOD.Studio.System system;
        private BankLoader bankLoader;

        public BankCache(FMOD.Studio.System system)
        {
            this.system = system;
            this.bankLoader = new(system);
        }

        public void LoadUnloadedBanks()
        {
            HashSet<string> loadedBanks = new();
            loadedBanks.UnionWith(CelesteAudio.Banks.Banks.Keys);
            loadedBanks.UnionWith(CelesteAudio.Banks.ModCache.Keys.Select(asset => asset.PathVirtual));

            foreach (string name in loadedBanks)
            {
                if (bankCache.ContainsKey(name))
                    continue;

                bankCache[name] = bankLoader.LoadBank(name, banksNeedingStringLoading.Contains(name));
            }
        }

        public void UnloadBanks()
        {
            foreach ((string name, Bank bank) in bankCache)
            {
                bank.unload();
                bankCache.Remove(name);
            }
        }
    }
}

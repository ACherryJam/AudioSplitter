﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Celeste.Mod.Core;
using FMOD;
using FMOD.Studio;
using Monocle;

using CelesteAudio = global::Celeste.Audio;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public class BankLoader
    {
        static private HashSet<string> banksNeedingStringLoading = new(); 
        static BankLoader()
        {
            On.Celeste.Audio.Banks.Load += OnAudioBanksLoad;
        }

        static Bank OnAudioBanksLoad(On.Celeste.Audio.Banks.orig_Load orig, string name, bool loadStrings)
        {
            banksNeedingStringLoading.Add(name);
            return orig(name, loadStrings);
        }

        private Dictionary<string, Bank> bankCache = new();
        
        private FMOD.Studio.System system;

        private ModdedBankLoader moddedLoader = null;
        private VanillaBankLoader vanillaLoader = null;

        public BankLoader(FMOD.Studio.System system) 
        {
            this.system = system;

            moddedLoader = new(this.system);
            vanillaLoader = new(this.system);
        }

        public void LoadBanks()
        {
            HashSet<string> loadedBanks = new();
            loadedBanks.UnionWith(CelesteAudio.Banks.Banks.Keys);
            loadedBanks.UnionWith(CelesteAudio.Banks.ModCache.Keys.Select(asset => asset.PathVirtual));

            foreach (string name in loadedBanks)
            {
                if (bankCache.ContainsKey(name))
                    continue;

                LoadBank(name, banksNeedingStringLoading.Contains(name));
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

        private Bank LoadBank(string name, bool loadStrings)
        {
            Bank bank;
            if (bankCache.TryGetValue(name, out bank))
                return bank;

            ModAsset asset;
            if (Everest.Content.TryGet<AssetTypeBank>($"Audio/{name}", out asset))
                bank = moddedLoader.LoadBank(asset);
            else
                bank = vanillaLoader.LoadBank(name);

            if (loadStrings)
            {
                if (Everest.Content.TryGet<AssetTypeBank>($"Audio/{name}.strings", out asset))
                    moddedLoader.LoadStringBank(asset);
                else
                    vanillaLoader.LoadStringBank(name);
            }

            return bankCache[name] = bank;
        }
    }

    internal class VanillaBankLoader
    {
        private FMOD.Studio.System system;

        public VanillaBankLoader(FMOD.Studio.System system) { this.system = system; }

        public Bank LoadBank(string name)
        {
            system.loadBankFile(
                Path.Combine(Engine.ContentDirectory, "FMOD", "Desktop", name + ".bank"),
                LOAD_BANK_FLAGS.NORMAL, out Bank bank
            ).CheckFMOD();
            return bank;
        }

        public Bank LoadStringBank(string name)
        {
            system.loadBankFile(
                Path.Combine(Engine.ContentDirectory, "FMOD", "Desktop", name + ".strings.bank"),
                LOAD_BANK_FLAGS.NORMAL, out Bank bank
            ).CheckFMOD();
            return bank;
        }
    }

    internal class ModdedBankLoader
    {
        private int lastUsedModBankHandle = 0x434A;
        private HashSet<string> loadedModBankPaths = new();
        private Dictionary<ModAsset, Bank> modBankCache = new();
        private Dictionary<IntPtr, ModAsset> modBankAssets = new();
        private Dictionary<IntPtr, Stream> modBankStreams = new();

        private FMOD.Studio.System system;

        public ModdedBankLoader(FMOD.Studio.System system) { this.system = system; }

        public Bank LoadBank(ModAsset asset)
        {
            loadedModBankPaths.Add(asset.PathVirtual);

            Bank bank;
            if (modBankCache.TryGetValue(asset, out bank))
                return bank;

            RESULT loadResult;
            if (CoreModule.Settings.UnpackFMODBanks)
            {
                loadResult = system.loadBankFile(asset.GetCachedPath(), LOAD_BANK_FLAGS.NORMAL, out bank);
            }
            else
            {
                IntPtr handle;
                modBankAssets[handle = (IntPtr)(++lastUsedModBankHandle)] = asset;
                BANK_INFO info = new BANK_INFO
                {
                    size = CelesteAudio.Banks.SizeOfBankInfo,
                    userdata = handle,
                    userdatalength = 0,
                    opencallback = ModBankOpen,
                    closecallback = ModBankClose,
                    readcallback = ModBankRead,
                    seekcallback = ModBankSeek
                };

                loadResult = system.loadBankCustom(info, LOAD_BANK_FLAGS.NORMAL, out bank);
            }

            if (loadResult == RESULT.ERR_EVENT_ALREADY_LOADED)
            {
                return null;
            }

            loadResult.CheckFMOD();
            return modBankCache[asset] = bank;
        }

        public Bank LoadStringBank(ModAsset asset) => LoadBank(asset);

        private RESULT ModBankOpen(StringWrapper name, ref uint filesize, ref IntPtr handle, IntPtr userdata)
        {
            Stream stream = modBankAssets[userdata].Stream;
            filesize = (uint)stream.Length;
            modBankStreams[handle = (IntPtr)(++lastUsedModBankHandle)] = stream;
            return RESULT.OK;
        }

        private RESULT ModBankClose(IntPtr handle, IntPtr userdata)
        {
            modBankStreams[handle].Dispose();
            modBankStreams[handle] = null;
            return RESULT.OK;
        }

        private RESULT ModBankRead(IntPtr handle, IntPtr buffer, uint sizebytes, ref uint bytesread, IntPtr userdata)
        {
            bytesread = 0;

            Stream stream = modBankStreams[handle];
            byte[] tmp = new byte[Math.Min(65536, sizebytes)];
            int read;
            while ((read = stream.Read(tmp, 0, Math.Min(tmp.Length, (int)(sizebytes - bytesread)))) > 0)
            {
                Marshal.Copy(tmp, 0, (IntPtr)(buffer + bytesread), read);
                bytesread += (uint)read;
            }

            if (bytesread < sizebytes)
                return RESULT.ERR_FILE_EOF;
            return RESULT.OK;
        }

        private RESULT ModBankSeek(IntPtr handle, uint pos, IntPtr userdata)
        {
            modBankStreams[handle].Seek(pos, SeekOrigin.Begin);
            return RESULT.OK;
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mod.AudioSplitter.Audio;
using Celeste.Mod.AudioSplitter.Module;
using Celeste.Mod.AudioSplitter.Utility;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AudioSplitter.Hooks
{
    public static class MultiLanguageFontHooks
    {
        [ApplyOnLoad]
        public static void Apply()
        {
            On.Celeste.Dialog.PostLanguageLoad += OnPostLanguageLoad;

            // Hook font unload blocker last to let other mods do stuff
            var afterConfig = new DetourConfig(nameof(InstanceDuplicatorHooks), after: new List<string> { "*" });
            var context = new DetourConfigContext(afterConfig);
            using (var scope = context.Use())
            {
                On.Celeste.Fonts.Unload += OnFontUnload;
            }
        }

        [RemoveOnUnload]
        public static void Remove()
        {
            On.Celeste.Dialog.PostLanguageLoad -= OnPostLanguageLoad;
            On.Celeste.Fonts.Unload -= OnFontUnload;
        }

        [SuppressMessage("Usage", "CL0003", Justification = "Dropping original call is the goal")]
        public static void OnFontUnload(On.Celeste.Fonts.orig_Unload orig, string face)
        {
            // aaaaaaaand do nothing!
        }

        public static void OnPostLanguageLoad(On.Celeste.Dialog.orig_PostLanguageLoad orig)
        {
            orig();
            foreach (Language lang in Dialog.Languages.Values)
            {
                Fonts.Load(lang.FontFace);
                Logger.Verbose(nameof(AudioSplitterModule), $"Loaded fontface {lang.FontFace} for language {lang.Label}");
            }
        }
    }
}

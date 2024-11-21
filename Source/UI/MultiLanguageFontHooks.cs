using Celeste.Mod.AudioSplitter.Module;

namespace Celeste.Mod.AudioSplitter.UI
{
    public static class MultiLanguageFontHooks
    {
        public static void Apply()
        {
            On.Celeste.Dialog.PostLanguageLoad += OnPostLanguageLoad;
            On.Celeste.LanguageSelectUI.SetNextLanguage += OnLanguageSelect;
        }

        public static void Remove()
        {
            On.Celeste.Dialog.PostLanguageLoad -= OnPostLanguageLoad;
            On.Celeste.LanguageSelectUI.SetNextLanguage -= OnLanguageSelect;
        }

        public static void OnLanguageSelect(On.Celeste.LanguageSelectUI.orig_SetNextLanguage orig, LanguageSelectUI self, Language next)
        {
            if (Settings.Instance.Language != next.Id)
            {
                Language language = Dialog.Languages[Settings.Instance.Language];
                Language language2 = Dialog.Languages["english"];
                Fonts.Load(next.FontFace);
                Settings.Instance.Language = next.Id;
                Settings.Instance.ApplyLanguage();
            }
        }

        public static void OnPostLanguageLoad(On.Celeste.Dialog.orig_PostLanguageLoad orig)
        {
            orig();
            foreach (Language lang in Dialog.Languages.Values)
            {
                Fonts.Load(lang.FontFace);
                Logger.Log(nameof(AudioSplitterModule), $"Loaded fontface {lang.FontFace} for language {lang.Label}");
            }
        }
    }
}

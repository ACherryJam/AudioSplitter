using System;
using System.Reflection;
using Celeste.Mod.AudioSplitter.Extensions;
using Celeste.Mod.AudioSplitter.Utility;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AudioSplitter.Hooks
{
    public static class MenuButtonDisablingHooks
    {
        static Hook get_SelectionColor = null;

        [ApplyOnLoad]
        public static void Apply()
        {
            On.Celeste.MenuButton.Confirm += OnMenuButtonConfirm;

            get_SelectionColor = new Hook(
                typeof(MenuButton).GetMethod("get_SelectionColor", BindingFlags.Public | BindingFlags.Instance),
                getSelectionColor,
                true
            );
        }

        [RemoveOnUnload]
        public static void Remove()
        {
            On.Celeste.MenuButton.Confirm -= OnMenuButtonConfirm;
            
            get_SelectionColor.Dispose();
            get_SelectionColor = null;
        }

        public static Color getSelectionColor(Func<MenuButton, Color> orig, MenuButton self)
        {
            if (MenuButtonDisablingExtensions.IsDisabled(self))
                return Color.DarkSlateGray;
            return orig(self);
        }

        public static void OnMenuButtonConfirm(On.Celeste.MenuButton.orig_Confirm orig, MenuButton self)
        {
            if (self.IsDisabled())
            {
                global::Celeste.Audio.Play("event:/ui/main/button_invalid");
                return;
            }
            orig(self);
        }
    }
}

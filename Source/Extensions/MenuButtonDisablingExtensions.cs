using System.Collections.Generic;

namespace Celeste.Mod.AudioSplitter.Extensions
{
    public static class MenuButtonDisablingExtensions
    {
        public static HashSet<MenuButton> DisabledButtons { get; private set; } = new();

        public static void Disable(this MenuButton button)
        {
            DisabledButtons.Add(button);
        }

        public static void Enable(this MenuButton button)
        {
            DisabledButtons.Remove(button);
        }

        public static bool IsDisabled(this MenuButton button)
        {
            return DisabledButtons.Contains(button);
        }
    }
}

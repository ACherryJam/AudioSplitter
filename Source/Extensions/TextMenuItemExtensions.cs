using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.AudioSplitter.Extensions
{
    public static class TextMenuItemExtensions
    {
        public static TextMenuExt.EaseInSubHeaderExt GetDescriptionText(this TextMenu.Item option)
        {
            var menu = option.Container;
            var description = menu.Items[menu.IndexOf(option) + 1];

            if (description.GetType() == typeof(TextMenuExt.EaseInSubHeaderExt))
                return (TextMenuExt.EaseInSubHeaderExt)description;
            return null;
        }
    }
}

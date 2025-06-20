using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.AudioSplitter.UI
{
    public class DrawableCelesteElement : DrawableGameComponent
    {
        public static readonly int UI_WIDTH = 1920;
        public static readonly int UI_HEIGHT = 1080;

        public DrawableCelesteElement() : base(Celeste.Instance) { }
    }
}

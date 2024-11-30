using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.AudioSplitter.UI
{
    public class CelesteDrawComponent : DrawableGameComponent
    {
        public static readonly int GameWidth = 1920;
        public static readonly int GameHeight = 1080;

        public CelesteDrawComponent() : base(Celeste.Instance) { }

        public void AddToGame() => Game.Components.Add(this);
        public void RemoveFromGame() => Game.Components.Remove(this);
    }
}

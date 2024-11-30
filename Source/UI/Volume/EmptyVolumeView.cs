using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.AudioSplitter.UI.Volume
{
    public class EmptyVolumeView : VolumeView
    {
        public EmptyVolumeView(Vector2 position = default, Vector2 origin = default) : base(position, origin) {}

        public override float Width() => 0;
        public override float Height() => 0;

        public override void Render() { }
    }
}

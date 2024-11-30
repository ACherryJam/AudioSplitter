using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.AudioSplitter.UI.Volume
{
    public class MinimalisticVolumeView : VolumeView
    {
        protected float padding = 5f;
        protected float gap = 10f;

        public MinimalisticVolumeView(Vector2 position = default, Vector2 origin = default) : base(position, origin) {}



        protected float LeftWidth() => Math.Max(
            ActiveFont.Measure("SFX").X,
            ActiveFont.Measure("Music").X
        );
        protected float RightWidth() => Math.Max(
            ActiveFont.Measure(Settings.Instance.SFXVolume.ToString()).X,
            ActiveFont.Measure(Settings.Instance.MusicVolume.ToString()).X
        );

        public override float Width() => padding + LeftWidth() + gap + RightWidth() + padding;
        public override float Height() => padding + 2 * ActiveFont.LineHeight + padding;

        public override void Render()
        {
            var textColor = Color.White * Alpha;
            var strokeColor = Color.Black * Alpha * Alpha * Alpha;

            var pos = Position - Origin * new Vector2(Width(), Height());
            pos += Vector2.One * padding;

            ActiveFont.DrawOutline("SFX", pos, Vector2.Zero, Vector2.One, textColor, 2f, strokeColor);
            ActiveFont.DrawOutline(Settings.Instance.SFXVolume.ToString(), pos + Vector2.UnitX * (LeftWidth() + gap), Vector2.Zero, Vector2.One, textColor, 2f, strokeColor);
            
            pos.Y += ActiveFont.LineHeight;
            ActiveFont.DrawOutline("Music", pos, Vector2.Zero, Vector2.One, textColor, 2f, strokeColor);
            ActiveFont.DrawOutline(Settings.Instance.MusicVolume.ToString(), pos + Vector2.UnitX * (LeftWidth() + gap), Vector2.Zero, Vector2.One, textColor, 2f, strokeColor);
        }
    }
}

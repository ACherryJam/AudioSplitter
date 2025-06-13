using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.AudioSplitter.UI
{
    public class LoadingMessage : DrawableGameComponent
    {
        public static readonly int UI_WIDTH = 1920;
        public static readonly int UI_HEIGHT = 1080;

        public string Label;
        public Vector2 Position;

        private float imageFrame;
        private List<MTexture> loadingImages;

        private bool added = false;

        public LoadingMessage(Game game, string label, Vector2 position) : base(game)
        {
            Label = label;
            Position = position;

            imageFrame = 0f;
            loadingImages = OVR.Atlas.GetAtlasSubtextures("loading/");

            DrawOrder = int.MaxValue;
        }

        public void Add()
        {
            if (!added)
            {
                Celeste.Instance.Components.Add(this);
                added = true;
            }
        }

        public void Remove()
        {
            if (added)
            {
                Celeste.Instance.Components.Remove(this);
                added = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Celeste.Instance.Components.Remove(this);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            imageFrame += Engine.DeltaTime * 10f;
            imageFrame %= loadingImages.Count;
        }

        public override void Draw(GameTime gameTime)
        {
            Monocle.Draw.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Engine.ScreenMatrix
            );

            var position = Position;
            var labelSize = ActiveFont.Measure(Label);

            var texture = loadingImages[(int)imageFrame];
            // origin doesn't work, have to change position :(
            texture.Draw(position - Vector2.UnitY * labelSize.Y, Vector2.Zero, Color.White, labelSize.Y / texture.Height);
            
            position.X += 10f + labelSize.Y;
            ActiveFont.DrawOutline(
                Label,
                position,
                Vector2.UnitY,
                Vector2.One,
                Color.White,
                2f,
                Color.Black
            );

            Monocle.Draw.SpriteBatch.End();
        }
    }
}

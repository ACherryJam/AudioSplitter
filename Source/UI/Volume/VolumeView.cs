using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.AudioSplitter.UI.Volume
{
    public abstract class VolumeView : CelesteDrawComponent
    {
        public Vector2 Position;
        public Vector2 Origin;

        protected float Alpha = 0f;
        protected VisibilityState visibility = VisibilityState.Invisible;

        protected const float visibilityThreshold = 2f;
        protected float visibilityTimer = 0f;

        public VolumeView(Vector2 position = default, Vector2 origin = default)
        {
            Position = position;
            Origin = origin;

            DrawOrder = int.MaxValue;
            AddToGame();
        }

        protected override void Dispose(bool disposing)
        {
            RemoveFromGame();
            base.Dispose(disposing);
        }

        public override void Draw(GameTime gameTime)
        {
            RenderStart();
            Render();
            RenderEnd();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            switch (visibility)
            {
                case VisibilityState.ShowingUp:
                    if (Alpha < 1f)
                        Alpha = Math.Min(Alpha + Engine.DeltaTime * 6f, 1f);
                    else
                    {
                        visibility = VisibilityState.Visible;
                        visibilityTimer = 0f;
                    }
                    break;

                case VisibilityState.Visible:
                    visibilityTimer += Engine.DeltaTime;
                    if (visibilityTimer >= visibilityThreshold)
                        visibility = VisibilityState.Hiding;
                    break;

                case VisibilityState.Hiding:
                    if (Alpha > 0f)
                        Alpha = Math.Max(Alpha - Engine.DeltaTime * 4f, 0f);
                    else
                    {
                        visibility = VisibilityState.Invisible;
                    }
                    break;
            }
        }

        public void Show()
        {
            visibility = VisibilityState.ShowingUp;
            visibilityTimer = 0f;
        }

        public void RenderStart()
        {
            Monocle.Draw.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.Default,
                RasterizerState.CullNone,
                null,
                Engine.ScreenMatrix
            );
        }

        public void RenderEnd()
        {
            Monocle.Draw.SpriteBatch.End();
        }

        public abstract void Render();

        public abstract float Width();
        public abstract float Height();

        protected enum VisibilityState
        {
            Invisible,
            ShowingUp,
            Visible,
            Hiding
        }
    }
}

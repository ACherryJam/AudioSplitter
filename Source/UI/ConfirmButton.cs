using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.AudioSplitter.UI
{
    /// <summary>
    /// Heavily based on TextMenu.SubMenu
    /// </summary>
    public class ConfirmButton : TextMenu.Item
    {
        public string Label;
        public bool Focused = false;
        public bool PerformAction = false;
        public new Action OnPressed;

        protected float ease;
        protected Wiggler wiggler;

        private static readonly float optionGap = 40f;
        private static readonly float verticalGap = 4f;

        public ConfirmButton(string label) : base()
        {
            Label = label;
            Selectable = true;

            ease = 0f;
        }

        public override void Update()
        {
            if (Focused)
                ease = Calc.Approach(this.ease, 1f, Engine.RawDeltaTime * 4f);
            else
                ease = Calc.Approach(this.ease, 0f, Engine.RawDeltaTime * 4f);

            if (Focused && ease > 0.9f)
            {
                if (!Input.MenuConfirm.Pressed && (Input.MenuCancel.Pressed || Input.ESC.Pressed || Input.Pause.Pressed))
                    Unfocus();
                if (Input.MenuLeft.Pressed)
                    LeftPressed();
                if (Input.MenuRight.Pressed)
                    RightPressed();
                if (Input.MenuConfirm.Pressed)
                    ConfirmPressed();
            }
        }

        public override void LeftPressed()
        {
            base.LeftPressed();
            if (Focused && PerformAction)
            {
                PerformAction = false;
                wiggler.StopAndClear();
                wiggler.Start();
                global::Celeste.Audio.Play("event:/ui/main/button_toggle_off");
            }
        }

        public override void RightPressed()
        {
            base.RightPressed();
            if (Focused && !PerformAction)
            {
                PerformAction = true;
                wiggler.StopAndClear();
                wiggler.Start();
                global::Celeste.Audio.Play("event:/ui/main/button_toggle_on");
            }
        }

        public override void ConfirmPressed()
        {
            if (!Focused)
            {
                if (Disabled)
                {
                    global::Celeste.Audio.Play("event:/ui/main/button_invalid");
                    return;
                }
                Container.Focused = false;
                Focused = true;
                wiggler.StopAndClear();
            }
            else
            {
                if (PerformAction)
                    OnPressed();
                Unfocus();
            }
            global::Celeste.Audio.Play("event:/ui/main/button_select");
        }

        public void Unfocus()
        {
            Container.Focused = true;
            Focused = false;
            PerformAction = false;
            global::Celeste.Audio.Play("event:/ui/main/button_back");
        }

        public new ConfirmButton Pressed(Action onPressed)
        {
            OnPressed = onPressed;
            return this;
        }

        public override void Added()
        {
            base.Added();
            Container.Add(wiggler = Wiggler.Create(0.25f, 3f, null, false, false));
            wiggler.UseRawDeltaTime = true;
        }

        public override string SearchLabel() => Label;

        public override float LeftWidth()
        {
            return Calc.Max(
                ActiveFont.Measure(Dialog.Clean("AUDIOSPLITTER_CONFIRMBUTTON_CONFIRMATION")).X,
                ActiveFont.Measure(Label).X
            );
        }

        public override float RightWidth()
        {
            if (Focused && ease > 0.9f)
            {
                return ActiveFont.Measure(Dialog.Clean("AUDIOSPLITTER_CONFIRMBUTTON_NO")).X +
                       ActiveFont.Measure(Dialog.Clean("AUDIOSPLITTER_CONFIRMBUTTON_YES")).X +
                       optionGap;
            }
            return 0f;
        }

        public override float Height()
        {
            return ActiveFont.LineHeight + ActiveFont.LineHeight * Ease.QuadOut(ease);
        }

        public override void Render(Vector2 position, bool highlighted)
        {
            Vector2 top = new Vector2(position.X, position.Y - (Height() / 2));

            float alpha = Container.Alpha;
            Color color = Disabled ? Color.DarkSlateGray : ((highlighted && !Focused ? Container.HighlightColor : Color.White) * alpha);
            Color strokeColor = Color.Black * (alpha * alpha * alpha);

            position = top + (Vector2.UnitY * ActiveFont.LineHeight / 2);
            ActiveFont.DrawOutline(Label, position, new Vector2(0f, 0.5f), Vector2.One, color, 2f, strokeColor);

            if (Focused && ease > 0.9f)
            {
                position += Vector2.UnitY * (ActiveFont.LineHeight + verticalGap);
                ActiveFont.DrawOutline(
                    Dialog.Clean("AUDIOSPLITTER_CONFIRMBUTTON_CONFIRMATION"),
                    position, new Vector2(0f, 0.5f), Vector2.One,
                    Color.White * alpha,
                    2f, strokeColor
                );

                position.X += Container.Width;
                position.X -= ActiveFont.Measure(Dialog.Clean("AUDIOSPLITTER_CONFIRMBUTTON_YES")).X;
                ActiveFont.DrawOutline(
                    Dialog.Clean("AUDIOSPLITTER_CONFIRMBUTTON_YES"),
                    position + Vector2.UnitY * (PerformAction ? wiggler.Value : 0f) * 8f,
                    new Vector2(0f, 0.5f), Vector2.One,
                    (PerformAction ? Container.HighlightColor : Color.White) * alpha,
                    2f, strokeColor
                );

                position.X -= optionGap;
                position.X -= ActiveFont.Measure(Dialog.Clean("AUDIOSPLITTER_CONFIRMBUTTON_NO")).X;
                ActiveFont.DrawOutline(
                    Dialog.Clean("AUDIOSPLITTER_CONFIRMBUTTON_NO"),
                    position + Vector2.UnitY * (!PerformAction ? wiggler.Value : 0f) * 8f,
                    new Vector2(0f, 0.5f), Vector2.One,
                    (!PerformAction ? Color.OrangeRed : Color.White) * alpha,
                    2f, strokeColor
                );
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;

using Monocle;

namespace Celeste.Mod.AudioSplitter.UI
{
    public class DropdownMenu<T> : TextMenuExt.SubMenu
    {
        public Action<T> OnOptionSelect;

        protected T option;
        protected String optionLabel = "";

        public T Option
        {
            get { return option; }
            set
            {
                option = value;
                this.optionLabel = value.ToString();
            }
        }
        protected List<T> Options = new List<T>();

        protected bool OptionInList
        {
            get
            {
                foreach (T option in Options)
                {
                    if (EqualityComparer<T>.Default.Equals(this.option, option))
                        return true;
                }
                return false;
            }
        }


        FieldInfo Icon;
        FieldInfo ease;

        public DropdownMenu(String label) : base(label, false)
        {
            Icon = typeof(DropdownMenu<T>).BaseType.GetField("Icon", BindingFlags.Instance | BindingFlags.NonPublic);
            ease = typeof(DropdownMenu<T>).BaseType.GetField("ease", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public DropdownMenu(String label, T option) : this(label)
        {
            Option = option;
        }

        public void SetOption(T option)
        {
            Option = option;
            OnOptionSelect(option);
        }

        public T GetOption()
        {
            return Option;
        }

        public override float RightWidth()
        {
            return MultiLanguageFont.Measure(optionLabel).X + ((MTexture)Icon.GetValue(this)).Width;
        }

        public void Add(T option)
        {
            if (option == null)
                Option = option;
            this.Options.Add(option);
            base.Add(new DropdownButton<T>(option, this));
        }

        public void Add(List<T> list)
        {
            foreach (T option in list)
                Add(option);
        }

        public new void Clear()
        {
            base.Clear();
            this.Options.Clear();
        }

        public String GetOptionLabel()
        {
            float width = MultiLanguageFont.Measure(optionLabel).X;
            float overflow_width = Container.Width - LeftWidth() - 100f;

            if (width > overflow_width)
            {
                String new_label = optionLabel;
                int length = optionLabel.Length;

                while (MultiLanguageFont.Measure(new_label + "...").X > overflow_width)
                    new_label = new_label.Substring(0, --length);
                new_label += "...";

                optionLabel = new_label;
            }

            return optionLabel;
        }

        public override void Render(Vector2 position, bool highlighted)
        {
            float _ease = (float)ease.GetValue(this);
            MTexture icon = (MTexture)Icon.GetValue(this);

            Vector2 top = new Vector2(position.X, position.Y - (Height() / 2));

            float alpha = Container.Alpha;
            Color color = Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : Color.White) * alpha);
            Color strokeColor = Color.Black * (alpha * alpha * alpha);

            bool uncentered = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;

            Vector2 titlePosition = top + (Vector2.UnitY * TitleHeight / 2) + (uncentered ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
            Vector2 justify = new Vector2(0f, 0.5f);
            ActiveFont.DrawOutline(Label, titlePosition, justify, Vector2.One, color, 2f, strokeColor);

            Vector2 position2 = titlePosition + new Vector2(Container.Width - RightWidth() - icon.Width - 10f, 0);
            Color itemColor = OptionInList ? color : Color.DarkSlateGray;
            MultiLanguageFont.DrawOutline(GetOptionLabel(), position2, justify, Vector2.One, itemColor, 2f, strokeColor);

            DrawIcon(position2, icon, new Vector2(MultiLanguageFont.Measure(GetOptionLabel()).X + icon.Width, 5f), true, (Disabled || Items.Count < 1 ? Color.DarkSlateGray : (Focused ? Container.HighlightColor : Color.White)) * alpha, 0.8f);

            if (Focused && _ease > 0.9f)
            {
                Vector2 menuPosition = new Vector2(top.X + ItemIndent, top.Y + TitleHeight + ItemSpacing);
                RecalculateSize();
                foreach (TextMenu.Item item in Items)
                {
                    if (item.Visible)
                    {
                        float height = item.Height();
                        Vector2 itemPosition = menuPosition + new Vector2(0f, height * 0.5f + item.SelectWiggler.Value * 8f);
                        if (itemPosition.Y + height * 0.5f > 0f && itemPosition.Y - height * 0.5f < Engine.Height)
                        {
                            item.Render(itemPosition, Focused && Current == item);
                        }
                        menuPosition.Y += height + ItemSpacing;
                    }
                }
            }
        }

        private static void DrawIcon(Vector2 position, MTexture icon, Vector2 justify, bool outline, Color color, float scale)
        {
            if (outline)
            {
                icon.DrawOutlineCentered(position + justify, color);
                return;
            }
            icon.DrawCentered(position + justify, color, scale);
        }

        internal class DropdownButton<T> : TextMenu.Button
        {
            protected T Option;
            public DropdownMenu<T> Parent;
            public DropdownButton(T option) : base(option.ToString())
            {
                this.Option = option;
            }

            public DropdownButton(T option, DropdownMenu<T> dropdownMenu) : this(option)
            {
                this.Parent = dropdownMenu;

                OnPressed += () => {
                    this.Parent.SetOption(Option);
                    this.Parent.Exit();
                };
            }

            public override float LeftWidth()
            {
                return 0f;
            }

            public override float RightWidth()
            {
                return MultiLanguageFont.Measure(Label).X;
            }

            public override void Render(Vector2 position, bool highlighted)
            {
                float alpha = Container.Alpha;
                Color color = (Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : Color.White) * alpha));
                Color strokeColor = Color.Black * (alpha * alpha * alpha);

                Vector2 position2 = position + new Vector2(Container.Width - RightWidth(), 0);
                MultiLanguageFont.DrawOutline(Label, position2, new Vector2(0f, 0.5f), Vector2.One, color, 2f, strokeColor);
            }
        }
    }
}

using System.Reflection;
using System.Collections.Generic;

using Monocle;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.AudioSplitter.UI
{
    public static class MultiLanguageFont
    {
        private static FieldInfo loadedFonts = typeof(Fonts).GetField("loadedFonts", BindingFlags.NonPublic | BindingFlags.Static);
        private static Dictionary<string, PixelFont> LoadedFonts
        {
            get
            {
                return (Dictionary<string, PixelFont>)loadedFonts.GetValue(null);
            }
        }

        public static bool ReplaceUnknownChars = true;

        public static Vector2 Measure(string text)
        {
            float base_size = ActiveFont.BaseSize;
            PixelFontSize font_size = ActiveFont.Font.Get(base_size);

            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            int lines = 1;
            float max_height = font_size.LineHeight;

            Vector2 vector = new Vector2(0f, 0f);
            float width = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lines++;
                    if (width > vector.X)
                        vector.X = width;

                    width = 0f;
                }
                else
                {
                    PixelFontCharacter c = null;

                    if (!font_size.Characters.TryGetValue(text[i], out c))
                    {
                        foreach (PixelFont replacement_font in LoadedFonts.Values)
                        {
                            PixelFontSize fontsize = replacement_font.Get(base_size);
                            if (fontsize.Characters.TryGetValue(text[i], out c))
                            {
                                if (fontsize.LineHeight > max_height)
                                    max_height = fontsize.LineHeight;
                                break;
                            }
                        }
                    }

                    if (c == null && ReplaceUnknownChars)
                        c = font_size.Get('?');

                    if (c != null)
                    {
                        width += c.XAdvance;
                        if (i < text.Length - 1 && c.Kerning.TryGetValue(text[i + 1], out int kerning))
                            width += kerning;
                    }
                }
            }

            if (width > vector.X)
                vector.X = width;
            vector.Y = lines * max_height;
            return vector;
        }

        public static void Draw(string text, Vector2 position, Vector2 justify, Vector2 scale, Color color, float edgeDepth, Color edgeColor, float stroke, Color strokeColor)
        {
            float base_size = ActiveFont.BaseSize;
            PixelFontSize font = ActiveFont.Font.Get(base_size);

            text = Emoji.Apply(text);

            if (string.IsNullOrEmpty(text))
                return;

            Vector2 offset = Vector2.Zero;
            Vector2 justifyOffs = new Vector2(
                ((justify.X != 0f) ? font.WidthToNextLine(text, 0) : 0f) * justify.X,
                font.HeightOf(text) * justify.Y
            );

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    offset.X = 0f;
                    offset.Y += font.LineHeight;
                    if (justify.X != 0f)
                        justifyOffs.X = font.WidthToNextLine(text, i + 1) * justify.X;
                    continue;
                }

                PixelFontCharacter c = null;
                if (!font.Characters.TryGetValue(text[i], out c))
                {
                    foreach (PixelFont replacement_font in LoadedFonts.Values)
                    {
                        PixelFontSize fontsize = replacement_font.Get(base_size);
                        if (fontsize.Characters.TryGetValue(text[i], out c))
                        {
                            break;
                        }
                    }

                    if (c == null && ReplaceUnknownChars)
                        c = font.Get('?');

                    if (c == null)
                        continue;
                }

                Vector2 pos = position + (offset + new Vector2(c.XOffset, c.YOffset) - justifyOffs) * scale;
                if (stroke > 0f && !font.Outline)
                {
                    if (edgeDepth > 0f)
                    {
                        c.Texture.Draw(pos + new Vector2(0f, -stroke), Vector2.Zero, strokeColor, scale);
                        for (float num2 = -stroke; num2 < edgeDepth + stroke; num2 += stroke)
                        {
                            c.Texture.Draw(pos + new Vector2(-stroke, num2), Vector2.Zero, strokeColor, scale);
                            c.Texture.Draw(pos + new Vector2(stroke, num2), Vector2.Zero, strokeColor, scale);
                        }
                        c.Texture.Draw(pos + new Vector2(-stroke, edgeDepth + stroke), Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(0f, edgeDepth + stroke), Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(stroke, edgeDepth + stroke), Vector2.Zero, strokeColor, scale);
                    }
                    else
                    {
                        c.Texture.Draw(pos + new Vector2(-1f, -1f) * stroke, Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(0f, -1f) * stroke, Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(1f, -1f) * stroke, Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(-1f, 0f) * stroke, Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(1f, 0f) * stroke, Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(-1f, 1f) * stroke, Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(0f, 1f) * stroke, Vector2.Zero, strokeColor, scale);
                        c.Texture.Draw(pos + new Vector2(1f, 1f) * stroke, Vector2.Zero, strokeColor, scale);
                    }
                }

                if (edgeDepth > 0f)
                    c.Texture.Draw(pos + Vector2.UnitY * edgeDepth, Vector2.Zero, edgeColor, scale);

                Color cColor = color;
                if (Emoji.Start <= c.Character &&
                    c.Character <= Emoji.Last &&
                    !Emoji.IsMonochrome((char)c.Character))
                {
                    cColor = new Color(color.A, color.A, color.A, color.A);
                }
                c.Texture.Draw(pos, Vector2.Zero, cColor, scale);

                offset.X += c.XAdvance;

                if (i < text.Length - 1 && c.Kerning.TryGetValue(text[i + 1], out int kerning))
                    offset.X += kerning;
            }
        }

        public static void DrawOutline(string text, Vector2 position, Vector2 justify, Vector2 scale, Color color, float stroke, Color strokeColor)
        {
            Draw(text, position, justify, scale, color, 0f, Color.Transparent, stroke, strokeColor);
        }
    }
}

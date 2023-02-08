using Cornifer.UI.Elements;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cornifer.UI
{
    public class ColorSelector : UIPanel
    {
        public delegate void ColorChangedDelegate(bool? result, Color color);

        static readonly Texture2D Gradient, Hue;
        static readonly Regex RGBRegex = new("^\\s*([0-9]{1,3}),\\s*([0-9]{1,3}),\\s*([0-9]{1,3})\\s*$", RegexOptions.Compiled);
        static readonly Regex HEXRegex = new("^([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})$", RegexOptions.Compiled);
        static readonly Regex HEXRegexShort = new("^([0-9a-fA-F]{1})([0-9a-fA-F]{1})([0-9a-fA-F]{1})$", RegexOptions.Compiled);

        UpdateSource UpdateSrc = UpdateSource.None;

        Vector2 GradientPos;
        float HuePos;

        Rectangle GradientRect = new(5, 25, 128, 128);
        Rectangle HueRect = new(138, 25, 15, 128);

        bool DraggingGradient;
        bool DraggingHue;

        UILabel TitleLabel;
        UIInput RGBInput;
        UIInput HEXInput;

        ColorChangedDelegate? Callback;
        Color OriginalColor;

        public Color CurrentColor = Color.White;

        static ColorSelector()
        {
            Gradient = new(Main.Instance.GraphicsDevice, 256, 256);
            Hue = new(Main.Instance.GraphicsDevice, 1, 360);

            Color[] gradientColors = new Color[256 * 256];
            for (int j = 0; j < 256; j++)
                for (int i = 0; i < 256; i++)
                {
                    float fx = i / 255f;
                    float fy = 1 - j / 255f;

                    float a = 1 - (fx * fy);

                    byte ab = (byte)(a * 255);
                    byte gs = (byte)(fy * (1 - fx) * 255);

                    gradientColors[i + j * 256] = new(gs, gs, gs, ab);
                }
            Gradient.SetData(gradientColors);

            Color[] hueColors = new Color[360];
            for (int i = 0; i < 360; i++)
            {
                hueColors[i] = HSVToRGB(i, 1, 1);
            }
            Hue.SetData(hueColors);
        }

        public ColorSelector()
        {
            Width = 158;
            Height = 227;

            Padding = 4;

            Elements.Add(TitleLabel = new UILabel
            {
                Top = 0,
                Height = 20,
                TextAlign = new(.5f),
                Text = "Title"
            });

            Elements.Add(new UILabel 
            {
                Top = 156,
                Height = 20,
                Width = 30,
                Text = "RGB:"
            });

            Elements.Add(RGBInput = new UIInput
            {
                Top = 153,
                Left = new(0, 1, -1),
                Height = 20,
                Width = new(-30, 1),
                Multiline = false,
                Text = "0, 0, 0",
            }.BeforeEvent(UIInput.CharacterTypedEvent, (inp, chr) => 
            {
                return char.IsDigit(chr.Character) || chr.Character == ',' || chr.Character == ' ';
            })
            .OnEvent(UIInput.TextChangedEvent, (inp, _) => RGBInputTextChanged(inp)));

            Elements.Add(new UILabel
            {
                Top = 179,
                Height = 20,
                Width = 30,
                Text = "HEX:"
            });

            Elements.Add(HEXInput = new UIInput
            {
                Top = 176,
                Left = new(0, 1, -1),
                Height = 20,
                Width = new(-30, 1),
                Multiline = false,
                Text = "000000",
            }.BeforeEvent(UIInput.CharacterTypedEvent, (inp, chr) =>
            {
                return char.IsDigit(chr.Character) || chr.Character >= 'A' && chr.Character <= 'F' || chr.Character >= 'a' && chr.Character <= 'f';
            })
            .OnEvent(UIInput.TextChangedEvent, (inp, _) => HEXInputTextChanged(inp)));

            Elements.Add(new UIButton
            {
                Top = 199,
                Left = new(-32, .5f, -.5f),
                Height = 20,
                Width = 60,
                Text = "Apply",
                TextAlign = new(.5f),
            }.OnEvent(UIElement.ClickEvent, (_, _) => 
            {
                Visible = false;
                Callback?.Invoke(true, CurrentColor with { A = OriginalColor.A });
            }));

            Elements.Add(new UIButton
            {
                Top = 199,
                Left = new(31, .5f, -.5f),
                Height = 20,
                Width = 60,
                Text = "Cancel",
                TextAlign = new(.5f),
            }.OnEvent(UIElement.ClickEvent, (_, _) =>
            {
                Visible = false;
                Callback?.Invoke(false, OriginalColor);
            }));

            ColorChanged();
        }

        public void Show(string title, Color color, ColorChangedDelegate callback)
        {
            Visible = true;
            TitleLabel.Text = title;
            CurrentColor = color;
            OriginalColor = color;
            Callback = callback;
            ColorChanged();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            Vector2 gradPos = ScreenRect.Position + GradientRect.Location.ToVector2();

            var state = spriteBatch.GetState();
            spriteBatch.End();
            spriteBatch.Begin(blendState: BlendState.AlphaBlend);

            spriteBatch.DrawRect(gradPos - Vector2.One, GradientRect.Size.ToVector2() + new Vector2(2), HSVToRGB(HuePos * 360, 1, 1), new(100, 100, 100));
            spriteBatch.Draw(Gradient, new Rectangle(gradPos.ToPoint(), GradientRect.Size), Color.White);

            spriteBatch.End();
            spriteBatch.Begin(state);

            Vector2 huePos = ScreenRect.Position + HueRect.Location.ToVector2();
            spriteBatch.DrawRect(huePos - Vector2.One, HueRect.Size.ToVector2() + new Vector2(2), HSVToRGB(HuePos * 360, 1, 1), new(100, 100, 100));
            spriteBatch.Draw(Hue, new Rectangle(huePos.ToPoint(), HueRect.Size), Color.White);

            Vector2 gradPointerCenter = GradientPos * GradientRect.Size.ToVector2() + gradPos;
            spriteBatch.DrawRect(gradPointerCenter - new Vector2(3), new Vector2(7), HSVToRGB(HuePos * 360, GradientPos.X, 1 - GradientPos.Y), HSVToRGB(0, 0, 1 - (1 - GradientPos.Y) * (1 - GradientPos.X)));

            Vector2 huePointerLeft = new Vector2(0, HuePos * HueRect.Height) + huePos;
            spriteBatch.DrawRect(huePointerLeft - new Vector2(2, 1), new Vector2(HueRect.Width + 4, 3), HSVToRGB(HuePos * 360, 1, 1), Color.White);
        }
        protected override void UpdateSelf()
        {
            base.UpdateSelf();

            switch (Root.MouseLeftKey)
            {
                case KeybindState.JustPressed:
                    if (GradientRect.Contains((Point)RelativeMouse))
                        DraggingGradient = true;

                    if (HueRect.Contains((Point)RelativeMouse))
                        DraggingHue = true;
                    break;

                case KeybindState.Pressed:
                    if (DraggingGradient)
                    {
                        Vector2 newGradientPos = (RelativeMouse - GradientRect.Location.ToVector2()) / GradientRect.Size.ToVector2();
                        newGradientPos.Clamp01();

                        if (newGradientPos != GradientPos)
                        {
                            GradientPos = newGradientPos;
                            UpdateSrc = UpdateSource.GradientHue;
                            CurrentColor = HSVToRGB(HuePos * 360, GradientPos.X, 1 - GradientPos.Y);
                            ColorChanged();
                            UpdateSrc = UpdateSource.None;
                        }
                    }
                    if (DraggingHue)
                    {
                        float newHuePos = Math.Clamp((RelativeMouse.Y - HueRect.Y) / HueRect.Height, 0, 1);
                        if (newHuePos != HuePos)
                        {
                            HuePos = newHuePos;
                            UpdateSrc = UpdateSource.GradientHue;
                            CurrentColor = HSVToRGB(HuePos * 360, GradientPos.X, 1 - GradientPos.Y);
                            ColorChanged();
                            UpdateSrc = UpdateSource.None;
                        }
                    }
                    break;

                case KeybindState.JustReleased:
                    DraggingGradient = false;
                    DraggingHue = false;
                    break;
            }
        }

        void RGBInputTextChanged(UIInput input)
        {
            if (UpdateSrc != UpdateSource.None)
            {
                input.BorderColor = new(100, 100, 100);
                return;
            }

            Match match = RGBRegex.Match(input.Text);
            if (!match.Success)
            {
                input.BorderColor = Color.Maroon;
                return;
            }

            input.BorderColor = new(100, 100, 100);

            byte r = (byte)Math.Min(255, int.Parse(match.Groups[1].ValueSpan));
            byte g = (byte)Math.Min(255, int.Parse(match.Groups[2].ValueSpan));
            byte b = (byte)Math.Min(255, int.Parse(match.Groups[3].ValueSpan));

            CurrentColor = new(r, g, b);
            UpdateSrc = UpdateSource.RGBInput;
            ColorChanged();
            UpdateSrc = UpdateSource.None;
        }
        void HEXInputTextChanged(UIInput input)
        {
            if (UpdateSrc != UpdateSource.None)
            {
                input.BorderColor = new(100, 100, 100);
                return;
            }
            string text = input.Text;
            Match match = HEXRegex.Match(text);

            byte r, g, b;
            if (match.Success)
            {
                r = byte.Parse(match.Groups[1].ValueSpan, NumberStyles.HexNumber);
                g = byte.Parse(match.Groups[2].ValueSpan, NumberStyles.HexNumber);
                b = byte.Parse(match.Groups[3].ValueSpan, NumberStyles.HexNumber);
            }
            else
            {
                match = HEXRegexShort.Match(text);
                if (match.Success)
                {
                    r = byte.Parse(match.Groups[1].ValueSpan, NumberStyles.HexNumber);
                    g = byte.Parse(match.Groups[2].ValueSpan, NumberStyles.HexNumber);
                    b = byte.Parse(match.Groups[3].ValueSpan, NumberStyles.HexNumber);

                    r |= (byte)(r << 4);
                    g |= (byte)(g << 4);
                    b |= (byte)(b << 4);
                }
                else
                {
                    input.BorderColor = Color.Maroon;
                    return;
                }
            }

            input.BorderColor = new(100, 100, 100);
            CurrentColor = new(r, g, b);
            UpdateSrc = UpdateSource.HEXInput;
            ColorChanged();
            UpdateSrc = UpdateSource.None;
        }

        void ColorChanged()
        {
            if (UpdateSrc != UpdateSource.GradientHue)
            {
                (float hue, float saturation, float value) = RGBToHSV(CurrentColor);
                HuePos = hue / 360;
                GradientPos = new(saturation, 1 - value);
            }

            if (UpdateSrc != UpdateSource.RGBInput)
            {
                RGBInput.Text = $"{CurrentColor.R}, {CurrentColor.G}, {CurrentColor.B}";
            }

            if (UpdateSrc != UpdateSource.HEXInput)
            {
                HEXInput.Text = $"{CurrentColor.R:x2}{CurrentColor.G:x2}{CurrentColor.B:x2}";
            }

            Callback?.Invoke(null, CurrentColor with { A = OriginalColor.A });
        }

        static Color HSVToRGB(float hue, float saturation, float value)
        {
            if (saturation <= 0)
            {
                return new(value, value, value);
            }

            hue %= 360;

            int part = (int)(hue / 60);
            float f = (hue % 60) / 60;

            float p = value * (1 - saturation);
            float q = value * (1 - (saturation * f));
            float t = value * (1 - (saturation * (1 - f)));

            switch (part)
            {
                case 0: return new(value, t, p);
                case 1: return new(q, value, p);
                case 2: return new(p, value, t);
                case 3: return new(p, q, value);
                case 4: return new(t, p, value);
                default:return new(value, p, q);
            }
        }
        static (float hue, float saturation, float value) RGBToHSV(Color color)
        {
            float delta, min;
            float h = 0, s, v;

            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            min = Math.Min(Math.Min(r, g), b);
            v = Math.Max(Math.Max(r, g), b);
            delta = v - min;

            if (v == 0.0)
                s = 0;
            else
                s = delta / v;

            if (s == 0)
                h = 0;

            else
            {
                if (r == v)
                    h = (g - b) / delta;
                else if (g == v)
                    h = 2 + (b - r) / delta;
                else if (b == v)
                    h = 4 + (r - g) / delta;

                h *= 60;

                if (h < 0.0)
                    h += 360;
            }

            return (h, s, v);
        }

        enum UpdateSource
        {
            None,
            GradientHue,
            RGBInput,
            HEXInput
        }
    }
}

using Cornifer.Renderers;
using Cornifer.UI;
using Cornifer.UI.Elements;
using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public class MapText : SelectableIcon
    {
        const string EmptyTextString = "[c:9](empty text)[/c]";

        private string text = "";
        private Vector2 size;
        private SpriteFont font;
        private float scale = 1;

        public override Vector2 Size => size + new Vector2(10);

        public Color Color = Color.White;
        public bool Shade;
        public Color ShadeColor = Color.Black;

        public string Text
        {
            get => text;
            set
            {
                text = value;
                ParamsChanged();
            }
        }
        public SpriteFont Font
        {
            get => font;
            set
            {
                font = value;
                ParamsChanged();
            }
        }
        public float Scale
        {
            get => scale;
            set
            {
                scale = value;
                ParamsChanged();
            }
        }

        public MapText()
        {
            font = Content.RodondoExt20;
        }

        public MapText(string name, SpriteFont font, string text)
        {
            Name = name;
            this.font = font;
            Text = text;
        }

        void ParamsChanged()
        {
            string text = Text.Length == 0 ? EmptyTextString : Text;
            size = Font is null ? Vector2.Zero : FormattedText.Measure(Font, text, Scale);

            IconPosAlign = new(Math.Min((Size.Y / 2) / Size.X, .5f), .5f);
        }

        public override void DrawIcon(Renderer renderer)
        {
            Vector2 capturePos = Vector2.Zero;
            SpriteBatchState spriteBatchState = default;

            if (renderer is CaptureRenderer capture)
            {
                capturePos = capture.Position;
                capture.Position = WorldPosition;
                spriteBatchState = Main.SpriteBatch.GetState();
                Main.SpriteBatch.End();
                capture.BeginCapture((int)Size.X, (int)Size.Y);
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            }

            string text = Text.Length == 0 ? EmptyTextString : Text;
            FormattedText.Draw(Main.SpriteBatch, Font, text, renderer.TransformVector(WorldPosition + new Vector2(5)), Color, Shade ? ShadeColor : null, scale * renderer.Scale);

            if (renderer is CaptureRenderer captureEnd)
            {
                captureEnd.Position = capturePos;
                Main.SpriteBatch.End();
                captureEnd.EndCapture(WorldPosition, (int)Size.X, (int)Size.Y);
                Main.SpriteBatch.Begin(spriteBatchState);
            }
        }

        protected override void BuildInnerConfig(UIList list)
        {
            list.Elements.Add(new UIResizeablePanel
            {
                Height = 100,

                CanGrabTop = false,
                CanGrabLeft = false,
                CanGrabRight = false,
                CanGrabBottom = true,

                BackColor = Color.Transparent,
                BorderColor = Color.Transparent,

                Elements =
                {
                    new UIInput
                    {
                        Text = Text,
                    }.OnEvent(UIInput.TextChangedEvent, (inp, _) => { if (inp.Active) Text = inp.Text; })
                }
            });
            list.Elements.Add(new UIButton
            {
                Text = "View formatting guide",
                Height = 20
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.TextFormattingVisible = true));
            list.Elements.Add(new UIElement { Height = 10 });
            list.Elements.Add(new UIButton
            {
                Text = "Set text color",
                Height = 20
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Text color", Color, (_, c) => Color = c)));
            list.Elements.Add(new UIButton
            {
                Text = "Set shade color",
                Height = 20
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Text shade color", ShadeColor, (_, c) => ShadeColor = c)));
            list.Elements.Add(new UIButton
            {
                Text = "Shade enabled",
                Height = 20,

                Selectable = true,
                Selected = Shade,

                SelectedTextColor = Color.Black,
                SelectedBackColor = Color.White,

            }.OnEvent(UIElement.ClickEvent, (btn, _) => Shade = btn.Selected));
            list.Elements.Add(new UIPanel
            {
                Height = 27,
                Padding = 4,

                Elements =
                {
                    new UILabel
                    {
                        Top = 3,
                        Width = 50,
                        Height = 20,
                        Text = "Scale:",
                    },
                    new UINumberInput
                    {
                        AllowNegative = false,

                        Width = new(-50, 1),
                        Left = 50,
                        Value = Scale,

                    }.OnEvent(UINumberInput.ValueChanged, (inp, _) => Scale = Math.Max(0.1f, (float)inp.Value))
                    .OnEvent(UIElement.ActiveChangedEvent, (inp, act) =>
                    {
                        if (!act && inp.Value < 0.1f)
                            inp.Value = 0.1f;
                    }),
                }
            });
            list.Elements.Add(new UIResizeablePanel
            {
                Height = 100,

                Padding = 4,

                CanGrabTop = false,
                CanGrabLeft = false,
                CanGrabRight = false,
                CanGrabBottom = true,

                Elements =
                {
                    new UILabel
                    {
                        Text = "Font",
                        Height = 15,
                        TextAlign = new(.5f)
                    },
                    new UIList
                    {
                        Top = 20,
                        Height = new(-20, 1),
                        ElementSpacing = 4
                    }.Execute(FillFontList)
                }
            });
        }

        void FillFontList(UIList list)
        {
            RadioButtonGroup group = new();

            foreach (var (name, font) in Cornifer.Content.Fonts)
            {
                UIButton button = new()
                {
                    Text = name,
                    Height = 20,
                    TextAlign = new(.5f),
                    RadioGroup = group,
                    Selectable = true,
                    Selected = Font == font,
                    RadioTag = font,
                    SelectedTextColor = Color.Black,
                    SelectedBackColor = Color.White,
                };

                list.Elements.Add(button);
            }

            group.ButtonClicked += (_, tag) =>
            {
                if (tag is not SpriteFont font)
                    return;
                Font = font;
            };
        }

        protected override JsonNode? SaveInnerJson()
        {
            return new JsonObject
            {
                ["text"] = Text,
                ["font"] = Content.Fonts.FirstOrDefault(kvp => kvp.Value == Font).Key,
                ["scale"] = Scale,
                ["color"] = Color.PackedValue,
                ["shade"] = Shade,
                ["shadeColor"] = ShadeColor.PackedValue,
            };
        }

        protected override void LoadInnerJson(JsonNode node)
        {
            if (node.TryGet("text", out string? text))
                this.text = text;

            if (node.TryGet("font", out string? font))
                this.font = Content.Fonts.GetValueOrDefault(font) ?? Content.RodondoExt20;

            if (node.TryGet("scale", out float scale))
                this.scale = scale;

            if (node.TryGet("color", out uint color))
                Color.PackedValue = color;

            if (node.TryGet("shade", out bool shade))
                Shade = shade;

            if (node.TryGet("shadeColor", out uint shadeColor))
                ShadeColor.PackedValue = shadeColor;

            ParamsChanged();
        }
    }
}

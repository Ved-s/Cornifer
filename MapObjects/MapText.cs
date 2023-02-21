using Cornifer.Json;
using Cornifer.MapObjects;
using Cornifer.Renderers;
using Cornifer.UI;
using Cornifer.UI.Elements;
using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.Fonts;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public class MapText : SelectableIcon
    {
        const string EmptyTextString = "[c:9](empty text)[/c]";

        private Vector2 size;

        public override Vector2 Size => size + new Vector2(10);

        public ObjectProperty<Color> Color = new("color", Microsoft.Xna.Framework.Color.White);
        public ObjectProperty<bool> Shade = new("shade", true);
        public ObjectProperty<Color> ShadeColor = new("shadeColor", Microsoft.Xna.Framework.Color.Black);

        public ObjectProperty<string> Text = new("text", "");
        public ObjectProperty<SpriteFont, string> Font = new("font", null!);
        public ObjectProperty<float> Scale = new("scale", 1);

        public override int ShadeSize => 5;
        public override RenderLayers RenderLayer => RenderLayers.Texts;

        Texture2D? TextShadeTexture;
        bool TextShadeTextureDirty;

        public MapText()
        {
            Font.OriginalValue = Main.DefaultSmallMapFont;

            Text.ValueChanged = ParamsChanged;
            Font.ValueChanged = ParamsChanged;
            Scale.ValueChanged = ParamsChanged;

            Font.SaveValue = f => Content.Fonts.FirstOrDefault(kvp => kvp.Value == f, new("", null!)).Key;
            Font.LoadValue = s => Content.Fonts.GetValueOrDefault(s) ?? Main.DefaultSmallMapFont;
        }

        public MapText(string name, SpriteFont font, string text) : this()
        {
            Name = name;
            Font.OriginalValue = font;
            Text.OriginalValue = text;
            ParamsChanged();
        }

        void ParamsChanged()
        {
            string text = Text.Value.Length == 0 ? EmptyTextString : Text.Value;
            size = Font is null ? Vector2.Zero : FormattedText.Measure(Font.Value, text, Scale.Value);

            IconPosAlign = new(Math.Min((Size.Y / 2) / Size.X, .5f), .5f);
            TextShadeTextureDirty = true;
            ShadeTextureDirty = true;
        }

        public override void DrawIcon(Renderer renderer)
        {
            Vector2 capturePos = Vector2.Zero;
            SpriteBatchState spriteBatchState = default;

            if (TextShadeTextureDirty && Shade.Value)
            {
                UpdateShadeTexture(ref TextShadeTexture, 1, new(5), Vector2.Zero);
                TextShadeTextureDirty = false;
            }

            if (renderer is CaptureRenderer capture)
            {
                capturePos = capture.Position;
                capture.Position = WorldPosition;
                spriteBatchState = Main.SpriteBatch.GetState();
                Main.SpriteBatch.End();
                capture.BeginCapture((int)Size.X, (int)Size.Y);
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            }

            if (Shade.Value && !Shading && TextShadeTexture is not null)
                renderer.DrawTexture(TextShadeTexture, WorldPosition);

            string text = Text.Value.Length == 0 ? EmptyTextString : Text.Value;

            FormattedText.Draw(text, new()
            {
                Font = Font.Value,
                OriginalPos = renderer.TransformVector(WorldPosition + new Vector2(5)),
                SpriteBatch = Main.SpriteBatch,
                Scale = Scale.Value * renderer.Scale,
                Color = Color.Value,
            });

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

                BackColor   = Microsoft.Xna.Framework.Color.Transparent,
                BorderColor = Microsoft.Xna.Framework.Color.Transparent,

                Elements =
                {
                    new UIInput
                    {
                        Text = Text.Value,
                    }.OnEvent(UIInput.TextChangedEvent, (inp, _) => { if (inp.Active) Text.Value = inp.Text; })
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
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Text color", Color.Value, (_, c) => Color.Value = c)));
            list.Elements.Add(new UIButton
            {
                Text = "Set shade color",
                Height = 20
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Text shade color", ShadeColor.Value, (_, c) => ShadeColor.Value = c)));
            list.Elements.Add(new UIButton
            {
                Text = "Shade enabled",
                Height = 20,

                Selectable = true,
                Selected = Shade.Value,

                SelectedTextColor = Microsoft.Xna.Framework.Color.Black,
                SelectedBackColor = Microsoft.Xna.Framework.Color.White,

            }.OnEvent(UIElement.ClickEvent, (btn, _) => Shade.Value = btn.Selected));
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
                        Value = Scale.Value,

                    }.OnEvent(UINumberInput.ValueChanged, (inp, _) => Scale.Value = Math.Max(0.1f, (float)inp.Value))
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

        protected override void GenerateShadeTexture()
        {
            UpdateShadeTexture(ref ShadeTexture, ShadeSize, new(ShadeSize + 5), new(ShadeSize));
        }

        void UpdateShadeTexture(ref Texture2D? texture, int shade, Vector2 textpos, Vector2 size)
        {
            Vector2 shadeSize = Size + size * 2;

            int shadeWidth = (int)Math.Ceiling(shadeSize.X);
            int shadeHeight = (int)Math.Ceiling(shadeSize.Y);

            if (ShadeRenderTarget is null || ShadeRenderTarget.Width < shadeWidth || ShadeRenderTarget.Height < shadeHeight)
            {
                int targetWidth = shadeWidth;
                int targetHeight = shadeHeight;

                if (ShadeRenderTarget is not null)
                {
                    targetWidth = Math.Max(targetWidth, ShadeRenderTarget.Width);
                    targetHeight = Math.Max(targetHeight, ShadeRenderTarget.Height);

                    ShadeRenderTarget?.Dispose();
                }
                ShadeRenderTarget = new(Main.Instance.GraphicsDevice, targetWidth, targetHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            UI.Structures.SpriteBatchState state = Main.SpriteBatch.GetState();

            Main.SpriteBatch.End();
            RenderTargetBinding[] targets = Main.Instance.GraphicsDevice.GetRenderTargets();
            Main.Instance.GraphicsDevice.SetRenderTarget(MapObject.ShadeRenderTarget);
            Main.Instance.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
            Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            string text = Text.Value.Length == 0 ? EmptyTextString : Text.Value;
            FormattedText.Draw(text, new()
            {
                Font = Font.Value,
                SpriteBatch = Main.SpriteBatch,
                OriginalPos = textpos,
                Scale = Scale.Value,
                Color = Microsoft.Xna.Framework.Color.Black
            });

            Main.SpriteBatch.End();
            Main.Instance.GraphicsDevice.SetRenderTargets(targets);
            Main.SpriteBatch.Begin(state);

            int shadePixels = shadeWidth * shadeHeight;
            Color[] pixels = ArrayPool<Color>.Shared.Rent(shadePixels);

            ShadeRenderTarget.GetData(0, new(0, 0, shadeWidth, shadeHeight), pixels, 0, shadePixels);
            ProcessShade(pixels, shadeWidth, shadeHeight, shade, shade + 1);
            
            if (texture is null || texture.Width != shadeWidth || texture.Height != shadeHeight)
            {
                texture?.Dispose();
                texture = new(Main.Instance.GraphicsDevice, shadeWidth, shadeHeight);
            }
            texture.SetData(pixels, 0, shadePixels);
            ArrayPool<Color>.Shared.Return(pixels);
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
                    Selected = Font.Value == font,
                    RadioTag = font,
                    SelectedTextColor = Microsoft.Xna.Framework.Color.Black,
                    SelectedBackColor = Microsoft.Xna.Framework.Color.White,
                };

                list.Elements.Add(button);
            }

            group.ButtonClicked += (_, tag) =>
            {
                if (tag is not SpriteFont font)
                    return;
                Font.Value = font;
            };
        }

        protected override JsonNode? SaveInnerJson()
        {
            return new JsonObject()
            .SaveProperty(Text)
            .SaveProperty(Font)
            .SaveProperty(Scale)
            .SaveProperty(Color)
            .SaveProperty(Shade)
            .SaveProperty(ShadeColor);
        }

        protected override void LoadInnerJson(JsonNode node)
        {
            Text.LoadFromJson(node);
            Font.LoadFromJson(node);
            Scale.LoadFromJson(node);
            Color.LoadFromJson(node);
            Shade.LoadFromJson(node);
            ShadeColor.LoadFromJson(node);

            ParamsChanged();
        }
    }
}

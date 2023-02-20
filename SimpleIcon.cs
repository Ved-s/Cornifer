using Cornifer.Renderers;
using Cornifer.UI;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public class SimpleIcon : SelectableIcon
    {
        public SimpleIcon()
        {
        }

        public SimpleIcon(string name, AtlasSprite sprite, Color? color = null) : this()
        {
            Name = name;
            Texture = sprite.Texture;
            Frame = sprite.Frame;
            Color.OriginalValue = color ?? sprite.Color;
            Shade.OriginalValue = sprite.Shade;
            Sprite = sprite;
        }

        public override int ShadeSize => BorderSize.Value;
        public override int? ShadeCornerRadius => ShadeSize + 1;

        public Texture2D? IconShadeTexture;
        public Texture2D? Texture;
        public Rectangle Frame;
        public ObjectProperty<Color> Color = new("color", Microsoft.Xna.Framework.Color.White);
        public ObjectProperty<bool> Shade = new("shade", true);
        public ObjectProperty<int> BorderSize = new("shadeSize", 5);
        public AtlasSprite? Sprite;

        public virtual bool SkipTextureSave { get; set; }
        public virtual bool DisableBorderConfig { get; set; }

        public override Vector2 Size => Frame.Size.ToVector2();

        public override void DrawIcon(Renderer renderer)
        {
            if (Texture is null)
                return;

            if (!Shading && Shade.Value && IconShadeTexture is null)
            {
                GenerateDefaultShadeTexture(ref IconShadeTexture, this, 1, null);
            }

            if (Shade.Value && IconShadeTexture is not null && !Shading)
            {
                renderer.DrawTexture(IconShadeTexture, WorldPosition - Vector2.One);
            }

            renderer.DrawTexture(Texture, WorldPosition, Frame, color: Color.Value);
        }

        protected override void BuildInnerConfig(UIList list)
        {
            list.Elements.Add(new UIButton
            {
                Height = 20,
                Text = "Set icon color",
                TextAlign = new(.5f),
            }.OnEvent(UIElement.ClickEvent, (_, _) =>
            {
                Interface.ColorSelector.Show("Icon color", Color.Value, (_, color) => Color.Value = color);
            }));

            list.Elements.Add(new UIButton
            {
                Height = 20,

                Selectable = true,
                Selected = Shade.Value,

                SelectedBackColor = Microsoft.Xna.Framework.Color.White,
                SelectedTextColor = Microsoft.Xna.Framework.Color.Black,

                Text = "Icon shade",
                TextAlign = new(.5f),
            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
            {
                Shade.Value = btn.Selected;
            }));

            if (!DisableBorderConfig)
            {
                list.Elements.Add(new UIPanel
                {
                    Height = 27,
                    Padding = 4,

                    Elements =
                    {
                        new UILabel
                        {
                            Top = 3,
                            Width = 90,
                            Height = 20,
                            Text = "Border size:",
                            WordWrap = false,
                        },
                        new UINumberInput
                        {
                            Width = new(-90, 1),
                            Left = new(0, 1, -1),
                            Value = BorderSize.Value,
                            AllowDecimal = false,
                            AllowNegative = false,

                        }.OnEvent(UINumberInput.ValueChanged, (inp, _) => BorderSize.Value = (int)inp.Value),
                    }
                });
            }
        }

        protected override JsonNode? SaveInnerJson()
        {
            JsonObject obj = new JsonObject()
                .SaveProperty(Color)
                .SaveProperty(Shade)
                .SaveProperty(BorderSize);

            if (!SkipTextureSave)
            {
                if (Sprite is not null)
                    obj["sprite"] = Sprite.Name;
                else
                {
                    obj["texture"] = Content.Textures.FirstOrDefault(t => t.Value == Texture).Key;
                    obj["frame"] = JsonTypes.SaveRectangle(Frame);
                }
            }
            return obj;
        }

        protected override void LoadInnerJson(JsonNode node)
        {
            if (!node.TryGet("sprite", out string? spriteName))
            {
                if (node.TryGet("texture", out string? texture))
                    Texture = Content.Textures.GetValueOrDefault(texture);
                if (node.TryGet("frame", out JsonNode? frame))
                    Frame = JsonTypes.LoadRectangle(frame);
            }
            else if (GameAtlases.Sprites.TryGetValue(spriteName, out AtlasSprite? sprite))
            {
                Texture = sprite.Texture;
                Frame = sprite.Frame;
                Color.OriginalValue = sprite.Color;
                Shade.OriginalValue = sprite.Shade;
                Sprite = sprite;
            }

            Color.LoadFromJson(node);
            Shade.LoadFromJson(node);
            BorderSize.LoadFromJson(node);
        }
    }
}

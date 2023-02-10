using Cornifer.Renderers;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Cornifer
{
    public class SimpleIcon : SelectableIcon
    {
        public SimpleIcon()
        { 
        }

        public SimpleIcon(string name, AtlasSprite sprite, Color? color = null)
        {
            Name = name;
            Texture = sprite.Texture;
            Frame = sprite.Frame;
            Color = color ?? sprite.Color;
            Shade = sprite.Shade;
            Sprite = sprite;
        }

        public Texture2D? Texture;
        public Rectangle Frame;
        public Color Color = Color.White;
        public bool Shade = true;
        public AtlasSprite? Sprite;

        public override Vector2 Size => Frame.Size.ToVector2();

        public override void DrawIcon(Renderer renderer)
        {
            if (Texture is null)
                return;

            if (Shade)
            {
                renderer.DrawTexture(Texture, WorldPosition + new Vector2(-1, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, WorldPosition + new Vector2(1, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, WorldPosition + new Vector2(-1, 1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, WorldPosition + new Vector2(1, 1), Frame, color: Color.Black);

                renderer.DrawTexture(Texture, WorldPosition + new Vector2(0, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, WorldPosition + new Vector2(1, 0), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, WorldPosition + new Vector2(-1, 0), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, WorldPosition + new Vector2(0, 1), Frame, color: Color.Black);
            }

            renderer.DrawTexture(Texture, WorldPosition, Frame, color: Color);
        }

        protected override UIElement? BuildInnerConfig()
        {
            return new UIContainer
            {
                Elements = 
                {
                    new UIButton
                    {
                        Height = 20,
                        Text = "Set icon color",
                        TextAlign = new(.5f),
                    }.OnEvent(UIElement.ClickEvent, (_, _) => 
                    {
                        Interface.ColorSelector.Show("Icon color", Color, (_, color) => Color = color);
                    }),

                    new UIButton
                    {
                        Top = 25,
                        Height = 20,

                        Selectable = true,
                        Selected = Shade,

                        SelectedBackColor = Color.White,
                        SelectedTextColor = Color.Black,

                        Text = "Icon shade",
                        TextAlign = new(.5f),
                    }.OnEvent(UIElement.ClickEvent, (btn, _) =>
                    {
                        Shade = btn.Selected;
                    }),
                }
            };
        }

        protected override JsonNode? SaveInnerJson()
        {
            if (Sprite is not null)
                return new JsonObject
                {
                    ["sprite"] = Sprite.Name,
                    ["color"] = Color.PackedValue,
                    ["shade"] = Shade
                };

            return new JsonObject
            {
                ["texture"] = Content.Textures.FirstOrDefault(t => t.Value == Texture).Key,
                ["frame"] = JsonTypes.SaveRectangle(Frame),
                ["color"] = Color.PackedValue,
                ["shade"] = Shade
            };
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
                Color = sprite.Color;
                Shade = sprite.Shade;
                Sprite = sprite;
            }

            if (node.TryGet("color", out uint color))
                Color.PackedValue = color;

            if (node.TryGet("shade", out bool shade))
                Shade = shade;
        }
    }
}

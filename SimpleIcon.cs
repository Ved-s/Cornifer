using Cornifer.Renderers;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cornifer
{
    public class SimpleIcon : SelectableIcon
    {
        public SimpleIcon()
        { 
        }

        public SimpleIcon(AtlasSprite sprite, Color? color = null)
        {
            Texture = sprite.Texture;
            Frame = sprite.Frame;
            Color = color ?? sprite.Color;
            Shade = sprite.Shade;
        }

        public Texture2D? Texture;
        public Rectangle Frame;
        public Color Color = Color.White;
        public bool Shade = true;

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
    }
}

using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public class SlugcatIcon : SelectableIcon
    {
        public static bool DrawIcons = true;
        public static bool DrawDiamond;

        public int Id;

        public bool ForceSlugcatIcon;

        public override bool Active => DrawIcons || ForceSlugcatIcon;
        public override Vector2 Size => DrawDiamond && !ForceSlugcatIcon ? new(9) : new(8);

        public override void DrawIcon(Renderer renderer)
        {
            Rectangle frame = DrawDiamond && !ForceSlugcatIcon ? new(Id*9, 8, 9, 9) : new(Id*8, 0, 8, 8);

            renderer.DrawTexture(Content.SlugcatIcons, Position, frame);
        }
    }

    public class SimpleIcon : SelectableIcon
    {
        public SimpleIcon()
        { 
        }

        public SimpleIcon(ISelectable? parent, AtlasSprite sprite, Color? color = null)
        {
            Parent = parent;
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

        public bool IconActive = true;
        public override bool Active => IconActive;

        public override void DrawIcon(Renderer renderer)
        {
            if (Texture is null)
                return;

            if (Shade)
            {
                renderer.DrawTexture(Texture, Position + new Vector2(-1, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(1, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(-1, 1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(1, 1), Frame, color: Color.Black);

                renderer.DrawTexture(Texture, Position + new Vector2(0, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(1, 0), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(-1, 0), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(0, 1), Frame, color: Color.Black);
            }

            renderer.DrawTexture(Texture, Position, Frame, color: Color);
        }
    }
}

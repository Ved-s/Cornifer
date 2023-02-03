using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public class SlugcatIcon : ISelectable, IDrawable
    {
        public static bool DrawDiamond;

        public int Id;

        public ISelectable Parent;

        public Vector2 Offset;
        public Vector2 Position
        {
            get => Parent.Position + Offset - Size / 2;
            set
            {
                if (!Parent.Selected)
                {
                    Offset = value - Parent.Position + Size / 2;
                }
            }
        }
        public Vector2 Size => DrawDiamond ? new(8) : new(9);

        public void Draw(Renderer renderer)
        {
            Vector2 parentPoint = renderer.TransformVector(Parent.Position + Parent.Size / 2 + new Vector2(.5f));
            Vector2 worldPoint = renderer.TransformVector(Position + Size / 2);

            Main.SpriteBatch.DrawLine(parentPoint, worldPoint, Color.Black, 3);
            Main.SpriteBatch.DrawRect(parentPoint - new Vector2(3), new(5), Color.Black);

            Main.SpriteBatch.DrawLine(parentPoint, worldPoint, new(90, 90, 90), 1);
            Main.SpriteBatch.DrawRect(parentPoint - new Vector2(2), new(3), new(90, 90, 90));

            Rectangle frame = DrawDiamond ? new(Id*9, 8, 9, 9) : new(Id*8, 0, 8, 8);

            renderer.DrawTexture(Content.SlugcatIcons, Position, frame);
        }
    }
}

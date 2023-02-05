using Cornifer.Renderers;
using Microsoft.Xna.Framework;

namespace Cornifer
{
    public abstract class SelectableIcon : ISelectable, IDrawable
    {
        public ISelectable? Parent;

        public Color LineColor = new(90, 90, 90);

        public Vector2 Offset;
        public Vector2 Position
        {
            get => Parent is null ? Offset : Parent.Position + Parent.Size / 2 + Offset - Size / 2;
            set
            {
                if (Parent is null)
                {
                    Offset = value;
                    return;
                }
                if (!Parent.Selected)
                {
                    Offset = value - Parent.Position + Size / 2 - Parent.Size / 2;
                }
            }
        }
        public abstract bool Active { get; }
        public abstract Vector2 Size { get; }

        public void Draw(Renderer renderer)
        {
            if (!Active)
                return;

            if (Parent is not null)
            {
                Vector2 parentPoint = renderer.TransformVector(Parent.Position + Parent.Size / 2 + new Vector2(.5f));
                Vector2 worldPoint = renderer.TransformVector(Position + Size / 2);

                Main.SpriteBatch.DrawLine(parentPoint, worldPoint, Color.Black, 3);
                Main.SpriteBatch.DrawRect(parentPoint - new Vector2(3), new(5), Color.Black);

                Main.SpriteBatch.DrawLine(parentPoint, worldPoint, LineColor, 1);
                Main.SpriteBatch.DrawRect(parentPoint - new Vector2(2), new(3), LineColor);
            }
            DrawIcon(renderer);
        }
        public abstract void DrawIcon(Renderer renderer);
    }
}

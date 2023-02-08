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
            get => Parent is null ? Offset : Parent.Position + Parent.Size  * ParentPosAlign + Offset - Size * IconPosAlign;
            set
            {
                if (Parent is null)
                {
                    Offset = value;
                    return;
                }
                if (!Parent.Selected)
                {
                    Offset = value - Parent.Position + Size * IconPosAlign - Parent.Size * ParentPosAlign;
                }
            }
        }
        public abstract bool Active { get; }
        public abstract Vector2 Size { get; }

        public virtual Vector2 ParentPosAlign { get; set; } = new(.5f);
        public virtual Vector2 IconPosAlign { get; set; } = new(.5f);

        public void Draw(Renderer renderer)
        {
            if (!Active)
                return;

            if (Parent is not null)
            {
                Vector2 parentPoint = renderer.TransformVector(Parent.Position + Parent.Size * ParentPosAlign + new Vector2(.5f));
                Vector2 worldPoint = renderer.TransformVector(Position + Size * IconPosAlign);

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

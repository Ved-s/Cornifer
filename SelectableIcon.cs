using Cornifer.Renderers;
using Microsoft.Xna.Framework;

namespace Cornifer
{
    public abstract class SelectableIcon : MapObject
    {
        public Color LineColor = new(90, 90, 90);

        Vector2 Offset;

        public override Vector2 ParentPosition
        {
            get => (Parent?.Size * ParentPosAlign ?? Vector2.Zero) + Offset - Size * IconPosAlign;
            set => Offset = value + Size * IconPosAlign - (Parent?.Size * ParentPosAlign ?? Vector2.Zero);
        }

        public virtual Vector2 ParentPosAlign { get; set; } = new(.5f);
        public virtual Vector2 IconPosAlign { get; set; } = new(.5f);

        protected override void DrawSelf(Renderer renderer)
        {
            if (Parent is not null)
            {
                Vector2 parentPoint = renderer.TransformVector(Parent.WorldPosition + Parent.Size * ParentPosAlign + new Vector2(.5f));
                Vector2 worldPoint = renderer.TransformVector(WorldPosition + Size * IconPosAlign);

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

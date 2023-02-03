﻿using Cornifer.Renderers;
using Microsoft.Xna.Framework;

namespace Cornifer
{
    public abstract class SelectableIcon : ISelectable, IDrawable
    {
        public ISelectable Parent;

        public Vector2 Offset;
        public Vector2 Position
        {
            get => Parent.Position + Parent.Size / 2 + Offset - Size / 2;
            set
            {
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

            Vector2 parentPoint = renderer.TransformVector(Parent.Position + Parent.Size / 2 + new Vector2(.5f));
            Vector2 worldPoint = renderer.TransformVector(Position + Size / 2);

            Main.SpriteBatch.DrawLine(parentPoint, worldPoint, Color.Black, 3);
            Main.SpriteBatch.DrawRect(parentPoint - new Vector2(3), new(5), Color.Black);

            Main.SpriteBatch.DrawLine(parentPoint, worldPoint, new(90, 90, 90), 1);
            Main.SpriteBatch.DrawRect(parentPoint - new Vector2(2), new(3), new(90, 90, 90));

            DrawIcon(renderer);
        }
        public abstract void DrawIcon(Renderer renderer);
    }
}
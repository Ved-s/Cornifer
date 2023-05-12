using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.Renderers
{
    public abstract class Renderer
    {
        public virtual Vector2 Position { get; set; } = Vector2.Zero;
        public virtual float Scale { get; set; } = 1.0f;
        public virtual Vector2 Size { get; set; } = Vector2.One;

        public virtual Matrix Transform => Matrix.Multiply(Matrix.CreateTranslation(-Position.X, -Position.Y, 0), Matrix.CreateScale(Scale));
        public virtual Matrix InverseTransform => Matrix.Multiply(Matrix.CreateScale(1 / Scale), Matrix.CreateTranslation(Position.X, Position.Y, 0));
        public virtual Matrix Projection => Matrix.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, 0, 1);

        public virtual Vector2 TransformVector(Vector2 vec)
        {
            return (vec - Position) * Scale;
        }
        public virtual Vector2 InverseTransformVector(Vector2 vec)
        {
            return vec / Scale + Position;
        }

        public abstract void DrawTexture(Texture2D texture, Vector2 worldPos, Rectangle? source = null, Vector2? worldSize = null, Color? color = null, Vector2? scaleOverride = null);
    }
}

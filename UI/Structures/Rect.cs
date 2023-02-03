using Microsoft.Xna.Framework;
using System;

namespace Cornifer.UI.Structures
{
    public struct Rect
    {
        public float X, Y, Width, Height;

        public float Top
        {
            get => Y;
            set => Y = value;
        }
        public float Left
        {
            get => X;
            set => X = value;
        }
        public float Right
        {
            get => X + Width;
            set => X = value - Width;
        }
        public float Bottom
        {
            get => Y + Height;
            set => Y = value - Height;
        }

        public Vec2 Position
        {
            get => new(X, Y);
            set { X = value.X; Y = value.Y; }
        }
        public Vec2 Size
        {
            get => new(Width, Height);
            set { Width = value.X; Height = value.Y; }
        }

        public Vec2 Center
        {
            get => Position + Size / 2;
            set => Position = value - Size / 2;
        }

        public Rect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect(Vec2 pos, Vec2 size)
        {
            X = pos.X;
            Y = pos.Y;
            Width = size.X;
            Height = size.Y;
        }

        public bool Contains(Vec2 vec) => vec.X >= Left && vec.Y >= Top && vec.X <= Right && vec.Y <= Bottom;
        public bool Intersects(Rect rect) => rect.Left < Right && Left < rect.Right && rect.Top < Bottom && Top < rect.Bottom;
        public Rect Intersect(Rect rect)
        {
            float num = Math.Min(Right, rect.Right);
            float num2 = Math.Max(X, rect.X);
            float num3 = Math.Max(Y, rect.Y);
            float num4 = Math.Min(Bottom, rect.Bottom);
            return new Rect(num2, num3, num - num2, num4 - num3);
        }

        public override bool Equals(object? obj)
        {
            return obj is Rect rect &&
                   X == rect.X &&
                   Y == rect.Y &&
                   Width == rect.Width &&
                   Height == rect.Height;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }
        public override string ToString()
        {
            return $"{X:0.##}, {Y:0.##} {Width:0.##}, {Height:0.##}";
        }

        public static Rect operator +(Rect rect, Offset4 off)
        {
            rect.X += off.Left;
            rect.Y += off.Top;
            rect.Width -= off.Horizontal;
            rect.Height -= off.Vertical;
            return rect;
        }
        public static Rect operator -(Rect rect, Offset4 off)
        {
            rect.X -= off.Left;
            rect.Y -= off.Top;
            rect.Width += off.Horizontal;
            rect.Height += off.Vertical;
            return rect;
        }

        public static bool operator ==(Rect a, Rect b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        public static bool operator !=(Rect a, Rect b) => a.X != b.X || a.Y != b.Y || a.Width != b.Width || a.Height != b.Height;

        public static implicit operator Rect(Rectangle rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
        public static explicit operator Rectangle(Rect rect) => new((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
    }
}

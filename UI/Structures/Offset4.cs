using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Structures
{
    public struct Offset4
    {
        public float Top, Left, Right, Bottom;

        public float Horizontal => Left + Right;
        public float Vertical => Top + Bottom;
        public float All { set => Top = Left = Right = Bottom = value; }

        public Vec2 TopLeft => new(Left, Top);
        public Vec2 TopRight => new(Right, Top);
        public Vec2 BottomLeft => new(Left, Bottom);
        public Vec2 BottomRight => new(Right, Bottom);
        public Vec2 AllPadding => new(Horizontal, Vertical);

        public Offset4(float top, float left, float right, float bottom)
        {
            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
        }

        public Offset4(float vertical, float horizontal)
        {
            Top = vertical;
            Left = horizontal;
            Right = horizontal;
            Bottom = vertical;
        }

        public Offset4(float all)
        {
            Top = all;
            Left = all;
            Right = all;
            Bottom = all;
        }

        public static implicit operator Offset4(float all) => new(all);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Structures
{
    public struct Direction
    {
        public static Direction TopLeft => new(-1, -1);
        public static Direction Top => new(0, -1);
        public static Direction TopRight => new(1, -1);

        public static Direction MiddleLeft => new(-1, 0);
        public static Direction Middle => new(0, 0);
        public static Direction MiddleRight => new(1, 0);

        public static Direction BottomLeft => new(-1, 1);
        public static Direction Bottom => new(0, 1);
        public static Direction BottomRight => new(1, 1);

        public sbyte X, Y;

        public Direction(sbyte x, sbyte y)
        {
            X = x;
            Y = y;
        }
    }
}

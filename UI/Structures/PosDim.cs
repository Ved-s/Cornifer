using System;

namespace Cornifer.UI.Structures
{
    public struct PosDim
    {
        public float Value;
        public float ParentSizeFactor;
        public float MySizeFactor;

        public PosDim(float value, float parentSizeFactor, float mySizeFactor)
        {
            Value = value;
            ParentSizeFactor = parentSizeFactor;
            MySizeFactor = mySizeFactor;
        }

        public PosDim(float value, float parentSizeFactor)
        {
            Value = value;
            ParentSizeFactor = parentSizeFactor;
            MySizeFactor = 0;
        }

        public PosDim(float value)
        {
            Value = value;
            ParentSizeFactor = 0;
            MySizeFactor = 0;
        }


        public float Calculate(float parentSize, float mySize)
        {
            return Value + parentSize * ParentSizeFactor + mySize * MySizeFactor;
        }
        public override string ToString()
        {
            return $"{Value:.##}px + {ParentSizeFactor * 100:0.##}% parent + {MySizeFactor * 100:0.##}% own";
        }

        public override bool Equals(object? obj)
        {
            return obj is PosDim dim &&
                   Value == dim.Value &&
                   ParentSizeFactor == dim.ParentSizeFactor &&
                   MySizeFactor == dim.MySizeFactor;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, ParentSizeFactor, MySizeFactor);
        }

        public static bool operator ==(PosDim a, PosDim b)
            => a.Value == b.Value && a.ParentSizeFactor == b.ParentSizeFactor && a.MySizeFactor == b.MySizeFactor;
        public static bool operator !=(PosDim a, PosDim b)
            => a.Value != b.Value || a.ParentSizeFactor != b.ParentSizeFactor || a.MySizeFactor != b.MySizeFactor;
        public static implicit operator PosDim(float value) => new(value);
    }
}

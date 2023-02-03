using System;

namespace Cornifer.UI.Structures
{
    public struct SizeDim
    {
        public float Value;
        public float ParentSizeFactor;

        public SizeDim(float value, float parentSizeFactor)
        {
            Value = value;
            ParentSizeFactor = parentSizeFactor;
        }
        public SizeDim(float value)
        {
            Value = value;
            ParentSizeFactor = 0;
        }

        public float Calculate(float parentSize)
        {
            return Value + parentSize * ParentSizeFactor;
        }

        public override bool Equals(object? obj)
        {
            return obj is SizeDim dim &&
                   Value == dim.Value &&
                   ParentSizeFactor == dim.ParentSizeFactor;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Value, ParentSizeFactor);
        }
        public override string ToString()
        {
            return $"{Value:.##}px + {ParentSizeFactor * 100:0.##}% parent";
        }

        public static bool operator ==(SizeDim a, SizeDim b)
            => a.Value == b.Value && a.ParentSizeFactor == b.ParentSizeFactor;
        public static bool operator !=(SizeDim a, SizeDim b)
            => a.Value != b.Value || a.ParentSizeFactor != b.ParentSizeFactor;
        public static implicit operator SizeDim(float value) => new(value);
    }
}

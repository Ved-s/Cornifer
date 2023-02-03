using System;

namespace Cornifer.UI.Structures
{
    public struct Angle
    {
        public float Radians;
        public float Degrees
        {
            get => Radians / MathF.PI * 180;
            set => Radians = value / 180 * MathF.PI;
        }

        public Angle(float radians)
        {
            Radians = radians;
        }

        public Angle FromRad(float radians) => new(radians);
        public Angle FromDeg(float degrees) => new() { Degrees = degrees };

        public override bool Equals(object? obj)
        {
            return obj is Angle angle &&
                   Radians == angle.Radians;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Radians);
        }
        public override string ToString()
        {
            return $"{Degrees:0.##}° | {Radians / MathF.PI:0.##}π";
        }

        public static Angle operator +(Angle a, Angle b) => new(a.Radians + b.Radians);
        public static Angle operator -(Angle a, Angle b) => new(a.Radians - b.Radians);

        public static Angle operator *(Angle a, float b) => new(a.Radians * b);
        public static Angle operator /(Angle a, float b) => new(a.Radians / b);

        public static bool operator ==(Angle a, Angle b) => a.Radians == b.Radians;
        public static bool operator !=(Angle a, Angle b) => a.Radians != b.Radians;
    }
}

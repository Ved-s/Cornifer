using Microsoft.Xna.Framework;
using System;
using System.Text.Json.Nodes;

namespace Cornifer.Structures
{
    public class ColorRef
    {
        public readonly string? Key;

        public readonly Color DefaultColor;
        public Color Color;

        public static ColorRef White => new(null, Color.White);
        public static ColorRef Black => new(null, Color.Black);

        public ColorRef(string? key, Color color)
        {
            Key = key;
            DefaultColor = color;
            Color = color;
        }

        public void ResetToDefault()
        {
            Color = DefaultColor;
        }

        public string GetKeyOrColorString()
        {
            if (Key is not null)
                return Key;
            return $"{Color.R:x2}{Color.G:x2}{Color.B:x2}";
        }

        public JsonValue SaveJson(bool valueOnly = false)
        {
            if (Key is not null && !valueOnly)
                return JsonValue.Create(Key)!;

            return JsonValue.Create($"{Color.R:x2}{Color.G:x2}{Color.B:x2}")!;
        }

        public override bool Equals(object? obj)
        {
            return obj is ColorRef other &&
                other.Key == Key &&
                other.Color == Color;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Color);
        }
    }
}

using CommunityToolkit.HighPerformance.Buffers;
using Cornifer.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public static class ColorDatabase
    {
        const string DatabasePath = "Assets/colors.txt";

        internal static Dictionary<string, ColorRef> Colors = new();

        static StringPool Strings = new();

        public static ColorRef? GetColor(string key) => Colors.GetValueOrDefault(key);
        public static ColorRef? GetColor(ReadOnlySpan<char> key) => Colors.GetValueOrDefault(Strings.GetOrAdd(key));

        public static ColorRef GetOrCreateColor(ReadOnlySpan<char> key, Color defaultColor)
            => GetOrCreateColor(Strings.GetOrAdd(key), defaultColor);

        public static ColorRef GetOrCreateColor(string key, Color defaultColor)
        {
            if (!Colors.TryGetValue(key, out ColorRef? colorRef))
            {
                colorRef = new(key, defaultColor);
                Colors.Add(key, colorRef);
            }
            return colorRef;
        }

        public static void Load()
        {
            string databasePath = Path.Combine(Main.MainDir, DatabasePath);

            if (!File.Exists(databasePath))
                return;

            foreach (string line in File.ReadLines(databasePath))
            {
                if (line.Length == 0 || line.StartsWith("//") || !line.Contains(':'))
                    continue;

                string[] split = line.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                    continue;

                Color? color = ParseColor(split[1]);
                if (!color.HasValue)
                    continue;

                Colors[split[0]] = new(split[0], color.Value);
            }
        }

        public static ColorRef GetRegionColor(string region, string? subregion, bool water = false)
        {
            const string regPrefix = "reg_";
            const string waterPostfix = "_water";

            if (subregion?.Length is 0)
                subregion = null;

            Span<char> subregionSpan = stackalloc char[subregion?.Length ?? 0];

            if (subregion is not null)
            {
                subregion.AsSpan().CopyTo(subregionSpan);
                ConvertSubregionName(ref subregionSpan);
            }

            // reg_{region}[_{subregionSpan}][_water]

            int keyLength = regPrefix.Length + region.Length;

            if (subregion is not null)
                keyLength += 1 + subregionSpan.Length;

            if (water)
                keyLength += waterPostfix.Length;

            Span<char> key = stackalloc char[keyLength];

            SpanBuilder<char> builder = new(key);
            builder.Append(regPrefix);
            builder.Append(region);
            if (subregion is not null)
            {
                builder.Append('_');
                builder.Append(subregionSpan);
            }
            if (water)
            {
                builder.Append(waterPostfix);
            }

            ColorRef? subregionColor = GetColor(builder.SliceSpan());
            if (subregionColor is not null)
                return subregionColor;

            keyLength = regPrefix.Length + region.Length;

            if (water)
                keyLength += waterPostfix.Length;

            Span<char> regionKey = stackalloc char[keyLength];
            SpanBuilder<char> regionBuilder = new(regionKey);
            regionBuilder.Append(regPrefix);
            regionBuilder.Append(region);
            if (water)
                regionBuilder.Append(waterPostfix);

            ColorRef? regionColor = GetColor(regionBuilder.SliceSpan());
            Color defaultColor = water ? Color.Blue : Color.White;
            if (regionColor is not null)
                defaultColor = regionColor.Color;

            return GetOrCreateColor(builder.SliceSpan(), defaultColor);
        }

        public static void ConvertSubregionName(ref Span<char> name)
        {
            int writeIndex = 0;

            for (int readIndex = 0; readIndex < name.Length; readIndex++)
            {
                char c = name[readIndex];
                if (c >= '0' && c <= '9' || c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z')
                {
                    c = char.ToLower(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    c = '-';
                }
                else
                {
                    continue;
                }

                name[writeIndex] = c;
                writeIndex++;
            }

            name = name.Slice(0, writeIndex);
        }

        public static string ConvertSubregionName(ReadOnlySpan<char> name)
        {
            Span<char> span = stackalloc char[name.Length];
            name.CopyTo(span);
            ConvertSubregionName(ref span);
            return new(span);
        }

        public static ColorRef LoadColorRefJson(ColorRef? existingRef, JsonValue node, Color defaultColor)
        {
            if (node.TryGetValue(out uint packedColor))
            {
                if (existingRef is not null)
                {
                    existingRef.Color = new() { PackedValue = packedColor };
                    return existingRef;
                }
                return new(null, new() { PackedValue = packedColor });
            }

            if (node.TryGetValue(out string? value))
            {
                Color? color = ParseColor(value);
                if (color.HasValue)
                    return new(null, color.Value);

                return GetOrCreateColor(value, defaultColor);
            }

            return new(null, defaultColor);
        }

        public static JsonObject SaveJson()
        {
            return new(Colors
                .Where(kvp => kvp.Value.Color != kvp.Value.DefaultColor)
                .Select(kvp => new KeyValuePair<string, JsonNode?>(kvp.Key, kvp.Value.SaveJson(true))));
        }

        public static void LoadJson(JsonObject obj)
        {
            foreach (ColorRef cref in Colors.Values)
                cref.Color = cref.DefaultColor;

            foreach (var (name, value) in obj)
            {
                if (value is not JsonValue colorValue || !colorValue.TryGetValue(out string? colorString))
                    continue;

                Color? color = ParseColor(colorString);
                if (!color.HasValue)
                    continue;

                if (Colors.TryGetValue(name, out ColorRef? cref))
                {
                    cref.Color = color.Value;
                    continue;
                }

                Colors[name] = new(name, color.Value);
            }
        }

        public static Color? ParseColor(ReadOnlySpan<char> text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!char.IsDigit(c) && (c < 'A' || c > 'F') && (c < 'a' || c > 'f'))
                    return null;
            }

            byte r, g, b, a = 255;

            if (text.Length == 1)
            {
                r = ParseHexChar(text[0]);
                r += (byte)(r << 4);

                g = r;
                b = r;
            }
            else if (text.Length == 3)
            {
                r = ParseHexChar(text[0]);
                g = ParseHexChar(text[1]);
                b = ParseHexChar(text[2]);

                r += (byte)(r << 4);
                g += (byte)(g << 4);
                b += (byte)(b << 4);
            }
            else if (text.Length == 6)
            {
                r = (byte)((ParseHexChar(text[0]) << 4) + ParseHexChar(text[1]));
                g = (byte)((ParseHexChar(text[2]) << 4) + ParseHexChar(text[3]));
                b = (byte)((ParseHexChar(text[4]) << 4) + ParseHexChar(text[5]));
            }
            else if (text.Length == 8)
            {
                r = (byte)((ParseHexChar(text[0]) << 4) + ParseHexChar(text[1]));
                g = (byte)((ParseHexChar(text[2]) << 4) + ParseHexChar(text[3]));
                b = (byte)((ParseHexChar(text[4]) << 4) + ParseHexChar(text[5]));
                a = (byte)((ParseHexChar(text[6]) << 4) + ParseHexChar(text[7]));
            }
            else
            {
                return null;
            }

            return new(r, g, b, a);
        }

        static byte ParseHexChar(char c)
        {
            if (char.IsDigit(c))
                return (byte)(c - '0');

            if (c >= 'A' && c <= 'F')
                return (byte)(c - 'A' + 10);

            if (c >= 'a' && c <= 'f')
                return (byte)(c - 'a' + 10);

            return 0;
        }
    }
}

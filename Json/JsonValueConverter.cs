using Microsoft.Xna.Framework;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cornifer.Json
{
    public static class JsonValueConverter<T>
    {
        public static Func<T, JsonNode> SaveValue = null!;
        public static Func<JsonNode, T>? LoadValue = null!;
        public static Func<JsonNode, T, T>? LoadValueWithExisting = null;
        public static Func<T?, T, bool>? SaveSkipCheckOverride = null;
    }

    public static class JsonValueConverter
    {
        public static void Load()
        {
            JsonValueConverter<bool>.SaveValue = v => JsonValue.Create(v);
            JsonValueConverter<bool>.LoadValue = n => n is JsonValue value ? value.Deserialize<bool>() : false;

            JsonValueConverter<int>.SaveValue = v => JsonValue.Create(v);
            JsonValueConverter<int>.LoadValue = n => n is JsonValue value ? value.Deserialize<int>() : 0;

            JsonValueConverter<float>.SaveValue = v => JsonValue.Create(v);
            JsonValueConverter<float>.LoadValue = n => n is JsonValue value ? value.Deserialize<float>() : 0;

            JsonValueConverter<string>.SaveValue = v => JsonValue.Create(v)!;
            JsonValueConverter<string>.LoadValue = n => n is JsonValue value ? value.Deserialize<string>() ?? "" : "";

            JsonValueConverter<Color>.SaveValue = v => JsonValue.Create(v.PackedValue);
            JsonValueConverter<Color>.LoadValue = n => n is JsonValue value ? new(value.Deserialize<uint>()) : Color.Magenta;

            JsonValueConverter<ColorRef>.SaveValue = v => v.SaveJson();
            JsonValueConverter<ColorRef>.LoadValueWithExisting = (n, v) => n is JsonValue value ? ColorDatabase.LoadColorRefJson(v, value, Color.White) : ColorRef.White;
            JsonValueConverter<ColorRef>.SaveSkipCheckOverride = (u, o) => u?.Key == o.Key || u.Key is not null || o.Color == o.DefaultColor;
        }
    }
}

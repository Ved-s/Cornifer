using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cornifer.Json
{
    public class ObjectProperty<T>
    {
        public bool UserValueSet;
        public T? UserValue = default;
        public T OriginalValue;

        public T Value
        {
            get => UserValueSet ? UserValue! : OriginalValue;
            set { UserValue = value; UserValueSet = true; ValueChanged?.Invoke(); }
        }
        public string JsonName;

        public Action? ValueChanged;

        public ObjectProperty(string jsonName, T originalValue)
        {
            JsonName = jsonName;
            OriginalValue = originalValue;
        }

        public void SaveToJson(JsonNode node)
        {
            if (!UserValueSet || Equals(UserValue, OriginalValue))
                return;

            node[JsonName] = JsonValueConverter<T>.SaveValue(Value);
        }

        public void LoadFromJson(JsonNode node)
        {
            JsonNode? value = node[JsonName];
            if (value is null)
                return;

            UserValue = JsonValueConverter<T>.LoadValue(value);
            UserValueSet = true;
        }
    }

    public class ObjectProperty<TValue, TJsonValue>
    {
        public bool UserValueSet;
        public TValue? UserValue = default;
        public TValue OriginalValue;

        public TValue Value
        {
            get => UserValueSet ? UserValue! : OriginalValue;
            set { UserValue = value; UserValueSet = true; ValueChanged?.Invoke(); }
        }
        public string JsonName;

        public Action? ValueChanged;

        public Func<TValue, TJsonValue> SaveValue;
        public Func<TJsonValue, TValue> LoadValue;

        public ObjectProperty(string jsonName, TValue originalValue)
        {
            JsonName = jsonName;
            OriginalValue = originalValue;
            SaveValue = null!;
            LoadValue = null!;
        }

        public ObjectProperty(string jsonName, TValue originalValue, Func<TValue, TJsonValue> saveValue, Func<TJsonValue, TValue> loadValue)
        {
            JsonName = jsonName;
            OriginalValue = originalValue;
            SaveValue = saveValue;
            LoadValue = loadValue;
        }

        public void SaveToJson(JsonNode node)
        {
            if (!UserValueSet || Equals(UserValue, OriginalValue))
                return;

            TJsonValue jsonValue = SaveValue(Value);

            if (jsonValue is JsonNode jsonNode)
                node[JsonName] = jsonNode;
            else
                node[JsonName] = JsonValueConverter<TJsonValue>.SaveValue(jsonValue);
        }

        public void LoadFromJson(JsonNode node)
        {
            JsonNode? value = node[JsonName];
            if (value is null)
                return;

            TJsonValue jsonValue;

            if (value is TJsonValue nodeValue)
                jsonValue = nodeValue;
            else
                jsonValue = JsonValueConverter<TJsonValue>.LoadValue(value);

            UserValue = LoadValue(jsonValue);
            UserValueSet = true;
        }
    }
}

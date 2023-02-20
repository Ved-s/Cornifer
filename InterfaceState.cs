using Cornifer.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public static class InterfaceState
    {
        public static Switch DrawTileWalls = new("tileWalls", true);
        public static Switch DrawPlacedObjects = new("placedObjects", true);
        public static Switch DrawPlacedPickups = new("placedPickups", false);

        public static Switch DrawSlugcatIcons = new("slugcatIcons", false);
        public static Switch DrawSlugcatDiamond = new("slugcatDiamond", true);

        public static Switch DrawBorders = new("borders", true);

        public static Switch MarkShortcuts = new("markShortcuts", true);
        public static Switch MarkExitsOnly = new("markExitsOnly", true);

        public static Switch DisableRoomCropping = new("disableRoomCropping", false);

        public static Slider WaterTransparency = new("waterTransparency", .3f);

        static List<Config> Configs = new();
        static InterfaceState()
        {
            static void UpdateRoomTilemaps() => Main.Region?.MarkRoomTilemapsDirty();

            DrawTileWalls.OnChanged = UpdateRoomTilemaps;
            WaterTransparency.OnChanged = UpdateRoomTilemaps;
            MarkShortcuts.OnChanged = UpdateRoomTilemaps;
            MarkExitsOnly.OnChanged = UpdateRoomTilemaps;
            DisableRoomCropping.OnChanged = UpdateRoomTilemaps;

            foreach (FieldInfo field in typeof(InterfaceState).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (field.FieldType.IsAssignableTo(typeof(Config)))
                    Configs.Add((Config)field.GetValue(null)!);
        }

        public static JsonNode SaveJson()
        {
            JsonObject obj = new();
            foreach (Config config in Configs)
                config.SaveToJson(obj);

            obj["hideObjects"] = new JsonArray(PlacedObject.HideObjectTypes.Select(s => JsonValue.Create(s)).ToArray());
            return obj;
        }

        public static void LoadJson(JsonNode node)
        {
            foreach (Config config in Configs)
                config.LoadFromJson(node);

            if (node.TryGet("hideObjects", out JsonArray? hideObjects))
            {
                PlacedObject.HideObjectTypes.Clear();
                foreach (JsonNode? objNode in hideObjects)
                {
                    if (objNode is not JsonValue)
                        continue;

                    string? type = objNode.Deserialize<string>();
                    if (type is null)
                        continue;

                    PlacedObject.HideObjectTypes.Add(type);
                }

                foreach (var (objType, button) in Interface.VisibilityPlacedObjects)
                    button.Selected = !PlacedObject.HideObjectTypes.Contains(objType);
            }
        }

        public abstract class Config
        {
            public abstract void SaveToJson(JsonNode node);
            public abstract void LoadFromJson(JsonNode node);
        }
        public abstract class Config<T> : Config
        {
            public string JsonName;
            private T value;

            public Action? OnChanged;
            public UIElement? Element;

            public T Value
            {
                get => value;
                set
                {
                    if (Equals(value, this.value))
                        return;

                    if (Element is not null)
                        UpdateElement();

                    this.value = value;
                    OnChanged?.Invoke();
                }
            }

            public Config(string jsonName, T value)
            {
                JsonName = jsonName;
                this.value = value;
            }

            protected void ChangeValueFromElement(T value)
            {
                if (Equals(value, this.value))
                    return;

                this.value = value;
                OnChanged?.Invoke();
            }

            public override void SaveToJson(JsonNode node)
            {
                node[JsonName] = JsonValueConverter<T>.SaveValue(Value);
            }

            public override void LoadFromJson(JsonNode node)
            {
                JsonNode? value = node[JsonName];
                if (value is null)
                    return;

                Value = JsonValueConverter<T>.LoadValue(value);
            }

            public abstract void BindElement();
            public abstract void UpdateElement();
        }

        public class Switch : Config<bool>
        {
            public Switch(string jsonName, bool value) : base(jsonName, value)
            {
            }

            public override void BindElement()
            {
                if (Element is UIButton { Selectable: true } button)
                    button.OnEvent(UIElement.ClickEvent, (btn, _) => ChangeValueFromElement(btn.Selected));
            }

            public override void UpdateElement()
            {
                if (Element is UIButton { Selectable: true } button)
                    button.Selected = Value;
            }
        }
        public class Slider : Config<float>
        {
            public Slider(string jsonName, float value) : base(jsonName, value)
            {
            }

            public override void BindElement()
            {
                if (Element is UIScrollBar scroller)
                    scroller.OnEvent(UIScrollBar.ScrollChanged, (_, v) => ChangeValueFromElement(v));
            }

            public override void UpdateElement()
            {
                if (Element is UIScrollBar scroller)
                    scroller.ScrollPosition = Value;
            }
        }
    }
}

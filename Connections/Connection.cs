using Cornifer.Json;
using Cornifer.MapObjects;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cornifer.Connections
{
    public class Connection
    {
        static List<Point> ShortcutTracingCache = new();

        public Room Source;
        public Room Destination;

        public Point SourcePoint;
        public Point DestinationPoint;

        public bool Invalid;
        public bool IsInRoomShortcut = false;

        public bool Active => Source.Active && Destination.Active && (!IsInRoomShortcut || Source.DrawInRoomShortcuts.Value);
        public Color Color => IsInRoomShortcut ? Color.Lerp(Color.White, Source.Subregion.Value.BackgroundColor.Color, .5f) : Color.White;
        public string JsonKey => IsInRoomShortcut ? $"#{Source.Name}~{SourcePoint.X}~{SourcePoint.Y}" : $"{Source.Name}~{Destination.Name}";

        public Color GuideColor => Color.White;

        public ObjectProperty<bool> AllowWhiteToRedPixel = new("whiteToRed", true);

        public List<ConnectionPoint> Points = new();

        public Connection(Room room, Room.Shortcut shortcut)
        {
            Source = Destination = room;
            IsInRoomShortcut = true;

            SourcePoint = shortcut.Entrance;
            DestinationPoint = shortcut.Target;

            ShortcutTracingCache.Clear();

            room.TraceShotrcut(SourcePoint, ShortcutTracingCache);

            foreach (Point point in ShortcutTracingCache)
            {
                Points.Add(new(this)
                {
                    Parent = Source,
                    ParentPosition = point.ToVector2()
                });
            }
        }

        public Connection(Room source, Room.Connection connection)
        {
            Invalid = true;
            if (source is null || connection.Target is null)
            {
                Main.LoadErrors.Add($"Tried mapping connection from {source?.Name ?? "NONE"} to {connection.Target?.Name ?? "NONE"}");
            }
            else if (connection.Exit < 0 || connection.Exit >= source.Exits.Length)
            {
                //if (source.Active)
                Main.LoadErrors.Add($"Tried mapping connection from nonexistent shortcut {connection.Exit} in {source?.Name ?? "NONE"}");
            }
            else if (connection.TargetExit < 0 || connection.TargetExit >= connection.Target.Exits.Length)
            {
                //if (connection.Target.Active)
                Main.LoadErrors.Add($"Tried mapping connection from nonexistent shortcut {connection.TargetExit} in {connection.Target?.Name ?? "NONE"}");
            }
            else
            {
                Invalid = false;
            }

            if (Invalid)
            {
                Source = null!;
                Destination = null!;
                return;
            }

            Source = source!;
            Destination = connection.Target!;

            SourcePoint = source!.Exits[connection.Exit];
            DestinationPoint = connection.Target!.Exits[connection.TargetExit];
        }

        internal void BuildConfig(UIList list)
        {
            list.Elements.Add(new UILabel
            {
                Text = "Connection config",
                Height = 20,
                TextAlign = new(.5f)
            });

            list.Elements.Add(new UIButton
            {
                Text = "Allow white-red ending",
                Height = 20,

                Selectable = true,
                Selected = AllowWhiteToRedPixel.Value,

                SelectedTextColor = Color.Black,
                SelectedBackColor = Color.White,

            }.OnEvent(UIElement.ClickEvent, (btn, _) => AllowWhiteToRedPixel.Value = btn.Selected));
        }

        public void LoadJson(JsonNode node)
        {
            if (node is JsonValue value)
            {
                int pointCount = value.Deserialize<int>();
                if (pointCount == 0)
                    return;

                Vector2 start = Source.WorldPosition + SourcePoint.ToVector2();
                Vector2 end = Destination.WorldPosition + DestinationPoint.ToVector2();

                Points.Clear();
                float tpp = 1 / (pointCount + 1);
                float t = tpp;
                for (int i = 0; i < pointCount; i++)
                {
                    ConnectionPoint newPoint = new(this)
                    {
                        ParentPosition = Vector2.Lerp(start, end, t),
                    };
                    Points.Add(newPoint);
                    t += tpp;
                }
            }
            else if (node is JsonArray pointsArray)
                LoadPointArray(pointsArray);
            else if (node is JsonObject obj)
            {
                if (obj.TryGet("points", out JsonArray? points))
                    LoadPointArray(points);
                AllowWhiteToRedPixel.LoadFromJson(obj);
            }
        }

        public JsonNode SaveJson()
        {
            return new JsonObject
            {
                ["points"] = new JsonArray(Points.Select(p => p.SaveJson()).ToArray())
            }.SaveProperty(AllowWhiteToRedPixel);
        }

        void LoadPointArray(JsonArray points)
        {
            Points.Clear();
            foreach (JsonNode? pointNode in points)
            {
                if (pointNode is null)
                    continue;

                ConnectionPoint newPoint = new(this);
                newPoint.LoadJson(pointNode);
                Points.Add(newPoint);

                if (IsInRoomShortcut)
                    newPoint.Parent = Source;
            }
        }

        public override string ToString()
        {
            return JsonKey;
        }
    }

}

using Cornifer.Input;
using Cornifer.Json;
using Cornifer.MapObjects;
using Cornifer.Renderers;
using Cornifer.Structures;
using Cornifer.UI;
using Cornifer.UI.Elements;
using Cornifer.UI.Structures;
using Cornifer.UndoActions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public class RegionConnections
    {
        public static Texture2D? ConnectionTexture;

        public Dictionary<(string src, string dst), Connection> RoomConnections = new();
        public Dictionary<(string room, Point shortcut), Connection> InRoomConnections = new();

        public IEnumerable<Connection> AllConnections => InRoomConnections.Values.Concat(RoomConnections.Values);

        public Region Region;
        public CompoundEnumerable<ConnectionPoint> PointObjectLists = new();

        public bool Hovered => HoveredConnection is not null;

        public ConnectionPoint? HoveredConnectionPoint
        {
            get => hoveredConnectionPoint;
            set
            {
                if (hoveredConnectionPoint is not null)
                    Main.FirstPrioritySelectionObjects.Remove(hoveredConnectionPoint);

                hoveredConnectionPoint = value;

                if (hoveredConnectionPoint is not null)
                    Main.FirstPrioritySelectionObjects.Add(hoveredConnectionPoint);
            }
        }

        private ConnectionPoint? hoveredConnectionPoint;
        public Connection? HoveredConnection;
        public int HoveredConnectionLine;
        public float HoveredLineDist;

        public RegionConnections(Region region)
        {
            Region = region;

            foreach (Room room in region.Rooms)
            {
                foreach (var connection in room.Connections)
                {
                    if (connection is null)
                        continue;

                    (string src, string dst) key1 = (room.Name!, connection.Target.Name!);
                    (string src, string dst) key2 = (connection.Target.Name!, room.Name!);

                    if (RoomConnections.ContainsKey(key1) || RoomConnections.ContainsKey(key2))
                        continue;

                    Connection regionConnection = new(room, connection);
                    if (regionConnection.Invalid)
                        continue;

                    PointObjectLists.Add(regionConnection.Points);
                    RoomConnections[key1] = regionConnection;
                }

                foreach (Room.Shortcut shortcut in room.Shortcuts)
                {
                    if (shortcut.Type != Room.Tile.ShortcutType.Normal)
                        continue;

                    if (InRoomConnections.ContainsKey((room.Name!, shortcut.Entrance)) || InRoomConnections.ContainsKey((room.Name!, shortcut.Target)))
                        continue;

                    Connection roomConnection = new(room, shortcut);
                    if (roomConnection.Invalid)
                        continue;

                    PointObjectLists.Add(roomConnection.Points);
                    InRoomConnections[(room.Name!, shortcut.Entrance)] = roomConnection;
                }
            }
        }

        public void Update(bool betweenRooms, bool inRooms)
        {
            if (!Main.Dragging && !Main.Selecting && !Interface.Hovered)
            {
                Vector2 mouseScreen = InputHandler.MouseState.Position.ToVector2();
                Vector2 mouseWorld = Main.WorldCamera.InverseTransformVector(mouseScreen);

                float lineBoundsOff = .5f * Main.WorldCamera.Scale;

                if (HoveredConnection is not null 
                 && HoveredConnectionLine <= HoveredConnection.Points.Count 
                 && HoveredConnection.Active 
                 && (HoveredConnection.IsInRoomShortcut ? inRooms : betweenRooms))
                {
                    (Vec2 start, Vec2 end) = GetLinePoints(Main.WorldCamera, HoveredConnection, HoveredConnectionLine);
                    Rect lineRect = GetLineBounds(start, end, lineBoundsOff, out float lineAngle);
                    if (!RotatedRectContains(lineRect, lineAngle, mouseScreen))
                        HoveredConnection = null;
                    else
                    {
                        Vec2 mouseLine = (Vec2)mouseScreen - start;

                        float mousePosAlong = RotateVector(mouseLine, -lineAngle, Vector2.Zero).X;
                        HoveredLineDist = mousePosAlong / (lineRect.Width + lineBoundsOff * 2);
                    }
                }
                else
                {
                    HoveredConnection = null;

                    foreach (Connection connection in AllConnections.Reverse())
                    {
                        if (!connection.Active || (connection.IsInRoomShortcut ? !inRooms : !betweenRooms))
                            continue;

                        for (int i = connection.Points.Count; i >= 0; i--)
                        {
                            (Vec2 start, Vec2 end) = GetLinePoints(Main.WorldCamera, connection, i);
                            Rect lineRect = GetLineBounds(start, end, lineBoundsOff, out float lineAngle);
                            if (RotatedRectContains(lineRect, lineAngle, mouseScreen))
                            {
                                HoveredConnection = connection;
                                HoveredConnectionLine = i;

                                Vec2 mouseLine = (Vec2)mouseScreen - start;

                                float mousePosAlong = RotateVector(mouseLine, -lineAngle, Vector2.Zero).X;
                                HoveredLineDist = mousePosAlong / (lineBoundsOff * 2);
                                break;
                            }
                        }
                        if (HoveredConnection is not null)
                            break;
                    }
                }

                HoveredConnectionPoint = null;
                if (HoveredConnection is null)
                {
                    float minDistSq = float.MaxValue;
                    ConnectionPoint? minDistPoint = null;
                    foreach (Connection connection in AllConnections)
                    {
                        if (!connection.Active || (connection.IsInRoomShortcut ? !inRooms : !betweenRooms))
                            continue;

                        foreach (ConnectionPoint point in connection.Points)
                        {
                            if (point.ContainsPoint(mouseWorld))
                            {
                                float distSq = Vector2.DistanceSquared(point.WorldPosition, mouseWorld);

                                if (distSq < minDistSq)
                                {
                                    minDistSq = distSq;
                                    minDistPoint = point;
                                }
                            }
                        }
                    }

                    HoveredConnectionPoint = minDistPoint;
                }
            }
            else
            {
                HoveredConnection = null;
                HoveredConnectionPoint = null;
            }

            if (HoveredConnection is not null && InputHandler.NewConnectionPoint.JustPressed)
            {
                (Vec2 start, Vec2 end) = GetLinePoints(null, HoveredConnection, HoveredConnectionLine);
                Vector2 pos = Vector2.Lerp(start, end, HoveredLineDist);

                ConnectionPoint newPoint = new(HoveredConnection)
                {
                    WorldPosition = pos,
                };
                HoveredConnection.Points.Insert(HoveredConnectionLine, newPoint);

                Main.Undo.Do(new MapObjectAdded<ConnectionPoint>(newPoint, HoveredConnection.Points, HoveredConnectionLine));

                Main.SelectedObjects.Clear();
                Main.SelectedObjects.Add(newPoint);
                Main.Dragging = true;
                Main.OldDragPos = Main.WorldCamera.InverseTransformVector(InputHandler.MouseState.Position.ToVector2());
            }

            if (InputHandler.DeleteConnection.JustPressed)
            {
                HashSet<ConnectionPoint> remove = new(Main.SelectedObjects.OfType<ConnectionPoint>());
                if (remove.Count > 0)
                {
                    foreach (var connection in remove.GroupBy(pt => pt.Connection))
                    {
                        Main.Undo.Do(new MapObjectsRemoved<ConnectionPoint>(connection, connection.Key.Points));

                        foreach (ConnectionPoint point in connection)
                            connection.Key.Points.Remove(point);
                    }

                    Main.SelectedObjects.ExceptWith(remove);
                }
            }
        }

        public void DrawShadows(Renderer renderer, bool betweenRooms, bool inRooms)
        {
            var state = Main.SpriteBatch.GetState();
            Main.SpriteBatch.End();

            int size = 5;

            foreach (Connection connection in AllConnections)
            {
                if (!connection.Active || (connection.IsInRoomShortcut ? !inRooms : !betweenRooms))
                    continue;

                BeginConnectionCapture(renderer, connection);
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointWrap);

                for (int i = 0; i <= connection.Points.Count; i++)
                {
                    if (ShouldDrawLine(connection, i))
                    {
                        (Vec2 start, Vec2 end) = GetLinePoints(null, connection, i);

                        float angle = (end - start).Angle.Radians;
                        int length = (int)Math.Ceiling((end - start).Length);

                        Main.SpriteBatch.Draw(Main.Pixel, renderer.TransformVector(start), new Rectangle(0, 0, length + size * 2 - 3, 1 + size * 2), Color.Black, angle, new(size + .5f - 2, size + .5f), renderer.Scale, SpriteEffects.None, 0);

                        if (i < connection.Points.Count)
                            Main.SpriteBatch.Draw(Main.Pixel, renderer.TransformVector(connection.Points[i].WorldPosition), new Rectangle(0, 0, size * 2 - 1, size * 2 - 1), Color.Black, 0f, new(size - 1f), renderer.Scale, SpriteEffects.None, 0);
                    }
                }

                Main.SpriteBatch.End();
                EndConnectionCapture(renderer);
            }

            Main.SpriteBatch.Begin(state);
        }

        public void DrawConnections(Renderer renderer, bool overRoomShadow, bool betweenRooms, bool inRooms)
        {
            if (ConnectionTexture is null)
            {
                ConnectionTexture = new(Main.Instance.GraphicsDevice, 2, 1);
                ConnectionTexture.SetData(new Color[] { Color.White, Color.Transparent });
            }

            var state = Main.SpriteBatch.GetState();
            Main.SpriteBatch.End();

            foreach (Connection connection in AllConnections)
            {
                if (!connection.Active || (connection.IsInRoomShortcut ? !inRooms : !betweenRooms))
                    continue;

                BeginConnectionCapture(renderer, connection);
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointWrap);

                bool GetPointShadow(int index)
                {
                    if (!overRoomShadow)
                        return false;

                    ConnectionPoint? point = index >= 0 && index < connection.Points.Count ? connection.Points[index] : null;

                    return point is not null && !point.NoShadow.Value;
                }

                Color connectionColor = connection.Color;
                int totalLength = 0;

                for (int i = 0; i <= connection.Points.Count; i++)
                {
                    bool startPointShadow = GetPointShadow(i - 1);
                    bool endPointShadow = GetPointShadow(i);

                    bool localShadow = overRoomShadow && startPointShadow && endPointShadow;

                    (Vec2 start, Vec2 end) = GetLinePoints(null, connection, i);

                    int length = (int)Math.Ceiling((end - start).Length);

                    if (ShouldDrawLine(connection, i))
                    {
                        float angle = (end - start).Angle.Radians;

                        Rectangle source = new Rectangle(totalLength, 0, length + 1, 1);
                        Vector2 origin = new(.5f);

                        if (i == 0)
                        {
                            source.Width -= 2;
                            origin.X -= 2;
                            length -= 2;
                        }
                        if (i == connection.Points.Count)
                        {
                            int dist = connection.AllowWhiteToRedPixel.Value ? 1 : 2;

                            source.Width -= dist;
                            length -= dist;
                        }

                        if (startPointShadow && (endPointShadow || GetPointShadow(i - 2)))
                        {
                            Main.SpriteBatch.Draw(Main.Pixel, renderer.TransformVector(start), new Rectangle(0, 0, 5, 5), new(0, 0, 0, 100), angle, new(2.5f), renderer.Scale, SpriteEffects.None, 0);
                        }

                        if (localShadow)
                        {
                            source.Width -= 6;
                            source.Height = 5;
                            origin.Y += 2f;
                            origin.X -= 3;

                            Main.SpriteBatch.Draw(Main.Pixel, renderer.TransformVector(start), source, new(0, 0, 0, 100), angle, origin, renderer.Scale, SpriteEffects.None, 0);
                        }
                        else if (!overRoomShadow)
                        {
                            ConnectionPoint? startPoint = i > 0 ? connection.Points[i - 1] : null;
                            ConnectionPoint? endPoint = i < connection.Points.Count ? connection.Points[i] : null;

                            if (startPoint is not null && startPoint.SkipPixelAfter.Value)
                            {
                                source.Width -= 1;
                                origin.X -= 1;

                                length += 1;
                            }

                            if (endPoint is not null && endPoint.SkipPixelBefore.Value)
                            {
                                source.Width -= 1;
                                length -= 1;
                            }

                            Main.SpriteBatch.Draw(ConnectionTexture, renderer.TransformVector(start), source, connectionColor, angle, origin, renderer.Scale, SpriteEffects.None, 0);
                        }
                    }
                    totalLength += length;
                }

                Main.SpriteBatch.End();
                EndConnectionCapture(renderer);
            }

            Main.SpriteBatch.Begin(state);
        }

        static Vector2 PrevCapturePos;
        static Point PrevCaptureSize;
        static void BeginConnectionCapture(Renderer renderer, Connection connection)
        {
            if (renderer is not CaptureRenderer capture)
                return;

            Vector2 tl = new(float.MaxValue);
            Vector2 br = new(float.MinValue);

            for (int i = 0; i < connection.Points.Count + 2; i++)
            {
                Vector2 pos =
                    i == 0 ? connection.Source.WorldPosition + connection.SourcePoint.ToVector2() :
                    i == 1 ? connection.Destination.WorldPosition + connection.DestinationPoint.ToVector2() :
                    connection.Points[i - 2].WorldPosition;

                tl.X = Math.Min(tl.X, pos.X);
                tl.Y = Math.Min(tl.Y, pos.Y);
                br.X = Math.Max(br.X, pos.X);
                br.Y = Math.Max(br.Y, pos.Y);
            }

            tl -= new Vector2(6);
            br += new Vector2(7);

            tl.Floor();
            br.Ceiling();

            int width = (int)Math.Ceiling(br.X - tl.X);
            int height = (int)Math.Ceiling(br.Y - tl.Y);

            capture.BeginCapture(width, height);
            PrevCaptureSize = new(width, height);
            PrevCapturePos = capture.Position;
            capture.Position = tl;
        }

        static void EndConnectionCapture(Renderer renderer)
        {
            if (renderer is not CaptureRenderer capture)
                return;

            Vector2 pos = capture.Position;

            capture.Position = PrevCapturePos;
            capture.EndCapture(pos, PrevCaptureSize.X, PrevCaptureSize.Y);
        }

        public void DrawGuideLines(Renderer renderer, bool betweenRooms, bool inRooms)
        {
            foreach (Connection connection in AllConnections)
            {
                if (!connection.Active || (connection.IsInRoomShortcut ? !inRooms : !betweenRooms))
                    continue;

                for (int i = 0; i <= connection.Points.Count; i++)
                {
                    (Vec2 start, Vec2 end) = GetLinePoints(renderer, connection, i);

                    float lineAngle = 0;
                    if (connection == HoveredConnection && i == HoveredConnectionLine)
                    {
                        Rect lineRect = GetLineBounds(start, end, .5f * Main.WorldCamera.Scale, out lineAngle);
                        Main.SpriteBatch.Draw(Main.Pixel, lineRect.Position, null, connection.GuideColor * .6f, lineAngle, Vector2.Zero, lineRect.Size, SpriteEffects.None, 0f);
                    }

                    if (HoveredConnectionPoint is not null && i < connection.Points.Count && connection.Points[i] == HoveredConnectionPoint && !HoveredConnectionPoint.Selected)
                    {
                        Main.SpriteBatch.Draw(
                            Main.Pixel,
                            Main.WorldCamera.TransformVector(HoveredConnectionPoint.VisualPosition),
                            null,
                            connection.GuideColor * .6f,
                            lineAngle,
                            Vector2.Zero,
                            HoveredConnectionPoint.VisualSize * Main.WorldCamera.Scale,
                            SpriteEffects.None, 0f);
                    }

                    bool smallStartPoint = i == 0;
                    bool smallEndPoint = i == connection.Points.Count;

                    bool visible = ShouldDrawLine(connection, i);

                    float dashSize = 8;
                    float shadeThickness = 3;

                    if (visible)
                        Main.SpriteBatch.DrawLine(start, end, Color.Black, 3);
                    else
                        Main.SpriteBatch.DrawDashLine(start, end, Color.Black, null, dashSize + shadeThickness, dashSize - shadeThickness, shadeThickness, -shadeThickness*.5f);

                    if (smallStartPoint)
                        Main.SpriteBatch.DrawRect(start - new Vector2(3), new(5), Color.Black);
                    if (smallEndPoint)
                        Main.SpriteBatch.DrawRect(end - new Vector2(3), new(5), Color.Black);

                    if (visible)
                        Main.SpriteBatch.DrawLine(start, end, visible ? connection.GuideColor : Color.Red, 1);
                    else
                        Main.SpriteBatch.DrawDashLine(start, end, visible ? connection.GuideColor : connection.GuideColor * .6f, null, dashSize, null, 1);

                    if (smallStartPoint)
                        Main.SpriteBatch.DrawRect(start - new Vector2(2), new(3), connection.GuideColor);
                    if (smallEndPoint)
                        Main.SpriteBatch.DrawRect(end - new Vector2(2), new(3), connection.GuideColor);

                    if (i > 0 && i <= connection.Points.Count)
                    {
                        Main.SpriteBatch.DrawRect(start - new Vector2(3), new(7), Color.Black);
                        Main.SpriteBatch.DrawRect(start - new Vector2(2), new(5), connection.GuideColor);
                    }

                    if (connection == HoveredConnection && i == HoveredConnectionLine && HoveredLineDist >= 0 && HoveredLineDist <= 1)
                    {
                        Vector2 point = Vector2.Lerp(start, end, HoveredLineDist);
                        Main.SpriteBatch.Draw(Main.Pixel, point, null, Color.Black, lineAngle, new Vector2(.5f), 9, SpriteEffects.None, 0f);
                        Main.SpriteBatch.Draw(Main.Pixel, point, null, connection.GuideColor, lineAngle, new Vector2(.5f), 7, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        public bool TryGetRoomConnection(string from, string to, [NotNullWhen(true)] out Connection? connection, out bool reversed)
        {
            if (RoomConnections.TryGetValue((from, to), out connection))
            {
                reversed = false;
                return true;
            }
            reversed = true;
            return RoomConnections.TryGetValue((to, from), out connection);
        }

        public bool TryGetInRoomConnection(string room, Point a, Point b, [NotNullWhen(true)] out Connection? connection, out bool reversed)
        {
            if (InRoomConnections.TryGetValue((room, a), out connection))
            {
                reversed = false;
                return true;
            }
            reversed = true;
            return InRoomConnections.TryGetValue((room, b), out connection);
        }

        public bool TryGetInRoomConnection(string room, Point pt, [NotNullWhen(true)] out Connection? connection)
        {
            connection = InRoomConnections.FirstOrDefault(kvp => kvp.Key.room == room && (kvp.Value.SourcePoint == pt || kvp.Value.DestinationPoint == pt)).Value;
            return connection is not null;
        }

        static bool ShouldDrawLine(Connection connection, int line)
        {
            (Vec2 start, Vec2 end) = GetLinePoints(null, connection, line);

            start = start.Floored();
            end = end.Floored();

            return start.X == end.X || start.Y == end.Y;
        }

        static (Vec2, Vec2) GetLinePoints(Renderer? transformer, Connection connection, int line)
        {
            Vec2 start = (Vec2)(line == 0
                        ? connection.SourcePoint.ToVector2() + connection.Source.WorldPosition
                        : connection.Points[line - 1].WorldPosition);

            Vec2 end = (Vec2)(line == connection.Points.Count
                ? connection.DestinationPoint.ToVector2() + connection.Destination.WorldPosition
                : connection.Points[line].WorldPosition);

            start += new Vec2(.5f);
            end += new Vec2(.5f);

            if (transformer is not null)
            {
                start = (Vec2)transformer.TransformVector(start);
                end = (Vec2)transformer.TransformVector(end);
            }

            return (start, end);
        }

        static Rect GetLineBounds(Vec2 start, Vec2 end, float offset, out float angle)
        {
            float rectHeight = 20;

            Vec2 diff = end - start;
            angle = diff.Angle.Radians;

            float length = diff.Length;
            if (length <= 0)
                return default;

            Vec2 dir = diff / length;

            start += dir * offset;
            length -= offset * 2;

            if (length <= 0)
                return default;

            Vec2 rectTL = dir;
            rectTL = new(rectTL.Y, -rectTL.X);

            rectTL *= rectHeight * .5f;
            rectTL += start;

            return new(rectTL, new(length, rectHeight));
        }

        static bool RotatedRectContains(Rect rect, float angle, Vector2 pos)
        {
            Vector2 rectLocalPos = RotateVector(pos, -angle, rect.Position);
            return rect.Contains((Vec2)rectLocalPos);
        }

        static Vector2 RotateVector(Vector2 vector, float angle, Vector2 center)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            vector -= center;
            Vector2 result = center;
            result.X += vector.X * cos - vector.Y * sin;
            result.Y += vector.X * sin + vector.Y * cos;
            return result;
        }

        public JsonNode SaveJson()
        {
            return new JsonObject(AllConnections
                .Where(c => c.Points.Count > 0)
                .Select(c => new KeyValuePair<string, JsonNode?>(c.JsonKey, c.SaveJson()))
                );
        }
        public void LoadJson(JsonNode json)
        {
            if (json is not JsonObject obj)
                return;

            foreach (var (name, con) in obj)
            {
                if (con is null)
                    continue;

                string[] split;
                Connection? connection;
                if (name.StartsWith("#"))
                {
                    split = name.Substring(1).Split('~', 3);
                    if (split.Length != 3 || !int.TryParse(split[1], out int scX) || !int.TryParse(split[2], out int scY))
                        continue;

                    Point scPt = new(scX, scY);
                    if (!TryGetInRoomConnection(split[0], scPt, out connection))
                        continue;
                }
                else
                {
                    split = name.Split('~', 2);
                    if (split.Length != 2)
                        continue;

                    if (!TryGetRoomConnection(split[0], split[1], out connection, out _))
                        continue;
                }
                connection.LoadJson(con);
            }
        }

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

        public class ConnectionPoint : MapObject
        {
            public override string? Name => $"Connection_{Connection.Source.Name}_{Connection.Destination.Name}_{Connection.Points.IndexOf(this)}";

            public override bool CanSetActive => false;

            public override bool Active => Connection.Active && Main.ActiveRenderLayers.HasFlag(Connection.IsInRoomShortcut ? RenderLayers.InRoomShortcuts : RenderLayers.Connections);
            public override bool LoadCreationForbidden => true;
            public override bool NeedsSaving => false;

            public Connection Connection = null!;

            public ObjectProperty<bool> SkipPixelBefore = new("skipBefore", false);
            public ObjectProperty<bool> SkipPixelAfter = new("skipAfter", false);
            public ObjectProperty<bool> NoShadow = new("noShadow", false);

            public ConnectionPoint() { }
            public ConnectionPoint(Connection connection)
            {
                Connection = connection;

                if (connection.IsInRoomShortcut)
                    NoShadow.OriginalValue = true;
            }

            public override RenderLayers RenderLayer => Connection.IsInRoomShortcut ? RenderLayers.InRoomShortcuts : RenderLayers.Connections;
            public override Vector2 VisualOffset => -(VisualSize - Vector2.One) / 2;
            public override Vector2 VisualSize => new(13);

            public JsonNode SaveJson()
            {
                return new JsonObject
                {
                    ["x"] = ParentPosition.X,
                    ["y"] = ParentPosition.Y,
                }.SaveProperty(SkipPixelBefore)
                .SaveProperty(SkipPixelAfter)
                .SaveProperty(NoShadow);
            }

            public new void LoadJson(JsonNode node)
            {
                ParentPosition = JsonTypes.LoadVector2(node);
                SkipPixelBefore.LoadFromJson(node);
                SkipPixelAfter.LoadFromJson(node);
                NoShadow.LoadFromJson(node);
            }

            protected override void DrawSelf(Renderer renderer) { }

            protected override void BuildInnerConfig(UIList list)
            {
                list.Elements.Add(new UIButton
                {
                    Text = "Skip pixel before",
                    Height = 20,

                    Selectable = true,
                    Selected = SkipPixelBefore.Value,

                    SelectedTextColor = Color.Black,
                    SelectedBackColor = Color.White,

                }.OnEvent(UIElement.ClickEvent, (btn, _) => SkipPixelBefore.Value = btn.Selected));
                list.Elements.Add(new UIButton
                {
                    Text = "Skip pixel after",
                    Height = 20,

                    Selectable = true,
                    Selected = SkipPixelAfter.Value,

                    SelectedTextColor = Color.Black,
                    SelectedBackColor = Color.White,

                }.OnEvent(UIElement.ClickEvent, (btn, _) => SkipPixelAfter.Value = btn.Selected));
                list.Elements.Add(new UIButton
                {
                    Text = "Disable shadow",
                    Height = 20,

                    Selectable = true,
                    Selected = NoShadow.Value,

                    SelectedTextColor = Color.Black,
                    SelectedBackColor = Color.White,

                }.OnEvent(UIElement.ClickEvent, (btn, _) => NoShadow.Value = btn.Selected));

                Connection.BuildConfig(list);
            }

            public override string ToString()
            {
                return $"Point {Connection.Points.IndexOf(this)} in {Connection}";
            }
        }
    }
}

using Cornifer.Renderers;
using Cornifer.UI;
using Cornifer.UI.Structures;
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

        public Dictionary<(string src, string dst), Connection> Connections = new();
        public Region Region;
        public CompoundEnumerable<ConnectionPoint> PointObjectLists = new();

        public bool Hovered => HoveredConnection is not null;

        public ConnectionPoint? HoveredConnectionPoint;
        public Connection? HoveredConnection;
        public int HoveredConnectionLine;
        public float HoveredLineDist;

        public RegionConnections(Region region)
        {
            Region = region;

            foreach (Room room in region.Rooms)
                foreach (var connection in room.Connections)
                {
                    if (connection is null)
                        continue;

                    (string src, string dst) key1 = (room.Name!, connection.Target.Name!);
                    (string src, string dst) key2 = (connection.Target.Name!, room.Name!);

                    if (Connections.ContainsKey(key1) || Connections.ContainsKey(key2))
                        continue;

                    Connection regionConnection = new(room, connection);
                    PointObjectLists.Add(regionConnection.Points);
                    Connections[key1] = regionConnection;
                }
        }

        public void Update()
        {
            HoveredConnection = null;
            HoveredConnectionPoint = null;

            if (!Main.Dragging && !Main.Selecting && !Interface.Hovered)
            {
                Vector2 mouseScreen = Main.MouseState.Position.ToVector2();
                Vector2 mouseWorld = Main.WorldCamera.InverseTransformVector(mouseScreen);

                bool finish = false;

                foreach (Connection connection in Connections.Values.Reverse())
                {
                    for (int i = connection.Points.Count; i >= 0; i--)
                    {
                        (Vec2 start, Vec2 end) = GetLinePoints(Main.WorldCamera, connection, i);
                        Rect lineRect = GetLineBounds(start, end, out float lineAngle);
                        if (RotatedRectContains(lineRect, lineAngle, mouseScreen))
                        {
                            HoveredConnection = connection;
                            HoveredConnectionLine = i;

                            Vec2 mouseLine = (Vec2)mouseScreen - start;

                            float mousePosAlong = RotateVector(mouseLine, -lineAngle, Vector2.Zero).X;
                            HoveredLineDist = mousePosAlong / lineRect.Width;
                            finish = true;
                            break;
                        }
                    }

                    foreach (ConnectionPoint point in connection.Points)
                        if (point.ContainsPoint(mouseWorld))
                        {
                            HoveredConnection = null;
                            HoveredConnectionPoint = point;
                            finish = true;
                            break;
                        }

                    if (finish)
                        break;
                }
            }

            if (HoveredConnection is not null && Main.MouseState.LeftButton == ButtonState.Pressed && Main.OldMouseState.LeftButton == ButtonState.Released)
            {
                (Vec2 start, Vec2 end) = GetLinePoints(null, HoveredConnection, HoveredConnectionLine);
                Vector2 pos = Vector2.Lerp(start, end, HoveredLineDist);

                ConnectionPoint newPoint = new(HoveredConnection)
                {
                    WorldPosition = pos,
                };
                HoveredConnection.Points.Insert(HoveredConnectionLine, newPoint);

                Main.SelectedObjects.Clear();
                Main.SelectedObjects.Add(newPoint);
                Main.Dragging = true;
            }

            if (Main.KeyboardState.IsKeyDown(Keys.Delete) && Main.OldKeyboardState.IsKeyUp(Keys.Delete))
            {
                HashSet<ConnectionPoint> remove = new();

                foreach (MapObject obj in Main.SelectedObjects)
                    if (obj is ConnectionPoint point)
                    {
                        point.Connection.Points.Remove(point);
                        remove.Add(point);
                    }

                Main.SelectedObjects.ExceptWith(remove);
            }
        }

        public void DrawShadows(Renderer renderer)
        {
            var state = Main.SpriteBatch.GetState();
            Main.SpriteBatch.End();

            int size = 5;

            foreach (Connection connection in Connections.Values)
            {
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

        //public void DrawConnectionsBelow(Renderer renderer)
        //{
        //    if (ConnectionTexture is null)
        //    {
        //        ConnectionTexture = new(Main.Instance.GraphicsDevice, 2, 1);
        //        ConnectionTexture.SetData(new Color[] { Color.White, Color.Transparent });
        //    }

        //    var state = Main.SpriteBatch.GetState();
        //    Main.SpriteBatch.End();

        //    foreach (Connection connection in Connections.Values)
        //    {
        //        if (connection.Points.Count <= 1)
        //            continue;

        //        BeginConnectionCapture(renderer, connection);
        //        Main.SpriteBatch.Begin(samplerState: SamplerState.PointWrap);

        //        int totalLength = 0;

        //        for (int i = 0; i < connection.Points.Count; i++)
        //        {
        //            (Vec2 start, Vec2 end) = GetLinePoints(null, connection, i);

        //            int length = (int)Math.Ceiling((end - start).Length);
        //            if (i > 0)
        //            {
        //                float angle = (end - start).Angle.Radians;
        //                Main.SpriteBatch.Draw(ConnectionTexture, renderer.TransformVector(start), new Rectangle(totalLength, 0, length + 1, 1), Color.White, angle, new(.5f), renderer.Scale, SpriteEffects.None, 0);
        //            }
        //            totalLength += length;
        //        }

        //        Main.SpriteBatch.End();
        //        EndConnectionCapture(renderer);
        //    }

        //    Main.SpriteBatch.Begin(state);
        //}
        public void DrawConnections(Renderer renderer, bool overRoomShadow)
        {
            if (ConnectionTexture is null)
            {
                ConnectionTexture = new(Main.Instance.GraphicsDevice, 2, 1);
                ConnectionTexture.SetData(new Color[] { Color.White, Color.Transparent });
            }

            var state = Main.SpriteBatch.GetState();
            Main.SpriteBatch.End();

            bool localShadow = false;

            foreach (Connection connection in Connections.Values)
            {
                BeginConnectionCapture(renderer, connection);
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointWrap);

                int totalLength = 0;

                for (int i = 0; i <= connection.Points.Count; i++)
                {
                    localShadow = overRoomShadow && i > 0 && i < connection.Points.Count;

                    (Vec2 start, Vec2 end) = GetLinePoints(null, connection, i);

                    int length = (int)Math.Ceiling((end - start).Length);

                    if (ShouldDrawLine(connection, i))
                    {

                        float angle = (end - start).Angle.Radians;

                        Rectangle source = new Rectangle(totalLength, 0, length + 1, 1);
                        Vector2 origin = new(.5f);
                        Color color = Color.White;

                        if (i == 0)
                        {
                            source.Width -= 2;
                            origin.X -= 2;
                        }
                        if (i == connection.Points.Count)
                        {
                            source.Width -= 2;
                        }

                        if (localShadow)
                        {
                            source.Width -= 6;
                            source.Height = 5;
                            origin.Y += 2f;
                            origin.X -= 3;
                            color = new(0, 0, 0, 100);

                            Main.SpriteBatch.Draw(Main.Pixel, renderer.TransformVector(start), source, color, angle, origin, renderer.Scale, SpriteEffects.None, 0);

                            Main.SpriteBatch.Draw(Main.Pixel, renderer.TransformVector(start), new Rectangle(0, 0, 5, 5), color, angle, new(2.5f), renderer.Scale, SpriteEffects.None, 0);

                            if (i == connection.Points.Count - 1)
                                Main.SpriteBatch.Draw(Main.Pixel, renderer.TransformVector(end), new Rectangle(0, 0, 5, 5), color, angle, new(2.5f), renderer.Scale, SpriteEffects.None, 0);
                        }
                        else
                        {
                            Main.SpriteBatch.Draw(ConnectionTexture, renderer.TransformVector(start), source, color, angle, origin, renderer.Scale, SpriteEffects.None, 0);
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
                    i == 0 ? connection.Source.WorldPosition + connection.SourcePoint :
                    i == 1 ? connection.Destination.WorldPosition + connection.DestinationPoint :
                    connection.Points[i - 2].WorldPosition;

                tl.X = Math.Min(tl.X, pos.X);
                tl.Y = Math.Min(tl.Y, pos.Y);
                br.X = Math.Max(br.X, pos.X);
                br.Y = Math.Max(br.Y, pos.Y);
            }

            tl -= new Vector2(5);
            br += new Vector2(5);

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

        public void DrawGuideLines(Renderer renderer)
        {
            foreach (Connection connection in Connections.Values)
                for (int i = 0; i <= connection.Points.Count; i++)
                {
                    (Vec2 start, Vec2 end) = GetLinePoints(renderer, connection, i);

                    float lineAngle = 0;
                    if (connection == HoveredConnection && i == HoveredConnectionLine)
                    {
                        Rect lineRect = GetLineBounds(start, end, out lineAngle);
                        Main.SpriteBatch.Draw(Main.Pixel, lineRect.Position, null, Color.Yellow * .6f, lineAngle, Vector2.Zero, lineRect.Size, SpriteEffects.None, 0f);
                    }

                    if (HoveredConnectionPoint is not null && i < connection.Points.Count && connection.Points[i] == HoveredConnectionPoint && !HoveredConnectionPoint.Selected)
                    {
                        Main.SpriteBatch.Draw(
                            Main.Pixel,
                            Main.WorldCamera.TransformVector(HoveredConnectionPoint.VisualPosition),
                            null,
                            Color.Yellow * .6f,
                            lineAngle,
                            Vector2.Zero,
                            HoveredConnectionPoint.VisualSize * Main.WorldCamera.Scale,
                            SpriteEffects.None, 0f);
                    }

                    bool smallStartPoint = i == 0;
                    bool smallEndPoint = i == connection.Points.Count;

                    Main.SpriteBatch.DrawLine(start, end, Color.Black, 3);

                    if (smallStartPoint)
                        Main.SpriteBatch.DrawRect(start - new Vector2(3), new(5), Color.Black);
                    if (smallEndPoint)
                        Main.SpriteBatch.DrawRect(end - new Vector2(3), new(5), Color.Black);

                    Main.SpriteBatch.DrawLine(start, end, Color.Yellow, 1);

                    if (smallStartPoint)
                        Main.SpriteBatch.DrawRect(start - new Vector2(2), new(3), Color.Yellow);
                    if (smallEndPoint)
                        Main.SpriteBatch.DrawRect(end - new Vector2(2), new(3), Color.Yellow);

                    if (i > 0 && i <= connection.Points.Count)
                    {
                        Main.SpriteBatch.DrawRect(start - new Vector2(4), new(7), Color.Black);
                        Main.SpriteBatch.DrawRect(start - new Vector2(3), new(5), Color.Yellow);
                    }

                    if (connection == HoveredConnection && i == HoveredConnectionLine)
                    {
                        Vector2 point = Vector2.Lerp(start, end, HoveredLineDist);
                        Main.SpriteBatch.Draw(Main.Pixel, point, null, Color.Black, lineAngle, new Vector2(.5f), 9, SpriteEffects.None, 0f);
                        Main.SpriteBatch.Draw(Main.Pixel, point, null, Color.Yellow, lineAngle, new Vector2(.5f), 7, SpriteEffects.None, 0f);
                    }
                }
        }

        public bool TryGetConnection(string from, string to, [NotNullWhen(true)] out Connection? connection, out bool reversed)
        {
            if (Connections.TryGetValue((from, to), out connection))
            {
                reversed = false;
                return true;
            }
            reversed = true;

            return Connections.TryGetValue((to, from), out connection);
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
                        ? connection.SourcePoint + connection.Source.WorldPosition
                        : connection.Points[line - 1].WorldPosition + new Vector2(.5f));

            Vec2 end = (Vec2)(line == connection.Points.Count
                ? connection.DestinationPoint + connection.Destination.WorldPosition
                : connection.Points[line].WorldPosition + new Vector2(.5f));

            if (transformer is not null)
            {
                start = (Vec2)transformer.TransformVector(start);
                end = (Vec2)transformer.TransformVector(end);
            }

            return (start, end);
        }

        static Rect GetLineBounds(Vec2 start, Vec2 end, out float angle)
        {
            Vec2 diff = end - start;
            angle = diff.Angle.Radians;

            if (diff.Length <= 0)
                return default;

            float rectHeight = 20;

            Vec2 rectTL = diff / diff.Length;
            rectTL = new(rectTL.Y, -rectTL.X);

            rectTL *= rectHeight * .5f;
            rectTL += start;

            return new(rectTL, new(diff.Length, rectHeight));
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
            return new JsonObject(Connections
                .Where(kvp => kvp.Value.Points.Count > 0)
                .Select(kvp => new KeyValuePair<string, JsonNode?>($"{kvp.Key.src}~{kvp.Key.dst}",
                    new JsonArray(kvp.Value.Points.Select(p => JsonTypes.SaveVector2(p.WorldPosition)).ToArray())
                    ))
                );
        }
        public void LoadJson(JsonNode json)
        {
            if (json is not JsonObject obj)
                return;

            foreach (var (name, con) in obj)
            {
                string[] split = name.Split('~', 2);
                if (split.Length != 2)
                    continue;

                if (!TryGetConnection(split[0], split[1], out Connection? connection, out _))
                    continue;

                connection.Points.Clear();

                if (con is JsonValue value)
                {

                    int pointCount = value.Deserialize<int>();
                    if (pointCount == 0)
                        continue;

                    Vector2 start = connection.Source.WorldPosition + connection.SourcePoint;
                    Vector2 end = connection.Destination.WorldPosition + connection.DestinationPoint;

                    float tpp = 1 / (pointCount + 1);
                    float t = tpp;
                    for (int i = 0; i < pointCount; i++)
                    {
                        ConnectionPoint newPoint = new(connection)
                        {
                            WorldPosition = Vector2.Lerp(start, end, t),
                        };
                        connection.Points.Add(newPoint);
                        t += tpp;
                    }
                }
                else if (con is JsonArray points)
                    foreach (JsonNode? posNode in points)
                    {
                        if (posNode is null)
                            continue;

                        ConnectionPoint newPoint = new(connection)
                        {
                            WorldPosition = JsonTypes.LoadVector2(posNode)
                        };
                        connection.Points.Add(newPoint);
                    }

            }
        }

        public class Connection
        {
            public Room Source;
            public Room Destination;

            public Vector2 SourcePoint;
            public Vector2 DestinationPoint;

            public Connection(Room source, Room.Connection connection)
            {
                Source = source;
                Destination = connection.Target;

                SourcePoint = source.Exits[connection.Exit].ToVector2() + new Vector2(.5f);
                DestinationPoint = connection.Target.Exits[connection.TargetExit].ToVector2() + new Vector2(.5f);
            }

            public List<ConnectionPoint> Points = new();
        }

        public class ConnectionPoint : MapObject
        {
            public override string? Name => $"Connection_{Connection.Source.Name}_{Connection.Destination.Name}_{Connection.Points.IndexOf(this)}";

            public override bool LoadCreationForbidden => true;
            public override bool NeedsSaving => false;

            public Connection Connection = null!;

            public ConnectionPoint() { }
            public ConnectionPoint(Connection connection)
            {
                Connection = connection;
            }

            public override Vector2 VisualOffset => -(VisualSize - Vector2.One) / 2;
            public override Vector2 VisualSize => new(13);

            protected override void DrawSelf(Renderer renderer) { }
        }
    }
}

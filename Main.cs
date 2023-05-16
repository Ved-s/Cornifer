using Cornifer.Helpers;
using Cornifer.Input;
using Cornifer.Json;
using Cornifer.MapObjects;
using Cornifer.Renderers;
using Cornifer.Structures;
using Cornifer.UI.Modals;
using Cornifer.UndoActions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Cornifer
{
    public class Main : Game
    {
#if DEBUG
        public static bool DebugMode => true;
#else
        public static bool DebugMode => DebugModeEnforcement || Debugger.IsAttached;
        static bool DebugModeEnforcement;
#endif

        public static GraphicsDeviceManager GraphicsManager = null!;
        public static SpriteBatch SpriteBatch = null!;

        public static Main Instance = null!;

        public static Region? Region;

        public static Texture2D Pixel = null!;
        public static Texture2D? OverlayImage = null;

        public static CameraRenderer WorldCamera = null!;

        public static List<MapObject> FirstPrioritySelectionObjects = new();
        public static List<MapObject> WorldObjects = new();
        public static CompoundEnumerable<MapObject> WorldObjectLists = new();
        public static HashSet<MapObject> SelectedObjects = new();

        public static EnabledDebugMetric DebugMetric = EnabledDebugMetric.None;

        public static Slugcat? SelectedSlugcat;

        public static Layer RoomsLayer = new("rooms", "Rooms", true, true);
        public static Layer ConnectionsLayer = new ConnectionsLayer(false, true);
        public static Layer InRoomConnectionsLayer = new ConnectionsLayer(true, true);
        public static Layer IconsLayer = new("icons", "Icons", true, true);
        public static Layer BroadcastsLayer = new("broadcasts", "Broadcasts", true, false);
        public static Layer VistaPointsLayer = new("vistapoints", "Vista points", true, false);
        public static Layer TextsLayer = new("texts", "Texts", true, true);

        static List<Layer> DefaultLayers = new()
        {
            RoomsLayer,
            ConnectionsLayer,
            InRoomConnectionsLayer,
            IconsLayer,
            BroadcastsLayer,
            VistaPointsLayer,
            TextsLayer,
        };

        public static List<Layer> Layers = new(DefaultLayers);

        public static object DrawLock = new();

        public static List<string> LoadErrors = new();

        public static SpriteFont DefaultSmallMapFont => Cornifer.Content.RodondoExt20M;
        public static SpriteFont DefaultBigMapFont => Cornifer.Content.RodondoExt30M;

        public static UndoRedo Undo = new();

        public static Stopwatch UpdateStopwatch = new();
        public static Stopwatch DrawStopwatch = new();
        public static TimeSpan OldDrawTime;

        public static int FpsCount;
        public static int FpsCounter;
        public static Stopwatch FpsStopwatch = new();

        public static Queue<Action> MainThreadQueue = new();

        internal static Vector2 SelectionStart;
        internal static Vector2 OldDragPos;
        internal static bool Selecting;
        internal static bool Dragging;

        internal static bool NoIdle = false;
        internal static bool Idlesound;
        internal static ulong Idle;

        static bool OldActive;
        static string? CurrentStatePath;

        public static readonly string MainDir = null!;

        static Main()
        {
            MainDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? "";
        }

        public Main()
        {
            Instance = this;
            GraphicsManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = Path.Combine(MainDir, "Content");
            IsMouseVisible = true;

            WorldObjectLists.Add(WorldObjects);
            WorldObjectLists.Add(FirstPrioritySelectionObjects);
        }

        protected override void Initialize()
        {
            NoIdle = File.Exists(Path.Combine(MainDir, "NoIdle.txt"));

            base.Initialize();

#if !DEBUG
            DebugModeEnforcement = File.Exists(Path.Combine(MainDir, "debugmode.txt"));

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                string text = e.IsTerminating ? "Critical exception" : "Unhandled exception!";
                text += "\nAfter clicking Ok you will be prompted to save map state.\n" +
                "Don't overwrite your existing state as it may be corrupted.\n";

                text += (e.ExceptionObject is Exception ex) ? $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : e.ExceptionObject.ToString() ?? "No data";

                Platform.MessageBox(text, "Unhandled exception!").Wait();
                SaveStateAs().Wait();
            };
#endif

            StaticData.Init();
            Profile.Load();
            GithubInfo.Load();
            RWAssets.Load();
            JsonValueConverter.Load();

            GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += (_, _) => Interface.Root?.Recalculate();
            LoadState();

            InputHandler.Init();
            Interface.Init();
            FpsStopwatch.Start();

            Thread.CurrentThread.Name = "Main thread";

            RWAssets.ShowDialogs();
        }

        private static async void LoadState()
        {
            var (externalStream, externalStreamSaveFile) = await Platform.GetStartupStateFileStream();

            if (externalStream is not null)
            {
                if (!TryCatchReleaseException(() =>
                {
                    JsonNode node = JsonSerializer.Deserialize<JsonNode>(externalStream)
                        ?? throw new NullReferenceException("Json was null");
                    LoadJson(node, false);
                }, "Exception has been thrown while opening external state."))
                {
                    CurrentStatePath = externalStreamSaveFile;
                    externalStream.Dispose();
                    return;
                }
                externalStream.Dispose();
            }

            FileInfo stateFile = new(Path.Combine(MainDir, "state.json"));

            if (stateFile.Exists && stateFile.Length > 0)
            {
#if !DEBUG
                try
                {
#endif
                using FileStream fs = File.OpenRead(stateFile.FullName);

                JsonNode? node = JsonSerializer.Deserialize<JsonNode>(fs);
                if (node is not null)
                    LoadJson(node, false);
#if !DEBUG
                }
                catch (Exception ex)
                {
                    var result = Platform.MessageBox(
                        $"Exception has been thrown while opening previous state.\n" +
                        $"Clicking Ok will delete current state (state.json) and continue normally.\n" +
                        $"Clicking Cancel will exit the application.\n" +
                        $"\n" +
                        $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Error", Platform.MessageBoxButtons.OkCancel).Result;

                    if (result != Platform.MessageBoxResult.Ok)
                    {
                        Environment.Exit(1);
                    }
                    File.Delete(stateFile.FullName);
                    Region = null;
                }
#endif
            }
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Cornifer.Content.Load(Content);

            Cornifer.Content.RodondoExt20M.LineSpacing -= 2;
            Cornifer.Content.RodondoExt30M.LineSpacing -= 5;

            Pixel = new(GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });

            WorldCamera = new(SpriteBatch);
            SpriteAtlases.Load();
            ColorDatabase.Load();
            DiamondPlacement.Load();
        }

        protected override void Update(GameTime gameTime)
        {
            UpdateStopwatch.Restart();
            base.Update(gameTime);

            while (MainThreadQueue.TryDequeue(out Action? action))
                action();

            InputHandler.Update();

            if (!NoIdle)
            {
                if (IsActive && (InputHandler.AllKeys.Any(k => InputHandler.KeyboardState[k] == Microsoft.Xna.Framework.Input.KeyState.Down)
                    || InputHandler.AllMouseKeys.Any(k => InputHandler.MouseState.IsKeyDown(k))))
                {
                    Idlesound = false;
                    Idle = 0;
                }
                else
                {
                    Idle++;

                    if (!Idlesound && Idle > 3600 && Random.Shared.Next(512) == 0)
                    {
                        Idlesound = true;
                        Cornifer.Content.Idle.Play(Random.Shared.NextSingle() * .2f + .2f, 0, 0);
                    }
                }
            }

            bool active = OldActive && IsActive;
            OldActive = IsActive;

            bool betweenRoomConnections = ConnectionsLayer.Visible;
            bool inRoomConnections = InRoomConnectionsLayer.Visible;
            bool anyConnections = betweenRoomConnections || inRoomConnections;
            if (active && anyConnections)
                Region?.Connections?.Update(betweenRoomConnections, inRoomConnections);

            foreach (Layer l in Layers)
                if (l.Visible)
                    l.Update();

            UpdateSelectionAndDrag(active);

            if (InputHandler.ClearErrors.JustPressed)
                LoadErrors.Clear();

            float keyMoveMultiplier = 1;
            if (InputHandler.MoveMultiplier.Pressed)
                keyMoveMultiplier = 10;

            if (active && !Interface.Active)
            {
                if (InputHandler.ModsDebug.JustPressed)
                    DebugMetric = DebugMetric == EnabledDebugMetric.Mods ? EnabledDebugMetric.None : EnabledDebugMetric.Mods;

                if (InputHandler.UndoDebug.JustPressed)
                    DebugMetric = DebugMetric == EnabledDebugMetric.Undos ? EnabledDebugMetric.None : EnabledDebugMetric.Undos;

                if (InputHandler.TimingsDebug.JustPressed)
                    DebugMetric = DebugMetric == EnabledDebugMetric.Timings ? EnabledDebugMetric.None : EnabledDebugMetric.Timings;

                if (InputHandler.MoveUp.JustPressed)
                    MoveSelectedObjects(new Vector2(0, -1) * keyMoveMultiplier);

                if (InputHandler.MoveDown.JustPressed)
                    MoveSelectedObjects(new Vector2(0, 1) * keyMoveMultiplier);

                if (InputHandler.MoveLeft.JustPressed)
                    MoveSelectedObjects(new Vector2(-1, 0) * keyMoveMultiplier);

                if (InputHandler.MoveRight.JustPressed)
                    MoveSelectedObjects(new Vector2(1, 0) * keyMoveMultiplier);

                if (InputHandler.DeleteObject.JustPressed)
                {
                    HashSet<MapObject> objectsToDelete = new(SelectedObjects);
                    objectsToDelete.IntersectWith(WorldObjects);

                    if (objectsToDelete.Count > 0)
                    {
                        Undo.Do(new MapObjectsRemoved<MapObject>(objectsToDelete, WorldObjects));

                        WorldObjects.RemoveAll(x => objectsToDelete.Contains(x));
                        SelectedObjects.ExceptWith(objectsToDelete);
                    }
                }

                if (InputHandler.Undo.JustPressed)
                {
                    StopDragging();
                    Undo.Undo();
                }

                if (InputHandler.Redo.JustPressed)
                {
                    StopDragging();
                    Undo.Redo();
                }

                if (InputHandler.Copy.JustPressed)
                {
                    JsonArray arr = new();

                    foreach (MapObject obj in SelectedObjects)
                    {
                        if (!obj.CanCopy || obj.LoadCreationForbidden)
                            continue;

                        JsonObject? node = obj.SaveJson(true);
                        if (node is not null)
                            arr.Add(node);
                    }

                    Platform.SetClipboard(JsonSerializer.Serialize(arr));
                }

                if (InputHandler.Paste.JustPressed)
                {
                    Task.Run(async () =>
                    {
                        string json = await Platform.GetClipboard();
                        if (json.Length == 0 || string.IsNullOrWhiteSpace(json))
                        {
                            await Platform.MessageBox("Clipboard is empty, nothing to paste", "Paste from clipboard");
                            return;
                        }
                        TryCatchReleaseException(() =>
                        {
                            JsonObject[] objectsJson = JsonSerializer.Deserialize<JsonObject[]>(json)!;

                            List<MapObject> objects = objectsJson.Select(j => MapObject.CreateObject(j, false)).OfType<MapObject>().ToList();

                            if (objects.Count == 0)
                                return;

                            Vector2 tl = objects[0].WorldPosition;
                            Vector2 br = objects[0].WorldPosition + objects[0].Size;

                            for (int i = 1; i < objects.Count; i++)
                            {
                                Vector2 pos = objects[i].WorldPosition;
                                Vector2 size = objects[i].Size;

                                tl.X = Math.Min(tl.X, pos.X);
                                tl.Y = Math.Min(tl.Y, pos.Y);

                                br.X = Math.Min(br.X, pos.X + size.X);
                                br.Y = Math.Min(tl.Y, pos.Y + size.Y);
                            }

                            Vector2 bunchSize = br - tl;
                            Vector2 bunchCenter = tl + bunchSize / 2;

                            Vector2 offset = WorldCamera.InverseTransformVector(InputHandler.MouseState.Position.ToVector2()) - bunchCenter;

                            foreach (MapObject obj in objects)
                                obj.WorldPosition += offset;

                            SelectedObjects.Clear();
                            WorldObjects.AddRange(objects);
                            SelectedObjects.UnionWith(objects);

                        }, "Exception has been caught while pasting objects");
                    });
                }
            }

            WorldCamera.Update();
            Interface.Update();
            UpdateStopwatch.Stop();
        }

        protected override void Draw(GameTime gameTime)
        {
            OldDrawTime = DrawStopwatch.Elapsed;
            DrawStopwatch.Restart();
            Viewport vp = GraphicsDevice.Viewport;
            GraphicsDevice.ScissorRectangle = new(0, 0, vp.Width, vp.Height);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (InterfaceState.OverlayBelow.Value)
                DrawOverlayImage();

            if (SelectedObjects.Count > 0)
            {
                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                foreach (MapObject obj in SelectedObjects)
                    SpriteBatch.DrawRect(WorldCamera.TransformVector(obj.WorldPosition + obj.VisualOffset) - new Vector2(2), obj.VisualSize * WorldCamera.Scale + new Vector2(4), Color.DarkGray * 0.4f);
                SpriteBatch.End();
            }

            DrawMap(WorldCamera, null, null);

            if (!InterfaceState.OverlayBelow.Value)
                DrawOverlayImage();

            if (SelectedObjects.Count > 0)
            {
                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                foreach (MapObject obj in SelectedObjects)
                    SpriteBatch.DrawRect(WorldCamera.TransformVector(obj.WorldPosition + obj.VisualOffset) - new Vector2(2), obj.VisualSize * WorldCamera.Scale + new Vector2(4), null, Color.White * 0.6f, 2);
                SpriteBatch.End();
            }

            if (Selecting)
            {
                Vector2 mouseWorld = WorldCamera.InverseTransformVector(InputHandler.MouseState.Position.ToVector2());
                Vector2 tl = new(Math.Min(mouseWorld.X, SelectionStart.X), Math.Min(mouseWorld.Y, SelectionStart.Y));
                Vector2 br = new(Math.Max(mouseWorld.X, SelectionStart.X), Math.Max(mouseWorld.Y, SelectionStart.Y));

                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                SpriteBatch.DrawRect(WorldCamera.TransformVector(tl), (br - tl) * WorldCamera.Scale, Color.LightBlue * 0.2f);
                SpriteBatch.End();
            }

            DrawDebugMetrics();

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, GithubInfo.Desc, new(5, vp.Height - Cornifer.Content.Consolas10.LineSpacing * 2 - 5), Color.White);
            SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, GithubInfo.Status, new(5, vp.Height - Cornifer.Content.Consolas10.LineSpacing - 5), Color.White);
            SpriteBatch.End();

            SpriteBatch.Begin();
            Interface.Draw();
            SpriteBatch.End();

            base.Draw(gameTime);

            DrawStopwatch.Stop();

            FpsCounter++;
            if (FpsStopwatch.ElapsedMilliseconds >= 1000)
            {
                FpsCount = FpsCounter;
                FpsCounter = 0;
                FpsStopwatch.Restart();
            }
        }

        private static void DrawDebugMetrics()
        {
            int y = 10;
            int x = 10;

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            switch (DebugMetric)
            {
                case EnabledDebugMetric.None when LoadErrors.Count > 0:

                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"{LoadErrors.Count} error(s) have occured during region loading.\nPress Esc to clear.", new(x, y), Color.OrangeRed);
                    y += Cornifer.Content.Consolas10.LineSpacing * 2 + 10;

                    foreach (string error in LoadErrors)
                    {
                        SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, error, new(x, y), Color.White);
                        y += Cornifer.Content.Consolas10.LineSpacing;
                    }

                    break;

                case EnabledDebugMetric.Mods:

                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"Loaded mods:", new(x, y), Color.OrangeRed);
                    y += Cornifer.Content.Consolas10.LineSpacing;

                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"Enabled", new(x, y), Color.White);
                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"Disabled", new(x + 60, y), Color.Gray);
                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"Ignored", new(x + 125, y), Color.Maroon);

                    y += Cornifer.Content.Consolas10.LineSpacing + 10;

                    foreach (RWMod mod in RWAssets.Mods)
                    {
                        if (!mod.Enabled)
                            continue;

                        SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"{mod.Name} ({mod.Id}) {(mod.Version is null ? "" : $"v{mod.Version}")}", new(x, y), mod.Active ? Color.White : Color.Maroon);
                        y += Cornifer.Content.Consolas10.LineSpacing;
                    }

                    y += Cornifer.Content.Consolas10.LineSpacing + 10;

                    foreach (RWMod mod in RWAssets.Mods)
                    {
                        if (mod.Active || mod.Enabled)
                            continue;

                        SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"{mod.Name} ({mod.Id}) {(mod.Version is null ? "" : $"v{mod.Version}")}", new(x, y), Color.Gray);
                        y += Cornifer.Content.Consolas10.LineSpacing;
                    }

                    break;

                case EnabledDebugMetric.Undos:

                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"Undo stack debug", new(x, y), Color.Yellow);
                    y += Cornifer.Content.Consolas10.LineSpacing * 2 + 10;

                    for (int i = 0; i < Undo.RedoBuffer.Count; i++)
                    {
                        SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, Undo.RedoBuffer[i].ToString()!, new(x, y), Color.White);
                        y += Cornifer.Content.Consolas10.LineSpacing;
                    }

                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, "--- Current position ---", new(x, y), Color.Lime);
                    y += Cornifer.Content.Consolas10.LineSpacing;

                    for (int i = -1; i >= -Undo.UndoBuffer.Count; i--)
                    {
                        SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, Undo.UndoBuffer[i].ToString()!, new(x, y), Color.White);
                        y += Cornifer.Content.Consolas10.LineSpacing;
                    }

                    break;

                case EnabledDebugMetric.Timings:

                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"FPS: {FpsCount}", new(x, y), Color.Yellow);
                    y += Cornifer.Content.Consolas10.LineSpacing;

                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"Update: {UpdateStopwatch.Elapsed.TotalMilliseconds:0.00}ms", new(x, y), Color.Yellow);
                    y += Cornifer.Content.Consolas10.LineSpacing;

                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"Draw: {OldDrawTime.TotalMilliseconds:0.00}ms", new(x, y), Color.Yellow);
                    y += Cornifer.Content.Consolas10.LineSpacing;

                    break;
            }

            SpriteBatch.End();
        }

        protected override void EndRun()
        {
            Profile.Save();

            using MemoryStream ms = new();
            try
            {
                JsonSerializer.Serialize(ms, SaveJson(), new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                Platform.MessageBox(
                    $"Exception has been thrown while saving app state.\n" +
                    $"Clicking Ok skip saving process and leave old state (state.json) intact.\n" +
                    $"\n" +
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Error").Wait();
                return;
            }
            FileStream fs = File.Create(Path.Combine(MainDir, "state.json"));
            ms.Position = 0;
            ms.CopyTo(fs);
            fs.Flush();
        }

        private void DrawOverlayImage()
        {
            if (OverlayImage is null || !InterfaceState.OverlayEnabled.Value)
                return;

            Vector2 pos = -(OverlayImage.Size() / 2);
            pos.Floor();

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);
            WorldCamera.DrawTexture(OverlayImage, pos, null, null, Color.White * InterfaceState.OverlayTransparency.Value);
            SpriteBatch.End();
        }

        private void UpdateSelectionAndDrag(bool active)
        {
            Vector2 mouseWorld = WorldCamera.InverseTransformVector(InputHandler.MouseState.Position.ToVector2());

            bool prevented = !active || Dragging || Selecting || Interface.Hovered || (Region?.Connections?.Hovered ?? false);

            KeybindState dragState = InputHandler.Drag.State;

            if (dragState == KeybindState.JustPressed)
            {
                if (prevented)
                    return;

                // Clicked on already selected object
                MapObject? underMouse = MapObject.FindSelectableAtPos(SelectedObjects, mouseWorld, false);
                if (underMouse is not null)
                {
                    if (InputHandler.SubFromSelection.Pressed)
                    {
                        SelectedObjects.Remove(underMouse);
                        return;
                    }

                    Undo.PreventNextUndoMerge();
                    Dragging = true;
                    OldDragPos = mouseWorld;
                    return;
                }

                // Clicked on not selected object
                MapObject? obj = MapObject.FindSelectableAtPos(WorldObjectLists, mouseWorld, true);
                if (obj is not null)
                {
                    if (InputHandler.AddToSelection.Released)
                        SelectedObjects.Clear();

                    SelectedObjects.Add(obj);
                    Undo.PreventNextUndoMerge();
                    Dragging = true;
                    OldDragPos = mouseWorld;
                    return;
                }
            }
            else if (active && InputHandler.Drag.AnyKeyPressed)
            {
                if (Dragging)
                {
                    Vector2 diff = mouseWorld - OldDragPos;

                    if (diff.X != 0 || diff.Y != 0)
                        MoveSelectedObjects(diff);

                    OldDragPos = mouseWorld;
                }
            }
            else
            {
                if (dragState == KeybindState.JustReleased)
                {
                    foreach (MapObject obj in SelectedObjects)
                        if (!obj.ParentSelected)
                        {
                            Vector2 pos = obj.WorldPosition;
                            pos.Round();
                            obj.WorldPosition = pos;
                        }
                    Undo.PreventNextUndoMerge();
                }

                Dragging = false;
            }

            if (InputHandler.Select.JustPressed)
            {
                if (prevented)
                    return;

                SelectionStart = mouseWorld;
                Selecting = true;
            }
            else if (active && InputHandler.Select.AnyKeyPressed)
            {
                if (Selecting)
                {
                    Vector2 tl = new(Math.Min(mouseWorld.X, SelectionStart.X), Math.Min(mouseWorld.Y, SelectionStart.Y));
                    Vector2 br = new(Math.Max(mouseWorld.X, SelectionStart.X), Math.Max(mouseWorld.Y, SelectionStart.Y));

                    if (InputHandler.AddToSelection.Released && InputHandler.SubFromSelection.Released)
                        SelectedObjects.Clear();

                    if (InputHandler.SubFromSelection.Pressed)
                        SelectedObjects.ExceptWith(MapObject.FindIntersectingSelectables(SelectedObjects, tl, br, true));
                    else
                        SelectedObjects.UnionWith(MapObject.FindIntersectingSelectables(WorldObjectLists, tl, br, true));
                }
            }
            else
            {
                Selecting = false;
            }
        }

        private void MoveSelectedObjects(Vector2 diff)
        {
            foreach (MapObject obj in SelectedObjects)
                if (!obj.ParentSelected)
                    obj.ParentPosition += diff;

            Undo.Do(new MapObjectsMoved(SelectedObjects.Where(o => !o.ParentSelected), diff));
        }

        private void StopDragging()
        {
            if (!Dragging)
                return;

            Dragging = false;
            Undo.PreventNextUndoMerge();
        }

        public static void AddWorldObject(MapObject obj)
        {
            WorldObjects.Add(obj);
            Undo.Do(new MapObjectAdded<MapObject>(obj, WorldObjects));
        }

        public static IEnumerable<MapObject> EnumerateAllObjects()
        {
            IEnumerable<MapObject> EnumerateRecursive(MapObject obj)
            {
                foreach (MapObject child in obj.Children)
                {
                    yield return child;
                    foreach (MapObject rec in EnumerateRecursive(child))
                        yield return rec;
                }
            }
            foreach (MapObject obj in WorldObjectLists)
            {
                yield return obj;
                foreach (MapObject rec in EnumerateRecursive(obj))
                    yield return rec;
            }
        }

        public static void DrawMap(Renderer renderer, Layer? layer, bool? border, bool ignoreVisibility = false, bool guides = true)
        {
            lock (DrawLock)
            {
                SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);

                ICapturingRenderer? capture = renderer as ICapturingRenderer;

                if (layer is not null)
                {
                    if (border is null && InterfaceState.DrawBorders.Value || border is true)
                    {
                        capture?.BeginLayerCapture(layer, true);
                        layer.DrawShade(renderer);
                        capture?.EndLayerCapture();
                    }

                    if (border is null or false)
                    {
                        capture?.BeginLayerCapture(layer, false);
                        layer.Draw(renderer);
                        capture?.EndLayerCapture();

                        if (guides)
                            layer.DrawGuides(renderer);
                    }
                }
                else
                {
                    if (border is null && InterfaceState.DrawBorders.Value || border is true)
                        foreach (Layer l in Layers)
                            if (l.Visible || ignoreVisibility)
                            {
                                capture?.BeginLayerCapture(l, true);
                                l.DrawShade(renderer);
                                capture?.EndLayerCapture();
                            }

                    if (border is null or false)
                    {
                        foreach (Layer l in Layers)
                            if (l.Visible || ignoreVisibility)
                            {
                                capture?.BeginLayerCapture(l, false);
                                l.Draw(renderer);
                                capture?.EndLayerCapture();
                            }

                        if (guides)
                            foreach (Layer l in Layers)
                                if (l.Visible || ignoreVisibility)
                                    l.DrawGuides(renderer);
                    }
                }

                SpriteBatch.End();
            }
        }

        public static Task LoadRegion(RegionInfo info)
        {
            List<string>? worldFile = RWAssets.ResolveUnmergedSlugcatFiles($"{info.Path}/world_{info.Id}.txt");
            List<string>? mapFile = RWAssets.ResolveUnmergedSlugcatFiles($"{info.Path}/map_{info.Id}.txt");
            List<string>? propertiesFile = RWAssets.ResolveUnmergedFiles($"{info.Path}/properties.txt");
            List<string>? slugcatPropertiesFile = SelectedSlugcat is null || RWAssets.CurrentInstallation?.IsLegacy is true ? null
                : RWAssets.ResolveUnmergedFiles($"{info.Path}/properties-{SelectedSlugcat.WorldStateSlugcat}.txt");

            if (worldFile is null)
            {
                LoadErrors.Add("Could not find world file");
                return Task.CompletedTask;
            }

            if (mapFile is null)
            {
                LoadErrors.Add("Could not find world map file");
                return Task.CompletedTask;
            }

            TaskCompletionSource completion = new();
            ThreadPool.QueueUserWorkItem((_) =>
            {
                [return: NotNullIfNotNull(nameof(paths))]
                static string? MergeFiles(List<string>? paths)
                {
                    if (paths is null)
                        return null;

                    if (paths.Count == 0)
                        return "";

                    if (paths.Count == 1)
                        return File.ReadAllText(paths[0]);

                    StringBuilder builder = new();

                    foreach (string path in paths)
                    {
                        if (builder.Length > 0 && builder[^1] != '\n')
                            builder.Append('\n');

                        builder.Append(File.ReadAllText(path));
                    }

                    return builder.ToString();
                }

                try
                {
                    lock (DrawLock)
                    {
                        Layers.Clear();
                        Layers.AddRange(DefaultLayers);
                        foreach (Layer l in Layers)
                            l.Visible = l.DefaultVisibility;
                        UI.Pages.Layers.UpdateLayerList();
                    }

                    Region region = new(info, MergeFiles(worldFile), MergeFiles(mapFile), MergeFiles(propertiesFile), MergeFiles(slugcatPropertiesFile));

                    MainThreadQueue.Enqueue(() =>
                    {
                        try
                        {
                            lock (DrawLock)
                            {
                                Region = region;
                            }
                            RegionLoaded(Region);
                            completion.SetResult();
                        }
                        catch (Exception ex)
                        {
                            completion.SetException(ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            });
            return completion.Task;
        }

        public static void ClearRegion()
        {
            SelectedObjects.Clear();
            Selecting = false;
            Dragging = false;

            WorldObjectLists.Clear();
            WorldObjectLists.Add(WorldObjects);
            WorldObjectLists.Add(FirstPrioritySelectionObjects);
            Region = null;
        }
        public static void RegionLoaded(Region region)
        {
            CurrentStatePath = null;
            SelectedObjects.Clear();
            Selecting = false;
            Dragging = false;

            WorldObjectLists.Clear();
            WorldObjectLists.Add(region.ObjectLists);
            WorldObjectLists.Add(WorldObjects);
            WorldObjectLists.Add(FirstPrioritySelectionObjects);
            if (region.Connections is not null)
                WorldObjectLists.Add(region.Connections.PointObjectLists);
            WorldObjects.Clear();

            foreach (MapObject obj in WorldObjectLists)
            {
                Vector2 pos = obj.WorldPosition;
                pos.Round();
                obj.WorldPosition = pos;
            }

            Interface.RegionChanged(region);
        }

        public static void FocusOnObject(MapObject obj)
        {
            WorldCamera.Position = obj.WorldPosition + obj.VisualOffset + obj.VisualSize / 2 - WorldCamera.Size / (2 * WorldCamera.Scale);
        }

        public static bool TestJsonState(JsonNode node)
        {
            if (node.TryGet("installFeatures", out int featuresInt))
            {
                RainWorldFeatures features = (RainWorldFeatures)featuresInt;
                RainWorldFeatures missingFeatures = RWAssets.CurrentInstallation is null ? features : ~RWAssets.CurrentInstallation.Features & features;

                if (missingFeatures != RainWorldFeatures.None)
                {
                    MessageBox.Show(
                        $"Cannot load this save state on current installation!\n" +
                        $"This installation is missing features: {RainWorldInstallation.GetFeaturesString(missingFeatures)}\n" +
                        $"Please select the correct installation.", MessageBox.ButtonsOk).ConfigureAwait(false);
                    return false;
                }
            }

            return true;
        }

        public static JsonObject SaveJson()
        {
            return new()
            {
                ["slugcat"] = SelectedSlugcat?.Id,
                ["installFeatures"] = (int?)(RWAssets.CurrentInstallation?.Features & RainWorldInstallation.StateEssentialFeatures),
                ["region"] = Region?.SaveJson(),
                ["connections"] = Region?.Connections?.SaveJson(),
                ["objects"] = new JsonArray(WorldObjectLists.Enumerate().Select(o => o.SaveJson()).OfType<JsonNode>().ToArray()),
                ["interface"] = InterfaceState.SaveJson(),
                ["colors"] = ColorDatabase.SaveJson(),
                ["layers"] = new JsonArray(Layers.Select(l =>
                {
                    JsonObject layer = new()
                    {
                        ["id"] = l.Id,
                        ["visible"] = l.Visible,
                    };
                    if (!l.Special)
                        layer["name"] = l.Name;

                    return layer;
                }).ToArray())
            };
        }
        public static void LoadJson(JsonNode node, bool shallow)
        {
            if (!TestJsonState(node))
                return;

            if (Region is not null)
            {
                Region.UnbindRooms();
            }

            if (node.TryGet("colors", out JsonObject? colors))
            {
                ColorDatabase.LoadJson(colors);
            }
            if (node.TryGet("slugcat", out string? slugcat))
            {
                SelectedSlugcat = StaticData.Slugcats.FirstOrDefault(s => s.Id.Equals(slugcat, StringComparison.InvariantCultureIgnoreCase));
            }
            if (!shallow && node.TryGet("region", out JsonNode? region))
            {
                ClearRegion();
                Region = new();
                Region.LoadJson(region);
                RegionLoaded(Region);
            }
            if (node.TryGet("connections", out JsonNode? connections))
                Region?.Connections?.LoadJson(connections);

            if (node.TryGet("layers", out JsonArray? layers))
            {
                Layers.Clear();

                foreach (Layer l in DefaultLayers)
                    l.Visible = l.DefaultVisibility;

                foreach (JsonNode? ln in layers)
                {
                    if (ln is not JsonObject layerObj)
                        continue;

                    if (!ln.TryGet("id", out string? id))
                        continue;

                    Layer? layer = Layers.FirstOrDefault(l => l.Id == id) ?? DefaultLayers.FirstOrDefault(l => l.Id == id);
                    if (layer is null)
                    {
                        if (!ln.TryGet("name", out string? name))
                            continue;

                        layer = new(id, name, false, true);
                        Layers.Add(layer);
                    }

                    if (!Layers.Contains(layer))
                        Layers.Add(layer);

                    if (ln.TryGet("visible", out bool visible))
                        layer.Visible = visible;
                }

                Layer? prevDefLayer = null;
                foreach (Layer dl in DefaultLayers)
                {
                    if (!Layers.Contains(dl))
                        Layers.Insert(prevDefLayer is null ? 0 : Layers.IndexOf(prevDefLayer) + 1, dl);

                    prevDefLayer = dl;
                }

                UI.Pages.Layers.UpdateLayerList();
            }

            if (node.TryGet("objects", out JsonArray? objects))
                foreach (JsonNode? objNode in objects)
                    if (objNode is not null && !MapObject.LoadObject(objNode, WorldObjectLists, shallow))
                    {
                        MapObject? obj = MapObject.CreateObject(objNode, shallow);

                        if (obj is null || obj.LoadCreationForbidden)
                            continue;

                        WorldObjects.Add(obj);
                    }

            if (node.TryGet("interface", out JsonNode? @interface))
                InterfaceState.LoadJson(@interface);

            Region?.PostRegionLoad();
        }

        public static async Task OpenState()
        {
            string? fileName = await Platform.OpenFileDialog("Select a state to open", "Cornifer map files|*.json;*.cornimap");
            if (fileName is null)
                return;

            if (TryCatchReleaseException(async () =>
            {
                using FileStream fs = File.OpenRead(fileName);

                JsonNode? node = JsonSerializer.Deserialize<JsonNode>(fs);
                if (node is not null && TestJsonState(node))
                {
                    int choice = await MessageBox.Show(
                        "Select state loading mode\n" +
                        "Full will load region from the state\n" +
                        "Shallow will load region from the game (if state has region data), then load changes from the state\n" +
                        "Overlay will overlay state with current region (if loaded)", new[]
                        {
                            ("Full", 0),
                            ("Shallow", 1),
                            ("Overlay", 2)
                        });

                    bool shallow = choice > 0;

                    if (choice == 1)
                    {
                        string? region = (node["region"]?["id"] as JsonValue)?.TryGetValue(out string? s) is true ? s : null;
                        string? slugcatId = (node["slugcat"] as JsonValue)?.TryGetValue(out s) is true ? s : null;

                        Slugcat? slugcat = slugcatId is null ? null :
                            StaticData.Slugcats.FirstOrDefault(s => s.Id.Equals(slugcatId, StringComparison.InvariantCultureIgnoreCase));

                        RegionInfo? regionInfo = region is null ? null : RWAssets.FindRegions(slugcat)
                            .AsNullable().FirstOrDefault(i => i!.Value.Id.Equals(region, StringComparison.InvariantCultureIgnoreCase));

                        if (regionInfo is null)
                        {
                            await UI.Pages.General.SelectRegionClicked();
                        }
                        else
                        {
                            SelectedSlugcat = slugcat;
                            await LoadRegion(regionInfo.Value);
                        }
                    }

                    LoadJson(node, shallow);
                    CurrentStatePath = fileName;
                }
            }, "Exception has been thrown while opening selected state."))
            {
                return;
            }
        }
        public static async Task SaveState()
        {
            if (CurrentStatePath is null)
            {
                string? newPath = await Platform.SaveFileDialog("Save map state", "Cornifer map files|*.cornimap");

                if (newPath is null)
                    return;

                CurrentStatePath = newPath;
            }

            using MemoryStream ms = new();

            if (TryCatchReleaseException(() =>
            {
                JsonSerializer.Serialize(ms, SaveJson());
            }, "Exception has been thrown while saving state."))
            {
                return;
            }

            FileStream fs = File.Create(CurrentStatePath!);
            ms.Position = 0;
            ms.CopyTo(fs);
        }
        public static async Task SaveStateAs()
        {
            string? newPath = await Platform.SaveFileDialog("Save map state as", "Cornifer map files|*.cornimap");

            if (newPath is null)
                return;

            CurrentStatePath = newPath;

            using MemoryStream ms = new();

            if (TryCatchReleaseException(() =>
            {
                JsonSerializer.Serialize(ms, SaveJson());
            }, "Exception has been thrown while saving state."))
            {
                return;
            }

            FileStream fs = File.Create(CurrentStatePath!);
            ms.Position = 0;
            ms.CopyTo(fs);
        }

        public static bool TryCatchReleaseException(Action action, string exceptionMessage)
        {
#if !DEBUG
            try
            {
#endif
            action();
            return false;
#if !DEBUG
            }
            catch (Exception ex)
            {
                Platform.MessageBox(
                    $"{exceptionMessage}\n" +
                    $"\n" +
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Error").ConfigureAwait(false);
                return true;
            }
#endif
        }
    }
}
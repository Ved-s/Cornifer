using Cornifer.Json;
using Cornifer.MapObjects;
using Cornifer.Renderers;
using Cornifer.UndoActions;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;

namespace Cornifer
{
    public class Main : Game
    {
        static Regex SteamLibraryPath = new(@"""path""[ \t]*""([^""]+)""", RegexOptions.Compiled);
        static Regex SteamManifestInstallDir = new(@"""installdir""[ \t]*""([^""]+)""", RegexOptions.Compiled);

        public static string[] AvailableSlugCatNames = new string[] { "White", "Yellow", "Red", "Gourmand", "Artificer", "Rivulet", "Spear", "Saint" };

        public static string[] SlugCatNames = new string[] { "White", "Yellow", "Red", "Night", "Gourmand", "Artificer", "Rivulet", "Spear", "Saint", "Inv" };
        public static Color[] SlugCatColors = new Color[] { new(255, 255, 255), new(255, 255, 115), new(255, 115, 115), new(25, 15, 48), new(240, 193, 151), new(112, 35, 60), new(145, 204, 240), new(79, 46, 105), new(170, 241, 86), new(0, 19, 58) };

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

        public static string? RainWorldRoot;

        public static RenderLayers ActiveRenderLayers = RenderLayers.All;

        public static KeyboardState KeyboardState;
        public static KeyboardState OldKeyboardState;

        public static MouseState MouseState;
        public static MouseState OldMouseState;

        public static string? SelectedSlugcat;

        public static List<string> LoadErrors = new();
        public static bool DrawUndoDebug;

        public static SpriteFont DefaultSmallMapFont => Cornifer.Content.RodondoExt20M;
        public static SpriteFont DefaultBigMapFont => Cornifer.Content.RodondoExt30M;

        public static UndoRedo Undo = new();

        static Vector2 SelectionStart;
        static Vector2 OldDragPos;
        internal static bool Selecting;
        internal static bool Dragging;

        static bool OldActive;
        static string? CurrentStatePath;

        public Main()
        {
            Instance = this;
            GraphicsManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            GithubInfo.Load();
            SearchRainWorld();
            JsonValueConverter.Load();

            GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += (_, _) => Interface.Root?.Recalculate();

            FileInfo stateFile = new("state.json");

            if (stateFile.Exists && stateFile.Length > 0)
            {
#if !DEBUG
                try
                {
#endif
                using FileStream fs = File.OpenRead("state.json");

                JsonNode? node = JsonSerializer.Deserialize<JsonNode>(fs);
                if (node is not null)
                    LoadJson(node);
#if !DEBUG
                }
                catch (Exception ex)
                {
                    var result = System.Windows.Forms.MessageBox.Show(
                        $"Exception has been thrown while opening previous state.\n" +
                        $"Clicking Ok will delete current state (state.json) and continue normally.\n" +
                        $"Clicking Cancel will exit the application.\n" +
                        $"\n" +
                        $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Error", System.Windows.Forms.MessageBoxButtons.OKCancel);

                    if (result != System.Windows.Forms.DialogResult.OK)
                    {
                        Environment.Exit(1);
                    }
                    File.Delete("state.json");
                    Region = null;
                }
#endif
            }

            Interface.Init();
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
            GameAtlases.Load();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            OldMouseState = MouseState;
            MouseState = Mouse.GetState();

            OldKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();

            OldActive = IsActive;

            bool active = OldActive && IsActive;

            bool betweenRoomConnections = ActiveRenderLayers.HasFlag(RenderLayers.Connections);
            bool inRoomConnections = ActiveRenderLayers.HasFlag(RenderLayers.InRoomShortcuts);
            bool anyConnections = betweenRoomConnections || inRoomConnections;

            if (active && anyConnections)
                Region?.Connections?.Update(betweenRoomConnections, inRoomConnections);

            UpdateSelectionAndDrag(active && MouseState.LeftButton == ButtonState.Pressed, active && OldMouseState.LeftButton == ButtonState.Pressed);

            if (KeyboardState.IsKeyDown(Keys.Escape) && OldKeyboardState.IsKeyUp(Keys.Escape))
                LoadErrors.Clear();

            float keyMoveMultiplier = 1;
            if (KeyboardState.IsKeyDown(Keys.LeftShift))
                keyMoveMultiplier = 10;

            if (active && !Interface.Active)
            {
                if (KeyboardState.IsKeyDown(Keys.F8) && OldKeyboardState.IsKeyUp(Keys.F8))
                    DrawUndoDebug = !DrawUndoDebug;

                if (KeyboardState.IsKeyDown(Keys.Up) && OldKeyboardState.IsKeyUp(Keys.Up))
                    MoveSelectedObjects(new Vector2(0, -1) * keyMoveMultiplier);

                if (KeyboardState.IsKeyDown(Keys.Down) && OldKeyboardState.IsKeyUp(Keys.Down))
                    MoveSelectedObjects(new Vector2(0, 1) * keyMoveMultiplier);

                if (KeyboardState.IsKeyDown(Keys.Left) && OldKeyboardState.IsKeyUp(Keys.Left))
                    MoveSelectedObjects(new Vector2(-1, 0) * keyMoveMultiplier);

                if (KeyboardState.IsKeyDown(Keys.Right) && OldKeyboardState.IsKeyUp(Keys.Right))
                    MoveSelectedObjects(new Vector2(1, 0) * keyMoveMultiplier);

                if (KeyboardState.IsKeyDown(Keys.Delete) && OldKeyboardState.IsKeyUp(Keys.Delete)
                 || KeyboardState.IsKeyDown(Keys.Back) && OldKeyboardState.IsKeyUp(Keys.Back))
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

                if (KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl))
                {
                    if (KeyboardState.IsKeyDown(Keys.Z) && OldKeyboardState.IsKeyUp(Keys.Z))
                    {
                        StopDragging();
                        Undo.Undo();
                    }

                    if (KeyboardState.IsKeyDown(Keys.Y) && OldKeyboardState.IsKeyUp(Keys.Y))
                    {
                        StopDragging();
                        Undo.Redo();
                    }
                }
            }

            WorldCamera.Update();
            Interface.Update();
        }

        protected override void Draw(GameTime gameTime)
        {
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

            DrawMap(WorldCamera, ActiveRenderLayers, null);

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
                Vector2 mouseWorld = WorldCamera.InverseTransformVector(MouseState.Position.ToVector2());
                Vector2 tl = new(Math.Min(mouseWorld.X, SelectionStart.X), Math.Min(mouseWorld.Y, SelectionStart.Y));
                Vector2 br = new(Math.Max(mouseWorld.X, SelectionStart.X), Math.Max(mouseWorld.Y, SelectionStart.Y));

                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                SpriteBatch.DrawRect(WorldCamera.TransformVector(tl), (br - tl) * WorldCamera.Scale, Color.LightBlue * 0.2f);
                SpriteBatch.End();
            }

            if (LoadErrors.Count > 0 && !DrawUndoDebug)
            {
                int y = 10;
                int x = 10;

                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

                SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, $"{LoadErrors.Count} error(s) have occured during region loading.\nPress Esc to clear.", new(x, y), Color.OrangeRed);
                y += Cornifer.Content.Consolas10.LineSpacing * 2 + 10;

                foreach (string error in LoadErrors)
                {
                    SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, error, new(x, y), Color.White);
                    y += Cornifer.Content.Consolas10.LineSpacing;
                }

                SpriteBatch.End();
            }

            if (DrawUndoDebug)
            {
                int y = 10;
                int x = 10;

                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

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

                SpriteBatch.End();
            }

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            //SpriteBatch.DrawString(Cornifer.Content.RodondoExt20, "test", new(5, 5), Color.White, 0f, Vector2.Zero, 10, SpriteEffects.None, 0);
            SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, GithubInfo.Desc, new(5, vp.Height - Cornifer.Content.Consolas10.LineSpacing * 2 - 5), Color.White);
            SpriteBatch.DrawStringShaded(Cornifer.Content.Consolas10, GithubInfo.Status, new(5, vp.Height - Cornifer.Content.Consolas10.LineSpacing - 5), Color.White);
            SpriteBatch.End();

            SpriteBatch.Begin();
            Interface.Draw();
            SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void EndRun()
        {
            using MemoryStream ms = new();
            try
            {
                JsonSerializer.Serialize(ms, SaveJson(), new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Exception has been thrown while saving app state.\n" +
                    $"Clicking Ok skip saving process and leave old state (state.json) intact.\n" +
                    $"\n" +
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Error");
                return;
            }
            FileStream fs = File.Create("state.json");
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

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            WorldCamera.DrawTexture(OverlayImage, pos, null, null, Color.White * InterfaceState.OverlayTransparency.Value);
            SpriteBatch.End();
        }

        private void UpdateSelectionAndDrag(bool drag, bool oldDrag)
        {
            Vector2 mouseWorld = WorldCamera.InverseTransformVector(MouseState.Position.ToVector2());

            bool prevented = !Dragging && !Selecting && (Interface.Hovered || (Region?.Connections?.Hovered ?? false));

            if (drag && !oldDrag && !prevented)
            {
                // Clicked on already selected object
                MapObject? underMouse = MapObject.FindSelectableAtPos(SelectedObjects, mouseWorld, false);
                if (underMouse is not null)
                {
                    if (KeyboardState.IsKeyDown(Keys.LeftControl))
                    {
                        SelectedObjects.Remove(underMouse);
                        return;
                    }

                    Undo.PreventNextUndoMerge();
                    Dragging = true;
                    OldDragPos = mouseWorld;
                    return;
                }
                if (Region is not null)
                {
                    // Clicked on not selected object
                    MapObject? obj = MapObject.FindSelectableAtPos(WorldObjectLists, mouseWorld, true);
                    if (obj is not null)
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedObjects.Clear();
                        SelectedObjects.Add(obj);
                        Undo.PreventNextUndoMerge();
                        Dragging = true;
                        OldDragPos = mouseWorld;
                        return;
                    }
                }

                SelectionStart = mouseWorld;
                Selecting = true;
            }
            else if (drag && oldDrag)
            {
                if (Dragging)
                {
                    Vector2 diff = mouseWorld - OldDragPos;

                    if (diff.X != 0 || diff.Y != 0)
                        MoveSelectedObjects(diff);

                    OldDragPos = mouseWorld;
                }

                if (Selecting)
                {
                    Vector2 tl = new(Math.Min(mouseWorld.X, SelectionStart.X), Math.Min(mouseWorld.Y, SelectionStart.Y));
                    Vector2 br = new(Math.Max(mouseWorld.X, SelectionStart.X), Math.Max(mouseWorld.Y, SelectionStart.Y));

                    if (!KeyboardState.IsKeyDown(Keys.LeftControl) && !KeyboardState.IsKeyDown(Keys.LeftShift))
                        SelectedObjects.Clear();

                    if (KeyboardState.IsKeyDown(Keys.LeftControl))
                        SelectedObjects.ExceptWith(MapObject.FindIntersectingSelectables(SelectedObjects, tl, br, true));
                    else if (Region is not null)
                        SelectedObjects.UnionWith(MapObject.FindIntersectingSelectables(WorldObjectLists, tl, br, true));
                }
            }
            else
            {
                if (!drag && oldDrag)
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

        public static void DrawMap(Renderer renderer, RenderLayers layers, bool? border)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            bool betweenRoomConnections = layers.HasFlag(RenderLayers.Connections);
            bool inRoomConnections = layers.HasFlag(RenderLayers.InRoomShortcuts);
            bool anyConnections = betweenRoomConnections || inRoomConnections;

            if (Region is not null)
            {
                if (border is null && InterfaceState.DrawBorders.Value || border is true)
                {
                    if (anyConnections)
                        Region.Connections?.DrawShadows(renderer, betweenRoomConnections, inRoomConnections);

                    foreach (MapObject obj in WorldObjectLists)
                        obj.DrawShade(renderer, layers);
                }

                if (border is null or false)
                {
                    foreach (MapObject obj in Region.Rooms)
                        obj.Draw(renderer, layers);

                    if (anyConnections)
                    {
                        Region.Connections?.DrawConnections(renderer, true, betweenRoomConnections, inRoomConnections);
                        Region.Connections?.DrawConnections(renderer, false, betweenRoomConnections, inRoomConnections);
                    }
                    foreach (MapObject obj in WorldObjects)
                        obj.Draw(renderer, layers);

                    if (anyConnections)
                        Region.Connections?.DrawGuideLines(renderer, betweenRoomConnections, inRoomConnections);
                }
            }

            SpriteBatch.End();
        }

        public static bool SearchRainWorld()
        {
            object? steampathobj =
                    Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) ??
                    Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null);
            if (steampathobj is not string steampath)
                return false;

            if (!FileExists(steampath, "steamapps/libraryfolders.vdf", out string libraryfolders))
            {
                if (DirExists(steampath, "steamapps/common/Rain World", out string rainworld))
                {
                    RainWorldRoot = rainworld;
                    return true;
                }
                return false;
            }

            foreach (Match libmatch in SteamLibraryPath.Matches(File.ReadAllText(libraryfolders)))
            {
                string libpath = Regex.Unescape(libmatch.Groups[1].Value);

                if (!FileExists(libpath, "steamapps/appmanifest_312520.acf", out string manifest))
                    continue;

                Match manmatch = SteamManifestInstallDir.Match(File.ReadAllText(manifest));
                if (!manmatch.Success)
                    continue;

                string appdir = Regex.Unescape(manmatch.Groups[1].Value);

                if (DirExists(libpath, $"steamapps/common/{appdir}", out string rainworld))
                {
                    RainWorldRoot = rainworld;
                    return true;
                }
            }

            return false;
        }

        public static void LoadRegion(string regionPath)
        {
            string id = Path.GetFileName(regionPath);

            string worldFile = Path.Combine(regionPath, $"world_{id}.txt");
            string mapFile = Path.Combine(regionPath, $"map_{id}.txt");
            string? propertiesFile = Path.Combine(regionPath, $"properties.txt");

            bool altWorld = TryCheckSlugcatAltFile(worldFile, out worldFile);
            bool altMap = TryCheckSlugcatAltFile(mapFile, out mapFile);
            bool altProperties = TryCheckSlugcatAltFile(propertiesFile, out propertiesFile);

            if (!altWorld || !altMap || !altProperties)
                if (TryFindParentDir(regionPath, "mods", out string? mods))
                {
                    foreach (string mod in Directory.EnumerateDirectories(mods))
                    {
                        string modRegion = Path.Combine(mod, $"world/{id}");
                        if (Directory.Exists(modRegion))
                        {
                            if (!altWorld && TryCheckSlugcatAltFile(Path.Combine(modRegion, $"world_{id}.txt"), out string modAltWorld))
                            {
                                worldFile = modAltWorld;
                                altWorld = true;
                            }

                            if (!altMap && TryCheckSlugcatAltFile(Path.Combine(modRegion, $"map_{id}.txt"), out string modAltMap))
                            {
                                mapFile = modAltMap;
                                altMap = true;
                            }

                            if (!altProperties && TryCheckSlugcatAltFile(Path.Combine(modRegion, $"properties.txt"), out string modAltProperties))
                            {
                                propertiesFile = modAltProperties;
                                altProperties = true;
                            }
                        }
                    }
                }

            if (!altWorld || !altMap || !altProperties)
                if (TryFindParentDir(regionPath, "mergedmods", out string? mergedmods))
                {
                    if (!altWorld && FileExists(mergedmods, $"world/{id}/world_{id}.txt", out string mergedworld))
                        worldFile = mergedworld;

                    if (!altMap && FileExists(mergedmods, $"world/{id}/map_{id}.txt", out string mergedmap))
                        mapFile = mergedmap;

                    if (!altProperties && FileExists(mergedmods, $"world/{id}/properties.txt", out string mergedproperties))
                        propertiesFile = mergedproperties;
                }

            if (!File.Exists(propertiesFile))
                propertiesFile = null;

            Region = new(id, worldFile, mapFile, propertiesFile, Path.Combine(regionPath, $"../{id}-rooms"));
            RegionLoaded(Region);
        }
        public static void RegionLoaded(Region region)
        {
            SelectedObjects.Clear();
            Selecting = false;
            Dragging = false;

            WorldObjectLists.Clear();
            WorldObjectLists.Add(region.Rooms);
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

            Interface.RegionSelectVisible = false;
            Interface.SlugcatSelectVisible = false;

            Interface.RegionChanged(region);
        }

        public static JsonObject SaveJson()
        {
            return new()
            {
                ["slugcat"] = SelectedSlugcat,
                ["region"] = Region?.SaveJson(),
                ["connections"] = Region?.Connections?.SaveJson(),
                ["objects"] = new JsonArray(WorldObjectLists.Enumerate().Select(o => o.SaveJson()).OfType<JsonNode>().ToArray()),
                ["interface"] = InterfaceState.SaveJson(),
            };
        }
        public static void LoadJson(JsonNode node)
        {
            if (node.TryGet("slugcat", out string? slugcat))
            {
                SelectedSlugcat = slugcat;
            }
            if (node.TryGet("region", out JsonNode? region))
            {
                Region = new();
                Region.LoadJson(region);
                RegionLoaded(Region);
            }
            if (node.TryGet("connections", out JsonNode? connections))
                Region?.Connections?.LoadJson(connections);

            if (node.TryGet("objects", out JsonArray? objects))
                foreach (JsonNode? objNode in objects)
                    if (objNode is not null && !MapObject.LoadObject(objNode, WorldObjectLists))
                    {
                        MapObject? obj = MapObject.CreateObject(objNode);

                        if (obj is null || obj.LoadCreationForbidden)
                            continue;

                        WorldObjects.Add(obj);
                    }

            if (node.TryGet("interface", out JsonNode? @interface))
                InterfaceState.LoadJson(@interface);

            Region?.BindRooms();
        }

        public static void OpenState()
        {
            string? fileName = null;
            Thread thread = new(() =>
            {
                System.Windows.Forms.OpenFileDialog ofd = new();

                ofd.Filter = "Cornifer map files|*.json;*.cornimap";

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    fileName = ofd.FileName;
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (fileName is null)
                return;

            try
            {
                using FileStream fs = File.OpenRead(fileName);

                JsonNode? node = JsonSerializer.Deserialize<JsonNode>(fs);
                if (node is not null)
                {
                    LoadJson(node);
                    CurrentStatePath = fileName;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Exception has been thrown while opening selected state.\n\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Error");
            }
        }
        public static void SaveState()
        {
            if (CurrentStatePath is null)
            {
                bool exit = false;
                Thread thread = new(() =>
                {
                    System.Windows.Forms.SaveFileDialog sfd = new();

                    sfd.Filter = "Cornifer map files|*.cornimap";

                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        CurrentStatePath = sfd.FileName;
                    else
                        exit = true;
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (exit)
                    return;
            }

            using MemoryStream ms = new();
            try
            {
                JsonSerializer.Serialize(ms, SaveJson(), new JsonSerializerOptions { WriteIndented = true });

                FileStream fs = File.Create(CurrentStatePath!);
                ms.Position = 0;
                ms.CopyTo(fs);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Exception has been thrown while saving state.\n" +
                    $"Clicking Ok skip saving process and leave old state (state.json) intact.\n" +
                    $"\n" +
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Error");
            }

        }
        public static void SaveStateAs()
        {
            bool exit = false;
            Thread thread = new(() =>
            {
                System.Windows.Forms.SaveFileDialog sfd = new();

                sfd.Filter = "Cornifer map files|*.cornimap";

                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    CurrentStatePath = sfd.FileName;
                else
                    exit = true;
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exit)
                return;

            using MemoryStream ms = new();
            try
            {
                JsonSerializer.Serialize(ms, SaveJson(), new JsonSerializerOptions { WriteIndented = true });

                FileStream fs = File.Create(CurrentStatePath!);
                ms.Position = 0;
                ms.CopyTo(fs);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Exception has been thrown while saving state.\n" +
                    $"Clicking Ok skip saving process and leave old state (state.json) intact.\n" +
                    $"\n" +
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Error");
            }
        }

        public static bool TryFindParentDir(string path, string dirName, [NotNullWhen(true)] out string? result)
        {
            string? dir = path;
            result = null;
            while (dir is not null)
            {
                result = Path.Combine(dir, dirName);
                if (Directory.Exists(result))
                    return true;
                dir = Path.GetDirectoryName(dir);
            }
            return false;
        }
        public static bool FileExists(string dir, string file, out string filepath)
        {
            filepath = Path.Combine(dir, file);
            return File.Exists(filepath);
        }
        public static bool DirExists(string dir, string name, out string dirpath)
        {
            dirpath = Path.Combine(dir, name);
            return Directory.Exists(dirpath);
        }
        public static string CheckSlugcatAltFile(string filepath)
        {
            TryCheckSlugcatAltFile(filepath, out string result);
            return result;
        }
        public static bool TryCheckSlugcatAltFile(string filepath, out string result)
        {
            result = filepath;
            if (SelectedSlugcat is null)
                return false;

            // path/to/file.ext -> path/to/file-name.ext
            string slugcatfile = Path.Combine(Path.GetDirectoryName(filepath) ?? "", $"{Path.GetFileNameWithoutExtension(filepath)}-{SelectedSlugcat}{Path.GetExtension(filepath)}");
            if (File.Exists(slugcatfile))
            {
                result = slugcatfile;
                return true;
            }
            return false;
        }
        public static IEnumerable<(string id, string name, string path)> FindRegions()
        {
            if (RainWorldRoot is null)
                yield break;

            HashSet<string> foundRegions = new();

            List<string> worlds = new()
            {
                Path.Combine(RainWorldRoot, "RainWorld_Data/StreamingAssets/world")
            };

            string mods = Path.Combine(RainWorldRoot, "RainWorld_Data/StreamingAssets/mods");

            if (Directory.Exists(mods))
                foreach (string mod in Directory.EnumerateDirectories(mods))
                    worlds.Add(Path.Combine(mod, "world"));

            foreach (string world in worlds)
                if (Directory.Exists(world))
                {
                    foreach (string region in Directory.EnumerateDirectories(world))
                    {
                        string displayname = Path.Combine(region, "displayname.txt");
                        if (File.Exists(displayname))
                        {
                            string name = File.ReadAllText(displayname);

                            if (foundRegions.Contains(name))
                                continue;

                            yield return (Path.GetFileName(region).ToUpper(), name, region);

                            foundRegions.Add(name);
                        }
                    }
                }
        }
    }

    [Flags]
    public enum RenderLayers
    {
        All = 0x1f,

        Rooms = 1,
        Connections = 2,
        InRoomShortcuts = 16,
        Icons = 4,
        Texts = 8,

        None = 255,
    }
}
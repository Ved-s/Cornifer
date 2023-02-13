using Cornifer.Renderers;
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

        public static CameraRenderer WorldCamera = null!;
        
        public static List<MapObject> WorldObjects = new();
        public static CompoundEnumerable<MapObject> WorldObjectLists = new();
        public static HashSet<MapObject> SelectedObjects = new();

        public static string? RainWorldRoot;

        public static KeyboardState KeyboardState;
        public static KeyboardState OldKeyboardState;

        public static MouseState MouseState;
        public static MouseState OldMouseState;

        public static string? SelectedSlugcat;

        public static List<string> LoadErrors = new();

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

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += (_, _) => Interface.Root?.Recalculate();

            FileInfo stateFile = new("state.json");

            if (stateFile.Exists && stateFile.Length > 0)
            {
                try
                {
                    using FileStream fs = File.OpenRead("state.json");

                    JsonNode? node = JsonSerializer.Deserialize<JsonNode>(fs);
                    if (node is not null)
                        LoadJson(node);
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
            }

            Interface.Init();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Cornifer.Content.Load(Content);

            FormattedText.FontSpaceOverride[Cornifer.Content.RodondoExt20] = 4;
            FormattedText.FontSpaceOverride[Cornifer.Content.RodondoExt30] = 4;

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

            UpdateSelectionAndDrag(active && MouseState.LeftButton == ButtonState.Pressed, active && OldMouseState.LeftButton == ButtonState.Pressed);

            if (KeyboardState.IsKeyDown(Keys.Escape) && OldKeyboardState.IsKeyUp(Keys.Escape))
                LoadErrors.Clear();

            float keyMoveMultiplier = 1;
            if (KeyboardState.IsKeyDown(Keys.LeftShift))
                keyMoveMultiplier = 10;

            if (active && !Interface.Active)
            {

                if (KeyboardState.IsKeyDown(Keys.Up) && OldKeyboardState.IsKeyUp(Keys.Up))
                    foreach (MapObject obj in SelectedObjects)
                        if (obj.Active && !obj.ParentSelected)
                            obj.ParentPosition += new Vector2(0, -1) * keyMoveMultiplier;

                if (KeyboardState.IsKeyDown(Keys.Down) && OldKeyboardState.IsKeyUp(Keys.Down))
                    foreach (MapObject obj in SelectedObjects)
                        if (obj.Active && !obj.ParentSelected)
                            obj.ParentPosition += new Vector2(0, 1) * keyMoveMultiplier;

                if (KeyboardState.IsKeyDown(Keys.Left) && OldKeyboardState.IsKeyUp(Keys.Left))
                    foreach (MapObject obj in SelectedObjects)
                        if (obj.Active && !obj.ParentSelected)
                            obj.ParentPosition += new Vector2(-1, 0) * keyMoveMultiplier;

                if (KeyboardState.IsKeyDown(Keys.Right) && OldKeyboardState.IsKeyUp(Keys.Right))
                    foreach (MapObject obj in SelectedObjects)
                        if (obj.Active && !obj.ParentSelected)
                            obj.ParentPosition += new Vector2(1, 0) * keyMoveMultiplier;

                if (KeyboardState.IsKeyDown(Keys.Delete) && OldKeyboardState.IsKeyUp(Keys.Delete))
                {
                    HashSet<MapObject> objectsToDelete = new(SelectedObjects);
                    objectsToDelete.IntersectWith(WorldObjects);

                    WorldObjects.RemoveAll(x => objectsToDelete.Contains(x));
                    SelectedObjects.ExceptWith(objectsToDelete);
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

            if (SelectedObjects.Count > 0)
            {
                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                foreach (MapObject obj in SelectedObjects)
                    SpriteBatch.DrawRect(WorldCamera.TransformVector(obj.WorldPosition) - new Vector2(2), obj.Size * WorldCamera.Scale + new Vector2(4), Color.White * 0.4f);
                SpriteBatch.End();
            }

            DrawMap(WorldCamera);

            if (Selecting)
            {
                Vector2 mouseWorld = WorldCamera.InverseTransformVector(MouseState.Position.ToVector2());
                Vector2 tl = new(Math.Min(mouseWorld.X, SelectionStart.X), Math.Min(mouseWorld.Y, SelectionStart.Y));
                Vector2 br = new(Math.Max(mouseWorld.X, SelectionStart.X), Math.Max(mouseWorld.Y, SelectionStart.Y));

                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                SpriteBatch.DrawRect(WorldCamera.TransformVector(tl), (br - tl) * WorldCamera.Scale, Color.LightBlue * 0.2f);
                SpriteBatch.End();
            }

            if (LoadErrors.Count > 0)
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

            SpriteBatch.Begin();
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

        private void UpdateSelectionAndDrag(bool drag, bool oldDrag)
        {
            Vector2 mouseWorld = WorldCamera.InverseTransformVector(MouseState.Position.ToVector2());

            if (drag && !oldDrag && !Interface.Hovered)
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
                        foreach (MapObject obj in SelectedObjects)
                            if (!obj.ParentSelected)
                                obj.ParentPosition += diff;

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
                    {
                        Vector2 pos = obj.WorldPosition;
                        pos.Round();
                        obj.WorldPosition = pos;
                    }
                }

                Dragging = false;
                Selecting = false;
            }
        }

        public static void DrawMap(Renderer renderer)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (Region is not null)
                foreach (MapObject obj in Region.Rooms)
                    obj.Draw(renderer);

            Region?.Draw(renderer);

            if (Region is not null)
                foreach (MapObject obj in WorldObjects)
                    obj.Draw(renderer);

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
                ["objects"] = new JsonArray(WorldObjectLists.Enumerate().Select(o => o.SaveJson()).ToArray())
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
                Main.RegionLoaded(Region);
            }
            if (node.TryGet("objects", out JsonArray? objects))
                foreach (JsonNode? objNode in objects)
                    if (objNode is not null && !MapObject.LoadObject(objNode, WorldObjectLists))
                    {
                        MapObject? obj = MapObject.CreateObject(objNode);

                        if (obj is Room)
                            continue;

                        if (obj is not null)
                            WorldObjects.Add(obj);
                    }
        }

        public static void OpenState()
        {
            string? fileName = null;
            Thread thread = new(() =>
            {
                System.Windows.Forms.OpenFileDialog ofd = new();

                ofd.Filter = "JSON Files|*.json";

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

                    sfd.Filter = "JSON Files|*.json";

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

                sfd.Filter = "JSON Files|*.json";

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
}
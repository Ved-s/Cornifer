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

namespace Cornifer
{
    public class Main : Game
    {
        public static string[] SlugCatNames = new[] { "White", "Yellow", "Red", "Night", "Gourmand", "Artificer", "Rivulet", "Spear", "Saint", "Inv" };

        public static GraphicsDeviceManager GraphicsManager = null!;
        public static SpriteBatch SpriteBatch = null!;

        public static Main Instance = null!;

        public static Region? Region;

        public static Texture2D Pixel = null!;

        public static CameraRenderer WorldCamera = null!;

        public static HashSet<ISelectable> SelectedObjects = new();

        public static string? RainWorldRoot;

        public static KeyboardState KeyboardState;
        public static KeyboardState OldKeyboardState;

        public static MouseState MouseState;
        public static MouseState OldMouseState;

        public static string? SelectedSlugcat;

        static Vector2 SelectionStart;
        static Vector2 OldDragPos;
        internal static bool Selecting;
        internal static bool Dragging;

        static bool OldActive;

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

            SearchRainWorld();

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += (_, _) => Interface.Root?.Recalculate();

            Interface.Init();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Cornifer.Content.Load(Content);

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

            WorldCamera.Update();
            Interface.Update();
        }

        private void UpdateSelectionAndDrag(bool drag, bool oldDrag)
        {
            Vector2 mouseWorld = WorldCamera.InverseTransformVector(MouseState.Position.ToVector2());

            if (drag && !oldDrag && !Interface.Hovered)
            {
                // Clicked on already selected room
                if (ISelectable.FindSelectableAtPos(SelectedObjects, mouseWorld) is not null)
                {
                    Dragging = true;
                    OldDragPos = mouseWorld;
                    return;
                }
                if (Region is not null)
                {
                    // Clicked on not selected room
                    ISelectable? selectable = ISelectable.FindSelectableAtPos(Region.EnumerateSelectables(), mouseWorld);
                    if (selectable is not null)
                    {
                        SelectedObjects.Clear();
                        SelectedObjects.Add(selectable);
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
                        foreach (ISelectable selectable in SelectedObjects)
                            selectable.Position += diff;

                    OldDragPos = mouseWorld;
                }

                if (Selecting)
                {
                    Vector2 tl = new(Math.Min(mouseWorld.X, SelectionStart.X), Math.Min(mouseWorld.Y, SelectionStart.Y));
                    Vector2 br = new(Math.Max(mouseWorld.X, SelectionStart.X), Math.Max(mouseWorld.Y, SelectionStart.Y));

                    if (!KeyboardState.IsKeyDown(Keys.LeftControl) && !KeyboardState.IsKeyDown(Keys.LeftShift))
                        SelectedObjects.Clear();

                    if (KeyboardState.IsKeyDown(Keys.LeftControl))
                        SelectedObjects.ExceptWith(ISelectable.FindIntersectingSelectables(SelectedObjects, tl, br));
                    else if (Region is not null)
                        SelectedObjects.UnionWith(ISelectable.FindIntersectingSelectables(Region.EnumerateSelectables(), tl, br));
                }
            }
            else
            {
                if (!drag && oldDrag)
                {
                    foreach (ISelectable selectable in SelectedObjects)
                    {
                        Vector2 pos = selectable.Position;
                        pos.Round();
                        selectable.Position = pos;
                    }
                }

                Dragging = false;
                Selecting = false;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            Viewport vp = GraphicsDevice.Viewport;
            GraphicsDevice.ScissorRectangle = new(0, 0, vp.Width, vp.Height);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (SelectedObjects.Count > 0)
            {
                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                foreach (ISelectable selectable in SelectedObjects)
                    SpriteBatch.DrawRect(WorldCamera.TransformVector(selectable.Position) - new Vector2(2), selectable.Size * WorldCamera.Scale + new Vector2(4), Color.White * 0.4f);
                SpriteBatch.End();
            }

            Region?.Draw(WorldCamera);

            if (Selecting)
            {
                Vector2 mouseWorld = WorldCamera.InverseTransformVector(MouseState.Position.ToVector2());
                Vector2 tl = new(Math.Min(mouseWorld.X, SelectionStart.X), Math.Min(mouseWorld.Y, SelectionStart.Y));
                Vector2 br = new(Math.Max(mouseWorld.X, SelectionStart.X), Math.Max(mouseWorld.Y, SelectionStart.Y));

                SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                SpriteBatch.DrawRect(WorldCamera.TransformVector(tl), (br - tl) * WorldCamera.Scale, Color.LightBlue * 0.2f);
                SpriteBatch.End();
            }

            SpriteBatch.Begin();
            Interface.Draw();
            SpriteBatch.End();

            base.Draw(gameTime);
        }

        public static bool SearchRainWorld()
        {
            object? steampathobj =
                    Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) ??
                    Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null);
            if (steampathobj is string steampath)
            {
                string rwpath = Path.Combine(steampath, "steamapps/common/Rain World");
                if (Directory.Exists(rwpath))
                {
                    RainWorldRoot = rwpath;
                    return true;
                }
            }
            return false;
        }

        public static void LoadRegion(string regionPath)
        {
            SelectedObjects.Clear();
            Selecting = false;
            Dragging = false;

            string id = Path.GetFileName(regionPath);

            string worldFile = Path.Combine(regionPath, $"world_{id}.txt");
            string mapFile = Path.Combine(regionPath, $"map_{id}.txt");

            bool altWorld = TryCheckSlugcatAltFile(worldFile, out worldFile);
            bool altMap = TryCheckSlugcatAltFile(mapFile, out mapFile); ;

            if (!altWorld || !altMap)
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
                        }
                    }
                }

            if (!altWorld || !altMap)
                if (TryFindParentDir(regionPath, "mergedmods", out string? mergedmods))
                {
                    if (!altWorld && FileExists(mergedmods, $"world/{id}/world_{id}.txt", out string mergedworld))
                        worldFile = mergedworld;

                    if (!altMap && FileExists(mergedmods, $"world/{id}/map_{id}.txt", out string mergedmap))
                        mapFile = mergedmap;
                }

            Region = new(id, worldFile, mapFile, Path.Combine(regionPath, $"../{id}-rooms"));

            foreach (ISelectable selectable in Region.EnumerateSelectables())
            {
                Vector2 pos = selectable.Position;
                pos.Round();
                selectable.Position = pos;
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
                foreach(string mod in Directory.EnumerateDirectories(mods))
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
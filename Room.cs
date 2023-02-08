using Cornifer.Interfaces;
using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Cornifer
{
    public class Room : ISelectable, ISelectableContainer
    {
        static Point[] Directions = new Point[] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };
        static HashSet<string> NonPickupObjectsWhitelist = new() { "GhostSpot", "BlueToken", "GoldToken", "RedToken", "WhiteToken", "DevToken", "DataPearl", "UniqueDataPearl" };

        public static bool DrawTileWalls = true;
        public static bool ForceWaterBehindSolid = false;
        public static bool DrawObjects = true;
        public static bool DrawPickUpObjects = true;
        public static bool DrawCropped = false;

        public static float WaterTransparency = .3f;

        public string Id;
        public string Name = null!;

        public bool IsGate;
        public bool IsShelter;
        public bool IsAncientShelter;

        public Point Size;
        public int WaterLevel;
        public bool WaterInFrontOfTerrain;
        public Tile[,] Tiles = null!;

        public Rectangle? NonSolidRect;

        public Point[] Exits = Array.Empty<Point>();
        public Shortcut[] Shortcuts = Array.Empty<Shortcut>();

        public int Layer;
        public int Subregion = 0;

        public Vector2 WorldPos;

        public Effect[] Effects = Array.Empty<Effect>();
        public Connection?[] Connections = Array.Empty<Connection>();
        public PlacedObject[] PlacedObjects = Array.Empty<PlacedObject>();

        public List<SelectableIcon> Icons = new();

        public Texture2D? TileMap;
        public bool TileMapDirty = false;

        public bool Loaded = false;

        public readonly Region Region;

        bool ISelectable.Active => true;
        Vector2 ISelectable.Position
        {
            get => WorldPos;
            set => WorldPos = value;
        }
        Vector2 ISelectable.Size => Size.ToVector2();

        public Room(Region region, string id)
        {
            Region = region;
            Id = id;
        }
        public Point TraceShotrcut(Point pos)
        {
            Point lastPos = pos;
            int? dir = null;
            bool foundDir = false;

            while (true)
            {
                if (dir is not null)
                {
                    Point dirVal = Directions[dir.Value];

                    Point testTilePos = pos + dirVal;

                    if (testTilePos.X >= 0 && testTilePos.Y >= 0 && testTilePos.X < Size.X && testTilePos.Y < Size.Y)
                    {
                        Tile tile = Tiles[testTilePos.X, testTilePos.Y];
                        if (tile.Shortcut == Tile.ShortcutType.Normal)
                        {
                            lastPos = pos;
                            pos = testTilePos;
                            continue;
                        }
                    }
                }
                foundDir = false;
                for (int j = 0; j < 4; j++)
                {
                    Point dirVal = Directions[j];
                    Point testTilePos = pos + dirVal;

                    if (testTilePos == lastPos || testTilePos.X < 0 || testTilePos.Y < 0 || testTilePos.X >= Size.X || testTilePos.Y >= Size.Y)
                        continue;

                    Tile tile = Tiles[testTilePos.X, testTilePos.Y];
                    if (tile.Shortcut == Tile.ShortcutType.Normal)
                    {
                        dir = j;
                        foundDir = true;
                        break;
                    }
                }
                if (!foundDir)
                    break;
            }

            return pos;
        }

        public Tile GetTile(int x, int y)
        {
            x = Math.Clamp(x, 0, Size.X - 1);
            y = Math.Clamp(y, 0, Size.Y - 1);
            return Tiles[x, y];
        }

        public void Load(string data, string? settings)
        {
            string[] lines = File.ReadAllLines(data);

            if (lines.TryGet(0, out string displayname))
                Name = displayname;

            if (lines.TryGet(1, out string sizeWater))
            {
                string[] swArray = sizeWater.Split('|');
                if (swArray.TryGet(0, out string size))
                {
                    string[] sArray = size.Split('*');
                    if (sArray.TryGet(0, out string widthStr) && int.TryParse(widthStr, out int width))
                        Size.X = width;
                    if (sArray.TryGet(1, out string heightStr) && int.TryParse(heightStr, out int height))
                        Size.Y = height;
                }
                if (swArray.TryGet(1, out string waterLevelStr) && int.TryParse(waterLevelStr, out int waterLevel))
                {
                    WaterLevel = waterLevel;
                }
                if (swArray.TryGet(2, out string waterInFrontStr))
                {
                    WaterInFrontOfTerrain = waterInFrontStr == "1";
                }
            }

            if (lines.TryGet(11, out string tiles))
            {
                Tiles = new Tile[Size.X, Size.Y];

                string[] tilesArray = tiles.Split('|');

                Point nonSolidTL = new(Size.X, Size.Y);
                Point nonSolidBR = new(0, 0);

                int x = 0, y = 0;
                for (int i = 0; i < tilesArray.Length; i++)
                {
                    if (tilesArray[i].Length == 0 || x < 0 || y < 0 || x >= Tiles.GetLength(0) || y >= Tiles.GetLength(1))
                        continue;

                    string[] tileArray = tilesArray[i].Split(',');
                    Tile tile = new();

                    for (int j = 0; j < tileArray.Length; j++)
                    {
                        if (j == 0)
                        {
                            if (!int.TryParse(tileArray[j], out int terrain))
                                continue;

                            tile.Terrain = (Tile.TerrainType)terrain;
                            continue;
                        }

                        switch (tileArray[j])
                        {
                            case "1": tile.Attributes |= Tile.TileAttributes.VerticalBeam; break;
                            case "2": tile.Attributes |= Tile.TileAttributes.HorizontalBeam; break;

                            case "3" when tile.Shortcut == Tile.ShortcutType.None:
                                tile.Shortcut = Tile.ShortcutType.Normal;
                                break;

                            case "4": tile.Shortcut = Tile.ShortcutType.RoomExit; break;
                            case "5": tile.Shortcut = Tile.ShortcutType.CreatureHole; break;
                            case "6": tile.Attributes |= Tile.TileAttributes.WallBehind; break;
                            case "7": tile.Attributes |= Tile.TileAttributes.Hive; break;
                            case "8": tile.Attributes |= Tile.TileAttributes.Waterfall; break;
                            case "9": tile.Shortcut = Tile.ShortcutType.NPCTransportation; break;
                            case "10": tile.Attributes |= Tile.TileAttributes.GarbageHole; break;
                            case "11": tile.Attributes |= Tile.TileAttributes.WormGrass; break;
                            case "12": tile.Shortcut = Tile.ShortcutType.RegionTransportation; break;
                        }
                    }

                    Tiles[x, y] = tile;

                    if (tile.Terrain != Tile.TerrainType.Solid)
                    {
                        if (x < nonSolidTL.X) nonSolidTL.X = x;
                        if (y < nonSolidTL.Y) nonSolidTL.Y = y;
                        if (x > nonSolidBR.X) nonSolidBR.X = x;
                        if (y > nonSolidBR.Y) nonSolidBR.Y = y;
                    }

                    y++;
                    if (y >= Size.Y)
                    {
                        x++;
                        y = 0;
                    }
                }

                nonSolidTL = new(Math.Max(0, nonSolidTL.X - 1), Math.Max(0, nonSolidTL.Y - 1));
                nonSolidBR = new(Math.Min(Size.X, nonSolidBR.X + 2), Math.Min(Size.Y, nonSolidBR.Y + 2));

                if (nonSolidTL.X < nonSolidBR.X && nonSolidTL.Y < nonSolidBR.Y)
                    NonSolidRect = new(nonSolidTL.X, nonSolidTL.Y, nonSolidBR.X - nonSolidTL.X, nonSolidBR.Y - nonSolidTL.Y);

                List<Point> exits = new();
                List<Point> shortcuts = new();

                for (int j = 0; j < Size.Y; j++)
                    for (int i = 0; i < Size.X; i++)
                    {
                        Tile tile = Tiles[i, j];

                        if (tile.Terrain == Tile.TerrainType.ShortcutEntrance)
                            shortcuts.Add(new Point(i, j));

                        if (tile.Shortcut == Tile.ShortcutType.RoomExit)
                            exits.Add(new Point(i, j));
                    }

                Point[] exitEntrances = new Point[exits.Count];

                for (int i = 0; i < exits.Count; i++)
                {
                    exitEntrances[i] = TraceShotrcut(exits[i]);
                }

                List<Shortcut> tracedShortcuts = new();

                foreach (Point shortcutIn in shortcuts)
                {
                    Point target = TraceShotrcut(shortcutIn);
                    Tile targetTile = GetTile(target.X, target.Y);
                    tracedShortcuts.Add(new(shortcutIn, target, targetTile.Shortcut));
                }

                Shortcuts = tracedShortcuts.ToArray();
                Exits = exitEntrances;
            }

            if (settings is not null)
                foreach (string line in File.ReadAllLines(settings))
                {
                    string[] split = line.Split(':', 2, StringSplitOptions.TrimEntries);

                    if (split[0] == "PlacedObjects")
                    {
                        string[] objects = split[1].Split(',', StringSplitOptions.TrimEntries);
                        List<PlacedObject> objectList = new();
                        foreach (string str in objects)
                        {
                            PlacedObject? obj = PlacedObject.Load(this, str);
                            if (obj is not null)
                                objectList.Add(obj);
                        }

                        PlacedObjects = objectList.ToArray();
                    }
                    else if (split[0] == "Effects")
                    {
                        List<Effect> effects = new();

                        foreach (string effectStr in split[1].Split(',', StringSplitOptions.TrimEntries))
                        {
                            string[] effectSplit = effectStr.Split('-');
                            if (effectSplit.Length == 4)
                            {
                                string name = effectSplit[0];
                                if (!float.TryParse(effectSplit[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
                                    amount = 0;

                                effects.Add(new(name, amount));
                            }
                        }

                        Effects = effects.ToArray();
                    }
                }

            if (IsShelter && GameAtlases.Sprites.TryGetValue("ShelterMarker", out var shelterMarker))
                Icons.Add(new SimpleIcon(this, shelterMarker));
            
            Loaded = true;
        }

        public Texture2D GetTileMap()
        {
            if (TileMap is null || TileMapDirty)
                UpdateTileMap();
            return TileMap!;
        }

        public void UpdateTileMap()
        {
            Color[] colors = ArrayPool<Color>.Shared.Rent(Size.X * Size.Y);
            try
            {
                bool invertedWater = Effects.Any(ef => ef.name == "InvertedWater");

                int waterLevel = WaterLevel;

                if (waterLevel < 0)
                {
                    Effect? waterFluxMin = Effects.FirstOrDefault(ef => ef.name == "WaterFluxMinLevel");
                    Effect? waterFluxMax = Effects.FirstOrDefault(ef => ef.name == "WaterFluxMaxLevel");

                    if (waterFluxMin is not null && waterFluxMax is not null)
                    {
                        float waterMid = 1 - ((waterFluxMax.amount + waterFluxMin.amount) / 2 * (22f / 20f));
                        waterLevel = (int)(waterMid * Size.Y) + 2;
                    }
                }

                Region.Subregion subregion = Region.Subregions[Subregion];

                for (int j = 0; j < Size.Y; j++)
                    for (int i = 0; i < Size.X; i++)
                    {
                        Tile tile = GetTile(i, j);

                        float gray = 1;

                        bool solid = tile.Terrain == Tile.TerrainType.Solid;

                        if (solid)
                            gray = 0;

                        else if (tile.Terrain == Tile.TerrainType.Floor)
                            gray = 0.35f;

                        else if (tile.Terrain == Tile.TerrainType.Slope)
                            gray = .4f;

                        else if (DrawTileWalls && tile.Attributes.HasFlag(Tile.TileAttributes.WallBehind))
                            gray = 0.75f;

                        if (tile.Attributes.HasFlag(Tile.TileAttributes.VerticalBeam) || tile.Attributes.HasFlag(Tile.TileAttributes.HorizontalBeam))
                            gray = 0.35f;

                        Color color = Color.Lerp(Color.Black, subregion.BackgroundColor, gray);

                        if ((!ForceWaterBehindSolid && WaterInFrontOfTerrain || !solid) && (invertedWater ? j <= waterLevel : j >= Size.Y - waterLevel))
                        {
                            color = Color.Lerp(subregion.WaterColor, color, WaterTransparency);
                        }

                        colors[i + j * Size.X] = color;
                    }

                foreach (Point p in Exits)
                    colors[p.X + p.Y * Size.X] = new(255, 0, 0);

                TileMap ??= new(Main.Instance.GraphicsDevice, Size.X, Size.Y);
                TileMap.SetData(colors, 0, Size.X * Size.Y);
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(colors);
            }
            TileMapDirty = false;
        }

        public void Draw(Renderer renderer)
        {
            if (!Loaded)
                return;

            if (DrawCropped && NonSolidRect.HasValue)
                renderer.DrawTexture(GetTileMap(), WorldPos + NonSolidRect.Value.Location.ToVector2(), NonSolidRect.Value);
            else 
                renderer.DrawTexture(GetTileMap(), WorldPos);

            Main.SpriteBatch.DrawStringAligned(Content.Consolas10, Name, renderer.TransformVector(WorldPos + new Vector2(Size.X / 2, .5f)), Color.Yellow, new(.5f, 0), Color.Black);

            if (DrawObjects)
                foreach (PlacedObject obj in PlacedObjects)
                    if (DrawPickUpObjects || NonPickupObjectsWhitelist.Contains(obj.Name))
                        obj.Draw(renderer);
            foreach (SelectableIcon icon in Icons)
                icon.Draw(renderer);
        }

        public IEnumerable<ISelectable> EnumerateSelectables()
        {
            foreach (SelectableIcon icon in ((IEnumerable<SelectableIcon>)Icons).Reverse())
                yield return icon;

            if (DrawObjects)
                foreach (PlacedObject obj in PlacedObjects.Reverse())
                    if (DrawPickUpObjects || NonPickupObjectsWhitelist.Contains(obj.Name))
                        foreach (ISelectable selectable in obj.EnumerateSelectables())
                            yield return selectable;

            yield return this;
        }

        public override string ToString()
        {
            return Id;
        }

        public record class Connection(Room Target, int Exit, int TargetExit)
        {
            public override string ToString()
            {
                return $"{Exit} -> {Target.Id}[{TargetExit}]";
            }
        }
        public record class Shortcut(Point entrance, Point target, Tile.ShortcutType type);
        public record class Effect(string name, float amount);

        public struct Tile
        {
            public TerrainType Terrain;
            public ShortcutType Shortcut;
            public TileAttributes Attributes;

            [Flags]
            public enum TileAttributes
            {
                None = 0,
                VerticalBeam = 1,
                HorizontalBeam = 2,
                WallBehind = 4,
                Hive = 8,
                Waterfall = 16,
                GarbageHole = 32,
                WormGrass = 64
            }

            public enum TerrainType
            {
                Air,
                Solid,
                Slope,
                Floor,
                ShortcutEntrance
            }

            public enum ShortcutType
            {
                None,
                Normal,
                RoomExit,
                CreatureHole,
                NPCTransportation,
                RegionTransportation,
            }
        }
    }
}
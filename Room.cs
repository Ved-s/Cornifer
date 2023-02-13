using Cornifer.Renderers;
using Cornifer.UI.Elements;
using Cornifer.UI.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public class Room : MapObject
    {
        static Point[] Directions = new Point[] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };
        public static HashSet<string> NonPickupObjectsWhitelist = new() { "GhostSpot", "BlueToken", "GoldToken", "RedToken", "WhiteToken", "DevToken", "DataPearl", "UniqueDataPearl" };
        public static Dictionary<string, Vector2> VistaRooms = new()
        {
            ["HI_B04"] = new(214f, 615f),
            ["HI_D01"] = new(1765f, 655f),
            ["HI_C04"] = new(800f, 768f),
            ["SU_B12"] = new(1180f, 382f),
            ["SU_A04"] = new(265f, 415f),
            ["SU_C01"] = new(450f, 1811f),
            ["GW_D01"] = new(1603f, 595f),
            ["GW_E02"] = new(2608f, 621f),
            ["GW_C09"] = new(607f, 595f),
            ["UW_A07"] = new(805f, 616f),
            ["UW_J01"] = new(860f, 1534f),
            ["UW_C02"] = new(493f, 490f),
            ["CC_B12"] = new(455f, 1383f),
            ["CC_A10"] = new(734f, 506f),
            ["CC_C05"] = new(449f, 2330f),
            ["DS_A19"] = new(467f, 545f),
            ["DS_A05"] = new(172f, 490f),
            ["DS_C02"] = new(541f, 1305f),
            ["SI_C07"] = new(539f, 2354f),
            ["SI_D07"] = new(200f, 400f),
            ["SI_D05"] = new(1045f, 1258f),
            ["SH_A14"] = new(273f, 556f),
            ["SH_C08"] = new(2159f, 481f),
            ["SH_B05"] = new(733f, 453f),
            ["SL_B04"] = new(390f, 2258f),
            ["SL_B01"] = new(389f, 1448f),
            ["SL_C04"] = new(542f, 1295f),
            ["LF_C01"] = new(2792f, 423f),
            ["LF_A10"] = new(421f, 412f),
            ["LF_D02"] = new(1220f, 631f),
            ["SB_H02"] = new(1559f, 472f),
            ["SB_E04"] = new(1668f, 567f),
            ["SB_D04"] = new(483f, 1045f),
            ["VS_H02"] = new(603f, 3265f),
            ["VS_C03"] = new(82f, 983f),
            ["VS_F02"] = new(1348f, 533f),
            ["OE_RUINCourtYard"] = new(2133f, 1397f),
            ["OE_TREETOP"] = new(468f, 1782f),
            ["OE_RAIL01"] = new(2420f, 1378f),
            ["LC_FINAL"] = new(2700f, 500f),
            ["LC_SUBWAY01"] = new(1693f, 564f),
            ["LC_tallestconnection"] = new(153f, 242f),
            ["RM_CONVERGENCE"] = new(1860f, 670f),
            ["RM_I03"] = new(276f, 2270f),
            ["RM_ASSEMBLY"] = new(1550f, 586f),
            ["DM_LEG06"] = new(400f, 388f),
            ["DM_O06"] = new(2178f, 2159f),
            ["DM_LAB1"] = new(486f, 324f),
            ["UG_GUTTER02"] = new(163f, 241f),
            ["UG_A16"] = new(640f, 354f),
            ["UG_D03"] = new(857f, 1826f),
            ["CL_C05"] = new(540f, 1213f),
            ["CL_H02"] = new(2407f, 1649f),
            ["CL_CORE"] = new(471f, 373f),
        };

        public static bool DrawTileWalls = true;
        public static bool ForceWaterBehindSolid = false;
        public static bool DrawObjects = true;
        public static bool DrawPickUpObjects = true;
        public static bool DrawCropped = false;

        public static float WaterTransparency = .3f;

        public bool IsGate;
        public bool IsShelter;
        public bool IsAncientShelter;
        public bool IsScavengerTrader;
        public bool IsScavengerOutpost;

        public Point TileSize;
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
        public List<string> BrokenForSlugcats = new();

        public Texture2D? TileMap;
        public bool TileMapDirty = false;

        public bool Loaded = false;

        public string? DataString;
        public string? SettingsString;

        public readonly Region Region;

        public override Vector2 ParentPosition { get => WorldPos; set => WorldPos = value; }
        public override Vector2 Size => TileSize.ToVector2();

        public Room() { }

        public Room(Region region, string id)
        {
            Region = region;
            Name = id;
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

                    if (testTilePos.X >= 0 && testTilePos.Y >= 0 && testTilePos.X < TileSize.X && testTilePos.Y < TileSize.Y)
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

                    if (testTilePos == lastPos || testTilePos.X < 0 || testTilePos.Y < 0 || testTilePos.X >= TileSize.X || testTilePos.Y >= TileSize.Y)
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
            x = Math.Clamp(x, 0, TileSize.X - 1);
            y = Math.Clamp(y, 0, TileSize.Y - 1);
            return Tiles[x, y];
        }

        public void Load(string data, string? settings)
        {
            SettingsString = settings;

            string[] lines = data.Split('\n', StringSplitOptions.TrimEntries);

            if (lines.TryGet(1, out string sizeWater))
            {
                string[] swArray = sizeWater.Split('|');
                if (swArray.TryGet(0, out string size))
                {
                    string[] sArray = size.Split('*');
                    if (sArray.TryGet(0, out string widthStr) && int.TryParse(widthStr, out int width))
                        TileSize.X = width;
                    if (sArray.TryGet(1, out string heightStr) && int.TryParse(heightStr, out int height))
                        TileSize.Y = height;
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
                Tiles = new Tile[TileSize.X, TileSize.Y];

                string[] tilesArray = tiles.Split('|');

                Point nonSolidTL = new(TileSize.X, TileSize.Y);
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
                    if (y >= TileSize.Y)
                    {
                        x++;
                        y = 0;
                    }
                }

                nonSolidTL = new(Math.Max(0, nonSolidTL.X - 1), Math.Max(0, nonSolidTL.Y - 1));
                nonSolidBR = new(Math.Min(TileSize.X, nonSolidBR.X + 2), Math.Min(TileSize.Y, nonSolidBR.Y + 2));

                if (nonSolidTL.X < nonSolidBR.X && nonSolidTL.Y < nonSolidBR.Y)
                    NonSolidRect = new(nonSolidTL.X, nonSolidTL.Y, nonSolidBR.X - nonSolidTL.X, nonSolidBR.Y - nonSolidTL.Y);

                List<Point> exits = new();
                List<Point> shortcuts = new();

                for (int j = 0; j < TileSize.Y; j++)
                    for (int i = 0; i < TileSize.X; i++)
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

            for (int i = 0; i < lines.Length; i++)
            {
                if (i != 0 && i != 1 && i != 11)
                    lines[i] = "";
            }
            DataString = string.Join('\n', lines);

            if (settings is not null)
                foreach (string line in settings.Split('\n', StringSplitOptions.TrimEntries))
                {
                    string[] split = line.Split(':', 2, StringSplitOptions.TrimEntries);

                    if (split[0] == "PlacedObjects")
                    {
                        HashSet<PlacedObject> objects = new();
                        List<PlacedObject> filters = new();
                        string[] objectStrings = split[1].Split(',', StringSplitOptions.TrimEntries);
                        foreach (string str in objectStrings)
                        {
                            PlacedObject? obj = PlacedObject.Load(str);
                            if (obj is not null)
                            {
                                if (obj.Type == "Filter")
                                    filters.Add(obj);
                                else 
                                    objects.Add(obj);
                            }
                        }
                        List<PlacedObject> remove = new();

                        foreach (PlacedObject filter in filters)
                        {
                            Vector2 filterPos = filter.RoomPos;
                            float filterRad = filter.HandlePos.Length() / 20;

                            foreach (PlacedObject obj in objects)
                            {
                                Vector2 diff = obj.RoomPos - filterPos;
                                if (diff.Length() > filterRad)
                                    continue;

                                if (obj.SlugcatAvailability.Count == 0)
                                    obj.SlugcatAvailability.UnionWith(Main.AvailableSlugCatNames);

                                obj.SlugcatAvailability.IntersectWith(filter.SlugcatAvailability);

                                if (obj.RemoveByAvailability && Main.SelectedSlugcat is not null && obj.SlugcatAvailability.Count > 0 && !obj.SlugcatAvailability.Contains(Main.SelectedSlugcat))
                                    remove.Add(obj);
                            }
                        }

                        objects.ExceptWith(remove);

                        foreach (PlacedObject obj in objects)
                        {
                            obj.AddAvailabilityIcons();
                            Children.Add(obj);
                        }
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
                Children.Add(new SimpleIcon("ShelterMarker", shelterMarker));

            if (IsScavengerOutpost)
            {
                Children.Add(new MapText("TollText", Content.RodondoExt20, "Scavenger toll")
                {
                    Shade = true
                });
                if (GameAtlases.Sprites.TryGetValue("ChieftainA", out var tollIcon))
                    Children.Add(new SimpleIcon("TollIcon", tollIcon));
            }

            if (IsScavengerTrader)
            {
                Children.Add(new MapText("TraderText", Content.RodondoExt20, "Scavenger trader")
                {
                    Shade = true
                });
                if (GameAtlases.Sprites.TryGetValue("ChieftainA", out var tollIcon))
                    Children.Add(new SimpleIcon("TraderIcon", tollIcon));
            }

            if (VistaRooms.TryGetValue(Name, out Vector2 vistaPoint))
            {
                Vector2 rel = (vistaPoint / 20) / Size;

                rel.Y = 1 - rel.Y;

                Children.Add(new MapText("VistaMarker", Content.RodondoExt20, "Expedition\nvista\npoint")
                {
                    ParentPosAlign = rel,
                    Shade = true,
                });
            }

            if (BrokenForSlugcats.Count > 0)
            {
                string text = "Broken for [a:.5]" + string.Join(' ', BrokenForSlugcats.Select(s => $"[ic:Slugcat_{s}]"));
                Children.Add(new MapText("BrokenShelterText", Content.RodondoExt20, text)
                {
                    Shade = true,
                });
            }

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
            Color[] colors = ArrayPool<Color>.Shared.Rent(TileSize.X * TileSize.Y);
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
                        waterLevel = (int)(waterMid * TileSize.Y) + 2;
                    }
                }

                Region.Subregion subregion = Region.Subregions[Subregion];

                for (int j = 0; j < TileSize.Y; j++)
                    for (int i = 0; i < TileSize.X; i++)
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

                        if ((!ForceWaterBehindSolid && WaterInFrontOfTerrain || !solid) && (invertedWater ? j <= waterLevel : j >= TileSize.Y - waterLevel))
                        {
                            color = Color.Lerp(subregion.WaterColor, color, WaterTransparency);
                        }

                        colors[i + j * TileSize.X] = color;
                    }

                foreach (Point p in Exits)
                    colors[p.X + p.Y * TileSize.X] = new(255, 0, 0);

                TileMap ??= new(Main.Instance.GraphicsDevice, TileSize.X, TileSize.Y);
                TileMap.SetData(colors, 0, TileSize.X * TileSize.Y);
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(colors);
            }
            TileMapDirty = false;
        }

        protected override void DrawSelf(Renderer renderer)
        {
            if (!Loaded)
                return;

            if (DrawCropped && NonSolidRect.HasValue)
                renderer.DrawTexture(GetTileMap(), WorldPos + NonSolidRect.Value.Location.ToVector2(), NonSolidRect.Value);
            else
                renderer.DrawTexture(GetTileMap(), WorldPos);

            if (base.Name is not null)
                Main.SpriteBatch.DrawStringAligned(Content.Consolas10, base.Name, renderer.TransformVector(WorldPos + new Vector2(TileSize.X / 2, .5f)), Color.Yellow, new(.5f, 0), Color.Black);
        }
        protected override void BuildInnerConfig(UIList list)
        {
            if (Region is not null)
            {
                list.Elements.Add(new UIResizeablePanel
                {
                    Height = 100,

                    Padding = 4,

                    CanGrabTop = false,
                    CanGrabLeft = false,
                    CanGrabRight = false,
                    CanGrabBottom = true,

                    Elements =
                    {
                        new UILabel
                        {
                            Text = "Subregion",
                            Height = 15,
                            TextAlign = new(.5f)
                        },
                        new UIList
                        {
                            Top = 20,
                            Height = new(-20, 1),
                            ElementSpacing = 4
                        }.Assign(out UIList subregionList)
                    }
                });

                RadioButtonGroup group = new();

                for (int i = 0; i < Region.Subregions.Length; i++)
                {
                    Region.Subregion subregion = Region.Subregions[i];
                    UIButton button = new()
                    {
                        Text = subregion.Name.Length == 0 ? "Main region" : subregion.Name,
                        Height = 20,
                        TextAlign = new(.5f),
                        RadioGroup = group,
                        Selectable = true,
                        Selected = i == Subregion,
                        RadioTag = i,
                        SelectedTextColor = Color.Black,
                        SelectedBackColor = Color.White,
                    };

                    subregionList.Elements.Add(button);
                }

                group.ButtonClicked += (_, tag) =>
                {
                    if (tag is not int index)
                        return;
                    Subregion = index;
                    TileMapDirty = true;
                };
            }
        }

        protected override JsonNode? SaveInnerJson()
        {
            return new JsonObject
            {
                ["subregion"] = Region?.Subregions[Subregion].Name,
            };
        }
        protected override void LoadInnerJson(JsonNode node)
        {
            if (node.TryGet("subregion", out string? subregion) && Region is not null)
                for (int i = 0; i < Region.Subregions.Length; i++)
                    if (Region.Subregions[i].Name == subregion)
                        Subregion = i;

            TileMapDirty = true;
        }

        public override string ToString()
        {
            return Name;
        }

        public record class Connection(Room Target, int Exit, int TargetExit)
        {
            public override string ToString()
            {
                return $"{Exit} -> {Target.Name}[{TargetExit}]";
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
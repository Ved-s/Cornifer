using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Cornifer
{
    public class Region
    {
        static Regex GateNameRegex = new("GATE_(.+)?_(.+)", RegexOptions.Compiled);

        public string Id = "";
        public List<Room> Rooms = new();

        public Subregion[] Subregions = Array.Empty<Subregion>();
        public RegionConnections? Connections;

        string? WorldString;
        string? MapString;
        string? PropertiesString;
        string? GateLockString;

        // room name -> region name 
        Dictionary<string, string> GateTargetRegions = new();

        public Region()
        {

        }

        public Region(string id, string worldFilePath, string mapFilePath, string? propertiesFilePath, string roomsDir)
        {
            Id = id;
            WorldString = File.ReadAllText(worldFilePath);
            PropertiesString = propertiesFilePath is null ? null : File.ReadAllText(propertiesFilePath);
            MapString = File.ReadAllText(mapFilePath);

            Load();

            List<string> roomDirs = new();

            string worldRooms = Path.Combine(Path.GetDirectoryName(worldFilePath)!, $"../{Id}-rooms");
            if (Directory.Exists(worldRooms))
                roomDirs.Add(worldRooms);

            if (Main.TryFindParentDir(worldFilePath, "mods", out string? mods))
            {
                foreach (string mod in Directory.EnumerateDirectories(mods))
                {
                    string modRooms = Path.Combine(mod, $"world/{Id}-rooms");
                    if (Directory.Exists(modRooms))
                        roomDirs.Add(modRooms);
                }
            }

            roomDirs.Add(roomsDir);

            if (Main.TryFindParentDir(worldFilePath, "mergedmods", out string? mergedmods))
            {
                string rwworld = Path.Combine(mergedmods, "../world");
                if (Directory.Exists(rwworld))
                    roomDirs.Add(Path.Combine(rwworld, Id));
            }

            foreach (Room r in Rooms)
            {
                string? settings = null;
                string? data = null;

                string roomPath = r.IsGate ? $"../gates/{r.Name}" : r.Name;

                foreach (string roomDir in roomDirs)
                {
                    string dataPath = Path.Combine(roomDir, $"{roomPath}.txt");

                    if (data is null && File.Exists(dataPath))
                        data = dataPath;

                    if (Main.TryCheckSlugcatAltFile(dataPath, out string altDataPath))
                        data = altDataPath;

                    string settingsPath = Path.Combine(roomDir, $"{roomPath}_settings.txt");

                    if (settings is null && File.Exists(settingsPath))
                        settings = settingsPath;

                    if (Main.TryCheckSlugcatAltFile(settingsPath, out string altSettingsPath))
                        settings = altSettingsPath;
                }

                if (data is null)
                {
                    Main.LoadErrors.Add($"Could not find data for room {r.Name}");
                    continue;
                }

                r.Load(File.ReadAllText(data!), settings is null ? null : File.ReadAllText(settings));
            }

            HashSet<string> gatesProcessed = new();
            List<string> lockLines = new();
            foreach (string roomDir in roomDirs)
            {
                string locksPath = Path.Combine(roomDir, "../gates/locks.txt");
                if (!File.Exists(locksPath))
                    continue;

                AddGateLocks(File.ReadAllText(locksPath), gatesProcessed, lockLines);
            }

            if (lockLines.Count > 0)
                GateLockString = string.Join("\n", lockLines);

            Dictionary<string, string> regionNames = GetSlugcatSpecificRegionNames(worldFilePath);
            if (regionNames.Count > 0)
            {
                foreach (Room room in Rooms)
                {
                    if (!room.IsGate)
                        continue;

                    Match match = GateNameRegex.Match(room.Name);
                    if (!match.Success)
                        continue;

                    string otherRegionId;

                    string rgLeft = match.Groups[1].Value;
                    string rgRight = match.Groups[2].Value;

                    if (id.Equals(rgLeft, StringComparison.InvariantCultureIgnoreCase))
                        otherRegionId = rgRight;
                    else if (id.Equals(rgRight, StringComparison.InvariantCultureIgnoreCase))
                        otherRegionId = rgLeft;
                    else
                        continue;

                    if (!regionNames.TryGetValue(otherRegionId, out string? otherRegionName))
                        continue;

                    GateTargetRegions[room.Name] = otherRegionName;
                }
            }
            AddGateTexts();
            LoadConnections();
        }

        private void Load()
        {
            if (WorldString is null || MapString is null)
                throw new InvalidOperationException($"Region {Id} is missing either world or map data and can't be loaded.");

            Dictionary<string, string[]> connections = new();

            bool readingRooms = false;
            bool readingConditionalLinks = false;

            List<(string room, string? target, int disconnectedTarget, string replacement)> connectionOverrides = new();
            List<(string room, int exit, string replacement)> resolvedConnectionOverrides = new();

            Dictionary<string, HashSet<string>> exclusiveRooms = new();
            Dictionary<string, HashSet<string>> hideRooms = new();

            foreach (string line in WorldString.Split('\n', StringSplitOptions.TrimEntries))
            {
                if (line.StartsWith("//"))
                    continue;

                if (line == "ROOMS")
                    readingRooms = true;
                else if (line == "END ROOMS")
                    readingRooms = false;
                else if (line == "CONDITIONAL LINKS")
                    readingConditionalLinks = true;
                else if (line == "END CONDITIONAL LINKS")
                    readingConditionalLinks = false;
                else if (readingRooms)
                {
                    string[] split = line.Split(':', StringSplitOptions.TrimEntries);

                    if (split.Length >= 1)
                    {
                        Room room = new(this, split[0]);

                        if (split.Length >= 2)
                            connections[room.Name] = split[1].Split(',', StringSplitOptions.TrimEntries);

                        if (split.Length >= 3)
                            switch (split[2])
                            {
                                case "GATE": room.IsGate = true; break;
                                case "SHELTER": room.IsShelter = true; break;
                                case "ANCIENTSHELTER": room.IsShelter = room.IsAncientShelter = true; break;
                                case "SCAVOUTPOST": room.IsScavengerOutpost = true; break;
                                case "SCAVTRADER": room.IsScavengerTrader = true; break;
                            }

                        Rooms.Add(room);
                    }
                }
                else if (readingConditionalLinks)
                {
                    string[] split = line.Split(':', StringSplitOptions.TrimEntries);

                    if (split[1] == "EXCLUSIVEROOM")
                    {
                        if (Main.SelectedSlugcat is not null)
                        {
                            string[] slugcats = split[0].Split(',', StringSplitOptions.TrimEntries);

                            if (!exclusiveRooms.TryGetValue(split[2], out HashSet<string>? roomCatNames))
                            {
                                exclusiveRooms[split[2]] = roomCatNames = new();
                            }

                            roomCatNames.UnionWith(slugcats);
                        }
                    }
                    else if (split[1] == "HIDEROOM")
                    {
                        if (Main.SelectedSlugcat is not null)
                        {
                            string[] slugcats = split[0].Split(',', StringSplitOptions.TrimEntries);

                            if (!hideRooms.TryGetValue(split[2], out HashSet<string>? roomCatNames))
                            {
                                hideRooms[split[2]] = roomCatNames = new();
                            }

                            roomCatNames.UnionWith(slugcats);
                        }
                    }
                    else
                    {
                        if (Main.SelectedSlugcat is not null)
                        {
                            string[] slugcats = split[0].Split(',', StringSplitOptions.TrimEntries);
                            if (slugcats.Contains(Main.SelectedSlugcat))
                            {
                                if (int.TryParse(split[2], out int disconnectedTarget))
                                    connectionOverrides.Add((split[1], null, disconnectedTarget, split[3]));
                                else
                                    connectionOverrides.Add((split[1], split[2], 0, split[3]));
                            }
                        }

                    }
                }
            }

            if (Main.SelectedSlugcat is not null)
            {
                foreach (var (room, slugcats) in exclusiveRooms)
                    if (!slugcats.Contains(Main.SelectedSlugcat))
                        Rooms.RemoveAll(r => r.Name.Equals(room, StringComparison.InvariantCultureIgnoreCase));

                foreach (var (room, slugcats) in hideRooms)
                    if (slugcats.Contains(Main.SelectedSlugcat))
                        Rooms.RemoveAll(r => r.Name.Equals(room, StringComparison.InvariantCultureIgnoreCase));
            }

            foreach (var (room, target, disconnectedTarget, replacement) in connectionOverrides)
                if (connections.TryGetValue(room, out string[]? roomConnections))
                {
                    if (target is not null)
                    {
                        for (int i = 0; i < roomConnections.Length; i++)
                            if (roomConnections[i].Equals(target, StringComparison.InvariantCultureIgnoreCase))
                                resolvedConnectionOverrides.Add((room, i, replacement));
                    }
                    else
                    {
                        int index = 0;
                        for (int i = 0; i < roomConnections.Length; i++)
                        {
                            if (roomConnections[i] == "DISCONNECTED")
                            {
                                index++;
                                if (index == disconnectedTarget)
                                {
                                    resolvedConnectionOverrides.Add((room, i, replacement));
                                    break;
                                }
                            }
                        }
                    }
                }

            foreach (var (room, exit, replacement) in resolvedConnectionOverrides)
                if (connections.TryGetValue(room, out string[]? roomConnections))
                    roomConnections[exit] = replacement;

            List<string> subregions = new() { "" };
            HashSet<Room> unmappedRooms = new(Rooms);

            foreach (string line in MapString.Split('\n', StringSplitOptions.TrimEntries))
            {
                if (!line.Contains(':'))
                    continue;

                string[] split = line.Split(':', 2, StringSplitOptions.TrimEntries);
                if (split[0] == "Connection" || split[0].StartsWith("OffScreenDen", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (!TryGetRoom(split[0], out Room? room))
                {
                    Main.LoadErrors.Add($"Tried to position unknown room {split[0]}");
                    continue;
                }

                string[] data = split[1].Split("><", StringSplitOptions.TrimEntries);

                if (data.Length > 3)
                {
                    if (float.TryParse(data[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                        room.WorldPos.X = MathF.Round(x / 2);
                    if (float.TryParse(data[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        room.WorldPos.Y = MathF.Round(-y / 2);
                }

                if (data.TryGet(4, out string layerstr) && int.TryParse(layerstr, out int layer))
                {
                    room.Layer = layer;
                }
                if (data.TryGet(5, out string subregion))
                {
                    int index = subregions.IndexOf(subregion);
                    if (index < 0)
                    {
                        index = subregions.Count;
                        subregions.Add(subregion);
                    }

                    room.Subregion = index;
                }

                unmappedRooms.Remove(room);
            }

            if (unmappedRooms.Count > 0)
            {
                Main.LoadErrors.Add($"{unmappedRooms.Count} rooms aren't positioned! Skipping them.");
                Rooms.RemoveAll(r => unmappedRooms.Contains(r));
            }

            Subregions = subregions.Select(s => new Subregion(s)).ToArray();

            foreach (var (roomName, roomConnections) in connections)
            {
                if (!TryGetRoom(roomName, out Room? room))
                    continue;

                room.Connections = new Room.Connection[roomConnections.Length];
                for (int i = 0; i < roomConnections.Length; i++)
                {
                    if (roomConnections[i] == "DISCONNECTED")
                        continue;

                    if (TryGetRoom(roomConnections[i], out Room? targetRoom))
                    {
                        string[] targetConnections = connections[roomConnections[i]];
                        int targetExit = Array.IndexOf(targetConnections, room.Name);

                        if (targetExit >= 0)
                        {
                            room.Connections[i] = new(targetRoom, i, targetExit);
                        }
                    }
                    else
                    {
                        if (hideRooms.ContainsKey(roomConnections[i]))
                            Main.LoadErrors.Add($"{room.Name} connects to hidden room {roomConnections[i]}!");
                        else if (exclusiveRooms.ContainsKey(roomConnections[i]))
                            Main.LoadErrors.Add($"{room.Name} connects to excluded room {roomConnections[i]}!");
                        else
                            Main.LoadErrors.Add($"{room.Name} connects to a nonexistent room {roomConnections[i]}!");
                    }
                }
            }

            if (PropertiesString is not null)
                foreach (string line in PropertiesString.Split('\n', StringSplitOptions.TrimEntries))
                {
                    string[] split = line.Split(':', StringSplitOptions.TrimEntries);

                    if (split[0] == "Broken Shelters" && split.Length >= 3)
                        foreach (string roomName in split[2].Split(',', StringSplitOptions.TrimEntries))
                            if (TryGetRoom(roomName, out Room? room))
                                room.BrokenForSlugcats.Add(split[1]);
                }
        }

        public void LoadConnections()
        {
            Connections = new(this);
        }

        private void AddGateLocks(string data, HashSet<string>? processed, List<string>? lockLines)
        {
            static void AddGateSymbol(Room room, string symbol, bool leftSide)
            {
                string? spriteName = symbol switch
                {
                    "1" => "smallKarmaNoRing0",
                    "2" => "smallKarmaNoRing1",
                    "3" => "smallKarmaNoRing2",
                    "4" => "smallKarmaNoRing3",
                    "5" => "smallKarmaNoRing4",
                    "R" => "smallKarmaNoRingR",
                    _ => null
                };
                if (spriteName is null)
                    return;

                Vector2 align = new(.4f, .5f);
                if (!leftSide)
                    align.X = 1 - align.X;

                if (!GameAtlases.Sprites.TryGetValue(spriteName, out AtlasSprite? sprite))
                    return;

                room.Children.Add(new SimpleIcon($"GateSymbol{(leftSide ? "Left" : "Right")}", sprite)
                {
                    ParentPosAlign = align
                });
            }

            foreach (string line in data.Split('\n', StringSplitOptions.TrimEntries))
            {
                string[] split = line.Split(':', StringSplitOptions.TrimEntries);
                if (processed is not null && processed.Contains(split[0]) || !TryGetRoom(split[0], out Room? gate))
                    continue;

                AddGateSymbol(gate, split[1], true);
                AddGateSymbol(gate, split[2], false);

                processed?.Add(split[0]);
                lockLines?.Add(line);
            }
        }
        private void AddGateTexts()
        {
            foreach (var (roomName, targetRegion) in GateTargetRegions)
            {
                if (!TryGetRoom(roomName, out Room? room))
                    continue;

                room.Children.Add(new MapText("TargetRegionText", Content.RodondoExt20, $"To {targetRegion}") { Shade = true });
            }
        }

        public bool TryGetRoom(string id, [NotNullWhen(true)] out Room? room)
        {
            foreach (Room r in Rooms)
                if (r.Name.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                {
                    room = r;
                    return true;
                }
            room = null;
            return false;
        }

        public void MarkRoomTilemapsDirty()
        {
            foreach (Room room in Rooms)
                room.TileMapDirty = true;
        }

        public JsonObject SaveJson()
        {
            return new()
            {
                ["id"] = Id,
                ["world"] = WorldString,
                ["properties"] = PropertiesString,
                ["map"] = MapString,
                ["gateTargets"] = JsonSerializer.SerializeToNode(GateTargetRegions),
                ["locks"] = GateLockString,
                ["rooms"] = new JsonArray(Rooms.Select(r => new JsonObject()
                {
                    ["id"] = r.Name,
                    ["data"] = r.DataString,
                    ["settings"] = r.SettingsString
                }).ToArray()),
                ["subregions"] = new JsonArray(Subregions.Select(s => new JsonObject
                {
                    ["name"] = s.Name,
                    ["background"] = s.BackgroundColor.PackedValue,
                    ["water"] = s.WaterColor.PackedValue,
                }).ToArray())
            };
        }

        public void LoadJson(JsonNode node)
        {
            if (node.TryGet("id", out string? id))
                Id = id;

            if (node.TryGet("world", out string? world))
                WorldString = world;

            if (node.TryGet("properties", out string? properties))
                PropertiesString = properties;

            if (node.TryGet("map", out string? map))
                MapString = map;

            if (node.TryGet("gateTargets", out JsonNode? gateTargets))
                GateTargetRegions = JsonSerializer.Deserialize<Dictionary<string, string>>(gateTargets) ?? new();

            if (node.TryGet("locks", out string? locks))
                GateLockString = locks;

            Load();

            if (GateLockString is not null)
                AddGateLocks(GateLockString, null, null);
            AddGateTexts();

            if (node.TryGet("rooms", out JsonArray? rooms))
            {
                foreach (JsonNode? roomNode in rooms)
                    if (roomNode is JsonObject roomObj
                        && roomObj.TryGet("id", out string? roomId)
                        && TryGetRoom(roomId, out Room? room)
                        && roomObj.TryGet("data", out string? roomData))
                    {
                        string? settings = roomObj.Get<string>("settings");
                        room.Load(roomData, settings);
                        room.Loaded = true;
                    }
            }

            if (node.TryGet("subregions", out JsonArray? subregions))
            {
                foreach (JsonNode? subNode in subregions)
                    if (subNode is JsonObject subObj
                        && subObj.TryGet("name", out string? name))
                    {
                        Subregion? subregion = Subregions.FirstOrDefault(s => s.Name == name);
                        if (subregion is null)
                            continue;

                        if (subObj.TryGet("background", out uint background))
                            subregion.BackgroundColor.PackedValue = background;

                        if (subObj.TryGet("water", out uint water))
                            subregion.WaterColor.PackedValue = water;
                    }
            }

            LoadConnections();
            MarkRoomTilemapsDirty();
        }

        static Dictionary<string, string> GetSlugcatSpecificRegionNames(string path)
        {
            Dictionary<string, string> names = new();
            if (!Main.TryFindParentDir(path, "mergedmods", out string? mergedmods))
                return names;

            string basePath = Path.GetDirectoryName(mergedmods)!;

            List<string> worlds = new();
            worlds.Add(Path.Combine(basePath, "world"));

            if (Main.DirExists(basePath, "mods", out string mods))
                foreach (string mod in Directory.EnumerateDirectories(mods)) 
                    worlds.Add(Path.Combine(mod, "world"));

            foreach (string world in worlds)
            {
                if (!Directory.Exists(world))
                    continue;

                foreach (string possibleRegion in Directory.EnumerateDirectories(world))
                {
                    string? displayname = null;
                    bool specific = false;
                    if (Main.SelectedSlugcat is not null && Main.FileExists(possibleRegion, $"displayname-{Main.SelectedSlugcat}.txt", out string specificDisplayname))
                    {
                        displayname = specificDisplayname;
                        specific = true;
                    }

                    if (displayname is null && Main.FileExists(possibleRegion, $"displayname.txt", out string mainDisplayname))
                    {
                        displayname = mainDisplayname;
                        specific = false;
                    }

                    if (displayname is null)
                        continue;

                    string regionId = Path.GetFileName(possibleRegion).ToUpper();

                    if (specific || !names.ContainsKey(regionId))
                        names[regionId] = File.ReadAllText(displayname);
                }
            }

            return names;
        }

        public class Subregion
        {
            public string Name;

            public Color BackgroundColor = Color.White;
            public Color WaterColor = Color.Blue;

            public Subregion(string name)
            {
                Name = name;
            }
        }
    }
}
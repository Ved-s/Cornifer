﻿using Cornifer.Structures;
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

        public Region() { }

        public Region(string id, string worldFilePath, string mapFilePath, string? propertiesFilePath)
        {
            Id = id.ToUpper();
            WorldString = File.ReadAllText(worldFilePath);
            PropertiesString = propertiesFilePath is null ? null : File.ReadAllText(propertiesFilePath);
            MapString = File.ReadAllText(mapFilePath);

            Load();
            LoadGates();

            foreach (Room r in Rooms)
            {
                string roomPath = r.IsGate ? $"world/gates/{r.Name}" : $"world/{id}-rooms/{r.Name}";

                string? settings = RWAssets.ResolveSlugcatFile(roomPath + "_settings.txt");
                string? data = RWAssets.ResolveSlugcatFile(roomPath + ".txt");

                if (data is null)
                {
                    Main.LoadErrors.Add($"Could not find data for room {r.Name}");
                    continue;
                }

                r.Load(File.ReadAllText(data!), settings is null ? null : File.ReadAllText(settings));
            }
            LoadConnections();
            BindRooms();
        }

        private void LoadGates()
        {
            HashSet<string> gatesProcessed = new();
            List<string> lockLines = new();

            string? locks = RWAssets.ResolveFile("world/gates/locks.txt");
            if (locks is not null)
                AddGateLocks(File.ReadAllText(locks), gatesProcessed, lockLines);

            if (lockLines.Count > 0)
                GateLockString = string.Join("\n", lockLines);

            Dictionary<string, string> regionNames = new(RWAssets.FindRegions(Main.SelectedSlugcat).Select(r => new KeyValuePair<string, string>(r.Id, r.Displayname)));
            if (regionNames.Count > 0)
            {
                foreach (Room room in Rooms)
                {
                    if (!room.IsGate || room.GateData is null)
                        continue;

                    Match match = GateNameRegex.Match(room.Name!);
                    if (!match.Success)
                        continue;

                    string? otherRegionId;

                    if (room.GateData.RightRegionId?.Equals(Id, StringComparison.InvariantCultureIgnoreCase) ?? false)
                        otherRegionId = room.GateData.LeftRegionId;

                    else if (room.GateData.LeftRegionId?.Equals(Id, StringComparison.InvariantCultureIgnoreCase) ?? false)
                        otherRegionId = room.GateData.RightRegionId;

                    else
                        otherRegionId = room.GateData.Swapped ? room.GateData.RightRegionId : room.GateData.LeftRegionId;

                    if (otherRegionId is null || !regionNames.TryGetValue(otherRegionId, out string? otherRegionName))
                        continue;

                    room.GateData.TargetRegionName = otherRegionName;
                }
            }
        }

        private void AddGateLocks(string data, HashSet<string>? processed, List<string>? lockLines)
        {
            foreach (string line in data.Split('\n', StringSplitOptions.TrimEntries))
            {
                string[] split = line.Split(':', StringSplitOptions.TrimEntries);
                if (processed is not null && processed.Contains(split[0]) || !TryGetRoom(split[0], out Room? gate))
                    continue;

                Match match = GateNameRegex.Match(split[0]);
                gate.GateData ??= new();

                if (split.Length >= 4 && split[3] == "SWAPMAPSYMBOL")
                    gate.GateData.Swapped = true;

                if (match.Success)
                {
                    string leftRegion = match.Groups[1].Value;
                    string rightRegion = match.Groups[2].Value;

                    if (gate.GateData.Swapped)
                        (leftRegion, rightRegion) = (rightRegion, leftRegion);

                    gate.GateData.LeftRegionId = leftRegion;
                    gate.GateData.RightRegionId = rightRegion;
                }

                if ((gate.GateData.LeftRegionId is null || !gate.GateData.LeftRegionId.Equals(Id, StringComparison.InvariantCultureIgnoreCase))
                 && (gate.GateData.RightRegionId is null || !gate.GateData.RightRegionId.Equals(Id, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (gate.GateData.Swapped)
                        gate.GateData.LeftRegionId = Id.ToUpper();
                    else
                        gate.GateData.RightRegionId = Id.ToUpper();
                }
                gate.GateData.LeftKarma = split[1];
                gate.GateData.RightKarma = split[2];

                processed?.Add(split[0]);
                lockLines?.Add(line);
            }
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
                            connections[room.Name!] = split[1].Split(',', StringSplitOptions.TrimEntries);

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
                    Vector2 worldPos = new();

                    if (float.TryParse(data[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                        worldPos.X = MathF.Round(x / 2);
                    if (float.TryParse(data[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        worldPos.Y = MathF.Round(-y / 2);

                    room.WorldPosition = worldPos;
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

                    room.Subregion.OriginalValue = index;
                }

                room.Positioned = true;
            }

            Subregions = subregions.Select(s => new Subregion(Id, s)).ToArray();
            ResetSubregionColors();

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

            bool any = true;
            while (any)
            {
                any = false;
                foreach (Room room in Rooms)
                {
                    if (!room.Positioned && room.Connections.Any(c => c is not null && c.Target.Positioned))
                    {
                        room.Positioned = true;
                        any = true;
                    }
                }
            }

            int nonPositioned = Rooms.RemoveAll(r => !r.Positioned);
            if (nonPositioned > 0)
                Main.LoadErrors.Add($"{nonPositioned} rooms aren't positioned! Removed them.");

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
        public void ResetSubregionColors()
        {
            foreach (Subregion subregion in Subregions)
            {
                string? subregionName = subregion.Name.Length == 0 ? null : subregion.Name;
                ColorDatabase.GetRegionColor(Id, subregionName, false).ResetToDefault();
                ColorDatabase.GetRegionColor(Id, subregionName, true).ResetToDefault();
            }

            MarkRoomTilemapsDirty();
        }

        public void LoadConnections()
        {
            Connections = new(this);
        }
        public void BindRooms()
        {
            foreach (Room room in Rooms)
                room.BindToRooms();
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
                ["gateTargets"] = new JsonObject(Rooms
                    .Where(r => r.IsGate && r.GateData?.TargetRegionName is not null)
                    .Select(r => new KeyValuePair<string, JsonNode?>(r.Name!, r.GateData!.TargetRegionName!))),
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
                    ["background"] = s.BackgroundColor.SaveJson(),
                    ["water"] = s.WaterColor.SaveJson(),
                }).ToArray())
            };
        }

        public void LoadJson(JsonNode node)
        {
            if (node.TryGet("id", out string? id))
                Id = id.ToUpper();

            if (node.TryGet("world", out string? world))
                WorldString = world;

            if (node.TryGet("properties", out string? properties))
                PropertiesString = properties;

            if (node.TryGet("map", out string? map))
                MapString = map;

            if (node.TryGet("locks", out string? locks))
                GateLockString = locks;

            Load();

            if (node.TryGet("gateTargets", out JsonObject? gateTargets))
                foreach (var (roomName, targetObj) in gateTargets)
                    if (TryGetRoom(roomName, out Room? room) && targetObj is JsonValue targetValue)
                    {
                        string? target = targetValue.Deserialize<string>();
                        if (target is null)
                            continue;

                        room.GateData ??= new();
                        room.GateData.TargetRegionName = target;
                    }

            if (GateLockString is not null)
                AddGateLocks(GateLockString, null, null);

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

                        if (subObj.TryGet("background", out JsonValue? background))
                            subregion.BackgroundColor = ColorDatabase.LoadColorRefJson(subregion.BackgroundColor, background, Color.White);

                        if (subObj.TryGet("water", out JsonValue? water))
                            subregion.WaterColor = ColorDatabase.LoadColorRefJson(subregion.WaterColor, water, Color.Blue);
                    }
            }

            LoadConnections();
            MarkRoomTilemapsDirty();
        }
    }
    
}
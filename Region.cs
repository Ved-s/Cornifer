using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Cornifer
{
    public class Region : ISelectableContainer
    {
        public string Id;
        public List<Room> Rooms = new();

        public string[] Subregions;

        HashSet<string> DrawnRoomConnections = new();

        public Region(string id, string worldFile, string mapFile, string roomsDir)
        {
            Id = id;

            Dictionary<string, string[]> connections = new();

            bool readingRooms = false;
            bool readingConditionalLinks = false;

            List<string> skipRooms = new();
            List<(string room, string? target, int disconnectedTarget, string replacement)> connectionOverrides = new();
            List<(string room, int exit, string replacement)> resolvedConnectionOverrides = new();

            foreach (string line in File.ReadAllLines(worldFile))
            {
                if (line == "ROOMS")
                    readingRooms = true;
                else if (line == "END ROOMS")
                    readingRooms = false;
                if (line == "CONDITIONAL LINKS")
                    readingConditionalLinks = true;
                else if (line == "END CONDITIONAL LINKS")
                    readingConditionalLinks = false;
                else if (readingRooms)
                {
                    string[] split = line.Split(':', StringSplitOptions.TrimEntries);

                    if (split.Length >= 1)
                    {
                        Room room = new(split[0]);

                        if (split.Length >= 2)
                            connections[room.Id] = split[1].Split(',', StringSplitOptions.TrimEntries);

                        if (split.Length >= 3)
                            switch (split[2])
                            {
                                case "GATE": room.IsGate = true; break;
                                case "SHELTER": room.IsShelter = true; break;
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
                            if (!slugcats.Contains(Main.SelectedSlugcat))
                                skipRooms.Add(split[2]);
                        }
                    }
                    else if (split[1] == "HIDEROOM")
                    {
                        if (Main.SelectedSlugcat is not null)
                        {
                            string[] slugcats = split[0].Split(',', StringSplitOptions.TrimEntries);
                            if (slugcats.Contains(Main.SelectedSlugcat))
                                skipRooms.Add(split[2]);
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

            foreach (string skipRoom in skipRooms)
                Rooms.RemoveAll(r => r.Id.Equals(skipRoom, StringComparison.InvariantCultureIgnoreCase));

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

            HashSet<string> subregions = new();

            foreach (string line in File.ReadLines(mapFile))
            {
                if (!line.Contains(':'))
                    continue;

                string[] split = line.Split(':', 2, StringSplitOptions.TrimEntries);
                if (!TryGetRoom(split[0], out Room? room))
                    continue;

                string[] data = split[1].Split("><", StringSplitOptions.TrimEntries);

                if (data.Length > 3)
                {
                    if (float.TryParse(data[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                        room.WorldPos.X = MathF.Floor(x / 2);
                    if (float.TryParse(data[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        room.WorldPos.Y = MathF.Floor(-y / 2);
                }

                if (data.TryGet(4, out string layerstr) && int.TryParse(layerstr, out int layer))
                {
                    room.Layer = layer;
                }
                if (data.TryGet(5, out string subregion) && subregion.Length > 0)
                {
                    room.Subregion = subregion;
                    subregions.Add(subregion);
                }
            }

            Subregions = subregions.ToArray();

            List<string> roomDirs = new();

            string worldRooms = Path.Combine(Path.GetDirectoryName(worldFile)!, $"../{Id}-rooms");
            if (Directory.Exists(worldRooms))
                roomDirs.Add(worldRooms);

            if (Main.TryFindParentDir(worldFile, "mods", out string? mods))
            {
                foreach (string mod in Directory.EnumerateDirectories(mods))
                {
                    string modRooms = Path.Combine(mod, $"world/{Id}-rooms");
                    if (Directory.Exists(modRooms))
                        roomDirs.Add(modRooms);
                }
            }

            foreach (var (roomName, roomConnections) in connections)
            {
                if (!TryGetRoom(roomName, out Room? room))
                    continue;

                room.Connections = new Room.Connection[roomConnections.Length];
                for (int i = 0; i < roomConnections.Length; i++)
                {
                    if (TryGetRoom(roomConnections[i], out Room? targetRoom))
                    {
                        string[] targetConnections = connections[roomConnections[i]];
                        int targetExit = Array.IndexOf(targetConnections, room.Id);

                        if (targetExit >= 0)
                        {
                            room.Connections[i] = new(targetRoom, i, targetExit);
                        }
                    }
                }
            }

            roomDirs.Add(roomsDir);

            foreach (Room r in Rooms)
            {
                string? settings = null;
                string? data = null;

                string roomPath = r.IsGate ? $"../gates/{r.Id}" : r.Id;

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
                    continue;

                r.Load(data!, settings);
            }
        }

        public bool TryGetRoom(string id, [NotNullWhen(true)] out Room? room)
        {
            foreach (Room r in Rooms)
                if (r.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase))
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

        public void Draw(Renderer renderer)
        {
            Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            foreach (Room room in Rooms)
                room.Draw(renderer);

            DrawnRoomConnections.Clear();
            foreach (Room room in Rooms)
            {
                foreach (Room.Connection? connection in room.Connections)
                {
                    if (connection is null || DrawnRoomConnections.Contains(connection.Target.Id))
                        continue;

                    Vector2 start = renderer.TransformVector(room.WorldPos + room.Exits[connection.Exit].ToVector2() + new Vector2(.5f));
                    Vector2 end = renderer.TransformVector(connection.Target.WorldPos + connection.Target.Exits[connection.TargetExit].ToVector2() + new Vector2(.5f));

                    Main.SpriteBatch.DrawLine(start, end, Color.Black, 3);
                    Main.SpriteBatch.DrawRect(start - new Vector2(3), new(5), Color.Black);
                    Main.SpriteBatch.DrawRect(end - new Vector2(3), new(5), Color.Black);

                    Main.SpriteBatch.DrawLine(start, end, Color.White, 1);
                    Main.SpriteBatch.DrawRect(start - new Vector2(2), new(3), Color.White);
                    Main.SpriteBatch.DrawRect(end - new Vector2(2), new(3), Color.White);
                }
                DrawnRoomConnections.Add(room.Id);
            }

            Main.SpriteBatch.End();
        }

        public IEnumerable<ISelectable> EnumerateSelectables()
        {
            foreach (Room room in ((IEnumerable<Room>)Rooms).Reverse())
                foreach (ISelectable selectable in room.EnumerateSelectables())
                    yield return selectable;
        }
    }
}
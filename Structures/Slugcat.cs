using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Cornifer.Structures
{
    public class Slugcat
    {
        public string Name = "";
        public string Id = "";

        public int IconId = -1;
        public bool Playable = false;

        public Color Color = Color.White;

        // Futureproof for Slugbase
        public string[]? PossibleStartingRooms;

        public Slugcat() { }

        public Slugcat(string id, string name, int iconId, bool playable, Color color, string? startingRoom)
        {
            Id = id;
            Name = name;
            IconId = iconId;
            Playable = playable;
            Color = color;
            PossibleStartingRooms = startingRoom is null ? null : new[] { startingRoom };
        }

        public Room? GetStartingRoom(Region region)
        {
            if (PossibleStartingRooms?.Length is null or 0)
                return null;

            foreach (string? roomName in PossibleStartingRooms)
            {
                if (region.TryGetRoom(roomName, out Room? room))
                    return room;
            }

            return null;
        }
    }
}

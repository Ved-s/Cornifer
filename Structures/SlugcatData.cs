using Microsoft.Xna.Framework;

namespace Cornifer.Structures
{
    public struct SlugcatData
    {
        public string Name;
        public string Id;

        public int IconId;
        public bool Playable;

        public Color Color;

        public SlugcatData(string id, string name, int iconId, bool playable, Color color)
        {
            Id = id;
            Name = name;
            IconId = iconId;
            Playable = playable;
            Color = color;
        }
    }
}

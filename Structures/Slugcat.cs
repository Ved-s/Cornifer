using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Buffers;

namespace Cornifer.Structures
{
    public class Slugcat
    {
        public string Name = "";
        public string Id = "";

        public bool Playable = false;

        public Color Color = Color.White;
        public Color EyeColor = Color.Black;

        public string[]? PossibleStartingRooms;
        public string[]? PossibleWorldStates;

        public string WorldStateSlugcat => PossibleWorldStates?.FirstOrDefault(s1 => StaticData.Slugcats.Any(s2 => s2.Id.Equals(s1, StringComparison.InvariantCultureIgnoreCase))) ?? Id;

        public Slugcat() { }

        public Slugcat(string id, string name, bool playable, Color color, Color eyeColor, string? startingRoom)
        {
            Id = id;
            Name = name;
            Playable = playable;
            Color = color;
            EyeColor = color;
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

        public void GenerateIcons()
        {
            int arraySize = Content.SlugcatIconTemplate.Width * Content.SlugcatIconTemplate.Height;
            Color[] colors = ArrayPool<Color>.Shared.Rent(arraySize);
            Content.SlugcatIconTemplate.GetData(colors, 0, arraySize);

            for (int i = 0; i < arraySize; i++)
            {
                float bodyColor = colors[i].R / 255f;
                float eyeColor = colors[i].G / 255f;
                byte alpha = colors[i].A;

                Color body = Color * bodyColor;
                Color eyes = EyeColor * eyeColor;

                colors[i] = new(body.R + eyes.R, body.G + eyes.G, body.B + eyes.B, alpha);
            }

            Texture2D texture = new(Main.Instance.GraphicsDevice, Content.SlugcatIconTemplate.Width, Content.SlugcatIconTemplate.Height);
            texture.SetData(colors, 0, arraySize);
            ArrayPool<Color>.Shared.Return(colors);

            AtlasSprite bigIcon   = new($"Slugcat_{Id}", texture, new(0, 9, 20, 19), Color.White, false);
            AtlasSprite smallIcon = new($"SlugcatIcon_{Id}", texture, new(9, 0, 8, 8), Color.White, false);
            AtlasSprite diamond   = new($"SlugcatDiamond_{Id}", texture, new(0, 0, 9, 9), Color.White, false);

            SpriteAtlases.Sprites[bigIcon.Name] = bigIcon;
            SpriteAtlases.Sprites[smallIcon.Name] = smallIcon;
            SpriteAtlases.Sprites[diamond.Name] = diamond;
        }
    }
}

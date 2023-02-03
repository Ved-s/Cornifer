using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer
{
    public class PlacedObject : ISelectable, ISelectableContainer, IDrawable
    {
        public string Name;

        public ISelectable Parent;

        public Vector2 ParentPosition;
        public Vector2 Offset;
        public Rectangle Frame;

        public List<ISelectable> SubObjects = new();

        public string[] SlugcatAvailability = Array.Empty<string>();

        public Vector2 Position 
        {
            get => Parent.Position + ParentPosition + Offset - Size / 2;
            set 
            {
                if (!Parent.Selected)
                {
                    Offset = value - Parent.Position - ParentPosition + Size / 2;
                }
            }
        }
        public Vector2 Size => Frame.Size.ToVector2();

        public void Draw(Renderer renderer)
        {
            Vector2 roomPoint = renderer.TransformVector(Parent.Position + ParentPosition + new Vector2(.5f));
            Vector2 worldPoint = renderer.TransformVector(Parent.Position + ParentPosition + Offset);

            Main.SpriteBatch.DrawLine(roomPoint, worldPoint, Color.Black, 3);
            Main.SpriteBatch.DrawRect(roomPoint - new Vector2(3), new(5), Color.Black);

            Main.SpriteBatch.DrawLine(roomPoint, worldPoint, new(90, 90, 90), 1);
            Main.SpriteBatch.DrawRect(roomPoint - new Vector2(2), new(3), new(90, 90, 90));

            renderer.DrawTexture(Content.Objects, Position, Frame);

            foreach (ISelectable selectable in SubObjects)
                if (selectable is IDrawable drawable)
                    drawable.Draw(renderer);
        }

        public IEnumerable<ISelectable> EnumerateSelectables()
        {
            foreach (ISelectable selectable in ((IEnumerable<ISelectable>)SubObjects).Reverse())
                if (selectable is ISelectableContainer container)
                    foreach (ISelectable subselectable in container.EnumerateSelectables())
                        yield return subselectable;
                else yield return selectable;
            yield return this;
        }

        public static PlacedObject? Load(ISelectable selectable, string data)
        {
            string[] split = data.Split("><", 4);

            Rectangle? frame = GetSourceFrame(split[0]);
            if (frame is null)
                return null;

            PlacedObject? obj = Load(selectable, split[0], new Vector2(float.Parse(split[1], CultureInfo.InvariantCulture) / 20, selectable.Size.Y - (float.Parse(split[2], CultureInfo.InvariantCulture) / 20)));
            if (obj is null)
                return null;

            if (split[0].Contains("Token"))
            {
                string[] subsplit = split[3].Split('~');

                string subname = subsplit[5];

                obj.SlugcatAvailability = GetTokenSlugcats(subsplit[6]);

                switch (split[0])
                {
                    case "BlueToken":
                        PlacedObject? subObject = Load(obj, subname, obj.Size / 2);
                        if (subObject is not null)
                            obj.SubObjects.Add(subObject);
                        break;
                }
            }

            if (Main.SelectedSlugcat is not null && obj.SlugcatAvailability.Length > 0 && !obj.SlugcatAvailability.Contains(Main.SelectedSlugcat))
                return null;

            foreach (string slugcat in obj.SlugcatAvailability)
            {
                obj.SubObjects.Add(new SlugcatIcon
                {
                    Id = Array.IndexOf(Main.SlugCatNames, slugcat),
                    Parent = obj
                });
            }

            return obj;
        }

        public static PlacedObject? Load(ISelectable selectable, string name, Vector2 pos)
        {
            Rectangle? frame = GetSourceFrame(name);
            if (frame is null)
                return null;

            return new()
            {
                Name = name,
                Parent = selectable,
                ParentPosition = pos,
                Frame = frame.Value
            };
        }

        public static Rectangle? GetSourceFrame(string name)
        {
            return name switch
            {
                "BubbleGrass"       => new(0, 0, 20, 26),
                "DangleFruit"       => new(21, 0, 15, 22),
                "FirecrackerPlant"  => new(38, 0, 20, 29),
                "FlareBomb"         => new(62, 0, 14, 17),
                "FlyLure"           => new(0, 30, 22, 28),
                "Hazer"             => new(23, 23, 15, 22),
                "JellyFish"         => new(39, 30, 22, 24),
                "KarmaFlower"       => new(0, 60, 23, 23),
                "Mushroom"          => new(24, 46, 12, 19),
                "NeedleEgg"         => new(62, 18, 16, 21),
                "PuffBall"          => new(37, 55, 21, 26),
                "SlimeMold"         => new(62, 40, 24, 23),
                "SporePlant"        => new(79, 0, 23, 23),
                "VultureGrub"       => new(59, 64, 20, 17),
                "WaterNut"          => new(79, 24, 13, 13),
                "SeedCob"           => new(105, 0, 35, 38),
                "GhostSpot"         => new(87, 39, 38, 48),
                "BlueToken"         => new(126, 39, 10, 20),
                "GoldToken"         => new(126, 60, 10, 20),
                "RedToken"          => new(24, 66, 10, 20),
                "DataPearl"         => new(93, 24, 11, 10),
                "UniqueDataPearl"   => new(93, 24, 11, 10),
                
                _ => null,
            };
        }

        static string[] GetTokenSlugcats(string availability)
        {
            if (availability.All(char.IsDigit))
            {
                List<string> slugcats = new();
                for (int i = 0; i < availability.Length; i++)
                {
                    if (availability[i] != '1')
                    {
                        continue;
                    }
                    switch (i)
                    {
                        case 0:
                            slugcats.Add("White");
                            break;
                        case 1:
                            slugcats.Add("Yellow");
                            break;
                        case 2:
                            slugcats.Add("Red");
                            break;
                        case 3:
                            slugcats.Add("Night");
                            break;
                    }
                }
                return slugcats.ToArray();
            }

            return availability.Split('|');
        }
    }
}

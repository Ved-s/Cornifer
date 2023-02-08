using Cornifer.Interfaces;
using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cornifer
{
    public class PlacedObject : SimpleIcon, ISelectableContainer
    {
        public string Name;

        public Vector2 ParentPosition;

        public List<ISelectable> SubObjects = new();

        public string[] SlugcatAvailability = Array.Empty<string>();

        public override Vector2 Size => Frame.Size.ToVector2();

        public override Vector2 ParentPosAlign => Parent is null ? Vector2.Zero : ParentPosition / Parent.Size;
        public override bool Active => true;

        public override void DrawIcon(Renderer renderer)
        {
            base.DrawIcon(renderer);

            foreach (ISelectable selectable in SubObjects)
                if (selectable is Interfaces.IDrawable drawable)
                    drawable.Draw(renderer);
        }

        public IEnumerable<ISelectable> EnumerateSelectables()
        {
            foreach (ISelectable selectable in ((IEnumerable<ISelectable>)SubObjects).Reverse())
                if (selectable.Active)
                {
                    if (selectable is ISelectableContainer container)
                        foreach (ISelectable subselectable in container.EnumerateSelectables())
                            yield return subselectable;
                    else yield return selectable;
                }
            yield return this;
        }

        public static PlacedObject? Load(ISelectable selectable, string data)
        {
            string[] split = data.Split("><", 4);

            if (split.Length < 3)
                return null;

            string objName = split[0];

            if (objName.EndsWith("Token"))
            {
                string[] subsplit = split[3].Split('~');

                if (subsplit[4] == "1")
                    objName = "BlueToken";
                else if (subsplit.Length > 7 && subsplit[7] == "1")
                    objName = "GreenToken";
                else if (subsplit.Length > 8 && subsplit[8] == "1")
                    objName = "WhiteToken";
                else if (subsplit.Length > 9 && subsplit[9] == "1")
                    objName = "RedToken";
                else if (subsplit.Length > 10 && subsplit[10] == "1")
                    objName = "DevToken";
                else
                    objName = "GoldToken";
            }

            PlacedObject? obj = Load(selectable, split[0], new Vector2(float.Parse(split[1], CultureInfo.InvariantCulture) / 20, selectable.Size.Y - (float.Parse(split[2], CultureInfo.InvariantCulture) / 20)));
            if (obj is null)
                return null;

            if (objName.EndsWith("Token"))
            {
                string[] subsplit = split[3].Split('~');

                string subname = subsplit[5];
                
                obj.SlugcatAvailability = GetTokenSlugcats(subsplit[6]);
                obj.Color.A = 150;
                obj.Shade = false;

                switch (objName)
                {
                    case "GreenToken":
                        int slugcatId = Array.IndexOf(Main.SlugCatNames, subname);
                        if (slugcatId >= 0)
                        {
                            obj.SubObjects.Add(new SlugcatIcon()
                            {
                                Id = slugcatId,
                                Offset = new(0, 8),
                                Parent = obj,
                                ForceSlugcatIcon = true,
                                LineColor = Color.Lime
                            });
                        }
                        break;

                    case "BlueToken":
                        PlacedObject? subObject = Load(obj, subname, obj.Size / 2);
                        if (subObject is not null)
                        {
                            subObject.Offset = new(0, 15);
                            obj.SubObjects.Add(subObject);
                        }
                        break;
                }
            }

            if (split[0].Contains("DataPearl"))
            {
                string[] subsplit = data.Split('~');

                if (subsplit.TryGet(4, out string type))
                    obj.Color = GetPearlColor(type);
                obj.Color.A = 165;
            }

            if (Main.SelectedSlugcat is not null && obj.SlugcatAvailability.Length > 0 && !obj.SlugcatAvailability.Contains(Main.SelectedSlugcat))
                return null;

            float iconAngle = 0.5f;
            float totalAngle = Math.Max(0, obj.SlugcatAvailability.Length - 1) * iconAngle;
            float currentAngle = (MathF.PI / 2) + totalAngle / 2;
            for (int i = 0; i < obj.SlugcatAvailability.Length; i++)
            {
                Vector2 offset = new Vector2(MathF.Cos(currentAngle), -MathF.Sin(currentAngle)) * 15;
                currentAngle -= iconAngle;
                string slugcat = obj.SlugcatAvailability[i];
                obj.SubObjects.Add(new SlugcatIcon
                {
                    Id = Array.IndexOf(Main.SlugCatNames, slugcat),
                    Parent = obj,
                    Offset = offset
                });
            }

            return obj;
        }

        public static PlacedObject? Load(ISelectable selectable, string name, Vector2 pos)
        {
            if (!GameAtlases.Sprites.TryGetValue("Object_" + name, out var atlas))
            {
                return null;
            }

            return new()
            {
                Name = name,
                Parent = selectable,
                ParentPosition = pos,
                Frame = atlas.Frame,
                Texture = atlas.Texture,
                Color = atlas.Color,
                Shade = atlas.Shade
            };
        }

        static string[] GetTokenSlugcats(string availability)
        {
            List<string> slugcats = new();
            if (availability.All(char.IsDigit))
            {
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

            slugcats.AddRange(Main.SlugCatNames);
            slugcats.Remove("Night");
            slugcats.Remove("Inv");
            foreach (string name in availability.Split('|'))
                slugcats.Remove(name);

            return slugcats.ToArray();
        }

        static Color GetPearlColor(string type)
        {
            return type switch
            {
                "SI_west" => new Color(0.01f, 0.01f, 0.01f),
                "SI_top" => new Color(0.01f, 0.01f, 0.01f),
                "SI_chat3" => new Color(0.01f, 0.01f, 0.01f),
                "SI_chat4" => new Color(0.01f, 0.01f, 0.01f),
                "SI_chat5" => new Color(0.01f, 0.01f, 0.01f),
                "Spearmasterpearl" => new Color(0.04f, 0.01f, 0.04f),
                "SU_filt" => new Color(1f, 0.75f, 0.9f),
                "DM" => new Color(0.95686275f, 0.92156863f, 0.20784314f),
                "LC" => new Color(0f, 0.4f, 0.01569f),
                "LC_second" => new Color(0.6f, 0f, 0f),
                "OE" => new Color(0.54901963f, 0.36862746f, 0.8f),
                "MS" => new Color(0.8156863f, 0.89411765f, 0.27058825f),
                "RM" => new Color(0.38431373f, 0.18431373f, 0.9843137f),
                "Rivulet_stomach" => new Color(0.5882353f, 0.87058824f, 0.627451f),
                "CL" => new Color(0.48431373f, 0.28431374f, 1f),
                "VS" => new Color(0.53f, 0.05f, 0.92f),
                "BroadcastMisc" => new Color(0.9f, 0.7f, 0.8f),

                "CC" => new Color(0.9f, 0.6f, 0.1f),
                "LF_west" => new Color(1f, 0f, 0.3f),
                "LF_bottom" => new Color(1f, 0.1f, 0.1f),
                "HI" => new Color(0.007843138f, 0.19607843f, 1f),
                "SH" => new Color(0.2f, 0f, 0.1f),
                "DS" => new Color(0f, 0.7f, 0.1f),
                "SB_filtration" => new Color(0.1f, 0.5f, 0.5f),
                "SB_ravine" => new Color(0.01f, 0.01f, 0.01f),
                "GW" => new Color(0f, 0.7f, 0.5f),
                "SL_bridge" => new Color(0.4f, 0.1f, 0.9f),
                "SL_moon" => new Color(0.9f, 0.95f, 0.2f),
                "SU" => new Color(0.5f, 0.6f, 0.9f),
                "UW" => new Color(0.4f, 0.6f, 0.4f),
                "SL_chimney" => new Color(1f, 0f, 0.55f),
                "Red_stomach" => new Color(0.6f, 1f, 0.9f),
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
        }
    }
}

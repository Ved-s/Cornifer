using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Texture2D Texture = null!;
        public Rectangle Frame;
        public bool Shade = false;
        public Color Color = Color.White;

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

        bool ISelectable.Active => true;

        public void Draw(Renderer renderer)
        {
            Vector2 roomPoint = renderer.TransformVector(Parent.Position + ParentPosition + new Vector2(.5f));
            Vector2 worldPoint = renderer.TransformVector(Parent.Position + ParentPosition + Offset);

            Main.SpriteBatch.DrawLine(roomPoint, worldPoint, Color.Black, 3);
            Main.SpriteBatch.DrawRect(roomPoint - new Vector2(3), new(5), Color.Black);

            Main.SpriteBatch.DrawLine(roomPoint, worldPoint, new(90, 90, 90), 1);
            Main.SpriteBatch.DrawRect(roomPoint - new Vector2(2), new(3), new(90, 90, 90));

            if (Shade)
            {
                renderer.DrawTexture(Texture, Position + new Vector2(-1, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(1, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(-1, 1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(1, 1), Frame, color: Color.Black);

                renderer.DrawTexture(Texture, Position + new Vector2(0, -1), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(1, 0), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(-1, 0), Frame, color: Color.Black);
                renderer.DrawTexture(Texture, Position + new Vector2(0, 1), Frame, color: Color.Black);
            }

            renderer.DrawTexture(Texture, Position, Frame, color: Color);

            foreach (ISelectable selectable in SubObjects)
                if (selectable is IDrawable drawable)
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

            Rectangle? frame = GetObjectFrame(split[0]);
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

            if (split[0].Contains("DataPearl"))
            {
                string[] subsplit = data.Split('~');

                if (subsplit.TryGet(4, out string type))
                    obj.Color = GetPearlColor(type);
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
            string? atlasName = GetAtlasName(name);

            Rectangle frame;
            Texture2D texture;
            Color color = Color.White;
            bool shade = false;

            if (atlasName is not null && GameAtlases.Sprites.TryGetValue(atlasName, out var atlas))
            {
                texture = atlas.Item1;
                frame = atlas.Item2;
                color = GetAtlasColor(name);
                shade = true;
            }
            else 
            {
                Rectangle? objectFrame = GetObjectFrame(name);

                if (objectFrame.HasValue)
                {
                    frame = objectFrame.Value;
                    texture = Content.Objects;
                }
                else 
                {
                    return null;
                }
            }

            return new()
            {
                Name = name,
                Parent = selectable,
                ParentPosition = pos,
                Frame = frame,
                Texture = texture,
                Color = color,
                Shade = shade
            };
        }

        public static string? GetAtlasName(string name)
        {
            return name switch
            {
                "Slugcat" => "Kill_Slugcat",
                "GreenLizard" => "Kill_Green_Lizard",
                "PinkLizard" => "Kill_Standard_Lizard",
                "BlueLizard" => "Kill_Standard_Lizard",
                "WhiteLizard" => "Kill_White_Lizard",
                "BlackLizard" => "Kill_Black_Lizard",
                "YellowLizard" => "Kill_Yellow_Lizard",
                "SpitLizard" => "Kill_Spit_Lizard",
                "ZoopLizard" => "Kill_White_Lizard",
                "CyanLizard" => "Kill_Standard_Lizard",
                "RedLizard" => "Kill_Standard_Lizard",
                "Salamander" => "Kill_Salamander",
                "EelLizard" => "Kill_Salamander",
                "Fly" => "Kill_Bat",
                "CicadaA" => "Kill_Cicada",
                "CicadaB" => "Kill_Cicada",
                "Snail" => "Kill_Snail",
                "Leech" => "Kill_Leech",
                "SeaLeech" => "Kill_Leech",
                "JungleLeech" => "Kill_Leech",
                "PoleMimic" => "Kill_PoleMimic",
                "TentaclePlant" => "Kill_TentaclePlant",
                "Scavenger" => "Kill_Scavenger",
                "ScavengerElite" => "Kill_ScavengerElite",
                "VultureGrub" => "Kill_VultureGrub",
                "Vulture" => "Kill_Vulture",
                "KingVulture" => "Kill_KingVulture",
                "SmallCentipede" => "Kill_Centipede1",
                "MediumCentipede" => "Kill_Centipede2",
                "BigCentipede" => "Kill_Centipede3",
                "RedCentipede" => "Kill_Centipede3",
                "Centiwing" => "Kill_Centiwing",
                "AquaCenti" => "Kill_Centiwing",
                "TubeWorm" => "Kill_Tubeworm",
                "Hazer" => "Kill_Hazer",
                "LanternMouse" => "Kill_Mouse",
                "Spider" => "Kill_SmallSpider",
                "BigSpider" => "Kill_BigSpider",
                "SpitterSpider" => "Kill_BigSpider",
                "MotherSpider" => "Kill_BigSpider",
                "MirosBird" => "Kill_MirosBird",
                "MirosVulture" => "Kill_MirosBird",
                "BrotherLongLegs" => "Kill_Daddy",
                "DaddyLongLegs" => "Kill_Daddy",
                "TerrorLongLegs" => "Kill_Daddy",
                "Inspector" => "Kill_Inspector",
                "Deer" => "Kill_RainDeer",
                "EggBug" => "Kill_EggBug",
                "FireBug" => "Kill_FireBug",
                "DropBug" => "Kill_DropBug",
                "SlugNPC" => "Kill_Slugcat",
                "BigNeedleWorm" => "Kill_NeedleWorm",
                "SmallNeedleWorm" => "Kill_SmallNeedleWorm",
                "JetFish" => "Kill_Jetfish",
                "Yeek" => "Kill_Yeek",
                "BigEel" => "Kill_BigEel",
                "BigJelly" => "Kill_BigJellyFish",
                "Rock" => "Symbol_Rock",
                "Spear" => "Symbol_Spear",
                "FireSpear" => "Symbol_FireSpear",
                "ElectricSpear" => "Symbol_ElectricSpear",
                "HellSpear" => "Symbol_HellSpear",
                "LillyPuck" => "Symbol_LillyPuck",
                "Pearl" => "Symbol_Pearl",
                "ScavengerBomb" => "Symbol_StunBomb",
                "SingularityBomb" => "Symbol_Singularity",
                "FireEgg" => "Symbol_FireEgg",
                "SporePlant" => "Symbol_SporePlant",
                "Lantern" => "Symbol_Lantern",
                "VultureMask" => "Kill_Vulture",
                "FlyLure" => "Symbol_FlyLure",
                "Mushroom" => "Symbol_Mushroom",
                "FlareBomb" => "Symbol_FlashBomb",
                "PuffBall" => "Symbol_PuffBall",
                "GooieDuck" => "Symbol_GooieDuck",
                "WaterNut" => "Symbol_WaterNut",
                "DandelionPeach" => "Symbol_DandelionPeach",
                "FirecrackerPlant" => "Symbol_Firecracker",
                "DangleFruit" => "Symbol_DangleFruit",
                "JellyFish" => "Symbol_JellyFish",
                "BubbleGrass" => "Symbol_BubbleGrass",
                "GlowWeed" => "Symbol_GlowWeed",
                "SlimeMold" => "Symbol_SlimeMold",
                "EnergyCell" => "Symbol_EnergyCell",
                "JokeRifle" => "Symbol_JokeRifle",

                "NeedleEgg" => "needleEggSymbol",
                _ => null,
            };
        }
        public static Color GetAtlasColor(string name)
        {
            return name switch
            {
                "Slugcat" => new(255, 255, 255),
                "GreenLizard" => new(51, 255, 0),
                "PinkLizard" => new(255, 0, 255),
                "BlueLizard" => new(0, 128, 255),
                "WhiteLizard" => new(255, 255, 255),
                "BlackLizard" => new(94, 94, 111),
                "YellowLizard" => new(255, 153, 0),
                "SpitLizard" => new(140, 102, 51),
                "ZoopLizard" => new(242, 186, 186),
                "CyanLizard" => new(0, 232, 230),
                "RedLizard" => new(230, 14, 14),
                "Salamander" => new(238, 199, 228),
                "EelLizard" => new(5, 199, 51),
                "Fly" => new(169, 164, 178),
                "CicadaA" => new(255, 255, 255),
                "CicadaB" => new(94, 94, 111),
                "Snail" => new(169, 164, 178),
                "Leech" => new(174, 40, 30),
                "SeaLeech" => new(13, 77, 179),
                "JungleLeech" => new(26, 179, 26),
                "PoleMimic" => new(169, 164, 178),
                "TentaclePlant" => new(169, 164, 178),
                "Scavenger" => new(169, 164, 178),
                "ScavengerElite" => new(169, 164, 178),
                "VultureGrub" => new(212, 202, 111),
                "Vulture" => new(212, 202, 111),
                "KingVulture" => new(212, 202, 111),
                "SmallCentipede" => new(255, 153, 0),
                "MediumCentipede" => new(255, 153, 0),
                "BigCentipede" => new(255, 153, 0),
                "RedCentipede" => new(230, 14, 14),
                "Centiwing" => new(14, 178, 60),
                "AquaCenti" => new(0, 0, 255),
                "TubeWorm" => new(13, 77, 179),
                "Hazer" => new(54, 202, 99),
                "LanternMouse" => new(169, 164, 178),
                "Spider" => new(169, 164, 178),
                "BigSpider" => new(169, 164, 178),
                "SpitterSpider" => new(174, 40, 30),
                "MotherSpider" => new(26, 179, 26),
                "MirosBird" => new(169, 164, 178),
                "MirosVulture" => new(230, 14, 14),
                "BrotherLongLegs" => new(116, 134, 78),
                "DaddyLongLegs" => new(0, 0, 255),
                "TerrorLongLegs" => new(77, 0, 255),
                "Inspector" => new(114, 230, 196),
                "Deer" => new(169, 164, 178),
                "EggBug" => new(0, 255, 120),
                "FireBug" => new(255, 120, 120),
                "DropBug" => new(169, 164, 178),
                "SlugNPC" => new(169, 164, 178),
                "BigNeedleWorm" => new(255, 152, 152),
                "SmallNeedleWorm" => new(255, 152, 152),
                "JetFish" => new(169, 164, 178),
                "Yeek" => new(230, 230, 230),
                "BigEel" => new(169, 164, 178),
                "BigJelly" => new(255, 217, 179),
                "Rock" => new(169, 164, 178),
                "Spear" => new(169, 164, 178),
                "FireSpear" => new(230, 14, 14),
                "ElectricSpear" => new(0, 0, 255),
                "HellSpear" => new(255, 120, 120),
                "LillyPuck" => new(44, 245, 255),
                "Pearl" => new(179, 179, 179),
                "ScavengerBomb" => new(230, 14, 14),
                "SingularityBomb" => new(5, 165, 217),
                "FireEgg" => new(255, 120, 120),
                "SporePlant" => new(174, 40, 30),
                "Lantern" => new(255, 146, 81),
                "VultureMask" => new(169, 164, 178),
                "FlyLure" => new(173, 68, 54),
                "Mushroom" => new(255, 255, 255),
                "FlareBomb" => new(187, 174, 255),
                "PuffBall" => new(169, 164, 178),
                "GooieDuck" => new(114, 230, 196),
                "WaterNut" => new(13, 77, 179),
                "DandelionPeach" => new(150, 199, 245),
                "FirecrackerPlant" => new(174, 40, 30),
                "DangleFruit" => new(0, 0, 255),
                "JellyFish" => new(169, 164, 178),
                "BubbleGrass" => new(14, 178, 60),
                "GlowWeed" => new(242, 255, 69),
                "SlimeMold" => new(255, 153, 0),
                "EnergyCell" => new(5, 165, 217),
                "JokeRifle" => new(169, 164, 178),
                "NeedleEgg" => new(45, 13, 20),
                _ => Color.White,
            };
        }

        public static Rectangle? GetObjectFrame(string name)
        {
            return name switch
            {
                "KarmaFlower"       => new(76, 0, 23, 23),
                "SeedCob"           => new(40, 0, 35, 38),
                "GhostSpot"         => new(0, 0, 38, 48),
                "BlueToken"         => new(76, 24, 10, 20),
                "GoldToken"         => new(87, 24, 10, 20),
                "RedToken"          => new(100, 0, 10, 20),
                "WhiteToken"        => new(98, 24, 10, 20),
                "GreenToken"        => new(111, 0, 10, 20),
                "DataPearl"         => new(39, 39, 11, 10),
                "UniqueDataPearl"   => new(39, 39, 11, 10),
                
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

        static Color GetPearlColor(string type)
        {
            return type switch
            {
                "CC" => new Color(0.9f, 0.6f, 0.1f),
                "SI_west" => new Color(0.01f, 0.01f, 0.01f),
                "SI_top" => new Color(0.01f, 0.01f, 0.01f),
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

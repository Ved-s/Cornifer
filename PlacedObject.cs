using Cornifer.MapObjects;
using Cornifer.Renderers;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Cornifer
{
    public class PlacedObject : SimpleIcon
    {
        public string Type = null!;

        public override string? Name => $"{Type}@{RoomPos.X:0},{RoomPos.Y:0}";

        public HashSet<string> SlugcatAvailability = new();
        public Vector2 RoomPos;
        public Vector2 HandlePos;

        public bool RemoveByAvailability = true;
        public string? DebugDisplay = null;

        public override int ShadeSize => 2;
        public override bool SkipTextureSave => true;
        public override bool DisableBorderConfig => true;

        public override Vector2 Size => Frame.Size.ToVector2();
        public override Vector2 ParentPosAlign => Parent is not Room ? new(.5f) : new Vector2(RoomPos.X / Parent.Size.X, 1 - (RoomPos.Y / Parent.Size.Y));
        public override bool Active
        {
            get => InterfaceState.DrawPlacedObjects.Value 
                && (Parent is not Room || !HideObjectTypes.Contains(Type))
                && (Parent is not Room || InterfaceState.DrawPlacedPickups.Value || Room.NonPickupObjectsWhitelist.Contains(Type))
                && base.Active;
        }

        public static HashSet<string> AllObjectTypes = new()
        {
            "GreenToken","WhiteToken","Germinator","RedToken","OEsphere","MSArteryPush","GooieDuck","LillyPuck",
            "GlowWeed","BigJellyFish","RotFlyPaper","MoonCloak","DandelionPeach","KarmaShrine","Stowaway",
            "HRGuard","DevToken","LightSource","FlareBomb","PuffBall","TempleGuard","LightFixture","DangleFruit",
            "CoralStem","CoralStemWithNeurons","CoralNeuron","CoralCircuit","WallMycelia","ProjectedStars","ZapCoil",
            "SuperStructureFuses","GravityDisruptor","SpotLight","DeepProcessing","Corruption","CorruptionTube",
            "CorruptionDarkness","StuckDaddy","SSLightRod","CentipedeAttractor","DandelionPatch","GhostSpot","DataPearl",
            "UniqueDataPearl","SeedCob","DeadSeedCob","WaterNut","JellyFish","KarmaFlower","Mushroom","SlimeMold",
            "FlyLure","CosmeticSlimeMold","CosmeticSlimeMold2","FirecrackerPlant","VultureGrub","DeadVultureGrub",
            "VoidSpawnEgg","ReliableSpear","SuperJumpInstruction","ProjectedImagePosition","ExitSymbolShelter",
            "ExitSymbolHidden","NoSpearStickZone","LanternOnStick","ScavengerOutpost","TradeOutpost","ScavengerTreasury",
            "ScavTradeInstruction","CustomDecal","InsectGroup","PlayerPushback","MultiplayerItem","SporePlant",
            "GoldToken","BlueToken","DeadTokenStalk","NeedleEgg","BrokenShelterWaterLevel","BubbleGrass","Filter",
            "ReliableIggyDirection","Hazer","DeadHazer","Rainbow","LightBeam","NoLeviathanStrandingZone",
            "FairyParticleSettings","DayNightSettings","EnergySwirl","LightningMachine","SteamPipe","WallSteamer",
            "Vine","VultureMask","SnowSource","DeathFallFocus","CellDistortion","LocalBlizzard","NeuronSpawner",
            "HangingPearls","Lantern","ExitSymbolAncientShelter","BlinkingFlower"
        };

        public static HashSet<string> HideObjectTypes = new()
        {
            "DevToken"
        };

        public static PlacedObject? Load(string data)
        {
            string[] split = data.Split("><", 4);

            if (split.Length < 3)
                return null;

            string objName = split[0];
            string[] subsplit = split[3].Split('~');

            if (objName.EndsWith("Token"))
            {
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

            PlacedObject? obj = Load(objName, new Vector2(float.Parse(split[1], CultureInfo.InvariantCulture) / 20, float.Parse(split[2], CultureInfo.InvariantCulture) / 20));
            if (obj is null)
                return null;

            if (objName.EndsWith("Token"))
            {
                string subname = subsplit[5];
                obj.DebugDisplay = $"Token target: {subname}";

                if (objName == "WhiteToken")
                    obj.SlugcatAvailability = new() { "Spear" };
                else if (objName == "DevToken")
                    obj.RemoveByAvailability = false;
                else
                    obj.SlugcatAvailability = ParseSlugcats(subsplit[6]);
                obj.Color.OriginalValue.A = 150;
                obj.Shade.OriginalValue = false;

                switch (objName)
                {
                    case "GreenToken":
                        int slugcatId = Array.IndexOf(Main.SlugCatNames, subname);
                        if (slugcatId >= 0)
                        {
                            obj.Children.Add(new SlugcatIcon("GreenTokenSlugcat")
                            {
                                Id = slugcatId,
                                ParentPosition = new(0, 8),
                                Parent = obj,
                                ForceSlugcatIcon = true,
                                LineColor = Microsoft.Xna.Framework.Color.Lime
                            });
                        }
                        break;

                    case "BlueToken":
                        PlacedObject? subObject = Load(subname, obj.Size / 2);
                        if (subObject is not null)
                        {
                            subObject.ParentPosition = new(0, 15);
                            obj.Children.Add(subObject);
                        }
                        break;
                }
            }

            if (objName == "Filter" && subsplit.Length >= 5)
            {
                float x = float.Parse(subsplit[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                float y = float.Parse(subsplit[1], NumberStyles.Float, CultureInfo.InvariantCulture);

                obj.HandlePos = new(x, y);
                obj.SlugcatAvailability = ParseSlugcats(subsplit[4]);
            }

            if (objName == "ScavengerOutpost")
            {
                float x = float.Parse(subsplit[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                float y = float.Parse(subsplit[1], NumberStyles.Float, CultureInfo.InvariantCulture);

                Vector2 handlePos = new Vector2(x, y);
                obj.RoomPos += handlePos / 20;

                handlePos.Normalize();
                obj.RoomPos += handlePos * 3.5f;
            }

            if (split[0].Contains("DataPearl"))
            {
                if (subsplit.TryGet(4, out string type))
                {
                    obj.DebugDisplay = $"Pearl id: {type}";

                    obj.Color.OriginalValue = GetPearlColor(type);

                    if (type != "Misc" && type != "BroadcastMisc")
                    {
                        obj.Children.Add(new MapText("PearlText", Main.DefaultSmallMapFont, $"[c:{obj.Color.Value.R:x2}{obj.Color.Value.G:x2}{obj.Color.Value.B:x2}]Colored[/c] pearl"));
                        if (GameAtlases.Sprites.TryGetValue("ScholarA", out AtlasSprite? sprite))
                            obj.Children.Add(new SimpleIcon("PearlIcon", sprite)
                            {
                                Shade = { OriginalValue = true }
                            });
                    }
                }
                obj.Color.OriginalValue.A = 165;
            }

            if (obj.RemoveByAvailability && Main.SelectedSlugcat is not null && obj.SlugcatAvailability.Count > 0 && !obj.SlugcatAvailability.Contains(Main.SelectedSlugcat))
                return null;

            return obj;
        }

        public static PlacedObject? Load(string name, Vector2 pos)
        {
            if (name == "Filter" || name == "ScavengerTreasury")
                return new()
                {
                    Type = name,
                    RoomPos = pos,
                    RemoveByAvailability = false,
                };

            if (!GameAtlases.Sprites.TryGetValue("Object_" + name, out var atlas))
            {
                return null;
            }

            return new()
            {
                Type = name,
                RoomPos = pos,
                Frame = atlas.Frame,
                Texture = atlas.Texture,
                Color = { OriginalValue = atlas.Color },
                Shade = { OriginalValue = atlas.Shade }
            };
        }

        public static bool CheckValidType(string type)
        {
            return GameAtlases.Sprites.ContainsKey($"Object_{type}");
        }

        public void AddAvailabilityIcons()
        {
            if (Type == "DevToken")
                return;

            float iconAngle = 0.5f;
            float totalAngle = Math.Max(0, SlugcatAvailability.Count - 1) * iconAngle;
            float currentAngle = (MathF.PI / 2) + totalAngle / 2;
            foreach (string slugcat in SlugcatAvailability)
            {
                Vector2 offset = new Vector2(MathF.Cos(currentAngle), -MathF.Sin(currentAngle)) * 15;
                currentAngle -= iconAngle;
                Children.Add(new SlugcatIcon($"Availability_{slugcat}")
                {
                    Id = Array.IndexOf(Main.SlugCatNames, slugcat),
                    ParentPosition = offset
                });
            }
        }

        protected override void BuildInnerConfig(UIList list)
        {
            base.BuildInnerConfig(list);

            if (DebugDisplay is not null)
            {
                list.Elements.Add(new UILabel
                {
                    Height = 0,
                    Text = DebugDisplay,
                    TextAlign = new(.5f)
                });
            }
        }

        static HashSet<string> ParseSlugcats(string availability)
        {
            HashSet<string> slugcats = new();
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
                return slugcats;
            }

            slugcats.UnionWith(Main.AvailableSlugCatNames);
            foreach (string name in availability.Split('|'))
                slugcats.Remove(name);

            return slugcats;
        }

        static Color GetPearlColor(string type)
        {
            Color color = GetPearlMainColor(type);
            Color? color2 = GetPearlHighlightColor(type);
            if (color2.HasValue)
            {
                // color = Custom.Screen(color, color2.Value * Custom.QuickSaturation(color2.Value) * 0.5f);

                float max = Math.Max(color2.Value.R, Math.Max(color2.Value.G, color2.Value.B)) / 255f;
                float min = Math.Min(color2.Value.R, Math.Min(color2.Value.G, color2.Value.B)) / 255f;

                float sat = (min - max) / -max;

                Color v = color2.Value * sat * 0.5f;

                color = new Color(1f - (1f - color.R/255f) * (1f - v.R/255f), 1f - (1f - color.G/255f) * (1f - v.G/255f), 1f - (1f - color.B/255f) * (1f - v.B/255f));
            }
            else
            {
                color = Microsoft.Xna.Framework.Color.Lerp(color, Microsoft.Xna.Framework.Color.White, 0.15f);
            }
            if (color.R / 255f < 0.1f && color.G / 255f < 0.1f && color.B / 255f < 0.1f)
            {
                // color = Color.Lerp(color, Menu.MenuRGB(Menu.MenuColors.MediumGrey), 0.3f);

                Color menurgb = new(169, 164, 178);
                color = Microsoft.Xna.Framework.Color.Lerp(color, menurgb, 0.3f);
            }

            return color;
        }
        static Color GetPearlMainColor(string type)
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
        static Color? GetPearlHighlightColor(string type)
        {
            return type switch
            {
                "SI_chat3" => new(0.4f, 0.1f, 0.6f),
                "SI_chat4" => new(0.4f, 0.6f, 0.1f),
                "SI_chat5" => new(0.6f, 0.1f, 0.4f),
                "Spearmasterpearl" => new(0.95f, 0f, 0f),
                "RM" => new(1f, 0f, 0f),
                "LC_second" => new(0.8f, 0.8f, 0f),
                "CL" => new(1f, 0f, 0f),
                "VS" => new(1f, 0f, 1f),
                //"BroadcastMisc" => new(0.4f, 0.9f, 0.4f),
                "CC" => new(1f, 1f, 0f),
                "GW" => new(0.5f, 1f, 0.5f),
                "HI" => new(0.5f, 0.8f, 1f),
                "SH" => new(1f, 0.2f, 0.6f),
                "SI_top" => new(0.1f, 0.4f, 0.6f),
                "SI_west" => new(0.1f, 0.6f, 0.4f),
                "SL_bridge" => new(1f, 0.4f, 1f),
                "SB_ravine" => new(0.6f, 0.1f, 0.4f),
                "UW" => new(1f, 0.7f, 1f),
                "SL_chimney" => new(0.8f, 0.3f, 1f),
                "Red_stomach" => new(1f, 1f, 1f),
                _ => null
            };
        }
    }
}

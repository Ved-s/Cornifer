using Cornifer.Structures;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cornifer.MapObjects
{
    public class PlacedObject : SimpleIcon
    {
        public string Type = null!;

        public override string? Name => $"{Type}@{RoomPos.X:0},{RoomPos.Y:0}";

        public List<SlugcatIcon>? AvailabilityIcons;
        public HashSet<string> SlugcatAvailability = new();
        public Vector2 RoomPos;
        public Vector2 HandlePos;

        public bool RemoveByAvailability = true;
        public string? DebugDisplay = null;

        public override bool SkipTextureSave => true;

        public override Vector2 Size => Frame.Size.ToVector2();
        public override Vector2 ParentPosAlign => Parent is not Room ? new(.5f) : new Vector2(RoomPos.X / Parent.Size.X, 1 - RoomPos.Y / Parent.Size.Y);
        public override bool Active
        {
            get => InterfaceState.DrawPlacedObjects.Value
                && (Parent is not Room || !HideObjectTypes.Contains(Type))
                && (Parent is not Room || InterfaceState.DrawPlacedPickups.Value || Room.NonPickupObjectsWhitelist.Contains(Type))
                && base.Active;
        }

        UIList? AvailabilityPresets;

        public static HashSet<string> HideObjectTypes = new()
        {
            "DevToken"
        };

        static Dictionary<string, string[]> TiedSandboxIDs = new()
        {
            ["CicadaA"] = new[] { "CicadaB" },
            ["SmallCentipede"] = new[] { "MediumCentipede" },
            ["BigNeedleWorm"] = new[] { "SmallNeedleWorm" },
        };

        static HashSet<string> HollowSlugcats = new() { "White", "Yellow", "Red", "Gourmand", "Artificer", "Rivulet", "Spear", "Saint" };

        public PlacedObject()
        {
            BorderSize.OriginalValue = 2;
        }

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

                obj.Color.OriginalValue.Color.A = 150;
                obj.Shade.OriginalValue = false;

                switch (objName)
                {
                    case "WhiteToken":
                        obj.RenderLayer.OriginalValue = Main.BroadcastsLayer;
                        break;

                    case "GreenToken":
                        Slugcat? slugcat = StaticData.Slugcats.FirstOrDefault(s => s.Name == subname || s.Id == subname);

                        if (slugcat is not null)
                        {
                            obj.Children.Add(new SlugcatIcon("GreenTokenSlugcat", slugcat, false)
                            {
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

                            List<PlacedObject> objects = new() { subObject };

                            if (TiedSandboxIDs.TryGetValue(subname, out string[]? tied))
                            {
                                foreach (string to in tied)
                                {
                                    PlacedObject? tiedObj = Load(to, obj.Size / 2);
                                    if (tiedObj is null)
                                        continue;

                                    obj.Children.Add(tiedObj);
                                    objects.Add(tiedObj);
                                }
                            }

                            if (objects.Count > 1)
                            {
                                float angle = 0;
                                float ad = MathF.PI * 2 / objects.Count;

                                foreach (PlacedObject o in objects)
                                {
                                    o.Offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 20;
                                    angle += ad;
                                }
                            }
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
                    type = FixDataPearlType(type);
                    obj.DebugDisplay = $"Pearl id: {type}";

                    obj.Color.OriginalValue.Color = StaticData.GetPearlColor(type);
                    
                    if (type != "Misc" && type != "BroadcastMisc")
                    {
                        obj.Children.Add(new MapText("PearlText", Main.DefaultSmallMapFont, $"[c:{obj.Color.Value.GetKeyOrColorString()}]Colored[/c] pearl"));
                        if (SpriteAtlases.Sprites.TryGetValue("ScholarA", out AtlasSprite? sprite))
                            obj.Children.Add(new SimpleIcon("PearlIcon", sprite)
                            {
                                Shade = { OriginalValue = true }
                            });
                    }
                }
                obj.Color.OriginalValue.Color.A = 165;
            }

            if (obj.RemoveByAvailability && Main.SelectedSlugcat is not null && obj.SlugcatAvailability.Count > 0 && !obj.SlugcatAvailability.Contains(Main.SelectedSlugcat.Id))
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

            if (!SpriteAtlases.Sprites.TryGetValue("Object_" + name, out var atlas))
            {
                return null;
            }

            return new()
            {
                Type = name,
                RoomPos = pos,
                Frame = atlas.Frame,
                Texture = atlas.Texture,
                Sprite = atlas,
                Color = { OriginalValue = new(null, atlas.Color) },
                Shade = { OriginalValue = atlas.Shade }
            };
        }

        public static bool CheckValidType(string type)
        {
            return SpriteAtlases.Sprites.ContainsKey($"Object_{type}");
        }

        public void AddAvailabilityIcons()
        {
            if (Type == "DevToken" || Type == "WhiteToken" || SlugcatAvailability.Count == 0)
                return;

            HashSet<string> cats = new(StaticData.Slugcats.Where(s => s.Playable).Select(s => s.Id));
            cats.SymmetricExceptWith(SlugcatAvailability);
            if (cats.Count == 0) // SlugcatAvailability contains all playable slugcats
                return;

            float iconAngle = 0.5f;
            float totalAngle = Math.Max(0, SlugcatAvailability.Count - 1) * iconAngle;
            float currentAngle = MathF.PI / 2 + totalAngle / 2;

            int i = 0;

            List<SlugcatIcon> icons = new();
            foreach (Slugcat slugcat in StaticData.Slugcats)
            {
                bool available = SlugcatAvailability.Contains(slugcat.Id);

                if (!available && !HollowSlugcats.Contains(slugcat.Id))
                    continue;

                Vector2 offset = new Vector2(MathF.Cos(currentAngle), -MathF.Sin(currentAngle)) * 15;
                currentAngle -= iconAngle;

                offset.Floor();
                SlugcatIcon icon = new($"Availability_{slugcat.Id}", slugcat, !available)
                {
                    ParentPosition = offset
                };
                Children.Add(icon);
                icons.Add(icon);
                i++;
            }
            AvailabilityIcons = icons;

            DiamondPlacement? placement = DiamondPlacement.Placements.FirstOrDefault(p => p.Positions.Length == SlugcatAvailability.Count);
            if (placement is not null)
                PositionAvailabilityIcons(placement, new(Size.X / 2, -10 - placement.Size.Y / 2));
        }

        public void PositionAvailabilityIcons(DiamondPlacement placement, Vector2? center)
        {
            if (AvailabilityIcons is null || placement.Positions.Length < 1)
                return;

            if (center is null)
            {
                Vector2 tl = AvailabilityIcons[0].ParentPosition;
                Vector2 br = AvailabilityIcons[0].ParentPosition + DiamondPlacement.DiamondSize;

                for (int i = 0; i < AvailabilityIcons.Count; i++)
                {
                    if (!AvailabilityIcons[i].Active)
                        continue;

                    tl.X = Math.Min(tl.X, AvailabilityIcons[i].ParentPosition.X);
                    tl.Y = Math.Min(tl.Y, AvailabilityIcons[i].ParentPosition.Y);

                    br.X = Math.Max(br.X, AvailabilityIcons[i].ParentPosition.X + AvailabilityIcons[i].Size.X);
                    br.Y = Math.Max(br.Y, AvailabilityIcons[i].ParentPosition.Y + AvailabilityIcons[i].Size.Y);
                }

                center = tl + (br - tl) / 2;
            }

            int iconInd = 0;

            for (int i = 0; i < placement.Positions.Length; i++)
            {
                if (i >= AvailabilityIcons.Count || iconInd >= AvailabilityIcons.Count)
                    break;

                Vector2 pos = placement.Positions[i] + center.Value;
                pos.Ceiling();

                while (iconInd < AvailabilityIcons.Count && !AvailabilityIcons[iconInd].Active)
                    iconInd++;

                if (iconInd >= AvailabilityIcons.Count)
                    break;

                AvailabilityIcons[iconInd].ParentPosition = pos;
                iconInd++;
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

            AvailabilityPresets = new()
            {
                Visible = false,
                AutoSize = true,
                Height = 0,
            };
            list.Elements.Add(AvailabilityPresets);
        }

        protected override void UpdateInnerConfig()
        {
            if (AvailabilityPresets is not null)
            {
                int visibleAvail = AvailabilityIcons?.Count(i => i.Active) ?? 0;
                AvailabilityPresets.Visible = false;

                if (visibleAvail > 1)
                {
                    UIFlow flow = new()
                    {
                        ElementSpacing = 4,
                    };

                    bool any = false;
                    foreach (DiamondPlacement placement in DiamondPlacement.Placements)
                    {
                        if (placement.Positions.Length != visibleAvail)
                            continue;

                        UIHoverPanel panel = new()
                        {
                            Width = DiamondPlacement.MaxSize.X + 8,
                            Height = DiamondPlacement.MaxSize.Y + 8,

                            Padding = 4,
                            Elements =
                        {
                            new UIDiamondPlacementDisplay
                            {
                                Placement = placement,
                                Icons = AvailabilityIcons
                            }
                        }
                        };
                        panel.OnEvent(UIElement.ClickEvent, (_, _) => PositionAvailabilityIcons(placement, null));
                        flow.Elements.Add(panel);
                        any = true;
                    }
                    if (any)
                    {
                        AvailabilityPresets.Elements.Clear();
                        AvailabilityPresets.Elements.Add(new UILabel
                        {
                            Height = 0,
                            TextAlign = new(.5f),

                            Text = "Diamond presets"
                        });
                        AvailabilityPresets.Elements.Add(flow);
                        AvailabilityPresets.Visible = true;
                        AvailabilityPresets.Recalculate();
                    }
                }
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

            slugcats.UnionWith(StaticData.Slugcats.Where(s => s.Playable).Select(s => s.Id));
            foreach (string name in availability.Split('|'))
                slugcats.Remove(name);

            return slugcats;
        }

        static string FixDataPearlType(string type)
        {
            if (int.TryParse(type, out int t))
                return t switch
                {
                    0 => "Misc",
                    1 => "Misc2",
                    2 => "CC",
                    3 => "SI_west",
                    4 => "SI_top",
                    5 => "LF_west",
                    6 => "LF_bottom",
                    7 => "HI",
                    8 => "SH",
                    9 => "DS",
                    10 => "SB_filtration",
                    11 => "SB_ravine",
                    12 => "GW",
                    13 => "SL_bridge",
                    14 => "SL_moon",
                    15 => "SU",
                    16 => "UW",
                    17 => "PebblesPearl",
                    18 => "SL_chimney",
                    19 => "Red_stomach",
                    _ => type
                };
            return type;
        }
    }
}

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

        public override int ShadeSize => 2;
        public override bool SkipTextureSave => true;
        public override bool DisableBorderConfig => true;

        public override Vector2 Size => Frame.Size.ToVector2();
        public override Vector2 ParentPosAlign => Parent is not Room ? new(.5f) : new Vector2(RoomPos.X / Parent.Size.X, 1 - RoomPos.Y / Parent.Size.Y);
        public override bool Active
        {
            get => InterfaceState.DrawPlacedObjects.Value
                && (Parent is not Room || !HideObjectTypes.Contains(Type))
                && (Parent is not Room || InterfaceState.DrawPlacedPickups.Value || Room.NonPickupObjectsWhitelist.Contains(Type))
                && base.Active;
        }

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
                obj.Color.OriginalValue.Color.A = 150;
                obj.Shade.OriginalValue = false;

                switch (objName)
                {
                    case "GreenToken":
                        SlugcatData slugcat = StaticData.Slugcats.FirstOrDefault(s => s.Name == subname || s.Id == subname);

                        obj.Children.Add(new SlugcatIcon("GreenTokenSlugcat")
                        {
                            Id = slugcat.IconId,
                            ParentPosition = new(0, 8),
                            Parent = obj,
                            ForceSlugcatIcon = true,
                            LineColor = Microsoft.Xna.Framework.Color.Lime
                        });
                        
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
            if (Type == "DevToken" || SlugcatAvailability.Count == 0)
                return;

            float iconAngle = 0.5f;
            float totalAngle = Math.Max(0, SlugcatAvailability.Count - 1) * iconAngle;
            float currentAngle = MathF.PI / 2 + totalAngle / 2;

            int i = 0;

            List<SlugcatIcon> icons = new();
            foreach (SlugcatData slugcat in StaticData.Slugcats)
            {
                if (!SlugcatAvailability.Contains(slugcat.Id))
                    continue;

                Vector2 offset = new Vector2(MathF.Cos(currentAngle), -MathF.Sin(currentAngle)) * 15;
                currentAngle -= iconAngle;

                offset.Floor();
                SlugcatIcon icon = new($"Availability_{slugcat}")
                {
                    Id = slugcat.IconId,
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
                    tl.X = Math.Min(tl.X, AvailabilityIcons[i].ParentPosition.X);
                    tl.Y = Math.Min(tl.Y, AvailabilityIcons[i].ParentPosition.Y);

                    br.X = Math.Max(br.X, AvailabilityIcons[i].ParentPosition.X + AvailabilityIcons[i].Size.X);
                    br.Y = Math.Max(br.Y, AvailabilityIcons[i].ParentPosition.Y + AvailabilityIcons[i].Size.Y);
                }

                center = tl + (br - tl) / 2;
            }

            for (int i = 0; i < placement.Positions.Length; i++)
            {
                if (i >= AvailabilityIcons.Count)
                    break;

                Vector2 pos = placement.Positions[i] + center.Value;
                pos.Ceiling();
                AvailabilityIcons[i].ParentPosition = pos;
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

            if (AvailabilityIcons?.Count is not null and > 1)
            {
                UIFlow flow = new()
                {
                    ElementSpacing = 4,
                };

                bool any = false;
                foreach (DiamondPlacement placement in DiamondPlacement.Placements)
                {
                    if (placement.Positions.Length != AvailabilityIcons.Count)
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
                    list.Elements.Add(new UILabel
                    {
                        Height = 0,
                        TextAlign = new(.5f),

                        Text = "Diamond presets"
                    });
                    list.Elements.Add(flow);
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
    }
}

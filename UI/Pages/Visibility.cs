using Cornifer.MapObjects;
using Cornifer.Structures;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Pages
{
    public class Visibility : Page
    {
        public static Dictionary<string, UIButton> PlacedObjects = new();
        public static UIButton SlugcatIcons = null!;
        public static UIList MapObjectVisibilityList = null!;

        public override int Order => 2;

        public Visibility() 
        {
            Elements = new(this)
            {
                new UIList()
                {
                    ElementSpacing = 2,

                    Elements =
                    {
                        new UICollapsedPanel
                        {
                            HeaderText = "General",

                            Content = new UIList
                            {
                                ElementSpacing = 2,
                                AutoSize = true,
                                Padding = 3,

                                Elements =
                                {
                                    new UIButton
                                    {
                                        Height = 20,
                                        Text = "Slugcat icons",

                                        Selectable = true,

                                        SelectedBackColor = Color.White,
                                        SelectedTextColor = Color.Black,

                                        TextAlign = new(.5f)
                                    }.BindConfig(InterfaceState.DrawSlugcatIcons)
                                    .Assign(out SlugcatIcons),

                                    new UIButton
                                    {
                                        Height = 20,
                                        Text = "Diamond icons",

                                        HoverText = "Draw diamonds instead of\nslugcat icons",

                                        Selectable = true,

                                        SelectedBackColor = Color.White,
                                        SelectedTextColor = Color.Black,

                                        TextAlign = new(.5f)
                                    }.BindConfig(InterfaceState.DrawSlugcatDiamond),

                                    new UIButton
                                    {
                                        Height = 20,
                                        Text = "Room objects",

                                        Selectable = true,

                                        SelectedBackColor = Color.White,
                                        SelectedTextColor = Color.Black,

                                        TextAlign = new(.5f)
                                    }.BindConfig(InterfaceState.DrawPlacedObjects),

                                    new UIButton
                                    {
                                        Height = 20,
                                        Text = "Room pickups",

                                        HoverText = "Draw placed items",

                                        Selectable = true,

                                        SelectedBackColor = Color.White,
                                        SelectedTextColor = Color.Black,

                                        TextAlign = new(.5f)
                                    }.BindConfig(InterfaceState.DrawPlacedPickups),

                                    new UIButton
                                    {
                                        Height = 20,

                                        Selectable = true,
                                        Text = "Tile walls",

                                        HoverText = "Render room tiles with walls",

                                        SelectedBackColor = Color.White,
                                        SelectedTextColor = Color.Black,

                                        TextAlign = new(.5f)

                                    }.BindConfig(InterfaceState.DrawTileWalls),

                                    new UIButton
                                    {
                                        Height = 20,

                                        Selectable = true,
                                        Text = "Shortcuts",

                                        SelectedBackColor = Color.White,
                                        SelectedTextColor = Color.Black,

                                        TextAlign = new(.5f)

                                    }.BindConfig(InterfaceState.MarkShortcuts),

                                    new UIButton
                                    {
                                        Height = 20,

                                        Selectable = true,
                                        Text = "Only exit shortcuts",

                                        SelectedBackColor = Color.White,
                                        SelectedTextColor = Color.Black,

                                        TextAlign = new(.5f)

                                    }.BindConfig(InterfaceState.MarkExitsOnly),

                                    new UIButton
                                    {
                                        Height = 20,

                                        Selectable = true,
                                        Text = "Borders",

                                        SelectedBackColor = Color.White,
                                        SelectedTextColor = Color.Black,

                                        TextAlign = new(.5f)

                                    }.BindConfig(InterfaceState.DrawBorders),
                                }
                            }
                        },

                        new UICollapsedPanel
                        {
                            HeaderText = "Map objects",
                            Collapsed = true,

                            Content = new UIResizeablePanel
                            {
                                CanGrabLeft = false,
                                CanGrabRight = false,
                                CanGrabTop = false,

                                BackColor = Color.Transparent,
                                BorderColor = Color.Transparent,

                                Height = 100,
                                Padding = 3,

                                Elements =
                                {
                                    new UIList
                                    {
                                        ElementSpacing = 4
                                    }.Assign(out MapObjectVisibilityList)
                                }
                            },
                        },

                        new UICollapsedPanel
                        {
                            HeaderText = "Room objects",
                            Collapsed = true,

                            Content = new UIResizeablePanel
                            {
                                Height = 150,

                                BackColor = Color.Transparent,
                                BorderColor = Color.Transparent,

                                Padding = 4,
                                CanGrabTop = false,
                                CanGrabLeft = false,
                                CanGrabRight = false,
                                CanGrabBottom = true,

                                Elements =
                                {
                                    new UIList
                                    {
                                        ElementSpacing = 4
                                    }.Execute((list) =>
                                    {
                                        PlacedObjects.Clear();

                                        foreach (string objectName in StaticData.PlacedObjectTypes.OrderBy(s => s))
                                        {
                                            if (!PlacedObject.CheckValidType(objectName))
                                                continue;

                                            UIButton btn = new()
                                            {
                                                Text = objectName,
                                                Height = 20,
                                                TextAlign = new(.5f),

                                                Selectable = true,
                                                Selected = !PlacedObject.HideObjectTypes.Contains(objectName),

                                                SelectedBackColor = Color.White,
                                                SelectedTextColor = Color.Black
                                            };
                                            btn.OnEvent(UIElement.ClickEvent, (btn, _) =>
                                            {
                                                if (btn.Selected)
                                                    PlacedObject.HideObjectTypes.Remove(objectName);
                                                else
                                                    PlacedObject.HideObjectTypes.Add(objectName);
                                            });

                                            PlacedObjects[objectName] = btn;
                                            list.Elements.Add(btn);
                                        }
                                    })
                                }
                            }
                        }
                    }
                }
            };
        }

        internal static void RegionChanged() 
        {
            if (MapObjectVisibilityList is not null)
            {
                MapObjectVisibilityList.Elements.Clear();
                foreach (MapObject obj in Main.WorldObjectLists.OrderBy(o => o.Name))
                {
                    if (!obj.CanSetActive)
                        continue;

                    UIPanel panel = new()
                    {
                        Padding = 2,
                        Height = 22,

                        BackColor = new(48, 48, 48),

                        Elements =
                        {
                            new UILabel
                            {
                                Text = obj.Name,
                                Top = 2,
                                Left = 2,
                                Height = 16,
                                WordWrap = false,
                                AutoSize = false,
                                Width = new(-22, 1)
                            },
                            new UIButton
                            {
                                Text = "A",

                                Selectable = true,
                                Selected = obj.ActiveProperty.Value,

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                Left = new(0, 1, -1),
                                Width = 18,
                            }.OnEvent(ClickEvent, (btn, _) => obj.ActiveProperty.Value = btn.Selected),
                        }
                    };
                    panel.OnEvent(ClickEvent, (p, _) => { if (p.Root?.Hover is not UIButton) Main.FocusOnObject(obj); });
                    MapObjectVisibilityList.Elements.Add(panel);
                }
            }
        }
    }
}

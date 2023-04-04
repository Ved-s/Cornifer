using Cornifer.Input;
using Cornifer.MapObjects;
using Cornifer.Structures;
using Cornifer.UI;
using Cornifer.UI.Elements;
using Cornifer.UI.Modals;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cornifer
{
    public static class Interface
    {
        public static UIRoot? Root;

        public static UIPanel SidePanel = null!;
        public static UIButton SlugcatIcons = null!;
        public static ColorSelector ColorSelector = null!;

        public static UILabel NoConfigObjectLabel = null!;
        public static UILabel NoConfigLabel = null!;
        public static UIPanel ConfigPanel = null!;
        public static UIList KeybindsTabList = null!;
        public static UIList MapObjectVisibilityList = null!;

        public static UIList SubregionColorList = null!;
        public static Dictionary<string, UIButton> VisibilityPlacedObjects = new();
        public static Dictionary<RenderLayers, UIButton> VisibilityRenderLayers = new();
        public static UIList InstallsList = null!;
        public static List<(RainWorldInstallation, UIHoverPanel)> InstallsPanels = new();

        public static bool Hovered => Root?.Hover is not null;
        public static bool Active => Root?.Active is not null;
        public static bool BlockUIHover => Main.Selecting || Main.Dragging || InputHandler.Pan.Pressed && !Hovered;

        static UIElement? ConfigElement;
        static MapObject? configurableObject;

        static List<Func<UIModal>>? ModalCreators;
        static Queue<TaskCompletionSource> ModalWaitTasks = new();
        internal static UIModal? CurrentModal;

        public static MapObject? ConfigurableObject
        {
            get => configurableObject;
            set
            {
                if (ConfigElement is null)
                {
                    NoConfigObjectLabel.Visible = Main.SelectedObjects.Count != 1;
                    NoConfigLabel.Visible = Main.SelectedObjects.Count == 1;
                }
                else
                {
                    NoConfigObjectLabel.Visible = false;
                    NoConfigLabel.Visible = false;
                }

                if (ReferenceEquals(configurableObject, value))
                    return;

                configurableObject = value;

                if (ConfigElement is not null)
                    ConfigPanel.Elements.Remove(ConfigElement);

                ConfigElement = value?.Config;

                if (ConfigElement is not null)
                    ConfigPanel.Elements.Add(ConfigElement);
            }
        }

        public static void Init()
        {
            if (ConfigurableObject is not null)
            {
                ConfigurableObject.ConfigCache = null;
            }

            if (ConfigElement is not null)
                ConfigPanel.Elements.Remove(ConfigElement);

            ConfigElement = configurableObject?.Config;

            Root = new(Main.Instance)
            {
                Font = Content.Consolas10,

                Elements =
                {
                    TaskProgress.InitUI(),

                    new UIResizeablePanel()
                    {
                        Left = new(0, 1, -1),

                        Width = 200,
                        Height = new(0, 1),
                        MaxWidth = new(0, 1f),
                        MinWidth = new(80, 0),

                        Margin = 5,

                        BackColor = Color.Transparent,
                        BorderColor = Color.Transparent,

                        CanGrabTop = false,
                        CanGrabRight = false,
                        CanGrabBottom = false,
                        SizingChangesPosition = false,

                        Elements =
                        {
                            new TabContainer
                            {
                                Tabs =
                                {
                                    new()
                                    {
                                        Name = "General",
                                        Element = InitGeneralTab()
                                    },
                                    new()
                                    {
                                        Name = "Visibility",
                                        Element = InitVisibilityTab()
                                    },
                                    new()
                                    {
                                        Name = "Subregions",
                                        Element = InitSubregionsTab(),
                                    },
                                    new()
                                    {
                                        Name = "Config",
                                        Element = InitObjectConfigTab(),
                                    },
                                    new()
                                    {
                                        Name = "Keybinds",
                                        Element = InitKeybindsTab(),
                                    },
                                    new()
                                    {
                                        Name = "Installs",
                                        Element = InitInstallationsTab(),
                                    }
                                }
                            }.Execute(tabs =>
                            {
                                if (Main.DebugMode)
                                {
                                    tabs.Tabs.Add(new()
                                    {
                                        Name = "Debug",
                                        Element = InitDebugTab()
                                    });
                                }
                            })
                        }
                    }.Assign(out SidePanel),

                    new ColorSelector
                    {
                        Top = 5,
                        Left = 5,
                        Visible = false,
                    }.Assign(out ColorSelector)
                }
            };
            CreateModals();
            Root.Recalculate();

            if (Main.Region is not null)
                RegionChanged(Main.Region);
        }

        static UIElement InitGeneralTab()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),

                Elements =
                {
                    new UIButton
                    {
                        Top = 0,

                        Height = 25,
                        Text = "Select region",

                        TextAlign = new(.5f)
                    }.OnEvent(UIElement.ClickEvent, async (_, _) =>
                    {
                        await SelectRegionClicked();
                    }),

                    new UIList()
                    {
                        Top = 35,
                        Height = new(-100, 1),
                        ElementSpacing = 2,

                        Elements =
                        {
                            new UIButton
                            {
                                Height = 20,

                                Selectable = true,
                                Text = "Disable room cropping",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)

                            }.BindConfig(InterfaceState.DisableRoomCropping),

                            new UIButton
                            {
                                Height = 20,

                                Text = "Disable better cutouts",

                                HoverText = "Sets \"Better tile cutouts\" to False for all rooms",

                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (_, _) => 
                            {
                                if (Main.Region is null)
                                    return;

                                foreach (Room r in Main.Region.Rooms)
                                {
                                    r.UseBetterTileCutout.Value = false;
                                    r.CutOutsDirty = true;
                                    r.TileMapDirty = true;
                                    r.ShadeTextureDirty = true;
                                }
                            }),

                            new UIPanel
                            {
                                Height = 40,
                                Padding = 3,

                                BorderColor = new(100, 100, 100),

                                Elements =
                                {
                                    new UILabel
                                    {
                                        Height = 20,
                                        Text = $"Water transparency: {InterfaceState.WaterTransparency.Value*100:0}%",
                                        TextAlign = new(0, .5f)
                                    }.OnConfigChange(InterfaceState.WaterTransparency, (label, value) => label.Text = $"Water transparency: {value*100:0}%"),
                                    new UIScrollBar
                                    {
                                        Top = 20,
                                        Height = 8,
                                        Margin = new(0, 5),

                                        BackColor = new(36, 36, 36),
                                        BorderColor = new(100, 100, 100),

                                        Horizontal = true,
                                        BarPadding = -4,
                                        BarSize = 7,
                                        BarSizeAbsolute = true,
                                        ScrollMin = 0,
                                        ScrollMax = 1,
                                    }.BindConfig(InterfaceState.WaterTransparency)
                                }
                            },

                            new UIElement { Height = 10 },

                            new UIButton
                            {
                                Height = 20,

                                Text = "Add icons to map",

                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, async (btn, _) => await AddIconSelect.Show()),

                            new UIButton
                            {
                                Height = 20,

                                Text = "Add text to map",

                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
                            {
                                Main.AddWorldObject(new MapText($"WorldText_{Random.Shared.Next():x}", Main.DefaultSmallMapFont, "Sample text")
                                {
                                    WorldPosition = Main.WorldCamera.Position + Main.WorldCamera.Size / Main.WorldCamera.Scale * .5f
                                });
                            }),

                            new UIElement { Height = 10 },

                            new UIButton
                            {
                                Height = 20,

                                Selectable = true,
                                Text = "Overlay image enabled",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)

                            }.BindConfig(InterfaceState.OverlayEnabled),

                            new UIButton
                            {
                                Height = 20,

                                Text = "Select overlay image",
                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, SelectOverlayClicked),

                            new UIButton
                            {
                                Height = 20,

                                Selectable = true,
                                Text = "Background overlay",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)

                            }.BindConfig(InterfaceState.OverlayBelow),

                            new UIPanel
                            {
                                Height = 40,
                                Padding = 3,

                                BorderColor = new(100, 100, 100),

                                Elements =
                                {
                                    new UILabel
                                    {
                                        Height = 20,
                                        Text = $"Overlay transparency: {InterfaceState.OverlayTransparency.Value*100:0}%",
                                        TextAlign = new(0, .5f)
                                    }.OnConfigChange(InterfaceState.OverlayTransparency, (label, value) => label.Text = $"Overlay transparency: {value*100:0}%"),
                                    new UIScrollBar
                                    {
                                        Top = 20,
                                        Height = 8,
                                        Margin = new(0, 5),

                                        BackColor = new(36, 36, 36),
                                        BorderColor = new(100, 100, 100),

                                        Horizontal = true,
                                        BarPadding = -4,
                                        BarSize = 7,
                                        BarSizeAbsolute = true,
                                        ScrollMin = 0,
                                        ScrollMax = 1,
                                    }.BindConfig(InterfaceState.OverlayTransparency)
                                }
                            },

                            new UIElement { Height = 10 },
                            new UIPanel
                            {
                                Height = 50,
                                Padding = 3,

                                BorderColor = new(100, 100, 100),
                                Elements =
                                {
                                    new UILabel
                                    {
                                        Height = 18,
                                        Text = "State",
                                        TextAlign = new(.5f)
                                    },
                                    new UIButton
                                    {
                                        Top = 20,
                                        Left = 0,
                                        Width = new(-1, .33f),
                                        Height = new(-20, 1),
                                        Text = "Open",
                                        TextAlign = new(.5f)
                                    }.OnEvent(UIElement.ClickEvent, async (_, _) => await Main.OpenState()),
                                    new UIButton
                                    {
                                        Top = 20,
                                        Left = new(1, .33f),
                                        Width = new(-1, .33f),
                                        Height = new(-20, 1),
                                        Text = "Save",
                                        TextAlign = new(.5f)
                                    }.OnEvent(UIElement.ClickEvent, async (_, _) => await Main.SaveState()),
                                    new UIButton
                                    {
                                        Top = 20,
                                        Left = new(2, .66f),
                                        Width = new(-2, .34f),
                                        Height = new(-20, 1),
                                        Text = "Save as",
                                        TextAlign = new(.5f)
                                    }.OnEvent(UIElement.ClickEvent, async (_, _) => await Main.SaveStateAs())
                                }
                            }
                        }
                    },

                    new UIButton
                    {
                        Top = new(-55, 1),

                        Height = 25,
                        Text = "Capture map",

                        TextAlign = new(.5f)
                    }.OnEvent(UIElement.ClickEvent, CaptureClicked),

                    new UIButton
                    {
                        Top = new(-25, 1),

                        Height = 25,
                        Text = "Capture map (Layered)",

                        TextAlign = new(.5f)
                    }.OnEvent(UIElement.ClickEvent, CaptureLayeredClicked),
                }
            };
        }
        static UIElement InitVisibilityTab()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),

                Elements =
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
                                            VisibilityPlacedObjects.Clear();

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

                                                VisibilityPlacedObjects[objectName] = btn;
                                                list.Elements.Add(btn);
                                            }
                                        })
                                    }
                                }
                            },

                            new UICollapsedPanel
                            {
                                HeaderText = "Map layers",
                                Collapsed = true,

                                Content = new UIResizeablePanel
                                {
                                    Height = 120,

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
                                            VisibilityRenderLayers.Clear();

                                            int all = (int)RenderLayers.All;
                                            for (int i = 0; i < 32; i++)
                                            {
                                                if ((all >> i) == 0)
                                                    break;

                                                int layerInt = 1 << i;
                                                if ((layerInt & all) == 0)
                                                    continue;

                                                RenderLayers layer = (RenderLayers)layerInt;

                                                UIButton btn = new()
                                                {
                                                    Text = layer.ToString(),
                                                    Height = 20,
                                                    TextAlign = new(.5f),

                                                    Selectable = true,
                                                    Selected = Main.ActiveRenderLayers.HasFlag(layer),

                                                    SelectedBackColor = Color.White,
                                                    SelectedTextColor = Color.Black
                                                };
                                                btn.OnEvent(UIElement.ClickEvent, (btn, _) =>
                                                {
                                                    if (btn.Selected)
                                                        Main.ActiveRenderLayers |= layer;
                                                    else
                                                        Main.ActiveRenderLayers &= ~layer;
                                                });

                                                VisibilityRenderLayers[layer] = btn;
                                                list.Elements.Add(btn);
                                            }
                                        })
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
        static UIElement InitSubregionsTab()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),

                Elements =
                {
                    new UILabel
                    {
                        Height = 20,
                        Text = "Subregion colors",
                        TextAlign = new(.5f)
                    },
                    new UIList
                    {
                        Top = 20,
                        Height = new(-20, 1),
                        ElementSpacing = 3,
                    }.Assign(out SubregionColorList),
                }
            };
        }
        static UIElement InitObjectConfigTab()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),

                Elements =
                {
                    new UILabel
                    {
                        Height = 20,
                        Text = "Select one object on the map to configure",
                        TextAlign = new(.5f),
                        Visible = ConfigurableObject is null && Main.SelectedObjects.Count != 1,
                    }.Assign(out NoConfigObjectLabel),
                    new UILabel
                    {
                        Height = 20,
                        Text = "Selected object is not configurable",
                        TextAlign = new(.5f),
                        Visible = ConfigurableObject is null && Main.SelectedObjects.Count == 1
                    }.Assign(out NoConfigLabel)
                }
            }.Assign(out ConfigPanel).Execute(panel =>
            {
                if (ConfigElement is not null)
                    panel.Elements.Add(ConfigElement);
            });
        }
        static UIElement InitKeybindsTab()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),

                Elements =
                {
                    new UIList
                    {
                        ElementSpacing = 2,
                    }.Assign(out KeybindsTabList)
                    .Execute(list =>
                    {
                        foreach (Keybind keybind in InputHandler.Keybinds.Values)
                        {
                            if (keybind.Name.Length == 0)
                                continue;

                            list.Elements.Add(new UILabel
                            {
                                Text = keybind.Name,
                                AutoSize = true,
                                Height = 0,
                                TextAlign = new(.5f)
                            });

                            UIList combos = new()
                            {
                                ElementSpacing = 4,
                                AutoSize = true,
                                Height = 0,
                            };

                            foreach (List<KeybindInput> inputs in keybind.Inputs)
                                AddKeyComboPanel(list, keybind, inputs);

                            list.Elements.Add(combos);
                            list.Elements.Add(new UIButton
                            {
                                Text = "Add keybind",
                                TextAlign = new(.5f),
                                Height = 20
                            }.OnEvent(UIElement.ClickEvent, async (_, _) =>
                            {
                                KeybindSelector.Show(keybind);
                                List<KeybindInput>? inputs = await KeybindSelector.Task;
                                if (inputs is null)
                                    return;

                                keybind.Inputs.Add(inputs);
                                AddKeyComboPanel(combos, keybind, inputs);
                                InputHandler.SaveKeybinds();
                                KeybindsTabList.Recalculate();
                            }));
                            list.Elements.Add(new UIElement { Height = 10 });
                        }
                    })
                }
            };
        }
        static UIElement InitDebugTab()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),

                Elements =
                {
                    new UIList 
                    {
                        ElementSpacing = 4,

                        Elements = 
                        {
                            new UIButton
                            {
                                Text = "Test diamond placement",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                            {
                                Vector2 pos = Main.WorldCamera.InverseTransformVector(Main.WorldCamera.Size / 2);

                                foreach (DiamondPlacement placement in DiamondPlacement.Placements)
                                {
                                    pos.X += placement.Size.X / 2;

                                    for (int i = 0; i < placement.Positions.Length; i++)
                                    {
                                        SimpleIcon icon = new(
                                            $"Debug_DiamondPlacement_{Random.Shared.Next():x}",
                                            SpriteAtlases.Sprites[$"SlugcatDiamond_{StaticData.Slugcats[i].Id}"]);
                                        icon.BorderSize.OriginalValue = 1;
                                        icon.WorldPosition = pos + placement.Positions[i];

                                        Main.WorldObjects.Add(icon);
                                    }

                                    pos.X += placement.Size.X / 2 + 5;
                                }
                            }),

                            new UIButton
                            {
                                Text = "Test Idle",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                            {
                                Content.Idle.Play(.5f, 0, 0);
                            }).OnEvent(UIElement.UpdateEvent, (b, _) => 
                            {
                                b.Text = $"Test Idle ({(Main.Idlesound?"S":"")}{Main.Idle})";
                            }),

                            new UIButton
                            {
                                Text = "UI Exception",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                            {
                                throw new Exception("UI exception");
                            }),

                            new UIButton
                            {
                                Text = "Thread Exception",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                            {
                                new Thread(() => throw new Exception("Thread exception")).Start();
                            }),

                            new UIButton
                            {
                                Text = "Task Exception",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                            {
                                Task.Run(async () => { await Task.Delay(100); throw new Exception("Task exception"); });
                            }),

                            new UIButton
                            {
                                Text = "Async Void Exception",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                            {
                                async void TestException()
                                {
                                    await Task.Delay(100);
                                    throw new Exception("Async void exception");
                                }

                                TestException();
                            })
                        }
                    }
                }
            };
        }
        static UIElement InitInstallationsTab()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),
                Elements =
                {
                    new UIList
                    {
                        Height = 0,
                        AutoSize = true,
                        ElementSpacing = 4,

                        Elements =
                        {
                            new UILabel
                            {
                                Text = "Rain World installations",
                                TextAlign = new(.5f),
                                Height = 0,
                            },
                            new UIList
                            {
                                Height = 0,
                                AutoSize = true,
                                ElementSpacing = 4,
                            }.Assign(out InstallsList)
                            .Execute(_ => PopulateInstallations()),
                            new UIButton
                            {
                                Height = 25,
                                TextAlign = new(.5f),
                                Text = "Add installation"
                            }.OnEvent(UIElement.ClickEvent, async (_, _) =>
                            {
                                RainWorldInstallation? install = await InstallationSelection.ShowDialog();
                                if (install is null)
                                    return;

                                RWAssets.AddInstallation(install);
                            })
                        }
                    }
                }
            };
        }

        static async void SelectOverlayClicked(UIButton btn, Empty _)
        {
            string? filename = await Platform.OpenFileDialog("Select overlay image file", "All supported images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tga;*.psd;*.hdr");
            if (filename is null)
                return;

            Main.TryCatchReleaseException(() => Main.OverlayImage = Texture2D.FromFile(Main.Instance.GraphicsDevice, filename), "Could not load overlay image");
        }
        static async void CaptureClicked(UIButton btn, Empty _)
        {
            string? renderFile = await Platform.SaveFileDialog("Select render save file", "PNG Image|*.png");
            if (renderFile is null)
                return;

            Main.MainThreadQueue.Enqueue(() =>
            {
                var capResult = Capture.CaptureMap();
                IImageEncoder encoder = new PngEncoder();
                using FileStream fs = File.Create(renderFile);
                capResult.Save(fs, encoder);
                capResult.Dispose();
                GC.Collect();
            });
        }
        static async void CaptureLayeredClicked(UIButton btn, Empty _)
        {
            if (Main.Region is null)
                return;

            string? renderDir = await Platform.FolderBrowserDialog("Select render save folder");
            if (renderDir is null)
                return;

            Main.MainThreadQueue.Enqueue(() =>
            {
                Capture.CaptureMapLayered(renderDir);
                GC.Collect();
            });
        }
        internal static async Task<bool> SelectRegionClicked()
        {
            SlugcatSelect.Result? slugcat = await SlugcatSelect.ShowDialog();
            if (!slugcat.HasValue)
                return false;

            RegionSelect.Result? region = await RegionSelect.ShowDialog(slugcat.Value.Slugcat);
            if (!region.HasValue)
                return false;

            Main.SelectedSlugcat = slugcat.Value.Slugcat;
            InterfaceState.DrawSlugcatIcons.Value = slugcat.Value.Slugcat is null;
            RWAssets.EnableMods = !region.Value.ExcludeMods;
            await Main.LoadRegion(region.Value.Region.Id);
            return true;
        }

        static void CreateModals()
        {
            if (ModalCreators is null)
            {
                Type modalType = typeof(Modal<,>);
                ModalCreators = new();
                foreach (Type type in Assembly.GetExecutingAssembly().GetExportedTypes())
                {
                    if (type.BaseType is null || !type.BaseType.IsGenericType || type.BaseType.GetGenericTypeDefinition() != modalType)
                        continue;

                    MethodInfo? creatorMethod = type.BaseType.GetMethod("CreateUIElement", BindingFlags.Public | BindingFlags.Static, Array.Empty<Type>());
                    if (creatorMethod is null || creatorMethod.ReturnType != typeof(UIModal))
                        continue;

                    Func<UIModal> creator = creatorMethod.CreateDelegate<Func<UIModal>>();
                    ModalCreators.Add(creator);
                }
            }
            if (Root is not null)
                foreach (Func<UIModal> creator in ModalCreators)
                    Root.Elements.Add(creator());
        }
        internal static void ModalClosed()
        {
            CurrentModal = null;
            while (ModalWaitTasks.TryDequeue(out TaskCompletionSource? waitingTask))
            {
                waitingTask.SetResult();
                if (CurrentModal is not null)
                    return;
            }
        }

        public static async Task WaitModal()
        {
            if (CurrentModal is null)
                return;

            TaskCompletionSource waitingTask = new();
            ModalWaitTasks.Enqueue(waitingTask);

            await waitingTask.Task;
        }

        public static void ActiveInstallChanged()
        {
            foreach (var (install, panel) in InstallsPanels)
            {
                if (install == RWAssets.CurrentInstallation)
                {
                    panel.BorderColor = Color.Lime;
                    panel.HoverBackColor = panel.BackColor;
                    panel.HoverBorderColor = Color.Lime;
                }
                else
                {
                    panel.BorderColor = new(100, 100, 100);
                    panel.HoverBackColor = new(.3f, .3f, .3f);
                    panel.HoverBorderColor = Color.Green;
                }
            }
        }
        public static void PopulateInstallations()
        {
            if (InstallsList is null)
                return;

            InstallsPanels.Clear();
            InstallsList.Elements.Clear();

            foreach (RainWorldInstallation install in RWAssets.Installations)
            {
                RainWorldInstallation inst = install;
                UIHoverPanel panel = new()
                {
                    Padding = 4,
                    Height = 60,

                    Elements =
                    {
                        new UIButton()
                        {
                            Visible = inst.CanSave,
                            Width = 18,
                            Height = 18,
                            Top = new(0, 1, -1),
                            Left = new(0, 1, -1),
                            AutoSize = false,
                            Text = "D",
                        }.OnEvent(UIElement.ClickEvent, (_, _) => RWAssets.RemoveInstallation(inst)),
                        new UILabel
                        {
                            Top = 0,
                            Height = 20,
                            Text = install.Name,
                            AutoSize = false,
                            WordWrap = false,
                        },
                        new UILabel
                        {
                            Top = 20,
                            Height = 20,
                            Text = install.Path,
                            TextAlign = new(1, 0),
                            MaxWidth = new(0, 1),
                            Width = 0,
                            AutoSize = true,
                            WordWrap = false,
                        },
                        new UILabel
                        {
                            Top = 40,
                            Height = 20,
                            Text = install.GetFeaturesString(),
                            AutoSize = false,
                            WordWrap = false,
                        }
                    }
                };

                panel.OnEvent(UIElement.ClickEvent, (panel, _) =>
                {
                    if (inst == RWAssets.CurrentInstallation || panel.Root?.Hover is IHoverable or null)
                        return;

                    RWAssets.SetActiveInstallation(inst);
                });

                InstallsList.Elements.Add(panel);
                InstallsPanels.Add((install, panel));
            }

            ActiveInstallChanged();

            InstallsList.Recalculate();
        }
        public static void RegionChanged(Region region)
        {
            if (SubregionColorList is not null)
            {
                SubregionColorList.Elements.Clear();

                SubregionColorList.Elements.Add(new UIButton
                {
                    TextAlign = new(.5f),
                    Height = 20,
                    Text = "Reset colors",
                    BorderColor = new(.3f, .3f, .3f)
                }.OnEvent(UIElement.ClickEvent, (_, _) => Main.Region?.ResetSubregionColors()));

                foreach (Subregion subregion in region.Subregions)
                {
                    UIPanel panel = new()
                    {
                        Height = 72,
                        Padding = 3,

                        Elements =
                        {
                            new UILabel
                            {
                                Text = subregion.Name.Length == 0 ? "Main region" : subregion.Name,
                                Height = 20,
                                TextAlign = new(.5f)
                            },
                            new UIButton
                            {
                                Top = 20,
                                Height = 20,
                                Text = "Set background color",
                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (_, _) => ColorSelector.Show("Background color", subregion.BackgroundColor, (_, color) =>
                            {
                                subregion.BackgroundColor = color;
                                Main.Region?.MarkRoomTilemapsDirty();
                            })),
                            new UIButton
                            {
                                Top = 45,
                                Height = 20,
                                Text = "Set water color",
                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (_, _) => ColorSelector.Show("Water color", subregion.WaterColor, (_, color) =>
                            {
                                subregion.WaterColor = color;
                                Main.Region?.MarkRoomTilemapsDirty();
                            }))
                        }
                    };

                    SubregionColorList.Elements.Add(panel);
                }

                SubregionColorList.Recalculate();
            }

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
                            }.OnEvent(UIElement.ClickEvent, (btn, _) => obj.ActiveProperty.Value = btn.Selected),
                        }
                    };
                    panel.OnEvent(UIElement.ClickEvent, (p, _) => { if (p.Root?.Hover is not UIButton) Main.FocusOnObject(obj); });
                    MapObjectVisibilityList.Elements.Add(panel);
                }
            }
        }

        static void AddKeyComboPanel(UIList list, Keybind keybind, List<KeybindInput> inputs)
        {
            UIPanel panel = new()
            {
                Height = 18,

                BackColor = Color.Transparent,
                BorderColor = Color.Transparent,

                Elements =
                {
                    new UIPanel
                    {
                        Height = 18,
                        Width = new(-20, 1),
                        BackColor = new(48, 48, 48),
                        BorderColor = new(100, 100, 100),

                        Elements =
                        {
                            new UILabel
                            {
                                Left = 3,
                                Top = 1,
                                Width = new(-3, 1),
                                Text = string.Join(" + ", inputs.Select(i => i.KeyName)),
                                TextAlign = new(.5f),
                                AutoSize = false,
                            },
                        }
                    },
                }
            };

            panel.Elements.Add(new UIButton
            {
                Text = "D",
                Height = 18,
                Width = 18,
                Left = new(0, 1, -1),
            }.OnEvent(UIElement.ClickEvent, (_, _) =>
            {
                list.Elements.Remove(panel);
                keybind.Inputs.Remove(inputs);
                list.Recalculate();
                InputHandler.SaveKeybinds();
            }));

            list.Elements.Add(panel);
        }

        public static void Update()
        {
            Root?.Update();

            if (InputHandler.ReinitUI.JustPressed)
                Init();

            if (Main.SelectedObjects.Count != 1)
                ConfigurableObject = null;
            else
                ConfigurableObject = Main.SelectedObjects.First();
        }
        public static void Draw()
        {
            Root?.Draw(Main.SpriteBatch);
        }
    }
}

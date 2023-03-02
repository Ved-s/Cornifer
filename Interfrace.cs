using Cornifer.MapObjects;
using Cornifer.UI;
using Cornifer.UI.Elements;
using Cornifer.UI.Modals;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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

        public static UIList SubregionColorList = null!;
        public static Dictionary<string, UIButton> VisibilityPlacedObjects = new();
        public static Dictionary<RenderLayers, UIButton> VisibilityRenderLayers = new();

        public static bool Hovered => Root?.Hover is not null;
        public static bool Active => Root?.Active is not null;
        public static bool BlockUIHover => Main.Selecting || Main.Dragging || InputHandler.Pan.Pressed && !Hovered;

        static UIElement? ConfigElement;
        static MapObject? configurableObject;

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
                    RegionSelect.CreateUIElement(),
                    SlugcatSelect.CreateUIElement(),
                    AddIconSelect.CreateUIElement(),
                    TextFormatting.CreateUIElement(),
                    KeybindSelector.CreateUIElement(),

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
                                    }
                                }
                            }
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
                        SlugcatSelect.Show();
                        SlugcatSelect.Result? slugcat = await SlugcatSelect.Task;
                        if (!slugcat.HasValue)
                            return;

                        RegionSelect.Show();
                        RegionSelect.Result? region = await RegionSelect.Task;
                        if (!region.HasValue)
                            return;

                        Main.SelectedSlugcat = slugcat.Value.Slugcat;
                        InterfaceState.DrawSlugcatIcons.Value = slugcat.Value.Slugcat is null;
                        Main.LoadRegion(region.Value.Path);
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

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => AddIconSelect.Show()),

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
                                    }.OnEvent(UIElement.ClickEvent, (_, _) => Main.OpenState()),
                                    new UIButton
                                    {
                                        Top = 20,
                                        Left = new(1, .33f),
                                        Width = new(-1, .33f),
                                        Height = new(-20, 1),
                                        Text = "Save",
                                        TextAlign = new(.5f)
                                    }.OnEvent(UIElement.ClickEvent, (_, _) => Main.SaveState()),
                                    new UIButton
                                    {
                                        Top = 20,
                                        Left = new(2, .66f),
                                        Width = new(-2, .34f),
                                        Height = new(-20, 1),
                                        Text = "Save as",
                                        TextAlign = new(.5f)
                                    }.OnEvent(UIElement.ClickEvent, (_, _) => Main.SaveStateAs())
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

                            new UIResizeablePanel
                            {
                                Height = 150,

                                Padding = 4,

                                CanGrabTop = false,
                                CanGrabLeft = false,
                                CanGrabRight = false,
                                CanGrabBottom = true,

                                Elements =
                                {
                                    new UILabel
                                    {
                                        Text = "Room objects",
                                        Height = 15,
                                        TextAlign = new(.5f)
                                    },
                                    new UIList
                                    {
                                        Top = 20,
                                        Height = new(-20, 1),
                                        ElementSpacing = 4
                                    }.Execute((list) =>
                                    {
                                        VisibilityPlacedObjects.Clear();

                                        foreach (string objectName in PlacedObject.AllObjectTypes.OrderBy(s => s))
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
                            },

                            new UIResizeablePanel
                            {
                                Height = 120,

                                Padding = 4,

                                CanGrabTop = false,
                                CanGrabLeft = false,
                                CanGrabRight = false,
                                CanGrabBottom = true,

                                Elements =
                                {
                                    new UILabel
                                    {
                                        Text = "Map layers",
                                        Height = 15,
                                        TextAlign = new(.5f)
                                    },
                                    new UIList
                                    {
                                        Top = 20,
                                        Height = new(-20, 1),
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
                        foreach (InputHandler.Keybind keybind in InputHandler.Keybinds.Values)
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

                            foreach (List<InputHandler.KeybindInput> inputs in keybind.Inputs)
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
                                List<InputHandler.KeybindInput>? inputs = await KeybindSelector.Task;
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

        static void SelectOverlayClicked(UIButton btn, Empty _)
        {
            string? filename = null;
            Thread thd = new(() =>
            {
                System.Windows.Forms.OpenFileDialog ofd = new();
                ofd.Title = "Select overlay image file";
                ofd.Filter = "All supported images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tga;*.psd;*.hdr";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    filename = ofd.FileName;
            });
            thd.SetApartmentState(ApartmentState.STA);
            thd.Start();
            thd.Join();

            if (filename is null)
                return;

            try
            {
                Main.OverlayImage = Texture2D.FromFile(Main.Instance.GraphicsDevice, filename);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString(), "Could not load overlay image");
            }
        }
        static void CaptureClicked(UIButton btn, Empty _)
        {
            if (Main.Region is null)
                return;

            string? renderFile = null;
            Thread thd = new(() =>
            {
                System.Windows.Forms.SaveFileDialog sfd = new();
                sfd.Title = "Select render save file";
                sfd.Filter = "PNG Image|*.png";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    renderFile = sfd.FileName;
            });
            thd.SetApartmentState(ApartmentState.STA);
            thd.Start();
            thd.Join();

            if (renderFile is not null)
            {
                var capResult = Capture.CaptureMap();
                IImageEncoder encoder = new PngEncoder();
                using FileStream fs = File.Create(renderFile);
                capResult.Save(fs, encoder);
                capResult.Dispose();
                GC.Collect();
            }
        }
        static void CaptureLayeredClicked(UIButton btn, Empty _)
        {
            if (Main.Region is null)
                return;

            string? renderDir = null;
            Thread thd = new(() =>
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new();
                fbd.Description = "Select render save folder";
                fbd.ShowNewFolderButton = true;
                fbd.UseDescriptionForTitle = true;
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    renderDir = fbd.SelectedPath;
            });
            thd.SetApartmentState(ApartmentState.STA);
            thd.Start();
            thd.Join();

            if (renderDir is not null)
            {
                Capture.CaptureMapLayered(renderDir);
                GC.Collect();
            }
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

                foreach (Region.Subregion subregion in region.Subregions)
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
        }

        static void AddKeyComboPanel(UIList list, InputHandler.Keybind keybind, List<InputHandler.KeybindInput> inputs)
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

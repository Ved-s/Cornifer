using Cornifer.UI;
using Cornifer.UI.Elements;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading;

namespace Cornifer
{
    public static class Interface
    {
        public static UIRoot? Root;

        public static UIModal RegionSelect = null!;
        public static UIModal SlugcatSelect = null!;
        public static UIModal AddIconSelect = null!;
        public static UIModal TextFormatting = null!;

        public static UIPanel SidePanel = null!;
        public static UIButton SlugcatIcons = null!;
        public static ColorSelector ColorSelector = null!;

        public static UILabel NoConfigObjectLabel = null!;
        public static UILabel NoConfigLabel = null!;
        public static UIPanel ConfigPanel = null!;

        public static UIList SubregionColorList = null!;

        public static bool Hovered => Root?.Hover is not null;
        public static bool Active => Root?.Active is not null;
        public static bool BlockUIHover => Main.Selecting || Main.Dragging || Main.MouseState.RightButton == ButtonState.Pressed && !Hovered;

        static bool regionSelectVisible = false;
        static bool slugcatSelectVisible = true;
        static bool addIconSelectVisible = false;
        static bool textFormattingVisible = false;

        public static bool RegionSelectVisible
        {
            get => regionSelectVisible;
            set { regionSelectVisible = value; if (RegionSelect is not null) RegionSelect.Visible = value; }
        }
        public static bool SlugcatSelectVisible
        {
            get => slugcatSelectVisible;
            set { slugcatSelectVisible = value; if (SlugcatSelect is not null) SlugcatSelect.Visible = value; }
        }
        public static bool AddIconSelectVisible
        {
            get => addIconSelectVisible;
            set { addIconSelectVisible = value; if (AddIconSelect is not null) AddIconSelect.Visible = value; }
        }
        public static bool TextFormattingVisible
        {
            get => textFormattingVisible;
            set { textFormattingVisible = value; if (TextFormatting is not null) TextFormatting.Visible = value; }
        }

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

        static (bool format, string text)[] FormattingInfo = new[]
        {
            (false,
            "Cornifer supports text formatting similar to BBCode.\n" +
            "Format consists of tags, formatted [tagName:tagData]tagContent[/tagName] or just [tagName:tagData].\n" +
            "Tags can be inside other tags. If tag is never closed, it will apply to the rest of the text.\n" +
            "Current tag list:\n"),

            (false, ""),
            (false,
            "[c:RRGGBB] Colored text\n" +
            "Color data can be RRGGBBAA, RRGGBB, RGB, or single grayscale hex letter."),
            (true, "\\[c:f00\\]Red text\\[/c\\] - [c:f00]Red text[/c]"),

            (false, ""),
            (false,
            "[s:RRGGBB] Shaded text\n" +
            "Shade color data can be RRGGBBAA, RRGGBB, RGB, or single grayscale hex letter."),
            (true, "\\[s:0\\]Shaded text\\[/s\\] - [s:0]Shaded text[/s]"),

            (false, ""),
            (false,
            "[ns] Non-Shaded text\n" +
            "Removes text shade."),
            (true, "\\[s:0\\]Shaded and \\[ns\\]non-shaded\\[/ns\\] text\\[/s\\] - [s:0]Shaded and [ns]non-shaded[/ns] text[/s]"),

            (false, ""),
            (false,
            "[i] Italic text\n" +
            "Makes text appear italic."),
            (true, "\\[i\\]Italic text\\[/i\\] - [i]Italic text[/i]"),

            (false, ""),
            (false,
            "[b] Bold text\n" +
            "Makes text appear bold."),
            (true, "\\[b\\]Bold text\\[/b\\] - [b]Bold text[/b]"),

            (false, ""),
            (false,
            "[u] Underlined text\n" +
            "Makes text underlined."),
            (true, "\\[u\\]Underlined text\\[/u\\] - [u]Underlined text[/u]"),

            (false, ""),
            (false,
            "[sc:float] Scaled text\n" +
            "Scales text."),
            (true, "\\[sc:0.5\\]Small text\\[/sc\\] and \\[sc:2\\]big text\\[/sc\\] - [sc:0.5]Small text[/sc] and [sc:2]big text[/sc]"),

            (false, ""),
            (false,
            "[a:float] Aligned text\n" +
            "Makes text aligned with text before by some value."),
            (true, "\\[sc:2\\]Big text,\\[/sc\\] normal \\[a:.6\\]and aligned\\[/a\\] - [sc:2]Big text,[/sc] normal [a:.6]and aligned[/a]"),

            (false, ""),
            (false,
            "[ic:name] [ic:name:color] Icon (this tag does not need to be closed)\n" +
            "Draws icons, found in \"Add icons to map\" menu."),
            (true, "Slugcat \\[ic:Slugcat_White\\] and their bat \\[ic:batSymbol:0\\] - Slugcat [ic:Slugcat_White] and their bat [ic:batSymbol:0]"),
        };

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
                    new UIModal
                    {
                        Top = new(0, .5f, -.5f),
                        Left = new(0, .5f, -.5f),

                        Width = 300,
                        Height = new(0, .9f),

                        Margin = 5,
                        Padding = new(5, 40),

                        Visible = RegionSelectVisible,

                        Elements =
                        {
                            new UILabel()
                            {
                                Top = 10,
                                Height = 20,

                                Text = "Select region",
                                TextAlign = new(.5f)
                            },
                            new UIList()
                            {
                                Top = 40,
                                Height = new(-100, 1),
                                ElementSpacing = 5,

                            }.Execute(list =>
                            {
                                foreach (var (id, name, path) in Main.FindRegions())
                                {
                                    list.Elements.Add(new UIButton
                                    {
                                        Text = $"{name} ({id})",
                                        Height = 20,
                                        TextAlign = new(.5f)
                                    }.OnEvent(UIElement.ClickEvent, (_, _) =>
                                    {
                                        RegionSelectVisible = false;
                                        Main.LoadRegion(path);
                                    }));
                                }

                                list.Recalculate();
                            }),
                            new UIButton
                            {
                                Top = new(-50, 1),

                                Height = 20,
                                Text = "Manual select",
                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                            {
                                Thread dirSelect = new(() =>
                                {
                                    System.Windows.Forms.FolderBrowserDialog fd = new();
                                    fd.UseDescriptionForTitle = true;
                                    fd.Description = "Select Rain World region folder. For example RainWorld_Data/StreamingAssets/world/su";
                                    if (fd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                                        return;

                                    RegionSelectVisible = false;
                                    Main.LoadRegion(fd.SelectedPath);
                                });
                                dirSelect.SetApartmentState(ApartmentState.STA);
                                dirSelect.Start();
                                dirSelect.Join();
                            }),
                            new UIButton
                            {
                                Top = new(-20, 1),
                                Left = new(0, .5f, -.5f),
                                Width = 80,
                                Height = 20,
                                Text = "Close",
                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (_, _) => RegionSelectVisible = false)
                        }
                    }.Assign(out RegionSelect),

                    new UIModal
                    {
                        Top = new(0, .5f, -.5f),
                        Left = new(0, .5f, -.5f),

                        Width = 200,
                        Height = 100,

                        Margin = 5,
                        Padding = new(5, 40),

                        Visible = SlugcatSelectVisible,

                        Elements =
                        {
                            new UILabel
                            {
                                Top = 15,
                                Height = 20,
                                Text = "Select slugcat",
                                TextAlign = new(.5f)
                            },
                            new UIButton
                            {
                                Top = new(-20, 1),
                                Left = new(0, .5f, -.5f),
                                Width = 80,
                                Height = 20,
                                Text = "Close",
                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (_, _) => SlugcatSelectVisible = false)
                        }

                    }.Execute(PopulateSlugcatSelect)
                    .Assign(out SlugcatSelect),

                    new UIModal
                    {
                        Top = new(0, .5f, -.5f),
                        Left = new(0, .5f, -.5f),

                        Width = new(0, .83f),
                        Height = new(0, .8f),

                        Margin = 5,
                        Padding = 5,

                        Visible = AddIconSelectVisible,

                        Elements =
                        {
                            new UILabel
                            {
                                Top = 10,
                                Height = 20,
                                Text = "Add icon to the map",
                                TextAlign = new(.5f)
                            },
                            new UIList
                            {
                                Top = 35,
                                Height = new(-60, 1),
                                Elements =
                                {
                                    new UIFlow
                                    {
                                        ElementSpacing = 5
                                    }
                                    .Execute(PopulateObjectSelect),
                                }
                            },

                            new UILabel
                            {
                                Top = new(-15, 1),
                                Height = 20,
                                Width = new(-80, 1),
                                Text = "Hold Shift to add multiple icons. To delete icons, select them and press Delete.",
                                TextAlign = new(.5f)
                            },
                            new UIButton
                            {
                                Top = new(-20, 1),
                                Left = new(-80, 1),
                                Width = 80,
                                Height = 20,
                                Text = "Close",
                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (_, _) => AddIconSelectVisible = false)
                        }

                    }
                    .Assign(out AddIconSelect),

                    new UIModal
                    {
                        Top = new(0, .5f, -.5f),
                        Left = new(0, .5f, -.5f),

                        Width = new(0, .9f),
                        Height = new(0, .9f),

                        Margin = 5,
                        Padding = 5,

                        Visible = TextFormattingVisible,

                        Elements =
                        {
                            new UILabel
                            {
                                Top = 10,
                                Height = 20,
                                Text = "Text formatting",
                                TextAlign = new(.5f)
                            },
                            new UIList
                            {
                                Top = 35,
                                Height = new(-60, 1),
                            }.Execute(GenerateFormattingInfoList),

                            new UIButton
                            {
                                Top = new(-20, 1),
                                Left = new(0, .5f, -.5f),
                                Width = 80,
                                Height = 20,
                                Text = "Close",
                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (_, _) => TextFormattingVisible = false)
                        }

                    }
                    .Assign(out TextFormatting),

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
                                        Name = "Subregions",
                                        Element = InitSubregionsTab(),
                                    },
                                    new()
                                    {
                                        Name = "Config",
                                        Element = InitObjectConfigTab(),
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
                    }.OnEvent(UIElement.ClickEvent, (_, _) => SlugcatSelectVisible = true),

                    new UIList()
                    {
                        Top = 50,
                        Height = new(-100, 1),
                        ElementSpacing = 2,

                        Elements =
                        {
                            new UIButton
                            {
                                Height = 20,
                                Text = "Draw slugcat icons",

                                Selectable = true,
                                Selected = SlugcatIcon.DrawIcons,

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (btn, _) => SlugcatIcon.DrawIcons = btn.Selected)
                            .Assign(out SlugcatIcons),

                            new UIButton
                            {
                                Height = 20,
                                Text = "Use diamonds",

                                HoverText = "Draw diamonds instead of\nslugcat icons",

                                Selectable = true,
                                Selected = SlugcatIcon.DrawDiamond,

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (btn, _) => SlugcatIcon.DrawDiamond = btn.Selected),

                            new UIButton
                            {
                                Height = 20,
                                Text = "Draw room objects",

                                Selectable = true,
                                Selected = Room.DrawObjects,

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Room.DrawObjects = btn.Selected),

                            new UIButton
                            {
                                Height = 20,
                                Text = "Draw pickups",

                                HoverText = "Draw placed items",

                                Selectable = true,
                                Selected = Room.DrawPickUpObjects,

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Room.DrawPickUpObjects = btn.Selected),

                            new UIButton
                            {
                                Height = 20,

                                Selectable = true,
                                Selected = Room.DrawTileWalls,
                                Text = "Draw tile walls",

                                HoverText = "Render room tiles with walls",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
                            {
                                Room.DrawTileWalls = btn.Selected;
                                Main.Region?.MarkRoomTilemapsDirty();
                            }),

                            new UIButton
                            {
                                Height = 20,

                                Text = "Add icons to map",

                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => AddIconSelectVisible = true),

                            new UIButton
                            {
                                Height = 20,

                                Text = "Add text to map",

                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => 
                            {
                                Main.WorldObjects.Add(new MapText($"WorldText_{Random.Shared.Next():x}", Content.RodondoExt20, "Sample text")
                                {
                                    Shade = true,
                                    WorldPosition = Main.WorldCamera.Position + Main.WorldCamera.Size / Main.WorldCamera.Scale * .5f
                                });
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
                                        Text = $"Water transparency: {Room.WaterTransparency*100:0}%",
                                        TextAlign = new(0, .5f)
                                    }.Assign(out UILabel waterTransparencyLabel),
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
                                        ScrollPosition = Room.WaterTransparency,
                                    }.OnEvent(UIScrollBar.ScrollChanged, (_, scroll) =>
                                    {
                                        Room.WaterTransparency = scroll;
                                        waterTransparencyLabel.Text = $"Water transparency: {Room.WaterTransparency*100:0}%";
                                        Main.Region?.MarkRoomTilemapsDirty();
                                    })
                                }
                            },

                            new UIElement { Height = 20 },
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
                        Top = new(-25, 1),

                        Height = 25,
                        Text = "Capture region",

                        TextAlign = new(.5f)
                    }.OnEvent(UIElement.ClickEvent, CaptureClicked),
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

        static void PopulateSlugcatSelect(UIModal select)
        {
            float y = 50;

            foreach (string slugcat in Main.SlugCatNames)
            {
                UIButton button = new()
                {
                    Text = slugcat,
                    Height = 20,
                    TextAlign = new(.5f),
                    Top = y
                };
                button.OnEvent(UIElement.ClickEvent, (_, _) =>
                {
                    Main.SelectedSlugcat = slugcat;
                    SlugcatIcon.DrawIcons = false;
                    SlugcatIcons.Selected = SlugcatIcon.DrawIcons;
                    SlugcatSelectVisible = false;
                    RegionSelectVisible = true;
                });
                select.Elements.Add(button);

                y += 25;
            }

            UIButton all = new()
            {
                Text = "All",
                Height = 20,
                TextAlign = new(.5f),
                Top = y
            };
            all.OnEvent(UIElement.ClickEvent, (_, _) =>
            {
                Main.SelectedSlugcat = null;
                SlugcatIcon.DrawIcons = true;
                SlugcatIcons.Selected = SlugcatIcon.DrawIcons;
                SlugcatSelectVisible = false;
                RegionSelectVisible = true;
            });
            select.Elements.Add(all);

            y += 25;

            select.Height = y + 40;
        }
        static void PopulateObjectSelect(UIFlow list)
        {
            foreach (var (name, sprite) in GameAtlases.Sprites.OrderBy(kvp => kvp.Key))
            {
                UIHoverPanel panel = new()
                {
                    Width = 120,
                    Height = 100,

                    Padding = 3,

                    Elements =
                    {
                        new UIImage
                        {
                            Width = 114,
                            Height = 79,

                            Texture = sprite.Texture,
                            TextureColor = sprite.Color,
                            TextureFrame = sprite.Frame,
                        },
                        new UILabel
                        {
                            Top = new(-15, 1),
                            Height = 15,
                            Text = name,
                            TextAlign = new(.5f)
                        }
                    }
                };
                panel.OnEvent(UIElement.UpdateEvent, (panel, _) =>
                {
                    if (panel.Hovered && panel.Root.MouseLeftKey == KeybindState.JustPressed)
                    {
                        Main.WorldObjects.Add(new SimpleIcon($"WorldIcon_{name}_{Random.Shared.Next():x}", sprite)
                        {
                            WorldPosition = Main.WorldCamera.Position + Main.WorldCamera.Size / Main.WorldCamera.Scale * .5f
                        });

                        if (Root!.ShiftKey == KeybindState.Released)
                            AddIconSelectVisible = false;
                    }
                });

                list.Elements.Add(panel);
            }
        }

        static void GenerateFormattingInfoList(UIList list)
        {
            foreach (var (formatted, text) in FormattingInfo)
            {
                if (text == "")
                {
                    list.Elements.Add(new UIElement { Height = 20 });
                    continue;
                }

                if (formatted)
                {
                    list.Elements.Add(new UIFormattedLabel
                    {
                        Height = 0,
                        Width = 0,

                        Text = text,
                    });
                }
                else
                {
                    list.Elements.Add(new UILabel
                    {
                        WordWrap = true,
                        Height = 0,
                        Width = 0,

                        Text = text,
                    });
                }
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

        public static void RegionChanged(Region region)
        {
            if (SubregionColorList is not null)
            {
                SubregionColorList.Elements.Clear();

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

        public static void Update()
        {
            Root?.Update();

            if (Main.KeyboardState.IsKeyDown(Keys.F12) && Main.OldKeyboardState.IsKeyUp(Keys.F12))
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

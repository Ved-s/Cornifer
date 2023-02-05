using Cornifer.UI.Elements;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Cornifer
{
    public static class Interface
    {
        public static UIRoot? Root;

        public static UIModal RegionSelect = null!;
        public static UIModal SlugcatSelect = null!;
        public static UIModal AddIconSelect = null!;

        public static UIPanel SidePanel = null!;

        public static UIButton SlugcatIcons = null!;

        public static bool Hovered => Root?.Hover is not null;
        public static bool BlockUIHover => Main.Selecting || Main.Dragging || Main.MouseState.RightButton == ButtonState.Pressed && !Interface.Hovered;

        static bool regionSelectVisible = false;
        static bool slugcatSelectVisible = true;
        static bool addIconSelectVisible = false;

        public static bool RegionSelectVisible
        {
            get => regionSelectVisible;
            set { regionSelectVisible = value; RegionSelect.Visible = value; }
        }

        public static bool SlugcatSelectVisible
        {
            get => slugcatSelectVisible;
            set { slugcatSelectVisible = value; SlugcatSelect.Visible = value; }
        }

        public static bool AddIconSelectVisible
        {
            get => addIconSelectVisible;
            set { addIconSelectVisible = value; AddIconSelect.Visible = value; }
        }

        public static void Init()
        {
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
                            })
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
                            }
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
                            InitSidePanel()
                        }
                    }.Assign(out SidePanel),
                }
            };
        }

        static UIElement InitSidePanel()
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
                                Text = "Crop rooms",

                                Selectable = true,
                                Selected = Room.DrawCropped,

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)
                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Room.DrawCropped = btn.Selected),

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

                                Selectable = true,
                                Selected = Room.ForceWaterBehindSolid,
                                Text = "Force water behind terrain",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
                            {
                                Room.ForceWaterBehindSolid = btn.Selected;
                                Main.Region?.MarkRoomTilemapsDirty();
                            }),

                            new UIButton
                            {
                                Height = 20,

                                Text = "Add icons to map",

                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => AddIconSelectVisible = true),
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

            select.Height = y + 15;
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
                        Main.Region?.Icons.Add(new SimpleIcon(null, sprite)
                            {
                                Position = Main.WorldCamera.Position + Main.WorldCamera.Size / Main.WorldCamera.Scale * .5f
                            });

                        if (Root!.ShiftKey == KeybindState.Released)
                            AddIconSelectVisible = false;
                    }
                });

                list.Elements.Add(panel);
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
                var capResult = Capture.CaptureRegion(Main.Region);
                IImageEncoder encoder = new PngEncoder();
                using FileStream fs = File.Create(renderFile);
                capResult.Save(fs, encoder);
                capResult.Dispose();
                GC.Collect();
            }
        }

        public static void Update()
        {
            Root?.Update();

            if (Main.KeyboardState.IsKeyDown(Keys.F12) && Main.OldKeyboardState.IsKeyUp(Keys.F12))
                Init();
        }

        public static void Draw()
        {
            Root?.Draw(Main.SpriteBatch);
        }
    }
}

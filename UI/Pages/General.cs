using Cornifer.MapObjects;
using Cornifer.UI.Elements;
using Cornifer.UI.Modals;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Cornifer.UI.Pages
{
    public class General : Page
    {
        public override int Order => 1;

        public General()
        {
            Elements = new(this)
            {
                new UIButton
                {
                    Top = 0,

                    Height = 25,
                    Text = "Select region",

                    TextAlign = new(.5f)
                }.OnEvent(ClickEvent, async (_, _) => await SelectRegionClicked()),

                new UIList()
                {
                    Top = 35,
                    Height = new(-70, 1),
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

                            Selectable = true,
                            Text = "Use BG for shortcuts",

                            SelectedBackColor = Color.White,
                            SelectedTextColor = Color.Black,

                            TextAlign = new(.5f)

                        }.BindConfig(InterfaceState.RegionBGShortcuts),

                        new UIButton
                        {
                            Height = 20,

                            Text = "Disable better cutouts",

                            HoverText = "Sets \"Better tile cutouts\" to False for all rooms",

                            TextAlign = new(.5f)

                        }.OnEvent(ClickEvent, (_, _) =>
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

                        }.OnEvent(ClickEvent, async (btn, _) => await AddIconSelect.Show()),

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

                        }.OnEvent(ClickEvent, SelectOverlayClicked),

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
                                }.OnEvent(ClickEvent, async (_, _) => await Main.OpenState()),
                                new UIButton
                                {
                                    Top = 20,
                                    Left = new(1, .33f),
                                    Width = new(-1, .33f),
                                    Height = new(-20, 1),
                                    Text = "Save",
                                    TextAlign = new(.5f)
                                }.OnEvent(ClickEvent, async (_, _) => await Main.SaveState()),
                                new UIButton
                                {
                                    Top = 20,
                                    Left = new(2, .66f),
                                    Width = new(-2, .34f),
                                    Height = new(-20, 1),
                                    Text = "Save as",
                                    TextAlign = new(.5f)
                                }.OnEvent(ClickEvent, async (_, _) => await Main.SaveStateAs())
                            }
                        },
                        new UIButton
                        {
                            Height = 20,

                            Text = "Select background color",
                            HoverText = "Select background color for the editor.\nThis does not affect resulting map.",
                            TextAlign = new(.5f)

                        }.OnEvent(ClickEvent, (_, _) => Interface.ColorSelector.Show("Background", new(null, Profile.Current.BackgroundColor, false), (r, c) => 
                        {
                            Profile.Current.BackgroundColor = c.Color;
                            if (r is true) {
                                Profile.Save();
                            }
                        })),
                    }
                },

                new UIButton
                {
                    Top = new(-25, 1),

                    Height = 25,
                    Text = "Capture map",

                    TextAlign = new(.5f)
                }.OnEvent(ClickEvent, async (_, _) => await CaptureSave.Show()),
            };
        }

        static async void SelectOverlayClicked(UIButton btn, Empty _)
        {
            string? filename = await Platform.OpenFileDialog("Select overlay image file", "All supported images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tga;*.psd;*.hdr");
            if (filename is null)
                return;

            Main.TryCatchReleaseException(() => Main.OverlayImage = Texture2D.FromFile(Main.Instance.GraphicsDevice, filename), "Could not load overlay image");
        }

        public static async Task<bool> SelectRegionClicked()
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
            await Main.LoadRegion(region.Value.Region);
            return true;
        }
    }
}

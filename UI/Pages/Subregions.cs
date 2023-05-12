using Cornifer.Structures;
using Cornifer.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Pages
{
    public class Subregions : Page
    {
        public static UIList SubregionColorList = null!;

        public override int Order => 3;

        public Subregions() 
        {
            Elements = new(this)
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
            };
        }

        internal static void RegionChanged(Region region)
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
                            }.OnEvent(UIElement.ClickEvent, (_, _) => Interface.ColorSelector.Show("Background color", subregion.BackgroundColor, (_, color) =>
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
                            }.OnEvent(UIElement.ClickEvent, (_, _) => Interface.ColorSelector.Show("Water color", subregion.WaterColor, (_, color) =>
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
    }
}

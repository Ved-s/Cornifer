using Cornifer.Structures;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer.UI.Modals
{
    public class RegionSelect : Modal<RegionSelect, RegionSelect.Result?>
    {
        UIList RegionList;
        static Slugcat? Slugcat;
        static bool DisableModRegionsValue;

        public RegionSelect()
        {
            Width = 300;
            Height = new(0, .9f);

            Margin = 5;
            Padding = new(5, 40);

            Elements = new(this)
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

                }.Assign(out RegionList),
                new UIButton
                {
                    Top = new(-50, 1),
                
                    Height = 20,
                    Text = "Disable mod regions",
                    Selectable = true,
                    Selected = DisableModRegionsValue,

                    SelectedBackColor = Color.White,
                    SelectedTextColor = Color.Black,

                    TextAlign = new(.5f)
                
                }.OnEvent(ClickEvent, (btn, _) =>
                {
                    DisableModRegionsValue = btn.Selected;
                    RebuildRegionList();
                }),
                new UIButton
                {
                    Top = new(-20, 1),
                    Left = new(0, .5f, -.5f),
                    Width = 80,
                    Height = 20,
                    Text = "Close",
                    TextAlign = new(.5f)
                }.OnEvent(ClickEvent, (_, _) => ReturnResult(null))
            };
        }

        public static async Task Show(Slugcat? slugcat)
        {
            Slugcat = slugcat;
            await Show();
        }

        public static async Task<Result?> ShowDialog(Slugcat? slugcat)
        {
            Slugcat = slugcat;
            await Show();
            return await Task;
        }

        protected override void Shown()
        {
            RebuildRegionList();
        }

        private void RebuildRegionList()
        {
            bool enableMods = RWAssets.EnableMods;
            RWAssets.EnableMods = !DisableModRegionsValue;

            RegionList.Elements.Clear();

            if (RWAssets.CurrentInstallation is null)
            {
                RegionList.Elements.Add(new UILabel
                {
                    Text = "Installation not selected",
                    TextAlign = new(.5f, 1f),
                    Height = 20,
                });

                return;
            }

            HashSet<string> foundMods = new();

            string? slugcatWorldName = Slugcat?.WorldStateSlugcat;
            foreach (var group in RWAssets.FindRegions(Slugcat).GroupBy(reg => reg.Mod))
            {
                foundMods.Add(group.Key.Id);
                RegionList.Elements.Add(new UILabel
                {
                    Text = group.Key.Name,
                    TextAlign = new(.5f, 1f),
                    Height = 20,
                });

                foreach (RegionInfo region in group)
                {
                    bool accessible = Slugcat is null
                        || (StaticData.SlugcatRegionAvailability.GetValueOrDefault(slugcatWorldName!)?.Contains(region.Id)
                        ?? StaticData.SlugcatRegionAvailability.GetValueOrDefault("")?.Contains(region.Id)
                        ?? true);

                    RegionList.Elements.Add(new UIButton
                    {
                        Text = $"{region.Displayname} ({region.Id})",
                        Height = 20,
                        TextAlign = new(.5f),
                        BorderColor = accessible ? new(100, 100, 100) : Color.Maroon
                    }.OnEvent(ClickEvent, (_, _) =>
                    {
                        ReturnResult(new()
                        {
                            Region = region,
                            ExcludeMods = DisableModRegionsValue
                        });
                    }));
                }
            }

            if (!DisableModRegionsValue)
            {
                foreach (RWMod mod in RWAssets.Mods)
                {
                    if (mod.Active || foundMods.Contains(mod.Id))
                        continue;

                    string modWorld = Path.Combine(mod.Path, "world");
                    if (!Directory.Exists(modWorld))
                        continue;

                    RegionList.Elements.Add(new UILabel
                    {
                        Text = $"{mod.Name} is disabled",
                        TextAlign = new(.5f),
                        Height = 20,
                    });
                }
            }

            RegionList.Recalculate();
            RWAssets.EnableMods = enableMods;
        }

        public struct Result
        {
            public RegionInfo Region;
            public bool ExcludeMods;
        }
    }
}

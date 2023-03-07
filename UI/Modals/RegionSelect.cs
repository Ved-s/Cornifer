using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cornifer.UI.Modals
{
    public class RegionSelect : Modal<RegionSelect, RegionSelect.Result?>
    {
        UIList RegionList;
        static string? Slugcat;

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
                    Text = "Manual select",
                    TextAlign = new(.5f)
                
                }.OnEvent(ClickEvent, async (_, _) =>
                {
                    string? regionPath = await Platform.FolderBrowserDialog("Select Rain World region folder. For example RainWorld_Data/StreamingAssets/world/su");
                    if (regionPath is not null)
                        ReturnResult(new() { Path = regionPath });
                }),
                new UIButton
                {
                    Top = new(-20, 1),
                    Left = new(0, .5f, -.5f),
                    Width = 80,
                    Height = 20,
                    Text = "Close",
                    TextAlign = new(.5f)
                }.OnEvent(UIElement.ClickEvent, (_, _) => ReturnResult(null))
            };
        }

        public static void Show(string? slugcat)
        {
            Slugcat = slugcat;
            Show();
        }

        public static async Task<Result?> ShowAsync(string? slugcat)
        {
            Slugcat = slugcat;
            Show();
            return await Task;
        }

        protected override void Shown()
        {
            RegionList.Elements.Clear();
            foreach (var (id, name, path) in Main.FindRegions(Slugcat))
            {
                bool accessible = Slugcat is null
                    || (StaticData.SlugcatRegionAvailability.GetValueOrDefault(Slugcat)?.Contains(id)
                    ?? StaticData.SlugcatRegionAvailability.GetValueOrDefault("")?.Contains(id)
                    ?? true);

                RegionList.Elements.Add(new UIButton
                {
                    Text = $"{name} ({id})",
                    Height = 20,
                    TextAlign = new(.5f),
                    BorderColor = accessible ? new(100, 100, 100) : Color.Maroon
                }.OnEvent(ClickEvent, (_, _) =>
                {
                    ReturnResult(new()
                    {
                        Path = path
                    });
                }));
            }

            RegionList.Recalculate();
        }

        public struct Result
        {
            public string Path;
        }
    }
}

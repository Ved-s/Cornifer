using Cornifer.UI.Elements;

namespace Cornifer.UI.Modals
{
    public class SlugcatSelect : Modal<SlugcatSelect, SlugcatSelect.Result?>
    {
        public SlugcatSelect()
        {
            Width = 200;
            Height = 100;

            Margin = 5;
            Padding = new(5, 40);

            Elements = new(this)
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
                }.OnEvent(UIElement.ClickEvent, (_, _) => ReturnResult(null))
            };

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
                    ReturnResult(new()
                    {
                        Slugcat = slugcat
                    });
                });
                Elements.Add(button);

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
                ReturnResult(new()
                {
                    Slugcat = null
                });
            });
            Elements.Add(all);

            y += 25;

            Height = y + 40;
        }

        public struct Result
        {
            public string? Slugcat;
        }
    }
}

using Cornifer.Structures;
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
            Padding = new(5, 25);

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
                }.OnEvent(ClickEvent, (_, _) => ReturnResult(null))
            };

            float y = 50;

            foreach (SlugcatData slugcat in StaticData.Slugcats)
            {
                UIButton button = new()
                {
                    Text = slugcat.Name,
                    Height = 20,
                    Left = 25,
                    Width = new(-25, 1),
                    TextAlign = new(.5f),
                    Top = y
                };
                button.OnEvent(ClickEvent, (_, _) =>
                {
                    ReturnResult(new()
                    {
                        Slugcat = slugcat.Id
                    });
                });
                Elements.Add(button);

                AtlasSprite? slugcatSprite = SpriteAtlases.GetSpriteOrNull($"Slugcat_{slugcat.Id}");
                if (slugcatSprite is not null)
                {
                    Elements.Add(new UIImage
                    {
                        Texture = slugcatSprite.Texture,
                        TextureFrame = slugcatSprite.Frame,
                        TextureColor = slugcatSprite.Color,

                        Height = 20,
                        Width = 20,
                        Top = y
                    });
                }
                y += 25;
            }

            UIButton all = new()
            {
                Text = "All",
                Height = 20,
                TextAlign = new(.5f),
                Top = y
            };
            all.OnEvent(ClickEvent, (_, _) =>
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

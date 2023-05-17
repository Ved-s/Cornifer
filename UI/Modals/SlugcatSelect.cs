using Cornifer.Structures;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;

namespace Cornifer.UI.Modals
{
    public class SlugcatSelect : Modal<SlugcatSelect, SlugcatSelect.Result?>
    {
        UILabel Label;
        UIButton Close;
        UIList List;

        public SlugcatSelect()
        {
            Width = 200;
            Height = new(0, .9f);

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
                }.Assign(out Label),
                new UIList 
                {
                    ElementSpacing = 4,
                    Top = 50,
                    Height = new(-90, 1),
                }.Assign(out List),
                new UIButton
                {
                    Top = new(-20, 1),
                    Left = new(0, .5f, -.5f),
                    Width = 80,
                    Height = 20,
                    Text = "Close",
                    TextAlign = new(.5f)
                }.OnEvent(ClickEvent, (_, _) => ReturnResult(null))
                .Assign(out Close)
            };
        }

        protected override void Shown()
        {
            List.Elements.Clear();

            foreach (Slugcat slugcat in StaticData.Slugcats)
            {
                UIPanel panel = new()
                {
                    BackColor = Color.Transparent,
                    BorderColor = Color.Transparent,

                    Height = 20,

                    Elements = 
                    {
                        new UIButton 
                        {
                            Text = slugcat.Name,
                            Left = 25,
                            Width = new(-25, 1),
                            TextAlign = new(.5f),
                            AutoSize = false
                        }.OnEvent(ClickEvent, (_, _) =>
                        {
                            ReturnResult(new()
                            {
                                Slugcat = slugcat
                            });
                        })
                    }
                };

                AtlasSprite? slugcatSprite = SpriteAtlases.GetSpriteOrNull($"Slugcat_{slugcat.Id}");
                if (slugcatSprite is not null)
                {
                    panel.Elements.Add(new UIImage
                    {
                        Texture = slugcatSprite.Texture,
                        TextureFrame = slugcatSprite.Frame,
                        TextureColor = slugcatSprite.Color,

                        Width = 20,
                    });
                }

                List.Elements.Add(panel);
            }

            List.Elements.Add(new UIButton 
            {
                Text = "All",
                Height = 20,
                TextAlign = new(.5f),
            }.OnEvent(ClickEvent, (_, _) =>
            {
                ReturnResult(new()
                {
                    Slugcat = null
                });
            }));
            Recalculate();
        }

        public struct Result
        {
            public Slugcat? Slugcat;
        }
    }
}

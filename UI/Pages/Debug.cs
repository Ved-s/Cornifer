using Cornifer.MapObjects;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cornifer.UI.Pages
{
    public class Debug : Page
    {
        public override int Order => 100;
        public override bool IsDebugOnly => true;

        public Debug()
        {
            Elements = new(this)
            {
                new UIList
                {
                    ElementSpacing = 4,

                    Elements =
                    {
                        new UIButton
                            {
                                Text = "Test diamond placement",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                        {
                            Vector2 pos = Main.WorldCamera.InverseTransformVector(Main.WorldCamera.Size / 2);

                            foreach (DiamondPlacement placement in DiamondPlacement.Placements)
                                {
                                    pos.X += placement.Size.X / 2;

                                    for (int i = 0; i < placement.Positions.Length; i++)
                                    {
                                        SimpleIcon icon = new(
                                            $"Debug_DiamondPlacement_{Random.Shared.Next():x}",
                                            SpriteAtlases.Sprites[$"SlugcatDiamond_{StaticData.Slugcats[i].Id}"]);
                                        icon.BorderSize.OriginalValue = 1;
                                        icon.WorldPosition = pos + placement.Positions[i];

                                        Main.WorldObjects.Add(icon);
                                    }

                                    pos.X += placement.Size.X / 2 + 5;
                                }
                        }),

                        new UIButton
                            {
                                Text = "Test Idle",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                        {
                            Content.Idle.Play(.5f, 0, 0);
                        }).OnEvent(UIElement.UpdateEvent, (b, _) =>
                            {
                                b.Text = $"Test Idle ({(Main.NoIdle ? "D " : Main.Idlesound ? "S " : "")}{Main.Idle})";
                            }),

                        new UIButton
                            {
                                Text = "UI Exception",
                                Height = 20,
                                TextAlign = new(.5f),
                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                        {
                            throw new Exception("UI exception");
                        }),

                        new UIButton
                        {
                            Text = "Thread Exception",
                            Height = 20,
                            TextAlign = new(.5f),
                        }.OnEvent(UIElement.ClickEvent, (_, _) =>
                        {
                            new Thread(() => throw new Exception("Thread exception")).Start();
                        }),

                        new UIButton
                        {
                            Text = "Task Exception",
                            Height = 20,
                            TextAlign = new(.5f),
                        }.OnEvent(UIElement.ClickEvent, (_, _) =>
                        {
                            Task.Run(async () => { await Task.Delay(100); throw new Exception("Task exception"); });
                        }),

                        new UIButton
                        {
                            Text = "Async Void Exception",
                            Height = 20,
                            TextAlign = new(.5f),
                        }.OnEvent(UIElement.ClickEvent, (_, _) =>
                        {
                            async void TestException()
                            {
                                await Task.Delay(100);
                                throw new Exception("Async void exception");
                            }

                            TestException();
                        })
                    }
                }
            };
        }
    }
}

﻿using Cornifer.MapObjects;
using Cornifer.UI.Elements;
using Cornifer.UI.Structures;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Modals
{
    public class AddIconSelect : Modal<AddIconSelect, Empty>
    {
        UIList IconList;
        UIFlow IconFlow;
        string Search = "";

        public AddIconSelect()
        {
            Width = new(0, .83f);
            Height = new(0, .8f);

            Margin = 5;
            Padding = 5;

            Elements = new(this)
            {
                new UIInput
                {
                    Top = 10,
                    Left = new(0, 1, -1),
                    Width = 150,
                    Height = 20,

                    HintText = "Search...",
                    Text = Search,
                }.OnEvent(UIInput.TextChangedEvent, (inp, _) => { Search = inp.Text; UpdateIcons(); }),

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
                        .Assign(out IconFlow),
                    }
                }.Assign(out IconList),

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
                }.OnEvent(UIElement.ClickEvent, (_, _) => ReturnResult(new()))
            };

            UpdateIcons();
        }
        private void UpdateIcons() 
        {
            IconFlow.Elements.Clear();
            foreach (var (name, sprite) in SpriteAtlases.Sprites.OrderBy(kvp => kvp.Key))
            {
                if (Search.Length > 0 && !name.Contains(Search, StringComparison.InvariantCultureIgnoreCase))
                    continue;

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
                        Main.AddWorldObject(new SimpleIcon($"WorldIcon_{name}_{Random.Shared.Next():x}", sprite)
                        {
                            WorldPosition = Main.WorldCamera.Position + Main.WorldCamera.Size / Main.WorldCamera.Scale * .5f
                        });

                        if (Root!.ShiftKey == KeybindState.Released)
                            ReturnResult(new());
                    }
                });

                IconFlow.Elements.Add(panel);
            }
            IconList.Recalculate();
        }
    }
}

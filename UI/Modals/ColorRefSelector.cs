using Cornifer.UI.Elements;
using Cornifer.UI.Helpers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Modals
{
    public class ColorRefSelector : Modal<ColorRefSelector, ColorRef?>
    {
        UIList Bindings;

        public static ColorRef? CurrentRef;

        public ColorRefSelector()
        {
            Width = 300;
            Height = new(0, .9f);

            Padding = 5;

            Elements = new(this)
            {
                new UILabel
                {
                    Height = 0,
                    Text = "Bind color to preset",
                    TextAlign = new(.5f)
                },

                new UIList
                {
                    Top = 16,
                    Height = new(-64, 1),
                    ElementSpacing = 4
                }.Assign(out Bindings),

                new UIButton
                {
                    Top = new(-44, 1),
                    Left = new(-2, .5f, -1),
                    Width = 110,
                    Height = 20,
                    Text = "Copy preset",
                    TextAlign = new(.5f)
                }.OnEvent(ClickEvent, (_, _) => 
                {
                    if (CurrentRef is null)
                        return;

                    ReturnResult(new(null, CurrentRef.Color));
                }),

                new UIButton
                {
                    Top = new(-44, 1),
                    Left = new(2, .5f),
                    Width = 110,
                    Height = 20,
                    Text = "Bind to preset",
                    TextAlign = new(.5f)
                }.OnEvent(ClickEvent, (_, _) =>
                {
                    if (CurrentRef is null)
                        return;

                    ReturnResult(CurrentRef);
                }),

                new UIButton
                {
                    Top = new(-20, 1),
                    Left = new(0, .5f, -.5f),
                    Width = 80,
                    Height = 20,
                    Text = "Cancel",
                    TextAlign = new(.5f)
                }.OnEvent(ClickEvent, (_, _) =>
                {
                    ReturnResult(null);
                })
            };
        }

        protected override void Shown()
        {
            Bindings.Elements.Clear();

            RadioButtonGroup group = new();

            foreach (var (name, color) in ColorDatabase.Colors)
            {
                UIPanel panel = new()
                {
                    Height = 20,

                    BackColor = Color.Transparent,
                    BorderColor = Color.Transparent,

                    Elements = 
                    {
                        new UIColorRefDisplay
                        {
                            Width = 20,
                            Height = 20,
                            Reference = color,
                            BorderColor = new(100, 100, 100),
                        },
                        new UIButton
                        {
                            Text = name,
                            TextAlign = new(0, .5f),
                            Left = 25,
                            Width = new(-25, 1),
                            Selectable = true,
                            Selected = color == CurrentRef,
                            RadioGroup = group,
                            RadioTag = color,

                            SelectedBackColor = Color.White,
                            SelectedTextColor = Color.Black,
                        }
                    }
                };
                Bindings.Elements.Add(panel);
            }

            group.ButtonClicked += (_, tag) =>
            {
                if (tag is ColorRef cref)
                    CurrentRef = cref;
            };
            Bindings.Recalculate();
        }

        protected override void Hidden()
        {
            Bindings.Elements.Clear();
        }
    }
}

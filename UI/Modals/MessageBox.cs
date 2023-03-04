using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Modals
{
    internal class MessageBox : Modal<MessageBox, int>
    {
        UIPanel Buttons;
        UILabel Text;

        public static readonly (string, int)[] ButtonsOk = new[] { ("Ok", 1) };
        public static readonly (string, int)[] ButtonsOkCancel = new[] { ("Ok", 1), ("Cancel", 0) };

        public MessageBox()
        {
            Width = 450;
            Height = 300;

            Margin = 5;
            Padding = 5;

            Elements = new(this)
            {
                new UILabel
                {
                    Height = 0,
                    Top = new(0, .4f, -.5f),
                    TextAlign = new(.5f),
                }.Assign(out Text),

                new UIPanel
                {
                    Height = 20,
                    BackColor = Color.Transparent,
                    BorderColor = Color.Transparent,

                    Top = new(-10, 1, -1),
                    Left = new(0, .5f, -.5f),
                }.Assign(out Buttons)
            };
        }

        public static Task<int> Show(string text, IEnumerable<(string, int)> buttons)
        {
            Instance ??= new();

            Instance.Text.Text = text;
            Instance.Buttons.Elements.Clear();

            foreach (var pair in buttons) 
            {
                UIButton button = new()
                {
                    Text = pair.Item1,
                    Width = 80,
                    Height = 20,
                    TextAlign = new(.5f),
                };
                int result = pair.Item2;
                button.OnEvent(ClickEvent, (_, _) => ReturnResult(result));
                Instance.Buttons.Elements.Add(button);
                button.Recalculate();
            }

            float spacing = 4;
            float width = Instance.Buttons.Elements.Sum(e => e.ScreenRect.Width) + (Instance.Buttons.Elements.Count - 1) * spacing;

            float x = (Instance.Buttons.ScreenRect.Width - width) / 2;

            foreach (var element in Instance.Buttons.Elements) 
            {
                element.Top = 0;
                element.Left = x;

                x += element.ScreenRect.Width + spacing;
            }

            Instance.Recalculate();
            Show();

            return Task;
        }

        protected override void Hidden()
        {
            Buttons.Elements.Clear();
        }
    }
}

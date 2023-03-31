using Cornifer.UI;
using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace Cornifer.UI.Elements
{
    public class UIButton : UIElement, IHoverable
    {
        public string? Text
        {
            get => text;
            set
            {
                text = value;
                Lines = text?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                Recalculate();
            }
        }

        public Offset4 Padding = new(3, 5);
        public Vec2 TextAlign;

        public Color TextColor = Color.White;
        public Color HoverTextColor = Color.White;
        public Color SelectedTextColor = Color.White;
        public Color ClickedTextColor = Color.White;

        public Color BackColor = new(48, 48, 48);
        public Color HoverBackColor = new(64, 64, 64);
        public Color SelectedBackColor = new(72, 72, 72);
        public Color ClickedBackColor = new(96, 96, 96);

        public Color BorderColor = new(100, 100, 100);

        public Texture2D? Image;
        public Rectangle? ImageFrame = null;
        public Vec2 ImageAlign;
        public bool AutoSize = true;

        public bool Selectable
        {
            get => selectable;
            set
            {
                selectable = value;
                if (!value)
                    Selected = false;
            }
        }
        public bool CanDeselect = true;
        public bool Selected
        {
            get => SelectedInternal;
            set
            {
                if (SelectedInternal == value)
                    return;
                SelectedInternal = value;
                RadioGroup?.SelectButton(value ? this : null);
            }
        }
        public RadioButtonGroup? RadioGroup
        {
            get => radioGroup;
            set
            {
                if (radioGroup == value)
                    return;

                radioGroup?.RemoveButton(this);
                radioGroup = value;
                radioGroup?.AddButton(this);
            }
        }
        public object? RadioTag;

        private string[]? Lines;
        private string? text;
        internal bool SelectedInternal;
        private RadioButtonGroup? radioGroup;
        private bool selectable;

        public override void Recalculate()
        {
            if (AutoSize)
            {
                MinWidth = 0;
                MinHeight = 0;
                if (Font is not null && Lines?.Length is not null and > 0)
                {
                    MinWidth = Lines.Select(line => Font.MeasureString(line).X).Max() + Padding.Horizontal;
                    MinHeight = Lines.Length * (Font.LineSpacing - 3) + Padding.Vertical;
                }
            }

            base.Recalculate();
        }

        protected override void UpdateSelf()
        {
            if (Hovered && Root.MouseLeftKey == KeybindState.JustPressed && (RadioGroup is not null || Selectable))
            {
                if (!Selected || CanDeselect)
                    Selected = !Selected;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Color textColor;

            if (Hovered && Root.MouseState.LeftButton == ButtonState.Pressed)
            {
                spriteBatch.FillRectangle(ScreenRect, ClickedBackColor);
                textColor = ClickedTextColor;
            }
            else if (Selected)
            {
                spriteBatch.FillRectangle(ScreenRect, SelectedBackColor);
                textColor = SelectedTextColor;
            }
            else if (Hovered)
            {
                spriteBatch.FillRectangle(ScreenRect, HoverBackColor);
                textColor = HoverTextColor;
            }
            else
            {
                spriteBatch.FillRectangle(ScreenRect, BackColor);
                textColor = TextColor;
            }

            spriteBatch.DrawRectangle(ScreenRect, BorderColor);

            if (Lines is not null && Font is not null)
            {
                float y = Padding.Top + ScreenRect.Y + (ScreenRect.Height - Padding.Vertical - Lines.Length * (Font.LineSpacing - 4)) * TextAlign.Y;

                foreach (string line in Lines)
                {
                    float lineWidth = Font.MeasureString(line).X;

                    float x = Padding.Left + ScreenRect.X + (ScreenRect.Width - Padding.Horizontal - lineWidth) * TextAlign.X;

                    spriteBatch.DrawString(Font, line, new(MathF.Floor(x), MathF.Floor(y)), textColor);

                    y += Font.LineSpacing - 4;
                }

            }
            if (Image is not null)
            {
                Vec2 size = (Vec2)(ImageFrame?.Size ?? Image.Bounds.Size);
                Vec2 pos = ImageAlign * (ScreenRect.Size - Padding.AllPadding - size) + Padding.TopLeft + ScreenRect.Position;

                spriteBatch.Draw(Image, pos, ImageFrame, Color.White);
            }
        }
    }
}

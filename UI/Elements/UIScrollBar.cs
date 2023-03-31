using Cornifer.UI;
using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;

namespace Cornifer.UI.Elements
{
    public class UIScrollBar : UIElement, IHoverable
    {
        public static readonly ElementEvent<float, UIScrollBar> ScrollChanged = new();

        public float ScrollPosition
        {
            get => scrollPosition;
            set
            {
                if (scrollPosition == value)
                    return;

                if (!Events.PreCall(ScrollChanged, value))
                    return;

                scrollPosition = value;
                Events.PostCall(ScrollChanged, value);
            }
        }

        public float ScrollMin = 0f;
        public float ScrollMax = 1f;

        public float BarSize = .1f;
        public float ScrollDistance = .1f;

        public Color BackColor = Color.Transparent;
        public Color BorderColor = Color.Transparent;
        public Color BarColor = Color.Gray;

        public Offset4 BarPadding;

        public bool Horizontal = false;
        public bool BarSizeAbsolute = false;

        protected Rect BarRect;
        protected bool Grabbed;
        protected Vec2 GrabPos;
        private float scrollPosition = 1f;

        protected override void UpdateSelf()
        {
            if (ScrollMin > ScrollMax) (ScrollMax, ScrollMin) = (ScrollMin, ScrollMax);

            float x = ScreenRect.X + BarPadding.Left;
            float y = ScreenRect.Y + BarPadding.Top;
            float height = ScreenRect.Height - BarPadding.Vertical;
            float width = ScreenRect.Width - BarPadding.Horizontal;

            if (BorderColor != Color.Transparent)
            {
                x += 1;
                y += 1;
                height -= 2;
                width -= 2;
            }

            ScrollPosition = Math.Clamp(ScrollPosition, ScrollMin, ScrollMax);

            if (ScrollMin == ScrollMax)
            {
                BarRect.X = x;
                BarRect.Y = y;
                BarRect.Width = width;
                BarRect.Height = height;
                ScrollPosition = ScrollMin;
                return;
            }

            float scrollDiff = ScrollMax - ScrollMin;

            float barLongSize = Horizontal ? width : height;

            float barRealSize = BarSizeAbsolute ? BarSize : BarSize / (scrollDiff + BarSize) * barLongSize;
            float barPos = (ScrollPosition - ScrollMin) / scrollDiff * (barLongSize - barRealSize) + (Horizontal ? x : y);

            BarRect = Horizontal ? new(barPos, y, barRealSize, height) : new(x, barPos, width, barRealSize);

            if (Root.MouseLeftKey == KeybindState.JustPressed && BarRect.Contains(Root.MousePosition) && !Grabbed)
            {
                Grabbed = true;
                GrabPos = Root.MousePosition;
            }
            if (Grabbed)
            {
                if (Root.MouseState.LeftButton == ButtonState.Released)
                    Grabbed = false;
                else
                {
                    Vec2 diff = Root.MousePosition - GrabPos;
                    if (!Horizontal && diff.Y != 0)
                    {
                        float mouseScrollValue = scrollDiff / (barLongSize - barRealSize);
                        ScrollPosition = Math.Clamp(ScrollPosition + diff.Y * mouseScrollValue, ScrollMin, ScrollMax);
                        GrabPos = Root.MousePosition;
                    }
                    else if (Horizontal && diff.X != 0)
                    {
                        float mouseScrollValue = scrollDiff / (barLongSize - barRealSize);
                        ScrollPosition = Math.Clamp(ScrollPosition + diff.X * mouseScrollValue, ScrollMin, ScrollMax);
                        GrabPos = Root.MousePosition;
                    }
                }
            }

            if (!Grabbed && Root.MouseState.LeftButton == ButtonState.Pressed && Hovered)
            {
                float mouseStart = (Horizontal ? BarPadding.Left : BarPadding.Top) + barRealSize / 2;
                float mouseEnd = (Horizontal ? ScreenRect.Width - BarPadding.Right : ScreenRect.Height - BarPadding.Bottom) - barRealSize / 2;

                Vec2 mouseRel = RelativeMouse;

                ScrollPosition =
                    (Math.Clamp(Horizontal ? mouseRel.X : mouseRel.Y, mouseStart, mouseEnd) - mouseStart)
                    / (barLongSize - barRealSize)
                    * scrollDiff + ScrollMin;

                barPos = ScrollPosition / scrollDiff * (barLongSize - barRealSize) + (Horizontal ? x : y);
                BarRect = Horizontal ? new(barPos, y, barRealSize, height) : new(x, barPos, width, barRealSize);
            }

            if (Hovered && Root.MouseScroll != 0)
            {
                ScrollPosition = Math.Clamp(ScrollPosition + ScrollDistance * -Root.MouseScroll, ScrollMin, ScrollMax);
            }


        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);
            DrawBar(spriteBatch);
        }

        protected virtual void DrawBackground(SpriteBatch spriteBatch)
        {
            if (BackColor != Color.Transparent)
                spriteBatch.FillRectangle(ScreenRect, BackColor);
            if (BorderColor != Color.Transparent)
                spriteBatch.DrawRectangle(ScreenRect, BorderColor);
        }

        protected virtual void DrawBar(SpriteBatch spriteBatch)
        {
            if (Grabbed || BarRect.Contains(Root.MousePosition))
                spriteBatch.FillRectangle(BarRect, BarColor * 1.2f);
            else
                spriteBatch.FillRectangle(BarRect, BarColor);
        }
    }
}

using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel;
using System.Linq;

namespace Cornifer.UI.Elements
{
    public class UIList : UIContainer, ILayoutContainer
    {
        static RasterizerState Scissors = new() { ScissorTestEnable = true };

        public UIScrollBar ScrollBar = new()
        {
            Width = 15,
            Height = new(0, 1),

            Left = new(0, 1),
            BarPadding = new(3),

            BackColor = Color.White * 0.1f,
            ScrollDistance = 20
        };

        public float ElementSpacing = 0f;
        public bool AutoSize = false;

        bool PerformingLayout = false;
        float OldScroll;
        float CurrentLayoutElementY;

        public override Rect ChildrenRect 
        {
            get 
            {
                if (AutoSize || !ScrollBar.Visible)
                    return ScreenRect;

                Rect rect = ScreenRect;
                rect.Width -= ScrollBar.ScreenRect.Width;
                return rect;
            }
        }

        protected override void PreUpdateChildren()
        {
            if (!AutoSize && (Elements.Count == 0 || Elements[0] != ScrollBar))
                Elements.Insert(0, ScrollBar);

            if (!AutoSize && OldScroll != ScrollBar.ScrollPosition)
                Recalculate();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            PreDrawChildren(spriteBatch);

            if (!AutoSize && ScrollBar.Visible)
                ScrollBar.Draw(spriteBatch);

            Rect rect = ScreenRect;

            rect.X += Padding.Left;
            rect.Y += Padding.Top;
            rect.Width -= Padding.Horizontal;
            rect.Height -= Padding.Vertical;

            if (!AutoSize && ScrollBar.Visible)
                rect.Width -= ScrollBar.ScreenRect.Width;

            if (AutoSize)
            {
                rect.Y--;
                rect.Height += 2;
            }

            spriteBatch.PushAndChangeState(rasterizerState: Scissors);

            Rectangle oldScissors = spriteBatch.GraphicsDevice.ScissorRectangle;
            spriteBatch.GraphicsDevice.ScissorRectangle = (Rectangle)rect.Intersect(oldScissors);

            foreach (UIElement element in Elements)
                if (element != ScrollBar && element.Visible)
                    element.Draw(spriteBatch);

            spriteBatch.RestoreState();
            spriteBatch.GraphicsDevice.ScissorRectangle = oldScissors;

            PostDrawChildren(spriteBatch);
        }

        public override void Recalculate()
        {
            MinHeight = 0;

            PerformingLayout = true;

            if (AutoSize)
            {
                SkipContainerLayout = true;
                base.Recalculate();
                MinHeight = GetContentHeight() + Padding.Vertical;
                base.Recalculate();
            }
            else
            {
                base.Recalculate();
            }

            PerformingLayout = false;
            PerformLayout();
        }

        public void LayoutChild(UIElement child, ref Rect screenRect)
        {
            if (child == ScrollBar)
                return;

            if (!PerformingLayout)
            {
                PerformLayout();
            }
            else
            {
                screenRect.Top = CurrentLayoutElementY + child.Margin.Top;
                screenRect.Left = ScreenRect.X + Padding.Left + child.Margin.Left;
                screenRect.Width = ScreenRect.Width - child.Margin.Horizontal - Padding.Horizontal - (!AutoSize && ScrollBar.Visible ? ScrollBar.ScreenRect.Width : 0);
            }
        }

        float GetContentHeight()
        {
            return Elements
                .Where(e => e != ScrollBar && e.Visible)
                .Select(e => e.ScreenRect.Height + e.Margin.Vertical)
                .Sum() + ElementSpacing * Math.Max(0, Elements.Count - (ScrollBar.Parent is null ? 1 : 2));
        }

        void PerformLayout()
        {
            PerformingLayout = true;

            if (!AutoSize)
            {
                if (Elements.Count == 0 || Elements[0] != ScrollBar)
                    Elements.Insert(0, ScrollBar);

                ScrollBar.ScrollMax = Math.Max(0, GetContentHeight() - ScreenRect.Height - Padding.Vertical);
                ScrollBar.ScrollPosition = Math.Min(ScrollBar.ScrollPosition, ScrollBar.ScrollMax);
                ScrollBar.BarSize = ScreenRect.Height - Padding.Vertical;

                if (ScrollBar.Visible != ScrollBar.ScrollMax > 0)
                {
                    ScrollBar.Visible = ScrollBar.ScrollMax > 0;
                    if (ScrollBar.Visible)
                        ScrollBar.Recalculate();
                }
            }

            float startPos = ScreenRect.Y + Padding.Top - ScrollBar.ScrollPosition;

            foreach (UIElement element in Elements)
            {
                if (element == ScrollBar || !element.Visible)
                    continue;

                CurrentLayoutElementY = startPos;
                element.Recalculate();
                startPos += element.ScreenRect.Height + element.Margin.Vertical + ElementSpacing;
            }
            OldScroll = ScrollBar.ScrollPosition;
            PerformingLayout = false;
        }
    }
}

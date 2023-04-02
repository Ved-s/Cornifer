using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Cornifer.UI.Elements
{
    public class UIList : UIContainer, ILayoutContainer
    {
        static RasterizerState Scissors = new() { ScissorTestEnable = true };

        public UIScrollBar ScrollBar = new()
        {
            //Width = 15,
            //Height = new(0, 1),
            //
            //Left = new(0, 1),
            BarPadding = new(3),

            BackColor = Color.White * 0.1f,
            ScrollDistance = 20
        };

        public float ScrollbarWidth = 15;
        public float ScrollbarSpacing = 2;
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
                rect.Width -= 15 + ScrollbarSpacing;
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
                rect.Width -= ScrollBar.ScreenRect.Width + ScrollbarSpacing;

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
            {
                screenRect.Y = ScreenRect.Top + Padding.Top;
                screenRect.X = ScreenRect.Right - Padding.Right - ScrollbarWidth;
                screenRect.Height = screenRect.Height - Padding.Vertical;
                screenRect.Width = ScrollbarWidth;
                return;
            }

            if (PerformingLayout)
            {
                screenRect.Top = CurrentLayoutElementY + child.Margin.Top;
                screenRect.Left = ScreenRect.X + Padding.Left + child.Margin.Left;
                screenRect.Width = ScreenRect.Width - child.Margin.Horizontal - Padding.Horizontal - (!AutoSize && ScrollBar.Visible ? ScrollBar.ScreenRect.Width + ScrollbarSpacing : 0);
                return;
            }

            UpdateScrollbar();
            int index = Elements.IndexOf(child);
            if (index < 0)
                return;

            PerformingLayout = true;

            float y = ScreenRect.Y + Padding.Top - ScrollBar.ScrollPosition;

            for (int i = 0; i < index; i++)
            {
                UIElement element = Elements[i];
                if (element == ScrollBar || !element.Visible)
                    continue;

                CurrentLayoutElementY = y;
                element.Recalculate();
                y += element.ScreenRect.Height + element.Margin.Vertical + ElementSpacing;
            }

            screenRect.Top = y + child.Margin.Top;
            screenRect.Left = ScreenRect.X + Padding.Left + child.Margin.Left;
            screenRect.Width = ScreenRect.Width - child.Margin.Horizontal - Padding.Horizontal - (!AutoSize && ScrollBar.Visible ? ScrollBar.ScreenRect.Width + ScrollbarSpacing : 0);
            y += child.ScreenRect.Height + child.Margin.Vertical + ElementSpacing;

            for (int i = index + 1; i < Elements.Count; i++)
            {
                UIElement element = Elements[i];
                if (element == ScrollBar || !element.Visible)
                    continue;

                CurrentLayoutElementY = y;
                element.Recalculate();
                y += element.ScreenRect.Height + element.Margin.Vertical + ElementSpacing;
            }

            PerformingLayout = false;
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
            UpdateScrollbar();

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

        private void UpdateScrollbar()
        {
            if (!AutoSize)
            {
                if (Elements.Count == 0 || Elements[0] != ScrollBar)
                    Elements.Insert(0, ScrollBar);

                ScrollBar.ScrollMax = Math.Max(0, GetContentHeight() - ScreenRect.Height);
                ScrollBar.ScrollPosition = Math.Min(ScrollBar.ScrollPosition, ScrollBar.ScrollMax);
                ScrollBar.BarSize = ScreenRect.Height - Padding.Vertical;

                if (ScrollBar.Visible != ScrollBar.ScrollMax > 0)
                {
                    ScrollBar.Visible = ScrollBar.ScrollMax > 0;
                    if (ScrollBar.Visible)
                        ScrollBar.Recalculate();
                }
            }
        }
    }
}

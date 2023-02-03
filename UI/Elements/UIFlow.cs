using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Elements
{
    public class UIFlow : UIContainer, ILayoutContainer
    {
        public float ElementSpacing = 0f;

        bool PerformingLayout;
        Vec2 CurrentLayoutElementPos;

        public override void Recalculate()
        {
            PerformingLayout = true;

            MinWidth = 0;
            MinHeight = 0;

            Vec2 pos = Vec2.Zero;
            Vec2 size = Vec2.Zero;
            float lineMaxHeight = 0;

            foreach (UIElement element in Elements)
            {
                if (pos.X + element.ScreenRect.Width + element.Margin.Horizontal > ScreenRect.Width)
                {
                    if (pos.X == 0)
                    {
                        size.X = Math.Max(size.X, element.ScreenRect.Width + element.Margin.Horizontal);
                        lineMaxHeight = element.ScreenRect.Height + element.Margin.Vertical;
                        continue;
                    }

                    pos.X = 0;
                    pos.Y += lineMaxHeight + ElementSpacing;
                    if (size.Y > 0)
                        size.Y += ElementSpacing;
                    size.Y += lineMaxHeight;
                    lineMaxHeight = 0;
                }
                lineMaxHeight = Math.Max(lineMaxHeight, element.ScreenRect.Height + element.Margin.Vertical);
                pos.X += element.ScreenRect.Width + ElementSpacing + element.Margin.Horizontal;
            }

            if (lineMaxHeight > 0)
            {
                if (size.Y > 0)
                    size.Y += ElementSpacing;
                size.Y += lineMaxHeight;
            }

            MinWidth = size.X;
            MinHeight = size.Y;

            base.Recalculate();
            PerformLayout();
        }

        public void LayoutChild(UIElement child, ref Rect screenRect)
        {
            if (!PerformingLayout)
                PerformLayout();
            else
                screenRect.Position = CurrentLayoutElementPos;
        }

        void PerformLayout()
        {
            PerformingLayout = true;
            Vec2 pos = ScreenRect.Position;
            float lineMaxHeight = 0;

            foreach (UIElement element in Elements)
            {
                if (pos.X + element.ScreenRect.Width + element.Margin.Horizontal > ScreenRect.Right)
                {
                    if (pos.X == 0)
                    {
                        CurrentLayoutElementPos = pos + element.Margin.TopLeft;
                        element.Recalculate();
                        lineMaxHeight = element.ScreenRect.Height + element.Margin.Vertical;
                        continue;
                    }

                    pos.X = ScreenRect.X;
                    pos.Y += lineMaxHeight + ElementSpacing;
                    lineMaxHeight = 0;
                }
                CurrentLayoutElementPos = pos + element.Margin.TopLeft;
                element.Recalculate();
                lineMaxHeight = Math.Max(lineMaxHeight, element.ScreenRect.Height + element.Margin.Vertical);
                pos.X += element.ScreenRect.Width + ElementSpacing + element.Margin.Horizontal;
            }

            PerformingLayout = false;
        }
    }
}

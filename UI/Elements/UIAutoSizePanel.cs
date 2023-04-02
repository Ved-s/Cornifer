using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Elements
{
    public class UIAutoSizePanel : UIPanel, ILayoutContainer
    {
        public bool AutoHeight = true;
        public bool AutoWidth = true;

        bool PerformingLayout = false;

        public void LayoutChild(UIElement child, ref Rect screenRect)
        {
            PerformLayout();
        }

        public override void Recalculate()
        {
            PerformLayout();
        }

        void PerformLayout()
        {
            if (PerformingLayout)
                return;

            PerformingLayout = true;

            if (AutoHeight) MinHeight = 0;
            if (AutoWidth) MinWidth = 0;

            base.Recalculate();

            Vec2 size = new();
            Vec2 pos = ScreenRect.Position;

            foreach (UIElement element in Elements)
            {
                Vec2 elementBR = element.ScreenRect.Position + element.ScreenRect.Size + element.Margin.BottomRight - pos;
                size.X = Math.Max(size.X, elementBR.X);
                size.Y = Math.Max(size.Y, elementBR.Y);
            }

            size += Padding.BottomRight;
            if (AutoHeight) MinHeight = size.Y;
            if (AutoWidth) MinWidth = size.X;
            base.Recalculate();

            PerformingLayout = false;
        }
    }
}

using Cornifer.UI.Elements;
using Cornifer.UI.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Helpers
{
    public interface ILayoutContainer
    {
        public void LayoutChild(UIElement child, ref Rect screenRect);
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Elements
{
    public class UIColorRefDisplay : UIElement
    {
        public Color BorderColor = Color.Transparent;
        public ColorRef? Reference;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            spriteBatch.FillRectangle(ScreenRect, Reference?.Color ?? Color.Magenta);
            spriteBatch.DrawRectangle(ScreenRect, BorderColor);
        }
    }
}

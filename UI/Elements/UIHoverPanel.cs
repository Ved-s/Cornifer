using Cornifer.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cornifer.UI.Elements
{
    public class UIHoverPanel : UIPanel
    {
        public Color HoverBackColor = new(.3f, .3f, .3f);
        public Color HoverBorderColor = new(.4f, .4f, .4f);

        protected override void PreDrawChildren(SpriteBatch spriteBatch)
        {
            if (Hovered)
            {
                spriteBatch.FillRectangle(ScreenRect, HoverBackColor);
                spriteBatch.DrawRectangle(ScreenRect, HoverBorderColor);
            }
            else
            {
                spriteBatch.FillRectangle(ScreenRect, BackColor);
                spriteBatch.DrawRectangle(ScreenRect, BorderColor);
            }
        }
    }
}

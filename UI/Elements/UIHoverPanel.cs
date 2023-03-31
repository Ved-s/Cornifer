using Cornifer.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cornifer.UI.Elements
{
    public class UIHoverPanel : UIPanel, IHoverable
    {
        public Color HoverBackColor = new(.3f, .3f, .3f);
        public Color HoverBorderColor = new(.4f, .4f, .4f);

        protected override void PreDrawChildren(SpriteBatch spriteBatch)
        {
            if (Hovered && (Root?.Hover == this || Root?.Hover is not IHoverable))
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

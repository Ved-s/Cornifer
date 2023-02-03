using Cornifer.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cornifer.UI.Elements
{
    public class UIPanel : UIContainer
    {
        public Color BackColor = new(.2f, .2f, .2f);
        public Color BorderColor = new(.3f, .3f, .3f);

        protected override void PreDrawChildren(SpriteBatch spriteBatch)
        {
            spriteBatch.FillRectangle(ScreenRect, BackColor);
            spriteBatch.DrawRectangle(ScreenRect, BorderColor);
        }
    }
}

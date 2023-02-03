using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Elements
{
    public class UIModal : UIPanel
    {
        public Color ModalBackgroundColor = new(30, 30, 30, 180);

        public virtual void DrawModalBackground(SpriteBatch spriteBatch)
        {
            spriteBatch.FillRectangle(Parent!.ScreenRect, ModalBackgroundColor);
        }
    }
}

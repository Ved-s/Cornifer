using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Elements
{
    public class UIProgressBar : UIElement
    {
        public Color ProgressBarColor = new(100, 100, 100);
        public Color BorderColor = new(100, 100, 100);

        public float HorizontalAlignment = 0;

        public float MaxProgress;
        public float MinProgress;
        public float Progress;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            float progress = (Progress - MinProgress) / (MaxProgress - MinProgress);
            if (!float.IsFinite(progress))
                progress = 0;

            Rect innerRect = ScreenRect + new Offset4(2);
            float newWidth = innerRect.Width * progress;
            innerRect.X += newWidth * HorizontalAlignment;
            innerRect.Width = newWidth;

            spriteBatch.FillRectangle(innerRect, ProgressBarColor);
            spriteBatch.DrawRectangle(ScreenRect, BorderColor);
        }
    }
}

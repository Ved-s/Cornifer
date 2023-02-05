using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Elements
{
    public class UIImage : UIElement
    {
        public Texture2D? Texture;
        public Color TextureColor = Color.White;
        public Rectangle? TextureFrame;
        public bool ScaleUp = false;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Texture is null)
                return;

            Vector2 textureSize = TextureFrame?.Size.ToVector2() ?? Texture.Size();
            float scale = Math.Min(ScreenRect.Width / textureSize.X, ScreenRect.Height / textureSize.Y);
            if (!ScaleUp && scale > 1)
                scale = 1;

            Vector2 scaledSize = textureSize * scale;

            spriteBatch.Draw(Texture, ScreenRect.Center - scaledSize / 2, TextureFrame, TextureColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cornifer.Renderers
{
    public class ScreenRenderer : Renderer
    {
        public SpriteBatch SpriteBatch { get; }

        public override Vector2 Size => SpriteBatch.GraphicsDevice.Viewport.Bounds.Size.ToVector2();

        public ScreenRenderer(SpriteBatch spriteBatch)
        {
            SpriteBatch = spriteBatch;
        }

        public void DrawTexture(Texture2D texture, Vector2 worldPos, Rectangle? source, Vector2? worldSize, Color? color, Vector2 origin, float rotation, Vector2? scaleOverride = null)
        {
            if (texture is null)
                return;

            Vector2 texScale;
            if (scaleOverride.HasValue)
                texScale = scaleOverride.Value;
            else
            {
                Vector2 texSize = source?.Size.ToVector2() ?? texture.Size();
                texScale = worldSize.HasValue ? worldSize.Value / texSize : Vector2.One;
                texScale *= Scale;
            }

            SpriteBatch.Draw(texture, TransformVector(worldPos), source, color ?? Color.White, rotation, origin, texScale, SpriteEffects.None, 0);
        }
        public override void DrawTexture(Texture2D texture, Vector2 worldPos, Rectangle? source, Vector2? worldSize, Color? color, Vector2? scaleOverride = null)
        {
            DrawTexture(texture, worldPos, source, worldSize, color, Vector2.Zero, 0f, scaleOverride);
        }
    }
}

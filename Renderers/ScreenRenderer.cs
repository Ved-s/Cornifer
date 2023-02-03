using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cornifer.Renderers
{
    public abstract class ScreenRenderer : Renderer
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

        public override void DrawRect(Vector2 worldPos, Vector2 size, Color? fill, Color? border = null, float thickness = 1)
        {
            SpriteBatch.DrawRect(TransformVector(worldPos), size * Scale, fill, border, thickness);
        }

        public override void DrawLine(Vector2 worldPosA, Vector2 worldPosB, Color color, float thickness = 1)
        {
            SpriteBatch.DrawLine(TransformVector(worldPosA), TransformVector(worldPosB), color, thickness);
        }
    }
}

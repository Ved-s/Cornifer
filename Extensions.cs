using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public static class Extensions
    {
        public static void SetAlpha(ref this Color color, float alpha)
        {
            color.A = (byte)(Math.Clamp(alpha, 0, 1) * 255);
        }

        public static bool TryGet<T>(this T[] array, int index, out T value)
        {
            if (index < array.Length)
            {
                value = array[index];
                return true;
            }
            value = default!;
            return false;
        }

        public static Vector2 Size(this Texture2D texture)
        {
            return new(texture.Width, texture.Height);
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color color, float thickness = 1)
        {
            Vector2 diff = b - a;
            float angle = MathF.Atan2(diff.Y, diff.X);
            spriteBatch.Draw(Main.Pixel, a, null, color, angle, new Vector2(0, .5f), new Vector2(diff.Length(), thickness), SpriteEffects.None, 0);
        }

        public static void DrawRect(this SpriteBatch spriteBatch, Vector2 pos, Vector2 size, Color? fill, Color? border = null, float thickness = 1)
        {
            if (fill.HasValue)
            {
                spriteBatch.Draw(Main.Pixel, pos, null, fill.Value, 0f, Vector2.Zero, size, SpriteEffects.None, 0);
            }
            if (border.HasValue)
            {
                spriteBatch.DrawRect(new(pos.X + thickness, pos.Y), new(size.X - thickness, thickness), border.Value);
                spriteBatch.DrawRect(pos, new(thickness, size.Y - thickness), border.Value);

                if (size.Y > thickness)
                    spriteBatch.DrawRect(new(pos.X, (pos.Y + size.Y) - thickness), new(Math.Max(thickness, size.X - thickness), thickness), border.Value);

                if (size.X > thickness)
                    spriteBatch.DrawRect(new((pos.X + size.X) - thickness, pos.Y + thickness), new(thickness, Math.Max(thickness, size.Y - thickness)), border.Value);
            }
        }

        public static void DrawStringAligned(this SpriteBatch spriteBatch, SpriteFont spriteFont, string text, Vector2 position, Color color, Vector2 align, Color? shade = null)
        {
            Vector2 size = spriteFont.MeasureString(text);
            Vector2 pos = position - size * align;

            if (shade.HasValue)
            {
                spriteBatch.DrawString(spriteFont, text, pos + new Vector2(0, -1), shade.Value);
                spriteBatch.DrawString(spriteFont, text, pos + new Vector2(0, 1), shade.Value);
                spriteBatch.DrawString(spriteFont, text, pos + new Vector2(-1, 0), shade.Value);
                spriteBatch.DrawString(spriteFont, text, pos + new Vector2(1, 0), shade.Value);
            }

            spriteBatch.DrawString(spriteFont, text, pos, color);
        }

        public static void DrawStringShaded(this SpriteBatch spriteBatch, SpriteFont spriteFont, string text, Vector2 position, Color color, Color? shadeColor = null)
        {
            shadeColor ??= Color.Black;

            spriteBatch.DrawString(spriteFont, text, position + new Vector2(0, -1), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(0, 1), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(-1, 0), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(1, 0), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position, color);
        }

        public static void DrawPoint(this SpriteBatch spriteBatch, Vector2 pos, float size, Color color)
        {
            spriteBatch.DrawRect(pos - new Vector2(size/2), new Vector2(size), color);
        }
    }
}

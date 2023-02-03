using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI
{
    public static class GraphicExtensions
    {
        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Rect rect, Color color)
        {
            if (color.A == 0)
                return;

            spriteBatch.Draw(texture, rect.Position, null, color, 0f, Vector2.Zero, rect.Size / new Vec2(texture.Width, texture.Height), SpriteEffects.None, 0);
        }

        public static void FillRectangle(this SpriteBatch spriteBatch, Rect rect, Color color)
        {
            if (color.A == 0)
                return;

            spriteBatch.Draw(Main.Pixel, rect, color);
        }
        public static void FillRectangle(this SpriteBatch spriteBatch, float x, float y, float w, float h, Color color)
        {
            if (color.A == 0)
                return;

            spriteBatch.FillRectangle(new(x, y, w, h), color);
        }

        public static void DrawRectangle(this SpriteBatch spriteBatch, Rect rect, Color color, int thickness = 1)
        {
            if (color.A == 0)
                return;

            spriteBatch.FillRectangle(rect.X + thickness, rect.Y, rect.Width - thickness, thickness, color);
            spriteBatch.FillRectangle(rect.X, rect.Y, thickness, rect.Height - thickness, color);

            if (rect.Height > thickness)
                spriteBatch.FillRectangle(rect.X, rect.Bottom - thickness, Math.Max(thickness, rect.Width - thickness), thickness, color);

            if (rect.Width > thickness)
                spriteBatch.FillRectangle(rect.Right - thickness, rect.Y + thickness, thickness, Math.Max(thickness, rect.Height - thickness), color);
        }

        public static void DrawRectangle(this SpriteBatch spriteBatch, Rect rect, Color color, Offset4 thickness)
        {
            if (color.A == 0)
                return;

            spriteBatch.FillRectangle(rect.X + thickness.Left, rect.Y, rect.Width - thickness.Left, thickness.Top, color);
            spriteBatch.FillRectangle(rect.X, rect.Y, thickness.Left, rect.Height - thickness.Bottom, color);

            if (rect.Height > thickness.Bottom)
                spriteBatch.FillRectangle(rect.X, rect.Bottom - thickness.Bottom, Math.Max(thickness.Left, rect.Width - thickness.Right), thickness.Bottom, color);

            if (rect.Width > thickness.Right)
                spriteBatch.FillRectangle(rect.Right - thickness.Right, rect.Y + thickness.Top, thickness.Right, Math.Max(thickness.Bottom, rect.Height - thickness.Top), color);
        }

        public static void DrawStringShaded(this SpriteBatch spriteBatch, SpriteFont font, string text, Vec2 pos, Color color, Color shadeColor, float shadeOsffset = 1)
        {
            spriteBatch.DrawString(font, text, pos + new Vec2(shadeOsffset, 0), Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vec2(0, shadeOsffset), Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vec2(-shadeOsffset, 0), Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vec2(0, -shadeOsffset), Color.Black);
            spriteBatch.DrawString(font, text, pos, color);
        }

        public static void DrawStrings(this SpriteBatch spriteBatch, SpriteFont font, IEnumerable<(string, Color)> strings, Vector2 startPos)
        {
            Vector2 pos = startPos;

            foreach (var str in strings)
            {
                spriteBatch.DrawString(font, str.Item1, pos, str.Item2);
                pos.X += font.MeasureString(str.Item1).X;
            }
        }
    }
}

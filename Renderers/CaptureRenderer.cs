using Cornifer.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

using Color = Microsoft.Xna.Framework.Color;
using SLColor = SixLabors.ImageSharp.Color;

namespace Cornifer.Renderers
{
    public class CaptureRenderer : Renderer, IDisposable
    {
        RenderTarget2D? RenderTarget = null;
        Rgba32[] Colors = Array.Empty<Rgba32>();
        Image<Rgba32>? ScreenImage = null;

        Image<Rgba32> Image;
        PointF[] LinePoints = new PointF[2];
        bool Capturing = false;

        public override Matrix Projection => Matrix.CreateOrthographicOffCenter(0, RenderTarget!.Width, RenderTarget!.Height, 0, 0, 1);

        public CaptureRenderer(Image<Rgba32> image)
        {
            Image = image;
        }

        public void BeginCapture(int width, int height)
        {
            EnsureRenderSize(width, height);

            Main.Instance.GraphicsDevice.SetRenderTarget(RenderTarget);
            Main.Instance.GraphicsDevice.Clear(Color.Transparent);
            Capturing = true;
        }

        public void EndCapture(Vector2 worldPos, int width, int height)
        {
            Main.Instance.GraphicsDevice.SetRenderTarget(null);

            RenderTarget!.GetData(Colors, 0, RenderTarget.Width * RenderTarget.Height);

            for (int j = 0; j < height; j++)
            {
                Colors.AsSpan(j * RenderTarget.Width, width)
                    .CopyTo(ScreenImage.DangerousGetPixelRowMemory(j).Span);
            }
            Vector2 drawPos = TransformVector(worldPos);
            Image.Mutate(f => f.DrawImage(ScreenImage, new SixLabors.ImageSharp.Point((int)drawPos.X, (int)drawPos.Y), 1));
            Capturing = false;
        }

        public void EnsureRenderSize(int width, int height)
        {
            if (RenderTarget is null || RenderTarget.Width < width || RenderTarget.Height < height)
            {
                RenderTarget?.Dispose();
                RenderTarget = new(Main.Instance.GraphicsDevice, width, height);

            }

            if (ScreenImage is null || ScreenImage.Width != width || ScreenImage.Height != height)
            {
                ScreenImage?.Dispose();
                ScreenImage = new(width, height);
            }

            if (Colors.Length < RenderTarget.Width * RenderTarget.Height)
                Colors = new Rgba32[RenderTarget.Width * RenderTarget.Height];
        }

        public override void DrawTexture(Texture2D texture, Vector2 worldPos, Microsoft.Xna.Framework.Rectangle? source = null, Vector2? worldSize = null, Color? color = null, Vector2? scaleOverride = null)
        {
            if (texture is null)
                return;

            int texWidth = source?.Width ?? texture.Width;
            int texHeight = source?.Height ?? texture.Height;
            if (Capturing)
            {
                Vector2 scale = worldSize is null ? Vector2.One : worldSize.Value / new Vector2(texWidth, texHeight);
                Main.SpriteBatch.Draw(texture, TransformVector(worldPos), source, color ?? Color.White, 0f, Vector2.Zero, scale * Scale, SpriteEffects.None, 0);
                return;
            }

            var prevState = Main.SpriteBatch.GetState();
            Main.SpriteBatch.End();
            BeginCapture(texWidth, texHeight);
            Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Main.SpriteBatch.Draw(texture, Vector2.Zero, source, color ?? Color.White, 0f, Vector2.Zero, scaleOverride ?? Vector2.One, SpriteEffects.None, 0);
            Main.SpriteBatch.End();
            EndCapture(worldPos, texWidth, texHeight);
            Main.SpriteBatch.Begin(prevState);
        }

        public override void DrawRect(Vector2 worldPos, Vector2 size, Color? fill, Color? border = null, float thickness = 1)
        {
            worldPos = TransformVector(worldPos);
            size *= Scale;

            if (border.HasValue)
            {
                SLColor color = new(new Rgba32() { PackedValue = border.Value.PackedValue });
                Image.Mutate(f => f.Draw(color, thickness, new RectangleF(worldPos.X, worldPos.Y, size.X, size.Y)));
            }
            if (fill.HasValue)
            {
                SLColor color = new(new Rgba32() { PackedValue = fill.Value.PackedValue });
                Image.Mutate(f => f.Fill(color, new RectangleF(worldPos.X, worldPos.Y, size.X, size.Y)));
            }
        }

        public override void DrawLine(Vector2 worldPosA, Vector2 worldPosB, Color color, float thickness = 1)
        {
            SLColor slcolor = new(new Rgba32() { PackedValue = color.PackedValue });

            worldPosA = TransformVector(worldPosA);
            worldPosB = TransformVector(worldPosB);

            LinePoints[0] = new(worldPosA.X, worldPosA.Y);
            LinePoints[1] = new(worldPosB.X, worldPosB.Y);

            Image.Mutate(f => f.DrawLines(slcolor, thickness, LinePoints));
        }

        public void Dispose()
        {
            RenderTarget?.Dispose();
            ScreenImage?.Dispose();
        }
    }

}

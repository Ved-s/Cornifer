﻿using Cornifer.MapObjects;
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
    public class CaptureRenderer : Renderer, IDisposable, ICapturingRenderer
    {
        RenderTarget2D? RenderTarget = null;
        Rgba32[] Colors = Array.Empty<Rgba32>();
        Image<Rgba32>? ScreenImage = null;

        public Image<Rgba32> Image;
        PointF[] LinePoints = new PointF[2];

        Vector2 BeforeCapturePos;
        Vector2 CapturePos;
        Vector2 CaptureSize;
        bool Capturing = false;

        public override Matrix Projection => Matrix.CreateOrthographicOffCenter(0, RenderTarget!.Width, RenderTarget!.Height, 0, 0, 1);

        public CaptureRenderer(Image<Rgba32> image)
        {
            Image = image;
        }

        public void BeginCapture(Vector2 pos, Vector2 size)
        {
            EnsureRenderSize((int)size.X, (int)size.Y);

            Main.Instance.GraphicsDevice.SetRenderTarget(RenderTarget);
            Main.Instance.GraphicsDevice.Clear(Color.Transparent);
            CapturePos = pos;
            CaptureSize = size;
            BeforeCapturePos = Position;
            Position = pos;
            Capturing = true;
        }

        public void EndCapture()
        {
            Position = BeforeCapturePos;
            Main.Instance.GraphicsDevice.SetRenderTarget(null);

            RenderTarget!.GetData(Colors, 0, RenderTarget.Width * RenderTarget.Height);

            int width = (int)CaptureSize.X;
            int height = (int)CaptureSize.Y;

            for (int j = 0; j < height; j++)
            {
                Colors.AsSpan(j * RenderTarget.Width, width)
                    .CopyTo(ScreenImage.DangerousGetPixelRowMemory(j).Span);
            }
            Vector2 drawPos = TransformVector(CapturePos);
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
            Vector2 scale = worldSize is null ? Vector2.One : worldSize.Value / new Vector2(texWidth, texHeight);

            if (Capturing)
            {
                Main.SpriteBatch.Draw(texture, TransformVector(worldPos), source, color ?? Color.White, 0f, Vector2.Zero, scale * Scale, SpriteEffects.None, 0);
                return;
            }

            int captureWidth = worldSize.HasValue ? (int)Math.Ceiling(worldSize.Value.X) : texWidth;
            int captureHeight = worldSize.HasValue ? (int)Math.Ceiling(worldSize.Value.Y) : texHeight;

            var prevState = Main.SpriteBatch.GetState();
            Main.SpriteBatch.End();
            BeginCapture(worldPos, new(captureWidth, captureHeight));
            Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Main.SpriteBatch.Draw(texture, Vector2.Zero, source, color ?? Color.White, 0f, Vector2.Zero, scaleOverride ?? scale * Scale, SpriteEffects.None, 0);
            Main.SpriteBatch.End();
            EndCapture();
            Main.SpriteBatch.Begin(prevState);
        }

        public void Dispose()
        {
            RenderTarget?.Dispose();
            ScreenImage?.Dispose();
        }

        public void BeginObjectCapture(MapObject obj, bool shade) { }

        public void EndObjectCapture() { }

        public void BeginLayerCapture(Layer layer, bool shade) { }

        public void EndLayerCapture() { }
    }

}

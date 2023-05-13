using Cornifer.Json;
using Cornifer.MapObjects;
using Cornifer.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace Cornifer.Renderers
{
    public class ImageMapRenderer : Renderer, IDisposable, ICapturingRenderer
    {
        RenderTarget2D? RenderTarget = null;

        Rgba32[] Colors = Array.Empty<Rgba32>();
        byte[] Bytes = Array.Empty<byte>();

        Vector2 BeforeCapturePos;
        Vector2 CapturePos;
        Vector2 CaptureSize;
        bool Capturing = false;

        public override Matrix Projection => Matrix.CreateOrthographicOffCenter(0, RenderTarget!.Width, RenderTarget!.Height, 0, 0, 1);

        JsonArray JsonObjectArray = new();

        MapObject? CurrentObject;
        bool CurrentObjectShade;
        List<ObjectTexture> ObjectTextures = new();

        Dictionary<MapObject, JsonObject> WrittenObjects = new();

        MemoryStream ImageStream = new();

        Point TopLeft;
        Point BottomRight;


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

            Image<Rgba32> image = new(width, height);

            for (int j = 0; j < height; j++)
            {
                Colors.AsSpan(j * RenderTarget.Width, width)
                    .CopyTo(image.DangerousGetPixelRowMemory(j).Span);
            }

            Point point = CapturePos.ToPoint();

            if (CurrentObject is null)
            {
                RegisterTLBR(point, image);
                WriteObject(null, image, point, false);
            }
            else
            {
                ObjectTextures.Add(new(point, image));
            }
            Capturing = false;
        }

        public void EnsureRenderSize(int width, int height)
        {
            if (RenderTarget is null || RenderTarget.Width < width || RenderTarget.Height < height)
            {
                RenderTarget?.Dispose();
                RenderTarget = new(Main.Instance.GraphicsDevice, width, height);

            }

            EnsureArraySize(ref Colors, RenderTarget.Width * RenderTarget.Height);
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

        public void BeginObjectCapture(MapObject obj, bool shade)
        {
            CurrentObject = obj;
            CurrentObjectShade = shade;
            ObjectTextures.Clear();
        }

        public void EndObjectCapture()
        {
            Image<Rgba32>? img = CombineImages(out Point pos);

            if (img is not null)
            {
                RegisterTLBR(pos, img);
                WriteObject(CurrentObject, img, pos, CurrentObjectShade);
            }
            CurrentObject = null;
        }

        public JsonObject Finish()
        {
            if (CurrentObject is not null)
                EndObjectCapture();

            return new()
            {
                ["dimensions"] = new JsonObject
                {
                    ["top"] = TopLeft.Y,
                    ["left"] = TopLeft.X,
                    ["bottom"] = BottomRight.Y,
                    ["right"] = BottomRight.X,
                },
                ["objects"] = JsonObjectArray
            };
        }

        public void Dispose()
        {
            RenderTarget?.Dispose();
            ImageStream.Dispose();
        }

        void RegisterTLBR(Point pos, Image<Rgba32> img)
        {
            TopLeft.X = Math.Min(TopLeft.X, pos.X);
            TopLeft.Y = Math.Min(TopLeft.Y, pos.Y);

            BottomRight.X = Math.Max(BottomRight.X, pos.X + img.Width);
            BottomRight.Y = Math.Max(BottomRight.Y, pos.Y + img.Height);
        }

        void WriteObject(MapObject? obj, Image<Rgba32> image, Point pos, bool shade)
        {
            if (obj is null || !WrittenObjects.TryGetValue(obj, out JsonObject? json))
            {
                json = new();
                JsonObjectArray.Add(json);

                if (obj is not null)
                {
                    WrittenObjects[obj] = json;
                    json["data"] = new JsonObject
                    {
                        ["name"] = obj.Name,
                        ["type"] = obj.GetType().Name,
                    };
                }
            }

            if (shade)
            {
                json["shade"] = SaveBase64Image(image);
                json["shade_pos"] = JsonTypes.SaveVector2(pos.ToVector2());
            }
            else 
            {
                json["image"] = SaveBase64Image(image);
                json["pos"] = JsonTypes.SaveVector2(pos.ToVector2());
            }
        }

        Image<Rgba32>? CombineImages(out Point pos)
        {
            Image<Rgba32>? image = null;
            pos = default;
            if (ObjectTextures.Count == 0)
            {

            }
            else if (ObjectTextures.Count == 1)
            {
                (pos, image) = ObjectTextures[0];
            }
            else
            {
                ObjectTexture first = ObjectTextures[0];

                Point tl = first.Position;
                Point br = first.Position + new Point(first.Image.Width, first.Image.Width);

                for (int i = 1; i < ObjectTextures.Count; i++)
                {
                    ObjectTexture texture = ObjectTextures[i];

                    tl.X = Math.Min(tl.X, texture.Position.X);
                    tl.Y = Math.Min(tl.Y, texture.Position.Y);

                    br.X = Math.Max(br.X, texture.Position.X + texture.Image.Width);
                    br.Y = Math.Max(br.Y, texture.Position.Y + texture.Image.Height);
                }

                image = new(br.X - tl.X, br.Y - tl.Y);
                pos = tl;

                image.Mutate(ctx =>
                {
                    for (int i = 0; i < ObjectTextures.Count; i++)
                    {
                        ObjectTexture tex = ObjectTextures[i];
                        Point drawpos = tex.Position - tl;
                        ctx.DrawImage(tex.Image, new SixLabors.ImageSharp.Point(drawpos.X, drawpos.Y), 1);
                    }
                });
            }
            ObjectTextures.Clear();
            return image;

        }

        string SaveBase64Image(Image<Rgba32> image)
        {
            ImageStream.Position = 0;
            ImageStream.SetLength(0);

            image.SaveAsPng(ImageStream);

            EnsureArraySize(ref Bytes, Base64.GetMaxEncodedToUtf8Length((int)ImageStream.Length));
            Base64.EncodeToUtf8(ImageStream.AsSpan(), Bytes, out _, out int bytesWritten);

            return Encoding.UTF8.GetString(Bytes, 0, bytesWritten);
        }

        void EnsureArraySize<T>(ref T[] arr, int size)
        {
            if (arr.Length < size)
                arr = new T[size];
        }

        record struct ObjectTexture(Point Position, Image<Rgba32> Image);
    }
}

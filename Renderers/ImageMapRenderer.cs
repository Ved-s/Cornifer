using Cornifer.MapObjects;
using Cornifer.UI;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.ImageSharp.Processing.Processors.Drawing;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace Cornifer.Renderers
{
    public class ImageMapRenderer : Renderer, IDisposable
    {
        RenderTarget2D? RenderTarget = null;

        Rgba32[] Colors = Array.Empty<Rgba32>();
        byte[] Bytes = Array.Empty<byte>();

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

            Image<Rgba32> image = new(width, height);

            for (int j = 0; j < height; j++)
            {
                Colors.AsSpan(j * RenderTarget.Width, width)
                    .CopyTo(image.DangerousGetPixelRowMemory(j).Span);
            }

            Point point = worldPos.ToPoint();

            if (CurrentObject is null)
            {
                RegisterTLBR(point, image);
                WriteObject(null, image, point, null);
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
            BeginCapture(captureWidth, captureHeight);
            Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Main.SpriteBatch.Draw(texture, Vector2.Zero, source, color ?? Color.White, 0f, Vector2.Zero, scaleOverride ?? scale * Scale, SpriteEffects.None, 0);
            Main.SpriteBatch.End();
            EndCapture(worldPos, captureWidth, captureHeight);
            Main.SpriteBatch.Begin(prevState);
        }

        public void StartObjectCapture(MapObject obj, bool shade)
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
                if (CurrentObjectShade)
                    WriteObject(CurrentObject, null, pos, img);
                else
                    WriteObject(CurrentObject, img, pos, null);
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

        void WriteObject(MapObject? obj, Image<Rgba32>? image, Point? pos, Image<Rgba32>? shade)
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
                        // TODO: object data
                    };
                }
            }

            if (image is not null)
                json["image"] = SaveBase64Image(image);

            if (pos is not null)
            {
                json["pos"] = new JsonObject
                {
                    ["x"] = pos.Value.X,
                    ["y"] = pos.Value.Y,
                };
            }

            if (shade is not null)
                json["shade"] = SaveBase64Image(shade);
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

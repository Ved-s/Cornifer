using Cornifer.Capture.PSD;
using Cornifer.MapObjects;
using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace Cornifer.Capture
{
    public static class Capture
    {
        public const int BorderSize = 30;

        static CaptureRenderer CreateRenderer(Predicate<MapObject>? objectPredicate = null)
        {
            Vector2 tl = Vector2.Zero;
            Vector2 br = Vector2.Zero;

            void ProcessObjectRect(MapObject obj)
            {
                if (!obj.Active)
                    return;

                tl.X = Math.Min(tl.X, obj.VisualPosition.X);
                tl.Y = Math.Min(tl.Y, obj.VisualPosition.Y);

                br.X = Math.Max(br.X, obj.VisualPosition.X + obj.VisualSize.X);
                br.Y = Math.Max(br.Y, obj.VisualPosition.Y + obj.VisualSize.Y);

                foreach (MapObject child in obj.Children)
                    ProcessObjectRect(child);
            }

            foreach (MapObject obj in Main.WorldObjectLists)
                if (objectPredicate is null || objectPredicate(obj))
                    ProcessObjectRect(obj);

            tl -= new Vector2(30);
            br += new Vector2(30);

            int width = (int)(br.X - tl.X);
            int height = (int)(br.Y - tl.Y);

            Image<Rgba32> image = new(width, height);

            CaptureRenderer renderer = new(image)
            {
                Position = tl
            };

            return renderer;
        }

        public static Image<Rgba32> CaptureObjects(Predicate<MapObject> predicate)
        {
            CaptureRenderer renderer = CreateRenderer(predicate);

            // TODO: move predicate to DrawMap
            lock (Main.DrawLock)
            {
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);

                if (InterfaceState.DrawBorders.Value)
                    foreach (Layer l in Main.Layers)
                        if (l.Visible)
                        {
                            renderer.BeginLayerCapture(l, true);
                            l.DrawShade(renderer, predicate);
                            renderer.EndLayerCapture();
                        }

                foreach (Layer l in Main.Layers)
                    if (l.Visible)
                    {
                        renderer.BeginLayerCapture(l, false);
                        l.Draw(renderer, predicate);
                        renderer.EndLayerCapture();
                    }
            }
            Main.SpriteBatch.End();

            return renderer.Image;
        }

        public static Image<Rgba32> CaptureMap()
        {
            CaptureRenderer renderer = CreateRenderer();

            Main.DrawMap(renderer, null, null);

            return renderer.Image;
        }

        public static void CaptureMapToLayerImages(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
            using CaptureRenderer renderer = CreateRenderer();
            int order = 1;
            CaptureMapLayered(renderer, info =>
            {
                string filePath = Path.Combine(dirPath, $"{Main.Region?.Id}_{order}_{info.Layer.Name}{(info.Shadow ? "Border" : "")}.png");
                order++;
                renderer.Image.SaveAsPng(filePath);
            });
        }

        public static void CaptureMapToPSD(string path)
        {
            using CaptureRenderer renderer = CreateRenderer();
            PSDFile psd = new()
            {
                Width = (uint)renderer.Image.Width,
                Height = (uint)renderer.Image.Height,
            };
            CaptureMapLayered(renderer, info =>
            {
                ImageBorder.GetEmptySides(renderer.Image, out int top, out int bottom, out int left, out int right);

                int croppedWidth = renderer.Image.Width - left - right;
                int croppedHeight = renderer.Image.Height - top - bottom;

                croppedHeight = Math.Max(1, croppedHeight);
                croppedWidth = Math.Max(1, croppedWidth);

                byte[] data = new byte[croppedWidth * croppedHeight * 4];

                for (int j = 0; j < croppedHeight; j++)
                {
                    Span<Rgba32> src = renderer.Image
                        .DangerousGetPixelRowMemory(j + top).Span
                        .Slice(left, croppedWidth);
                    Span<byte> dst = data.AsSpan(j * croppedWidth * 4, croppedWidth * 4);

                    src.CopyTo(MemoryMarshal.Cast<byte, Rgba32>(dst));
                }

                psd.Layers.Add(new()
                {
                    X = (uint)left,
                    Y = (uint)top,
                    Width = (uint)croppedWidth,
                    Height = (uint)croppedHeight,
                    Data = data,
                    Name = info.Shadow ? $"{info.Layer.Name}_Shadow" : info.Layer.Name,
                    Opacity = 255,
                    Visible = info.Layer.Visible
                });
            });

            uint top = psd.Height;
            uint left = psd.Width;
            uint bottom = psd.Height;
            uint right = psd.Width;

            foreach (PSDFile.Layer layer in psd.Layers) 
            {
                left = Math.Min(left, layer.X);
                top = Math.Min(top, layer.Y);

                bottom = Math.Min(bottom, psd.Height - (layer.Y + layer.Height));
                right = Math.Min(right, psd.Width - (layer.X + layer.Width));
            }

            int xd = BorderSize - (int)left;
            int yd = BorderSize - (int)top;

            psd.Width = (BorderSize - left) + psd.Width + (BorderSize - right);
            psd.Height = (BorderSize - top) + psd.Height + (BorderSize - bottom);

            for (int i = 0; i < psd.Layers.Count; i++)
            {
                PSDFile.Layer layer = psd.Layers[i];
                layer.X = (uint)(layer.X + xd);
                layer.Y = (uint)(layer.Y + yd);
                psd.Layers[i] = layer;
            }

            ThreadPool.QueueUserWorkItem((_) =>
            {
                using FileStream fs = File.Create(path);
                psd.Write(fs);
                psd = null!;
                GC.Collect();
            });
        }

        public static void CaptureImageMap(string jsonPath)
        {
            using ImageMapRenderer renderer = new();

            Main.DrawMap(renderer, null, null, true, false);

            using (FileStream fs = File.Create(jsonPath))
                JsonSerializer.Serialize(fs, renderer.Finish());
        }

        static void CaptureMapLayered(CaptureRenderer renderer, Action<CapturedLayerInfo> layerHandler)
        {
            foreach (Layer layer in Main.Layers)
            {
                CaptureMapLayer(renderer, layer, true);
                layerHandler(new(layer, true));
            }

            foreach (Layer layer in Main.Layers)
            {
                CaptureMapLayer(renderer, layer, false);
                layerHandler(new(layer, false));
            }
        }

        static void CaptureMapLayer(CaptureRenderer renderer, Layer layer, bool shadow)
        {
            for (int j = 0; j < renderer.Image.Height; j++)
            {
                Span<Rgba32> row = renderer.Image.DangerousGetPixelRowMemory(j).Span;

                for (int i = 0; i < row.Length; i++)
                    row[i] = new(0);
            }

            Main.DrawMap(renderer, layer, shadow);
        }

        record struct CapturedLayerInfo(Layer Layer, bool Shadow);
    }
}

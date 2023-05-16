using Cornifer.Capture.PSD;
using Cornifer.MapObjects;
using Cornifer.Renderers;
using Cornifer.Structures;
using Cornifer.UI.Pages;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace Cornifer.Capture
{
    public static class Capture
    {
        static CaptureRenderer CreateRenderer()
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
                byte[] data = new byte[renderer.Image.Height * renderer.Image.Width * 4];

                for (int j = 0; j < renderer.Image.Height; j++)
                {
                    Span<Rgba32> src = renderer.Image.DangerousGetPixelRowMemory(j).Span;
                    Span<byte> dst = data.AsSpan(j * renderer.Image.Width * 4, renderer.Image.Width * 4);

                    src.CopyTo(MemoryMarshal.Cast<byte, Rgba32>(dst));
                }

                psd.Layers.Add(new()
                {
                    Data = data,
                    Name = info.Shadow ? $"{info.Layer.Name}_Shadow" : info.Layer.Name,
                    Opacity = 255,
                    Visible = info.Layer.Visible
                });
            });

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

            Main.DrawMap(renderer, null, null);

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

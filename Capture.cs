using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer
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

            Main.DrawMap(renderer, Main.ActiveRenderLayers, null);

            return renderer.Image;
        }

        public static void CaptureMapLayered(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
            CaptureRenderer renderer = CreateRenderer();

            for (int i = 0; i < 4; i++)
            {
                RenderLayers layer = (RenderLayers)(1 << i);

                CaptureMapLayer(renderer, layer, true, dirPath);
                CaptureMapLayer(renderer, layer, false, dirPath);
            }

            renderer.Dispose();
        }

        static void CaptureMapLayer(CaptureRenderer renderer, RenderLayers layer, bool shadow, string dir)
        {
            for (int j = 0; j < renderer.Image.Height; j++)
            {
                Span<Rgba32> row = renderer.Image.DangerousGetPixelRowMemory(j).Span;

                for (int i = 0; i < row.Length; i++)
                    row[i] = new(0);
            }

            Main.DrawMap(renderer, layer, shadow);

            string filePath = Path.Combine(dir, $"{Main.Region?.Id}_{layer}{(shadow ? "Border" : "")}.png");
            renderer.Image.SaveAsPng(filePath);
        }
    }
}

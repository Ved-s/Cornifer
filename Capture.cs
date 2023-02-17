using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer
{
    public static class Capture
    {
        public static Image<Rgba32> CaptureMap()
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

            Main.DrawMap(renderer);

            return image;
        }
    }
}

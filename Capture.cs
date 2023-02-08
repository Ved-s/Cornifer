using Cornifer.Interfaces;
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
        public static Image<Rgba32> CaptureRegion(Region region)
        {
            Vector2 tl = Vector2.Zero;
            Vector2 br = Vector2.Zero;

            foreach (ISelectable selectable in region.EnumerateSelectables())
            {
                tl.X = Math.Min(tl.X, selectable.Position.X);
                tl.Y = Math.Min(tl.Y, selectable.Position.Y);

                br.X = Math.Max(br.X, selectable.Position.X + selectable.Size.X);
                br.Y = Math.Max(br.Y, selectable.Position.Y + selectable.Size.Y);
            }

            int width = (int)(br.X - tl.X);
            int height = (int)(br.Y - tl.Y);

            Image<Rgba32> image = new(width, height);

            CaptureRenderer renderer = new(image)
            {
                Position = tl
            };

            region.Draw(renderer);

            return image;
        }
    }
}

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public static class ImageExtensions
    {
        public static Image<Rgba32> ValidateBorder(this Image<Rgba32> image, int borderSize = 30)
        {
            int top = -1;
            int bottom = -1;

            int left = image.Width;
            int right = image.Width;

            for (int j = 0; j < image.Height && top < 0; j++)
            {
                Span<Rgba32> row = image.DangerousGetPixelRowMemory(j).Span;
                for (int i = 0; i < image.Width; i++)
                    if (row[i].A > 0)
                    {
                        top = j;
                        break;
                    }
            }

            // image is empty
            if (top < 0)
                return image;

            for (int j = image.Height - 1; j >= 0 && bottom < 0; j--)
            {
                Span<Rgba32> row = image.DangerousGetPixelRowMemory(j).Span;
                for (int i = 0; i < image.Width; i++)
                    if (row[i].A > 0)
                    {
                        bottom = image.Height - 1 - j;
                        break;
                    }
            }

            for (int j = 0; j < image.Height; j++)
            {
                Span<Rgba32> row = image.DangerousGetPixelRowMemory(j).Span;
                for (int i = 0; i < image.Width; i++)
                    if (row[i].A > 0)
                    {
                        left = Math.Min(left, i);
                        break;
                    }

                for (int i = image.Width - 1; i >= 0; i--)
                    if (row[i].A > 0)
                    {
                        right = Math.Min(right, image.Width - 1 - i);
                        break;
                    }
            }

            int newWidth = (borderSize - left) + image.Width + (borderSize - right);
            int newHeight = (borderSize - top) + image.Height + (borderSize - bottom);

            Rectangle src = new(left, top, image.Width - left - right, image.Height - top - bottom);
            Rectangle dst = new(borderSize, borderSize, src.Width, src.Height);

            image.Mutate(i => i.Resize(newWidth, newHeight, KnownResamplers.NearestNeighbor, src, dst, false));

            return image;
        }
    }
}

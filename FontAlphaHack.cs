using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public static class FontAlphaHack
    {
        public static void Apply(SpriteFont font, byte alphaThreshold)
        {
            Texture2D tex = font.Texture;

            if (tex.Format != SurfaceFormat.Dxt3)
                return;

            int size = font.Texture.Width * font.Texture.Height / 16;
            Dxt3Chunk[] chunks = ArrayPool<Dxt3Chunk>.Shared.Rent(size);
            tex.GetData(chunks, 0, size);

            for (int i = 0; i < size; i++)
            {
                Dxt3Chunk chunk = chunks[i];

                chunk.Color0 = 0;
                chunk.Color1 = 65535;

                for (int j = 0; j < 16; j++)
                {
                    int alphaShift = j * 4;
                    int codesShift = j * 2;
                    byte alpha = (byte)((chunk.Alpha >> alphaShift) & 0x0f);
                    byte code  = (byte)((chunk.Codes >> codesShift) & 0x03);

                    if (alpha >= alphaThreshold)
                    {
                        alpha = 15;
                        code = 1;
                    }
                    else
                    {
                        alpha = 0;
                        code = 0;
                    }
                
                    chunk.Alpha = (chunk.Alpha & ~(0x0fUL << alphaShift)) | ((ulong)alpha << alphaShift);
                    chunk.Codes = (chunk.Codes & ~(0x03U << codesShift)) | ((uint)code << codesShift);
                }
                chunks[i] = chunk;
            }

            tex.SetData(chunks, 0, size);

            ArrayPool<Dxt3Chunk>.Shared.Return(chunks);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Dxt3Chunk
        {
            public ulong Alpha;
            public ushort Color0;
            public ushort Color1;
            public uint Codes;
        }
    }
}

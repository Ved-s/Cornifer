using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cornifer
{
    // Currently does not work, something is wrong in channel data writing
    public class PSDFile
    {
        static readonly byte[] Magic8BPS = Encoding.ASCII.GetBytes("8BPS");
        static readonly byte[] Magic8BIM = Encoding.ASCII.GetBytes("8BIM");
        static readonly byte[] BlendNormal = Encoding.ASCII.GetBytes("norm");

        public uint Width, Height;
        public List<Layer> Layers = new();

        public void Write(Stream stream)
        {
            BigEndianWriter writer = new(stream);

            WriteHeader(writer);
            WriteColorModeData(writer);
            WriteImageResources(writer);
            WriteLayerMaskInfo(writer);
        }

        void WriteHeader(BigEndianWriter writer)
        {
            writer.Write(Magic8BPS);
            writer.Write((ushort)1); // Version
            writer.Write(0u);        // Reserved
            writer.Write((ushort)0); // Reserved
            writer.Write((ushort)4); // Channels
            writer.Write(Height);
            writer.Write(Width);
            writer.Write((ushort)8); // Bits per channel
            writer.Write((ushort)3); // Color mode, RGB
        }

        void WriteColorModeData(BigEndianWriter writer)
        {
            writer.Write((uint)0);   // Length
        }

        void WriteImageResources(BigEndianWriter writer)
        {
            writer.Write((uint)0);   // Length
        }

        void WriteLayerMaskInfo(BigEndianWriter writer)
        {
            SizedWrite(writer, false, writer => // Layer and mask information
            {
                SizedWrite(writer, true, writer => // Layer info
                {
                    writer.Write((ushort)Layers.Count);

                    foreach (Layer layer in Layers) // Layer records
                    {
                        writer.Write(0u); // Top
                        writer.Write(0u); // Left
                        writer.Write(Height); // Bottom
                        writer.Write(Width); // Right
                        writer.Write((ushort)4); // Number of channels

                        uint channellength = (uint)layer.Data.Length / 4;

                        

                        for (int i = 0; i < 4; i++)
                        {
                            writer.Write((ushort)i);      // Channel type
                            writer.Write(channellength);  // Channel data length
                        }

                        writer.Write(Magic8BIM);
                        writer.Write(BlendNormal);
                        writer.Write(layer.Opacity);
                        writer.Write((byte)0);            // Clipping
                        writer.Write(layer.Visible ? (byte)0x02 : (byte)0x00); // Flags. bit 1 = Visible
                        writer.Write((byte)0);            // Filler

                        SizedWrite(writer, false, writer =>
                        {
                            writer.Write(0u);                 // Layer mask length
                            writer.Write(0u);                 // Layer blending ranges length

                            byte[] namebytes = Encoding.UTF8.GetBytes(layer.Name);
                            writer.Write((byte)namebytes.Length);
                            writer.Write(namebytes);

                            int pad = 4 - ((1 + namebytes.Length) % 4);
                            for (int i = 0; i < pad; i++)
                                writer.Write((byte)0);
                        });
                    }

                    foreach (Layer layer in Layers) // Channel image data
                    {
                        for (int c = 0; c < 4; c++)
                        {
                            writer.Write((ushort)0); // No compression
                            for (int i = c; i < layer.Data.Length; i += 4)
                            {
                                writer.Write(layer.Data[i]);
                            }
                            //if ((layer.Data.Length / 4) % 2 == 1)
                            //    writer.Write((byte)0);
                        }

                       
                    }
                });
            });

            while (writer.BaseStream.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write(0u); // Global layer mask data
        }

        void SizedWrite(BigEndianWriter writer, bool roundEvenLength, Action<BigEndianWriter> callback)
        {
            uint length;

            if (!writer.BaseStream.CanSeek)
            {
                MemoryStream ms = new();
                BigEndianWriter mswriter = new(ms);
                callback(mswriter);

                length = (uint)ms.Length;
                if (length % 2 == 1 && roundEvenLength)
                    length++;

                writer.Write(length);
                ms.Position = 0;
                ms.CopyTo(writer.BaseStream);
                return;
            }

            long lenpos = writer.BaseStream.Position;
            writer.Write(0u);
            long datastart = writer.BaseStream.Position;
            callback(writer);
            long curpos = writer.BaseStream.Position;
            writer.BaseStream.Seek(lenpos, SeekOrigin.Begin);
            length = (uint)(curpos - datastart);

            if (length % 2 == 1 && roundEvenLength)
                length++;

            writer.Write(length);
            writer.BaseStream.Seek(curpos, SeekOrigin.Begin);
        }

        public struct Layer
        {
            public bool Visible;
            public byte[] Data;
            public byte Opacity;
            public string Name;
        }
    }

    public class BigEndianWriter
    {
        public Stream BaseStream { get; }

        public BigEndianWriter(Stream stream)
        {
            BaseStream = stream;
        }

        public void Write(byte[] data)
        {
            BaseStream.Write(data, 0, data.Length);
        }

        public void Write(byte[] data, int offset, int count)
        {
            BaseStream.Write(data, offset, count);
        }

        public void Write(byte v)
        {
            byte[] buf = ArrayPool<byte>.Shared.Rent(1);
            buf[0] = v;
            Write(buf, 0, 1);
            ArrayPool<byte>.Shared.Return(buf);
        }

        public void Write(short v)
        {
            byte[] buf = ArrayPool<byte>.Shared.Rent(2);
            buf[0] = (byte)(v >> 8);
            buf[1] = (byte)v;
            Write(buf, 0, 2);
            ArrayPool<byte>.Shared.Return(buf);
        }

        public void Write(ushort v)
        {
            byte[] buf = ArrayPool<byte>.Shared.Rent(2);
            buf[0] = (byte)(v >> 8);
            buf[1] = (byte)v;
            Write(buf, 0, 2);
            ArrayPool<byte>.Shared.Return(buf);
        }

        public void Write(int v)
        {
            byte[] buf = ArrayPool<byte>.Shared.Rent(4);
            buf[0] = (byte)(v >> 24);
            buf[1] = (byte)(v >> 16);
            buf[2] = (byte)(v >> 8);
            buf[3] = (byte)v;
            Write(buf, 0, 4);
            ArrayPool<byte>.Shared.Return(buf);
        }

        public void Write(uint v)
        {
            byte[] buf = ArrayPool<byte>.Shared.Rent(4);
            buf[0] = (byte)(v >> 24);
            buf[1] = (byte)(v >> 16);
            buf[2] = (byte)(v >> 8);
            buf[3] = (byte)v;
            Write(buf, 0, 4);
            ArrayPool<byte>.Shared.Return(buf);
        }

        public void Write(long v)
        {
            byte[] buf = ArrayPool<byte>.Shared.Rent(8);
            buf[0] = (byte)(v >> 56);
            buf[1] = (byte)(v >> 48);
            buf[2] = (byte)(v >> 40);
            buf[3] = (byte)(v >> 32);
            buf[4] = (byte)(v >> 24);
            buf[5] = (byte)(v >> 16);
            buf[6] = (byte)(v >> 8);
            buf[7] = (byte)v;
            Write(buf, 0, 8);
            ArrayPool<byte>.Shared.Return(buf);
        }

        public void Write(ulong v)
        {
            byte[] buf = ArrayPool<byte>.Shared.Rent(8);
            buf[0] = (byte)(v >> 56);
            buf[1] = (byte)(v >> 48);
            buf[2] = (byte)(v >> 40);
            buf[3] = (byte)(v >> 32);
            buf[4] = (byte)(v >> 24);
            buf[5] = (byte)(v >> 16);
            buf[6] = (byte)(v >> 8);
            buf[7] = (byte)v;
            Write(buf, 0, 8);
            ArrayPool<byte>.Shared.Return(buf);
        }
    }
}

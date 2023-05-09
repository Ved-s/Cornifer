using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cornifer
{
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

            using (TaskProgress mainprog = new("Writing PSD", 5))
            {
                WriteHeader(writer);
                mainprog.Progress = 1;
                WriteColorModeData(writer);
                mainprog.Progress = 2;
                WriteImageResources(writer);
                mainprog.Progress = 3;
                WriteLayerMaskInfo(writer);
                mainprog.Progress = 4;
                WriteImageData(writer);
                mainprog.Progress = 5;
            }
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

                    using MemoryStream layersImageData = new();
                    BigEndianWriter imgWriter = new(layersImageData);

                    using MemoryStream rleTemp = new();
                    using RleStream rle = new(rleTemp);

                    using (TaskProgress prog = new("Writing layers", Layers.Count))
                    {
                        foreach (Layer layer in Layers) // Layer records
                        {
                            writer.Write(0u); // Top
                            writer.Write(0u); // Left
                            writer.Write(Height); // Bottom
                            writer.Write(Width); // Right
                            writer.Write((ushort)4); // Number of channels

                            int channelLength = WriteChannel(imgWriter, rleTemp, rle, layer, 0);

                            writer.Write((ushort)0);                // Channel type: Red
                            writer.Write((uint)channelLength);      // Channel data length

                            channelLength = WriteChannel(imgWriter, rleTemp, rle, layer, 1);

                            writer.Write((ushort)1);                // Channel type: Green
                            writer.Write((uint)channelLength);      // Channel data length

                            channelLength = WriteChannel(imgWriter, rleTemp, rle, layer, 2);

                            writer.Write((ushort)2);                // Channel type: Blue
                            writer.Write((uint)channelLength);      // Channel data length

                            channelLength = WriteChannel(imgWriter, rleTemp, rle, layer, 3);

                            writer.Write((short)-1);                // Channel type: Transparency
                            writer.Write((uint)channelLength);      // Channel data length

                            writer.Write(Magic8BIM);
                            writer.Write(BlendNormal);
                            writer.Write(layer.Opacity);
                            writer.Write((byte)0);            // Clipping
                            writer.Write(layer.Visible ? (byte)0x00 : (byte)0x02); // Flags. bit 1 = Invisible
                            writer.Write((byte)0);            // Filler

                            SizedWrite(writer, false, writer =>
                            {
                                writer.Write(0u);                 // Layer mask length
                                writer.Write(0u);                 // Layer blending ranges length

                                byte[] namebytes = Encoding.UTF8.GetBytes(layer.Name);
                                writer.Write((byte)namebytes.Length);
                                writer.Write(namebytes);

                                while (writer.BaseStream.Position % 4 != 0)
                                    writer.Write((byte)0);

                                writer.Write((ushort)0);

                                //int pad = 4 - ((1 + namebytes.Length) % 4);
                                //for (int i = 0; i < pad; i++)
                                //    writer.Write((byte)0);
                            });

                            prog.Progress += 1;
                        }

                        //foreach (Layer layer in Layers) // Channel image data
                        //{
                        //    for (int c = 0; c < 4; c++)
                        //    {
                        //        // writer.Write((ushort)0); // No compression
                        //        // for (int i = c; i < layer.Data.Length; i += 4)
                        //        //     writer.Write(layer.Data[i]);
                        //
                        //        prog.Progress += .25f;
                        //    }
                        //}
                    }

                    layersImageData.Position = 0;
                    layersImageData.CopyTo(writer.BaseStream);
                });

                //writer.Write(0u); // Global layer mask data length
            });
        }

        int WriteChannel(BigEndianWriter writer, MemoryStream temp, RleStream rle, Layer layer, int c)
        {
            temp.Position = 0;
            temp.SetLength(0);

            long start = writer.BaseStream.Position;

            writer.Write((ushort)1); // RLE compression

            long rowStart = 0;
            int posCounter = c;
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    rle.WriteByte(layer.Data[posCounter]);
                    posCounter += 4;
                }
                rle.Flush();

                ushort rowLength = (ushort)(temp.Position - rowStart);
                writer.Write(rowLength);
                rowStart = temp.Position;
            }

            temp.Position = 0;
            temp.CopyTo(writer.BaseStream);

            int length = (int)(writer.BaseStream.Position - start);
            //if (length % 2 == 1)
            //{
            //    writer.Write((byte)0);
            //    length++;
            //}

            return length;
        }

        void WriteImageData(BigEndianWriter writer)
        {
            //writer.Write((ushort)0); // Compression: None
            //for (int i = 0; i < Width * Height; i++)
            //    writer.Write(0u);    // Empty preview

            using MemoryStream temp = new();
            RleStream rle = new(temp);

            writer.Write((ushort)1);   // RLE compressed zeroes!

            temp.Position = 0;
            temp.SetLength(0);

            long rowStart = 0;
            for (int c = 0; c < 4; c++)
            {
                for (int j = 0; j < Height; j++)
                {
                    for (int i = 0; i < Width; i++)
                    {
                        rle.WriteByte(0);
                    }
                    rle.Flush();

                    ushort rowLength = (ushort)(temp.Position - rowStart);
                    writer.Write(rowLength);
                    rowStart = temp.Position;
                }

            }
            temp.Position = 0;
            temp.CopyTo(writer.BaseStream);

            //writer.Write((uint)0);
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
}

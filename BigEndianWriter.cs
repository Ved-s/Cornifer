using System.Buffers;
using System.IO;

namespace Cornifer
{
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

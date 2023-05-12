using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Cornifer.Capture.PSD
{
    class RleStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => Pos; set => throw new NotSupportedException(); }

        long Pos = 0;
        Stream Stream;
        List<byte> LastWrittenBytes = new();
        bool Running = false;
        int RunLength;
        byte RunningByte;

        public RleStream(Stream stream)
        {
            Stream = stream;
        }

        public override void Flush()
        {
            if (Running)
                EndRun();

            FlushLWB();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                Write(buffer[i + offset]);
        }

        void FlushLWB()
        {
            while (LastWrittenBytes.Count > 0)
            {
                Span<byte> span = CollectionsMarshal.AsSpan(LastWrittenBytes);
                if (span.Length > 128)
                    span = span.Slice(0, 128);

                Stream.WriteByte((byte)(span.Length - 1));
                Stream.Write(span);

                LastWrittenBytes.RemoveRange(0, span.Length);
            }
        }

        void EndRun()
        {
            if (!Running)
                return;

            if (RunLength == 1)
            {
                Running = false;
                LastWrittenBytes.Add(RunningByte);
                return;
            }

            while (RunLength > 0)
            {
                int write = Math.Min(RunLength, 128);

                byte b = (byte)(write - 2 ^ 0xff);
                Stream.WriteByte(b);
                Stream.WriteByte(RunningByte);
                RunLength -= write;
            }
            Running = false;
        }

        void Write(byte b)
        {
            if (Running)
            {
                if (b != RunningByte)
                {
                    EndRun();
                }
                else
                {
                    RunLength++;
                    if (RunLength == 128)
                        EndRun();
                    return;
                }
            }

            LastWrittenBytes.Add(b);

            if (LastWrittenBytes.Count < 2)
                return;

            if (LastWrittenBytes[^1] == LastWrittenBytes[^2])
            {
                RunningByte = LastWrittenBytes[^1];
                LastWrittenBytes.RemoveRange(LastWrittenBytes.Count - 2, 2);
                FlushLWB();
                Running = true;
                RunLength = 2;
                return;
            }

            if (LastWrittenBytes.Count >= 128)
                FlushLWB();
        }
    }
}

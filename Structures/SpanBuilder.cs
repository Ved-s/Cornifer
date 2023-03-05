using System;

namespace Cornifer.Structures
{
    public ref struct SpanBuilder<T>
    {
        public Span<T> Span { get; private set; }
        public int Position { get; set; }

        public SpanBuilder(Span<T> span)
        {
            Span = span;
            Position = 0;
        }

        public void Append(ReadOnlySpan<T> value)
        {
            value.CopyTo(Span.Slice(Position, value.Length));
            Position += value.Length;
        }

        public void Append(T value)
        {
            Span[Position] = value;
            Position++;
        }

        public Span<T> SliceSpan()
        {
            return Span.Slice(0, Position);
        }
    }
}

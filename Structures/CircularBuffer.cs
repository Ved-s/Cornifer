using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Cornifer.Structures
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        T[] Array;
        int Position = 0;

        public int Count { get; private set; }
        public int Capacity => Array.Length;

        public CircularBuffer(int capacity)
        {
            Array = new T[capacity];
        }

        public T this[int index]
        {
            get
            {
                if (index >= 0) return Array[FixIndex(Position - Count + index)];
                return Array[FixIndex(index + Position)];
            }
            set
            {
                if (index >= 0) Array[FixIndex(Position - Count + index)] = value;
                else Array[FixIndex(index + Position)] = value;
            }
        }

        public void Clear()
        {
            if (Count == 0)
                return;

            for (int i = 0; i < Count; i++)
                this[i] = default!;
            Count = 0;
            Position = 0;
        }

        public void Push(T item)
        {
            Array[Position] = item;
            Position = (Position + 1) % Capacity;
            Count = Math.Min(Capacity, Count + 1);
        }

        public bool TryPeek([NotNullWhen(true)] out T? item)
        {
            if (Count <= 0)
            {
                item = default;
                return false;
            }
            item = this[-1]!;
            return true;
        }

        public bool TryPop([NotNullWhen(true)] out T? item)
        {
            if (Count <= 0)
            {
                item = default;
                return false;
            }
            Position--;
            if (Position < 0)
                Position = Capacity - 1;

            item = Array[Position]!;
            Count--;
            return true;
        }

        int FixIndex(int index)
        {
            if (index < 0)
                return (Capacity - Math.Abs(index) % Capacity) % Capacity;
            return index % Capacity;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

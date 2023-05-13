using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.Helpers
{
    public class CompoundEnumerable<T> : IEnumerable<T>
    {
        readonly List<IEnumerable<T>> Enumerables = new();

        public void Add(IEnumerable<T> enumerable)
        {
            Enumerables.Add(enumerable);
        }

        public void Remove(IEnumerable<T> enumerable)
        {
            Enumerables.Remove(enumerable);
        }

        public void Clear()
        {
            Enumerables.Clear();
        }

        public IEnumerable<T> Enumerate()
        {
            return Enumerables.SelectMany(x => x);
        }

        public IEnumerable<T> EnumerateBackwards()
        {
            return Enumerables.Reverse<IEnumerable<T>>().SelectMany(i => i.SmartReverse());
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

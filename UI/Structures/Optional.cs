using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Structures
{
    public struct Optional<T>
    {

        public readonly T? Value { get; }

        [MemberNotNullWhen(true, nameof(Value))]
        public readonly bool HasValue { get; }

        public Optional()
        {
            Value = default;
            HasValue = false;
        }
        public Optional(T value)
        {
            Value = value;
            HasValue = true;
        }

        public static implicit operator Optional<T>(T value) => new(value);
        public static explicit operator T(Optional<T> optional) => optional.HasValue ? optional.Value! : throw new NullReferenceException("Optional value is not provided");
    }
}

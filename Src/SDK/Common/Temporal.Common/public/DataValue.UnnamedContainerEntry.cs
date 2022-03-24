using System;
using Candidly.Util;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        internal struct UnnamedContainerEntry<TEntry> : DataValue.IUnnamedContainerEntry
        {
            private readonly int _index;
            private readonly TEntry _value;

            internal UnnamedContainerEntry(int index, TEntry value)
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                _index = index;
                _value = value;
            }

            public int Index { get { return _index; } }

            public Type ValueType
            {
                get { return DataValue.GetTypeOfValue(_value); }
            }

            public object ValueObject
            {
                get { return (object) _value; }
            }

            public TVal GetValue<TVal>()
            {
                return _value.Cast<TEntry, TVal>();
            }

            public bool TryGetValue<TVal>(out TVal value)
            {
                return _value.TryCast<TEntry, TVal>(out value);
            }
        }
    }
}

using System;
using Candidly.Util;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        internal struct NamedContainerEntry<TEntry> : DataValue.INamedContainerEntry
        {
            private readonly string _name;
            private readonly TEntry _value;

            internal NamedContainerEntry(string name, TEntry value)
            {
                Validate.NotNullOrWhitespace(name);

                _name = name;
                _value = value;
            }

            public string Name { get { return _name; } }

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

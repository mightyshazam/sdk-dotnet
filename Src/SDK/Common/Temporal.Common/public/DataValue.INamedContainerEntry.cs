using System;
using System.Collections.Generic;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        public interface INamedContainerEntry
        {
            string Name { get; }
            object ValueObject { get; }
            Type ValueType { get; }
            TVal GetValue<TVal>();
            bool TryGetValue<TVal>(out TVal value);
        }
    }
}

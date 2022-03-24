using System;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        public interface IUnnamedContainerEntry
        {
            int Index { get; }
            object ValueObject { get; }
            Type ValueType { get; }
            TVal GetValue<TVal>();
            bool TryGetValue<TVal>(out TVal value);
        }
    }
}

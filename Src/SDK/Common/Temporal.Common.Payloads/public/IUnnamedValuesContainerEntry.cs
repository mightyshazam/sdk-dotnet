using System;

namespace Temporal.Common.Payloads
{
    public interface IUnnamedValuesContainerEntry
    {
        int Index { get; }
        object ValueObject { get; }
        TVal GetValue<TVal>();
        bool TryGetValue<TVal>(out TVal value);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Temporal.Util;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        /// <summary>
        /// <c>PayloadContainers.IUnnamed</c> implementation backed by actual values (rather than a raw payload).
        /// </summary>
        public static partial class Unnamed
        {
            public struct InstanceBacked<T> : PayloadContainers.IUnnamed, IPayload
            {
                private readonly IReadOnlyList<T> _values;

                public InstanceBacked(IReadOnlyList<T> values)
                {
                    Validate.NotNull(values);

                    _values = values;
                }

                public int Count
                {
                    get { return _values.Count; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].Cast<T, TVal>();
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].TryCast<T, TVal>(out value);
                    }

                    value = default;
                    return false;
                }

                public Type GetValueType(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].TypeOf();
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public IEnumerable<PayloadContainers.UnnamedEntry> Values
                {
                    get
                    {
                        int i = 0;
                        foreach (T value in _values)
                        {
                            yield return new PayloadContainers.UnnamedEntry(i++, this);
                        }
                    }
                }

                public IEnumerator<PayloadContainers.UnnamedEntry> GetEnumerator()
                {
                    return new PayloadContainers.UnnamedEnumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public PayloadContainers.UnnamedEntry this[int index]
                {
                    get
                    {
                        if (index >= 0 && index < Count)
                        {
                            return new PayloadContainers.UnnamedEntry(index, this);
                        }

                        throw CreateNoSuchIndexException(index, Count);
                    }
                }

                private static ArgumentException CreateNoSuchIndexException(int index, int containerItemCount)
                {
                    if (index < 0)
                    {
                        return new ArgumentOutOfRangeException(nameof(index), $"The value of {nameof(index)} may not be negative,"
                                                                            + $" but `{index}` was specified.");
                    }

                    if (index >= containerItemCount)
                    {
                        return new ArgumentOutOfRangeException(nameof(index),
                                                               $"This {nameof(PayloadContainers.IUnnamed)} includes"
                                                             + $" {containerItemCount} items, but the {nameof(index)}=`{index}` was specified.");
                    }

                    return new ArgumentException(message: $"Invalid value of {nameof(index)}: {index}.", paramName: nameof(index));
                }
            }
        }
    }
}

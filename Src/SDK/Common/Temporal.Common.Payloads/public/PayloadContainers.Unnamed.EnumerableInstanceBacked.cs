using System;
using System.Collections;
using System.Collections.Generic;
using Temporal.Util;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        public static partial class Unnamed
        {
            public struct EnumerableInstanceBacked : PayloadContainers.IUnnamed, IPayload
            {
                private readonly IEnumerable _value;

                public EnumerableInstanceBacked(IEnumerable value)
                {
                    _value = value;
                }

                public int Count
                {
                    get { return (_value == null) ? 0 : 1; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _value.Cast<IEnumerable, TVal>();
                    }

                    throw PayloadContainers.Util.CreateNoSuchIndexException(index, Count, this);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _value.TryCast<IEnumerable, TVal>(out value);
                    }

                    throw PayloadContainers.Util.CreateNoSuchIndexException(index, Count, this);
                }

                public Type GetValueType(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _value.TypeOf();
                    }

                    throw PayloadContainers.Util.CreateNoSuchIndexException(index, Count, this);
                }

                public IEnumerable GetEnumerable()
                {
                    return GetEnumerable<IEnumerable>();
                }

                public TVal GetEnumerable<TVal>() where TVal : IEnumerable
                {
                    return GetValue<TVal>(0);
                }

                public IEnumerable<PayloadContainers.UnnamedEntry> Values
                {
                    get
                    {
                        for (int i = 0; i < Count; i++)
                        {
                            yield return new PayloadContainers.UnnamedEntry(i, this);
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

                        throw PayloadContainers.Util.CreateNoSuchIndexException(index, Count, this);
                    }
                }
            }
        }
    }
}

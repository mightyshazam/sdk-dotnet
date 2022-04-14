using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;
using Temporal.Serialization;

using SerializedPayload = Temporal.Api.Common.V1.Payload;
using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        /// <summary>
        /// <c>IUnnamedValuesContainer</c> implementation that contains no items.
        /// </summary>
        public static partial class ForUnnamedValues
        {
            public class Empty : IUnnamedValuesContainer, IPayload
            {
                public Empty()
                {
                }

                public int Count
                {
                    get { return 0; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    throw CreateNoSuchIndexException(index);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    throw CreateNoSuchIndexException(index);
                }

                public IEnumerable<IUnnamedValuesContainerEntry> Values
                {
                    get
                    {
                        yield break;
                    }
                }

                public IEnumerator<IUnnamedValuesContainerEntry> GetEnumerator()
                {
                    return new UnnamedValuesContainerEnumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public IUnnamedValuesContainerEntry this[int index]
                {
                    get
                    {
                        throw CreateNoSuchIndexException(index);
                    }
                }
    
                private static ArgumentOutOfRangeException CreateNoSuchIndexException(int index)
                {
                    return new ArgumentOutOfRangeException(nameof(index), $"This container is empty, but `{nameof(index)}={index}` was specified.");
                }
            }
        }
    }
}

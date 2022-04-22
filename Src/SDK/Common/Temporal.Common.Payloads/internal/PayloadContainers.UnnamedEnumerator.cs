using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        internal class UnnamedEnumerator : IEnumerator<PayloadContainers.UnnamedEntry>
        {
            private readonly IEnumerator<PayloadContainers.UnnamedEntry> _payloadsEnumerator;

            public UnnamedEnumerator(PayloadContainers.IUnnamed container)
            {
                Validate.NotNull(container);

                IEnumerator<PayloadContainers.UnnamedEntry> payloadsEnumerator = container.Values?.GetEnumerator();
                Validate.NotNull(payloadsEnumerator);

                _payloadsEnumerator = payloadsEnumerator;
            }

            public PayloadContainers.UnnamedEntry Current
            {
                get { return _payloadsEnumerator.Current; }
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public void Dispose()
            {
                _payloadsEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                return _payloadsEnumerator.MoveNext();
            }

            public void Reset()
            {
                _payloadsEnumerator.Reset();
            }
        }
    }
}

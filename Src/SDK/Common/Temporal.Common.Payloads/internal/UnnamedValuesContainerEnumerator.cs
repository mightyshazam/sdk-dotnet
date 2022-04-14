using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;

namespace Temporal.Common.Payloads
{
    internal class UnnamedValuesContainerEnumerator : IEnumerator<UnnamedValuesContainerEntry>
    {
        private readonly IEnumerator<UnnamedValuesContainerEntry> _payloadsEnumerator;

        public UnnamedValuesContainerEnumerator(IUnnamedValuesContainer container)
        {
            Validate.NotNull(container);

            IEnumerator<UnnamedValuesContainerEntry> payloadsEnumerator = container.Values?.GetEnumerator();
            Validate.NotNull(payloadsEnumerator);

            _payloadsEnumerator = payloadsEnumerator;
        }

        public UnnamedValuesContainerEntry Current
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

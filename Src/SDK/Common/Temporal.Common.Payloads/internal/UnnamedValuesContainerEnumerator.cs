using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;

namespace Temporal.Common.Payloads
{
    internal class UnnamedValuesContainerEnumerator : IEnumerator<IUnnamedValuesContainerEntry>
    {
        private readonly IEnumerator<IUnnamedValuesContainerEntry> _payloadsEnumerator;

        public UnnamedValuesContainerEnumerator(IUnnamedValuesContainer container)
        {
            Validate.NotNull(container);

            IEnumerator<IUnnamedValuesContainerEntry> payloadsEnumerator = container.Values?.GetEnumerator();
            Validate.NotNull(payloadsEnumerator);

            _payloadsEnumerator = payloadsEnumerator;
        }

        public IUnnamedValuesContainerEntry Current
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

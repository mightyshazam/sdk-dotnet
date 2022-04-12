using System;
using Candidly.Util;

namespace Temporal.Common.Payloads
{
    internal struct UnnamedValuesContainerEntry<T> : IUnnamedValuesContainerEntry
    {
        private readonly int _index;
        private readonly IUnnamedValuesContainer _container;

        internal UnnamedValuesContainerEntry(int index, IUnnamedValuesContainer container)
        {
            Validate.NotNull(container);

            if (index < 0 || container.Count <= index)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index.ToString());
            }

            _index = index;
            _container = container;
        }

        public int Index { get { return _index; } }

        public object ValueObject
        {
            get { return _container.GetValue<object>(_index); }
        }

        public TVal GetValue<TVal>()
        {
            return _container.GetValue<TVal>(_index);
        }

        public bool TryGetValue<TVal>(out TVal value)
        {
            return _container.TryGetValue<TVal>(_index, out value);
        }
    }
}

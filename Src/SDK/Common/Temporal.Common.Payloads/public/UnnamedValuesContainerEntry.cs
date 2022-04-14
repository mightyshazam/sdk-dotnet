﻿using System;
using Candidly.Util;

namespace Temporal.Common.Payloads
{
    public struct UnnamedValuesContainerEntry
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

        private IUnnamedValuesContainer Container
        {
            get
            {
                if (_container == null)
                {
                    throw new InvalidOperationException($"The {nameof(_container)} of this {nameof(UnnamedValuesContainerEntry)} is null;"
                                                      + $" make sure to always use the ctor that takes a valid"
                                                      + $" {nameof(IUnnamedValuesContainer)} container parameter.");
                }

                return _container;
            }
        }

        public object ValueObject
        {
            get { return Container.GetValue<object>(_index); }
        }

        public TVal GetValue<TVal>()
        {
            return Container.GetValue<TVal>(_index);
        }

        public bool TryGetValue<TVal>(out TVal value)
        {
            return Container.TryGetValue<TVal>(_index, out value);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Temporal.Util;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        public class Enumerable : IPayload, IEnumerable
        {
            private readonly IEnumerable _enumerable = null;

            //public Enumerable()
            //{
            //}

            public Enumerable(IEnumerable enumerable)
            {
                Validate.NotNull(enumerable);
                _enumerable = enumerable;
            }

            protected internal Enumerable(object enumerableObj)
            {
                Validate.NotNull(enumerableObj);

                if (enumerableObj is IEnumerable enumerable)
                {
                    _enumerable = enumerable;
                }
                else
                {
                    throw new ArgumentException($"{nameof(enumerableObj)} must be an {nameof(IEnumerable)}, but it is not."
                                              + $" (Actual type: {Format.QuoteOrNull(enumerableObj?.GetType().FullName)}.)");
                }
            }

            //internal virtual void Set(object enumerableObj)
            //{
            //    Validate.NotNull(enumerableObj);

            //    if (enumerableObj is IEnumerable enumerable)
            //    {
            //        _enumerable = enumerable;
            //    }
            //    else
            //    {
            //        throw new ArgumentException($"{nameof(enumerableObj)} must be an {nameof(IEnumerable)}, but it is not."
            //                                  + $" (Actual type: {Format.QuoteOrNull(enumerableObj?.GetType().FullName)}.)");
            //    }
            //}

            public IEnumerable BackingEnumerable
            {
                get { return _enumerable; }
            }

            public virtual Type BackingEnumerableType
            {
                get { return _enumerable.TypeOf(); }
            }

            public virtual Type BackingElementType
            {
                get { return null; }
            }

            public virtual IEnumerator GetEnumerator()
            {
                return _enumerable.GetEnumerator();
            }
        }

        public sealed class Enumerable<T> : PayloadContainers.Enumerable, IPayload, IEnumerable<T>
        {
            //public Enumerable()
            //    : base()
            //{
            //}

            public Enumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            {
            }

            internal Enumerable(object enumerableObj)
                : base(AsEnumerable(enumerableObj))
            {
            }

            private static IEnumerable<T> AsEnumerable(object enumerableObj)
            {
                Validate.NotNull(enumerableObj);

                return (enumerableObj is IEnumerable<T> enumerable)
                            ? enumerable
                            : throw new ArgumentException($"{nameof(enumerableObj)} must be an {nameof(IEnumerable)}<{typeof(T).Name}>, but it is not."
                                                       + $" (Actual type: {Format.QuoteOrNull(enumerableObj?.GetType().FullName)}.)");
            }

            //internal override void Set(object enumerableObj)
            //{
            //    Validate.NotNull(enumerableObj);

            //    if (enumerableObj is IEnumerable<T> enumerable)
            //    {
            //        base.Set(enumerable);
            //    }
            //    else
            //    {
            //        throw new ArgumentException($"{nameof(enumerableObj)} must be an {nameof(IEnumerable)}<{typeof(T).Name}>, but it is not."
            //                                  + $" (Actual type: {Format.QuoteOrNull(enumerableObj?.GetType().FullName)}.)");
            //    }
            //}

            public new IEnumerable<T> BackingEnumerable
            {
                get { return (IEnumerable<T>) base.BackingEnumerable; }
            }

            public override Type BackingElementType
            {
                get { return typeof(T); }
            }

            public new IEnumerator<T> GetEnumerator()
            {
                return this.BackingEnumerable.GetEnumerator();
            }
        }
    }
}

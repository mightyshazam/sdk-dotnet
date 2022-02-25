using System;
using System.Collections;
using System.Collections.Generic;

namespace Temporal.Collections
{
    public interface IPaginatedEnumerablePage<out T> : IPaginatedDataPage, IEnumerable<T>, IEnumerable
    {
    }
}

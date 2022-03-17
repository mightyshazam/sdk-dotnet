using System;
using System.Collections.Generic;

namespace Temporal.Collections
{
    public interface IPaginatedReadOnlyListPage<out T> : IPaginatedDataPage, IReadOnlyList<T>
    {
    }
}

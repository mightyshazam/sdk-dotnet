using System;
using System.Collections.Generic;

namespace Temporal.Collections
{
    public interface IPaginatedReadOnlyCollectionPage<out T> : IPaginatedDataPage, IReadOnlyCollection<T>
    {
    }
}

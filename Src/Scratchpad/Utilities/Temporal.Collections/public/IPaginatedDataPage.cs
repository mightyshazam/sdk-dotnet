using System;

namespace Temporal.Collections
{
    public interface IPaginatedDataPage
    {
        bool HasNextPage { get; }
        IPaginationToken NextPageToken { get; }
        bool TryGetNextPageToken(out IPaginationToken nextPageToken);
    }
}

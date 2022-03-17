using System;
using System.Collections;
using System.Collections.Generic;

namespace Temporal.Collections
{
    internal class PaginatedReadOnlyListPage<T> : IPaginatedReadOnlyListPage<T>, IPaginatedReadOnlyCollectionPage<T>, IPaginatedEnumerablePage<T>
    {
        private readonly IReadOnlyList<T> _pageItems;

        public PaginatedReadOnlyListPage(IReadOnlyList<T> pageItems, IPaginationToken nextPageToken)
        {
            NextPageToken = nextPageToken;
            _pageItems = pageItems;
        }

        public IPaginationToken NextPageToken { get; }

        public int Count
        {
            get { return _pageItems.Count; }            
        }

        public bool HasNextPage
        {
            get { return NextPageToken != null; }
        }

        public T this[int index]
        {
            get { return _pageItems[index]; }            
        }

        public bool TryGetNextPageToken(out IPaginationToken nextPageToken)
        {
            nextPageToken = NextPageToken;
            return HasNextPage;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _pageItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _pageItems).GetEnumerator();
        }
    }
}

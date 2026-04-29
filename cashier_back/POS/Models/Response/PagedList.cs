namespace POS.Models.Response
{
    public class PagedList<T>
    {
        public List<T> Items { get; }
        public int? TotalItems { get; }
        public int PageIndex { get; }
        public int PageSize { get; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        public bool HasPreviousPage => PageIndex > 0;
        public bool HasNextPage => PageIndex < TotalPages - 1;

        public PagedList(List<T> items, int totalItems, int pageIndex, int pageSize)
        {
            Items = items
             .Skip(pageIndex * pageSize)
             .Take(pageSize)
             .ToList();
            TotalItems = totalItems;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }
}

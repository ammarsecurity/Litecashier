namespace POS.Models.Response
{
    public class PagedList<T>
    {
        public List<T> Items { get; }
        public int? TotalItems { get; }
        public int PageIndex { get; }
        public int PageSize { get; }

        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling((double)(TotalItems ?? 0) / PageSize)
            : 0;

        public bool HasPreviousPage => PageIndex > 0;
        public bool HasNextPage => PageIndex < TotalPages - 1;

        public PagedList(List<T> items, int totalItems, int pageIndex, int pageSize)
        {
            Items = items;
            TotalItems = totalItems;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }
}

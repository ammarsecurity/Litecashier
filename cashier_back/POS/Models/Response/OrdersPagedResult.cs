using POS.Models.Dtos;

namespace POS.Models.Response
{
    public class OrdersPagedResult
    {
        public List<OrderDto> Items { get; }
        public int? TotalItems { get; }
        public int PageIndex { get; }
        public int PageSize { get; }
        public OrdersSummaryDto Summary { get; }

        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling((double)(TotalItems ?? 0) / PageSize)
            : 0;

        public bool HasPreviousPage => PageIndex > 0;
        public bool HasNextPage => PageIndex < TotalPages - 1;

        public OrdersPagedResult(
            List<OrderDto> items,
            int totalItems,
            int pageIndex,
            int pageSize,
            OrdersSummaryDto summary)
        {
            Items = items;
            TotalItems = totalItems;
            PageIndex = pageIndex;
            PageSize = pageSize;
            Summary = summary;
        }
    }
}

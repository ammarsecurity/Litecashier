namespace POS.Models.Dtos
{
    public class CatalogClearRequest
    {
        public string? Password { get; set; }
    }

    public class CatalogClearResultDto
    {
        public int TagsCleared { get; set; }
        public int ItemsCleared { get; set; }
        public int OrdersCleared { get; set; }
        public int OrderItemsCleared { get; set; }
        public int CardPaymentsCleared { get; set; }
        public int StockMovementsCleared { get; set; }
        public int SuppliersCleared { get; set; }
        public int TagPrintersCleared { get; set; }
    }
}

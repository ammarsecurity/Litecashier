namespace RestaurantPOS.Models.Requests
{
    public class UpdateStockMovementRequest
    {
        public string? MaterialName { get; set; }
        public decimal? Quantity { get; set; }
        public string? SupplierName { get; set; }
        public decimal? Amount { get; set; }
        public string? UnitType { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Notes { get; set; }
        public string? ReceivedByEmployeeName { get; set; }
    }
}

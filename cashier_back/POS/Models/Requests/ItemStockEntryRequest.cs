namespace POS.Models.Requests
{
    public class ItemStockEntryRequest
    {
        public int ItemId { get; set; }
        public int? WarehouseId { get; set; }
        /// <summary>In or Out</summary>
        public string MovementType { get; set; } = "In";
        public int Quantity { get; set; }
        /// <summary>Null = inherit current item selling price (In only).</summary>
        public decimal? SellingPrice { get; set; }
        /// <summary>Null = inherit; when set on In used as new batch cost for weighted average.</summary>
        public decimal? PurchasingPrice { get; set; }
        public decimal? WholesalePrice { get; set; }
        public decimal? DisCountPrice { get; set; }
        public string? Notes { get; set; }
    }
}

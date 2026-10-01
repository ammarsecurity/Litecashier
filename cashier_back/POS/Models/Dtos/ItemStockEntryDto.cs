namespace POS.Models.Dtos
{
    public class ItemStockEntryDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string MovementType { get; set; } = "In";
        public int Quantity { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal PurchasingPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal DisCountPrice { get; set; }
        public decimal? BatchPurchasingPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime InsertDate { get; set; }
        public int InsertByUserId { get; set; }
        public string? InsertByUserName { get; set; }
    }
}

namespace POS.Models.Dtos
{
    public class WarehouseStockDto
    {
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int Quantity { get; set; }
        public int? LowStockAlertQuantity { get; set; }
        public bool IsDefault { get; set; }
    }

    public class WarehouseStockInputDto
    {
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public int? LowStockAlertQuantity { get; set; }
    }

    public class TransferStockRequest
    {
        public int ItemId { get; set; }
        public int FromWarehouseId { get; set; }
        public int ToWarehouseId { get; set; }
        public int Quantity { get; set; }
    }

    public class WarehouseRequest
    {
        public string Name { get; set; } = "";
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

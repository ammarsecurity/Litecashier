namespace POS.Models.Dtos
{
    public class OrderForReturnDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime InsertDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public List<OrderForReturnLineDto> Lines { get; set; } = new();
    }

    public class OrderForReturnLineDto
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ItemCode { get; set; }
        public decimal UnitPrice { get; set; }
        public int SoldQty { get; set; }
        public int AlreadyReturnedQty { get; set; }
        public int ReturnableQty { get; set; }
    }

    public class CatalogStockReturnDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ItemCode { get; set; }
        public int Quantity { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public int? CustomerOrderId { get; set; }
        public string? OrderCode { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime InsertDate { get; set; }
        public string? CreatedByUsername { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
    }
}

namespace RestaurantPOS.Models.Dtos
{
    public class ItemsSoldItemDto
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ItemCode { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
    }

    public class ItemsSoldByCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalSales { get; set; }
        public List<ItemsSoldItemDto> Items { get; set; } = new();
    }
}

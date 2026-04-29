namespace RestaurantPOS.Models.Dtos
{
    public class GeneratedItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "مواد اخرى";
        public decimal SellingPrice { get; set; }
        public decimal PurchasingPrice { get; set; }
        public decimal DisCountPrice { get; set; }
        public string? Description { get; set; }
    }
}


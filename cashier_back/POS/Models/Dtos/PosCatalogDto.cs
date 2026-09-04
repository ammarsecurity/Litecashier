namespace POS.Models.Dtos
{
    public class PosCatalogItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Image { get; set; }
        public string? Code { get; set; }
        public List<string> ExtraCodes { get; set; } = new();
        public decimal SellingPrice { get; set; }
        public decimal DisCountPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public int Quantity { get; set; }
        public string? Tags { get; set; }
        public bool IsNonInventory { get; set; }
    }

    public class PosCatalogDto
    {
        public int WarehouseId { get; set; }
        public List<PosCatalogItemDto> Items { get; set; } = new();
    }
}

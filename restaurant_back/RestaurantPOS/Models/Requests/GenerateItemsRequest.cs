using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class GenerateItemsRequest
    {
        [Required]
        public string Description { get; set; } = string.Empty;
        public int MaxItems { get; set; } = 15;
        public List<ItemInfo>? ExistingItems { get; set; }
    }

    public class ItemInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
    }
}


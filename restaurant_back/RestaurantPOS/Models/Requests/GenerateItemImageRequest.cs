using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class GenerateItemImageRequest
    {
        [Required]
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
    }
}


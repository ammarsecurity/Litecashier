using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class GenerateCategoriesRequest
    {
        [Required]
        public string Description { get; set; } = string.Empty;
        public int MaxCategories { get; set; } = 15;
        public List<string>? ExistingCategories { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class ExpenseCategoryRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }
    }
}


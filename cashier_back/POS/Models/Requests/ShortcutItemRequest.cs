using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class ShortcutItemRequest
    {
        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal SellingPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal WholesalePrice { get; set; }
    }
}

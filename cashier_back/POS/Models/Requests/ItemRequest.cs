using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class ItemRequest
    {

        [Required]
        public required string Name { get; set; }

        public IFormFile? Image { get; set; }

        public string? Description { get; set; }
        [Required]
        public decimal SellingPrice { get; set; }
        public decimal PurchasingPrice { get; set; }
        public decimal DisCountPrice { get; set; }
        public int Quantity { get; set; } = 0; // Inventory quantity
        public string? Tags { get; set; }
        public string? Code { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class DeleteStockLineRequest
    {
        [Required]
        [StringLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ReceiptNumber { get; set; }
    }
}

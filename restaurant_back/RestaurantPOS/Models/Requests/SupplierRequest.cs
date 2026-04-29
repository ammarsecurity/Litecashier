using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class SupplierRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}

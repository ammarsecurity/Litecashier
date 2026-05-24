using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class CustomerOrderItemRequest
    {
        [Required]
        public int ItemId { get; set; }
        [Required]
        public int Quantity { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

    }
}

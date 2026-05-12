using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class LogReturnedOrderItemRequest
    {
        [Required]
        public int SourceOrderItemId { get; set; }

        public int? DeletedQuantity { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class TransferOrderItemRequest
    {
        [Required]
        public int SourceTableId { get; set; }

        [Required]
        public int DestinationTableId { get; set; }

        [Required]
        public int SourceOrderItemId { get; set; }

        public int? TransferQuantity { get; set; }
    }
}

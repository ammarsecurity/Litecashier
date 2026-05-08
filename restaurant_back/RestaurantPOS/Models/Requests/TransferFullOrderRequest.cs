using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class TransferFullOrderRequest
    {
        [Required]
        public int SourceTableId { get; set; }

        [Required]
        public int DestinationTableId { get; set; }
    }
}

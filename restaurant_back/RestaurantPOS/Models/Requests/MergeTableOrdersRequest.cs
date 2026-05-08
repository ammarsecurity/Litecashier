using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class MergeTableOrdersRequest
    {
        [Required]
        public int SourceTableId { get; set; }

        [Required]
        public int DestinationTableId { get; set; }
    }
}

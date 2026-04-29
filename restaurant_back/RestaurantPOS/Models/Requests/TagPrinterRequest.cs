using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class TagPrinterRequest
    {
        [Required]
        public int TagId { get; set; }

        [Required]
        public int PrinterId { get; set; }
    }
}

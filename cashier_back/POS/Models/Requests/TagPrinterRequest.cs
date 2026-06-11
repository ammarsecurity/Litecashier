using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class TagPrinterRequest
    {
        [Required]
        public int TagId { get; set; }

        [Required]
        public int PrinterId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class CustomerOrderItemRequest
    {
        [Required]
        public int ItemId { get; set; }
        [Required]
        public int Quantity { get; set; }

    }
}
using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class ReturnFromOrderRequest
    {
        [Required]
        public int OrderId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>Optional target warehouse; defaults to the order warehouse then commercial default.</summary>
        public int? WarehouseId { get; set; }

        [Required]
        [MinLength(1)]
        public List<ReturnFromOrderLineRequest> Lines { get; set; } = new();
    }

    public class ReturnFromOrderLineRequest
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class RestockItemRequest
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>Target warehouse; defaults to commercial default warehouse when omitted.</summary>
        public int? WarehouseId { get; set; }
    }
}

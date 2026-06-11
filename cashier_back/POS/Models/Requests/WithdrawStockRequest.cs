using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class WithdrawStockRequest
    {
        [Required]
        [StringLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ReceiptNumber { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من الصفر")]
        public decimal Quantity { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(200)]
        public string? ReceivedByEmployeeName { get; set; }
    }
}

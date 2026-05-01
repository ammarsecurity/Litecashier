using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class WithdrawStockRequest
    {
        [Required]
        [StringLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        /// <summary>رقم الوصل لدفعة المخزن المستهدفة (فارغ = دفعة بدون رقم وصل أو قديمة)</summary>
        [StringLength(200)]
        public string? ReceiptNumber { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من الصفر")]
        public decimal Quantity { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>اسم الموظف الذي استلم الكمية المسحوبة (إلزامي)</summary>
        [StringLength(200)]
        public string? ReceivedByEmployeeName { get; set; }
    }
}

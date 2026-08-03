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

        /// <summary>معرف القسم (رئيسي أو فرعي) الذي يُسحب له</summary>
        public int? TagId { get; set; }

        /// <summary>اسم القسم/القسم الفرعي عند الإرسال المباشر (بديل عن TagId)</summary>
        [StringLength(200)]
        public string? ReceivedByDepartmentName { get; set; }

        /// <summary>توافق قديم — يُستخدم كاحتياطي لاسم الجهة المستلمة</summary>
        [StringLength(200)]
        public string? ReceivedByEmployeeName { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    /// <summary>سجل حركة مخزون: إضافة (دخول) أو سحب</summary>
    public class StockMovement : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>اسم المادة (كتابة حرة، لا علاقة بالأطباق/المشروبات)</summary>
        [Required]
        [StringLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        /// <summary>Add = إضافة، Withdraw = سحب</summary>
        [Required]
        [StringLength(20)]
        public string MovementType { get; set; } = "Add";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        /// <summary>اسم المورد (للإضافة فقط)</summary>
        [StringLength(200)]
        public string? SupplierName { get; set; }

        /// <summary>المبلغ (للإضافة فقط)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        /// <summary>نوع الوحدة: كارتون، قطعة، كيلو، إلخ (للإضافة فقط)</summary>
        [StringLength(50)]
        public string? UnitType { get; set; }

        /// <summary>مسار مرفق الوصل بعد الرفع (للإضافة فقط)</summary>
        [StringLength(500)]
        public string? ReceiptAttachmentPath { get; set; }

        /// <summary>رقم الوصل المرتبط بهذه الدفعة (مع اسم المادة يحدّد سطراً مستقلاً في المخزن)</summary>
        [StringLength(200)]
        public string? ReceiptNumber { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>اسم الموظف الذي استلم السحب (لحركات السحب فقط)</summary>
        [StringLength(200)]
        public string? ReceivedByEmployeeName { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class PayrollLine : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(PayrollRun))]
        public int PayrollRunId { get; set; }
        public PayrollRun? PayrollRun { get; set; }

        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalarySnapshot { get; set; }

        public SalaryType SalaryTypeSnapshot { get; set; }

        /// <summary>أيام العمل المستخدمة لحساب اليومي (قابل للتعديل في المسودة).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal WorkDays { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimeAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DeductionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AbsenceAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvanceDeducted { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public int? LinkedExpenseId { get; set; }

        /// <summary>تم تسليم الراتب للموظف وطباعة الإيصال.</summary>
        public bool IsHandedOver { get; set; }

        public DateTime? HandedOverAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class SalaryAdjustment : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public SalaryAdjustmentType Type { get; set; }

        /// <summary>مبلغ الإضافي أو الخصم. للغياب يُحسب من أيام الغياب × الأجر اليومي إن تُرك 0.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>أيام الغياب (لنوع Absence فقط).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AbsenceDays { get; set; }

        public DateTime Date { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(User))]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

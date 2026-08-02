using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class PayrollRun : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(User))]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }

        public List<PayrollLine>? Lines { get; set; }
    }
}

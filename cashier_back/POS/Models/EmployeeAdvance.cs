using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class EmployeeAdvance : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        public DateTime Date { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool IsClosed { get; set; }

        [ForeignKey(nameof(User))]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

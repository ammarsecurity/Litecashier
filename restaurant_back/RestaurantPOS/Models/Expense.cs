using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class Expense : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // المبلغ

        [Required]
        public DateTime Date { get; set; } // تاريخ الصرف

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty; // الفئة (رواتب، إيجار، فواتير، صيانة، إلخ)

        [StringLength(1000)]
        public string? Description { get; set; } // الوصف

        [ForeignKey("EmployeeId")]
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [ForeignKey("TagId")]
        public int? TagId { get; set; }
        public Tag? Tag { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}


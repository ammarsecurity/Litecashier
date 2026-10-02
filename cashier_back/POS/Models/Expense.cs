using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class Expense : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [ForeignKey("EmployeeId")]
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [ForeignKey("TagId")]
        public int? TagId { get; set; }
        public Tag? Tag { get; set; }

        /// <summary>Optional receipt/invoice attachment file name under wwwroot/Images/Expenses.</summary>
        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        [NotMapped]
        public string? AttachmentUrl { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

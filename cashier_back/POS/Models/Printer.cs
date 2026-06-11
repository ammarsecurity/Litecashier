using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class Printer : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        public string PrinterName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PrinterType { get; set; } = "windows";

        [StringLength(50)]
        public string? PrintCategory { get; set; }

        [StringLength(500)]
        public string? Configuration { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsMain { get; set; } = false;

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

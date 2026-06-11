using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class StockMovement : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string MovementType { get; set; } = "Add";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [StringLength(200)]
        public string? SupplierName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        [StringLength(50)]
        public string? UnitType { get; set; }

        [StringLength(500)]
        public string? ReceiptAttachmentPath { get; set; }

        [StringLength(200)]
        public string? ReceiptNumber { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(200)]
        public string? ReceivedByEmployeeName { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

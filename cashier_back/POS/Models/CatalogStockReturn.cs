using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class CatalogStockReturn : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Item))]
        public int ItemId { get; set; }

        public Item? Item { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReturnType { get; set; } = "Manual"; // Order | Manual

        [ForeignKey(nameof(CustomerOrder))]
        public int? CustomerOrderId { get; set; }

        public CustomerOrder? CustomerOrder { get; set; }

        [MaxLength(50)]
        public string? OrderCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(User))]
        public int InsertByUserId { get; set; }

        public User? User { get; set; }
    }
}

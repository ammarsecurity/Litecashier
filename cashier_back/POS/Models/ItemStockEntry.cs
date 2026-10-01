using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    /// <summary>Manual stock ledger row for a retail Item (In/Out from Items screen).</summary>
    public class ItemStockEntry : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Item))]
        public int ItemId { get; set; }

        public Item? Item { get; set; }

        [Required]
        [ForeignKey(nameof(Warehouse))]
        public int WarehouseId { get; set; }

        [JsonIgnore]
        public Warehouse? Warehouse { get; set; }

        /// <summary>In = increase, Out = decrease.</summary>
        [Required]
        [MaxLength(10)]
        public string MovementType { get; set; } = "In";

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SellingPrice { get; set; }

        /// <summary>Applied item purchase cost after this movement (weighted average when In).</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal PurchasingPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal WholesalePrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal DisCountPrice { get; set; }

        /// <summary>Batch purchase unit cost entered on In (before averaging). Null on Out or when inherited.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? BatchPurchasingPrice { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(User))]
        public int InsertByUserId { get; set; }

        public User? User { get; set; }
    }
}

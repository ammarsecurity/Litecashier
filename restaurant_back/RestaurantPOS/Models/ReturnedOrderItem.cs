using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantPOS.Models.Restaurant;

namespace RestaurantPOS.Models
{
    public class ReturnedOrderItem : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerOrderId { get; set; }

        [ForeignKey("CustomerOrderId")]
        public CustomerOrder? CustomerOrder { get; set; }

        [Required]
        public int CustomerOrderItemId { get; set; }

        [ForeignKey("CustomerOrderItemId")]
        public CustomerOrderItem? CustomerOrderItem { get; set; }

        public int? TableId { get; set; }

        [ForeignKey("TableId")]
        public Table? Table { get; set; }

        [Required]
        [ForeignKey("ItemId")]
        public int ItemId { get; set; }

        public Item? Item { get; set; }

        [Required]
        [MaxLength(120)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string OrderCode { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? TableNumber { get; set; }

        [MaxLength(250)]
        public string? MergedTableNumbers { get; set; }

        [MaxLength(50)]
        public string? OrderType { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        [Required]
        [MaxLength(50)]
        public string Reason { get; set; } = "DeletedFromPOS";

        [Required]
        [ForeignKey("DeletedByUserId")]
        public int DeletedByUserId { get; set; }

        [ForeignKey("DeletedByUserId")]
        public User? DeletedByUser { get; set; }

        [MaxLength(120)]
        public string? DeletedByUsername { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }

        [ForeignKey("InsertByUserId")]
        public User? InsertByUser { get; set; }
    }
}

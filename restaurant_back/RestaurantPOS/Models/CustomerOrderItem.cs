using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RestaurantPOS.Models
{
    public class CustomerOrderItem : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("ItemId")]
        public int ItemId { get; set; }
  
        public Item? Item { get; set; }

        [Required]
        [ForeignKey("CustomerOrderId")]
        public int CustomerOrderId { get; set; }
        [JsonIgnore]
        public CustomerOrder? CustomerOrder { get; set; }


        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal SellingPrice { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public decimal PurchasingPrice { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public  User? User { get; set; }
    }

}

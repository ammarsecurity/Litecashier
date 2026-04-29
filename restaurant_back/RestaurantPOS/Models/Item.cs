using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RestaurantPOS.Models
{
    public class Item : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }

        public decimal DisCountPrice { get; set; }

        [Required]
        public decimal SellingPrice { get; set; }
        public decimal PurchasingPrice { get; set; }
        
        [Required]
        public bool IsAvailable { get; set; } = true; // متوفر أو غير متوفر

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public  User? User { get; set; }

        public string? Tags { get; set; }
        [Required]
        public string? Code { get; set; }
        [JsonIgnore]
        public List<CustomerOrderItem> CustomerOrderItems { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using POS.Models.Dtos;

namespace POS.Models
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

        /// <summary>Wholesale unit price. When 0, POS wholesale mode falls back to SellingPrice.</summary>
        public decimal WholesalePrice { get; set; }
        
        [Required]
        public int Quantity { get; set; } = 0; // Inventory quantity

        /// <summary>When set, alert when Quantity is at or below this value. Null = no alert.</summary>
        public int? LowStockAlertQuantity { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public  User? User { get; set; }

        public string? Tags { get; set; }
        [Required]
        public string? Code { get; set; }

        [JsonIgnore]
        public List<CustomerOrderItem> CustomerOrderItems { get; set; } = new();

        [JsonIgnore]
        public List<ItemCode> ItemCodes { get; set; } = new();

        [JsonIgnore]
        public List<ItemWarehouseStock> WarehouseStocksNav { get; set; } = new();

        /// <summary>Per-warehouse quantities for API responses (not mapped).</summary>
        [NotMapped]
        public List<WarehouseStockDto>? WarehouseStocks { get; set; }
    }
}

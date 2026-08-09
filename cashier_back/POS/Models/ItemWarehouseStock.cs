using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    public class ItemWarehouseStock : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Item))]
        public int ItemId { get; set; }

        [JsonIgnore]
        public Item? Item { get; set; }

        [ForeignKey(nameof(Warehouse))]
        public int WarehouseId { get; set; }

        [JsonIgnore]
        public Warehouse? Warehouse { get; set; }

        public int Quantity { get; set; }

        /// <summary>Per-warehouse low-stock alert threshold. Null = disabled for this warehouse.</summary>
        public int? LowStockAlertQuantity { get; set; }
    }
}

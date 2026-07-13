using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    /// <summary>Additional scannable barcode/QR code for an item (beyond Item.Code).</summary>
    public class ItemCode : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        [JsonIgnore]
        public Item? Item { get; set; }

        [Required]
        [MaxLength(200)]
        public string Code { get; set; } = string.Empty;

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    public class Warehouse : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(User))]
        public int InsertByUserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [JsonIgnore]
        public List<ItemWarehouseStock> Stocks { get; set; } = new();
    }
}

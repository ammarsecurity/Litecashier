using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    /// <summary>مورد — يُضاف مسبقاً ويُختار عند إدخال المخزن</summary>
    public class Supplier : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

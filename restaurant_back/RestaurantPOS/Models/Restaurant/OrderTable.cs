using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantPOS.Models;

namespace RestaurantPOS.Models.Restaurant
{
    public class OrderTable : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public CustomerOrder Order { get; set; } = null!;

        [Required]
        public int TableId { get; set; }

        [ForeignKey("TableId")]
        public Table Table { get; set; } = null!;

        [Required]
        public bool IsPrimary { get; set; } = false; // الطاولة الأساسية (الأولى)

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}


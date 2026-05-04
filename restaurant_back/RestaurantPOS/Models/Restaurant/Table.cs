using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantPOS.Models;

namespace RestaurantPOS.Models.Restaurant
{
    public class Table : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string TableNumber { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public string Status { get; set; } = "Available"; // Available, Occupied, Reserved, OutOfService

        public string? Zone { get; set; } // مثل: داخلية، خارجية، شرفة

        /// <summary>موضع الطاولة على المخطط (0–1 من عرض اللوحة).</summary>
        public double? LayoutPosX { get; set; }

        /// <summary>موضع الطاولة على المخطط (0–1 من ارتفاع اللوحة).</summary>
        public double? LayoutPosY { get; set; }

        public string? Notes { get; set; }

        // العلاقة مع الطلبات الحالية
        public int? CurrentOrderId { get; set; }

        [ForeignKey("CurrentOrderId")]
        public CustomerOrder? CurrentOrder { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}



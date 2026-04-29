using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class DeliveryDriver : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty; // اسم السائق

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty; // رقم الهاتف

        [StringLength(500)]
        public string? Address { get; set; } // العنوان

        [StringLength(50)]
        public string? VehicleType { get; set; } // نوع المركبة (دراجة، سيارة، إلخ)

        [StringLength(50)]
        public string? VehicleNumber { get; set; } // رقم المركبة

        [StringLength(500)]
        public string? Notes { get; set; } // ملاحظات

        public bool IsActive { get; set; } = true; // هل السائق مفعل

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

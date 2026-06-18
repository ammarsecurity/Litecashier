using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class PaymentDevice : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string BaseUrl { get; set; } = "http://localhost:9092";

        /// <summary>Usb, Wifi, Cloud</summary>
        [Required]
        [StringLength(20)]
        public string ConnectionType { get; set; } = "Usb";

        [StringLength(20)]
        public string? ComPort { get; set; }

        [StringLength(200)]
        public string? WifiHost { get; set; }

        public int? WifiPort { get; set; }

        public string? WifiConfigJson { get; set; }

        public string? CloudConfigJson { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }

        public User? User { get; set; }
    }
}

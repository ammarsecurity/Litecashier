using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class Printer : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty; // اسم الطابعة

        [StringLength(500)]
        public string? Description { get; set; } // وصف الطابعة

        [Required]
        [StringLength(100)]
        public string PrinterName { get; set; } = string.Empty; // اسم الطابعة في النظام (Windows printer name)

        [Required]
        [StringLength(50)]
        public string PrinterType { get; set; } = "windows"; // windows, usb, serial, network, file

        [StringLength(50)]
        public string? PrintCategory { get; set; } // Receipt, Kitchen, CustomerOrder, Report, etc.

        [StringLength(500)]
        public string? Configuration { get; set; } // JSON configuration for printer settings

        public bool IsActive { get; set; } = true; // هل الطابعة مفعلة

        public bool IsMain { get; set; } = false; // هل الطابعة رئيسية (تطبع كل شيء)

        public bool IsPublicOrderPrinter { get; set; } = false; // طابعة إيصالات الطلبات العامة

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}


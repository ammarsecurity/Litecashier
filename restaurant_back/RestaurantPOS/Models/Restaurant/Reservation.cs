using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantPOS.Models;

namespace RestaurantPOS.Models.Restaurant
{
    public class Reservation : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string CustomerName { get; set; }

        [Required]
        public required string PhoneNumber { get; set; }

        public string? Email { get; set; }

        [Required]
        public DateTime ReservationDateTime { get; set; }

        public int? TableId { get; set; }

        [ForeignKey("TableId")]
        public Table? Table { get; set; }

        [Required]
        public int NumberOfGuests { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Seated, Completed, Cancelled

        public string? Notes { get; set; }

        public string? SpecialRequests { get; set; }

        // العلاقة مع الطلب بعد الجلوس
        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public CustomerOrder? Order { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}



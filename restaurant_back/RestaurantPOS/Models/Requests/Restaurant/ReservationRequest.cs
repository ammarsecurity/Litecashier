using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests.Restaurant
{
    public class ReservationRequest
    {
        [Required]
        public required string CustomerName { get; set; }

        [Required]
        public required string PhoneNumber { get; set; }

        public string? Email { get; set; }

        [Required]
        public DateTime ReservationDateTime { get; set; }

        public int? TableId { get; set; }

        [Required]
        [Range(1, 50)]
        public int NumberOfGuests { get; set; }

        public string Status { get; set; } = "Pending";

        public string? Notes { get; set; }

        public string? SpecialRequests { get; set; }
    }
}



















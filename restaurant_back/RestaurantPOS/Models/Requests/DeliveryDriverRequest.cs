using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class DeliveryDriverRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? VehicleType { get; set; }

        public string? VehicleNumber { get; set; }

        public string? Notes { get; set; }

        public bool? IsActive { get; set; }
    }
}

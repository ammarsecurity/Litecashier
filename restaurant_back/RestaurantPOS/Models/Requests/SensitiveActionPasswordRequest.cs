using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class SensitiveActionPasswordRequest
    {
        [Required]
        public required string Password { get; set; }

        public string? ActionKey { get; set; }
    }
}

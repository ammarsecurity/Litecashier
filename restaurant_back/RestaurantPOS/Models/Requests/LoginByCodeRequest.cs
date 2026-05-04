using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class LoginByCodeRequest
    {
        [Required]
        [StringLength(20, MinimumLength = 4)]
        public required string LoginCode { get; set; }
    }
}

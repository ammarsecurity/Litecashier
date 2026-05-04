using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RestaurantPOS.Models.Requests
{
    public class UserRequest
    {
        [Required]  
        public required string Name { get; set; }
        [Required]
        public required string PhoneNumber { get; set; }
        // Password is optional for updates, required for new users (validated in controller)
        public string? Password { get; set; }
        public required string Username { get; set; }
        public required string Role { get; set; }
        public IFormFile? Logo { get; set; }
        public string? RestaurantName { get; set; }
        /// <summary>رمز دخول الحساب التجاري (اختياري، مثل 45443)</summary>
        public string? LoginCode { get; set; }
    }
}

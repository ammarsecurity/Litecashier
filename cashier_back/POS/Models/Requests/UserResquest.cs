using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace POS.Models.Requests
{
    public class UpdateMyProfileRequest
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? StoreName { get; set; }
        public IFormFile? Logo { get; set; }
    }

    public class UserRequest
    {
        [Required]
        public required string Name { get; set; }
        [Required]
        public required string PhoneNumber { get; set; }
        public string? Password { get; set; }
        public required string Username { get; set; }
        public required string Role { get; set; }
        public IFormFile? Logo { get; set; }
        public string? StoreName { get; set; }
        public string? LoginCode { get; set; }
        public string? AllowedSectionsJson { get; set; }
        public bool? CanUseOwnLoginCodeForSensitiveActions { get; set; }
        public string? FooterCreditText { get; set; }
        public string? FooterCreditPhone { get; set; }
    }
}

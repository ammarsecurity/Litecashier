using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class UserRequest
    {
        [Required]  
        public required string Name { get; set; }
        [Required]
        public required string PhoneNumber { get; set; }
        [Required]
        public required string Password { get; set; }
        public required string Username { get; set; }
        public required string Role { get; set; }
    }
}

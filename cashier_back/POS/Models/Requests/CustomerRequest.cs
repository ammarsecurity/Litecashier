using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class CustomerRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Notes { get; set; }

        public bool? IsActive { get; set; }
    }
}

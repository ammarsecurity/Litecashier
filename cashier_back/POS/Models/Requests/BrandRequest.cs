using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class BrandRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = "";
    }
}

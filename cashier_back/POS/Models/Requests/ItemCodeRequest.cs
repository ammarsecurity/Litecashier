using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class ItemCodeRequest
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Code { get; set; } = string.Empty;
    }
}

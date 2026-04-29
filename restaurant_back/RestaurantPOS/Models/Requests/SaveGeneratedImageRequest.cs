using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class SaveGeneratedImageRequest
    {
        [Required]
        public string ImageUrl { get; set; } = string.Empty;
    }
}


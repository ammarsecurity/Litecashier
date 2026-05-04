using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests.Restaurant
{
    public class TableRequest
    {
        [Required]
        public required string TableNumber { get; set; }

        [Required]
        [Range(1, 50)]
        public int Capacity { get; set; }

        public string Status { get; set; } = "Available";

        public string? Zone { get; set; }

        /// <summary>موضع على المخطط 0–1 (اختياري).</summary>
        public double? LayoutPosX { get; set; }

        /// <summary>موضع على المخطط 0–1 (اختياري).</summary>
        public double? LayoutPosY { get; set; }

        public string? Notes { get; set; }
    }
}



















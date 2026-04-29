using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests.Restaurant
{
    public class BulkTableRequest
    {
        [Required]
        [Range(1, 100)]
        public int NumberOfTables { get; set; }

        [Required]
        [Range(1, 50)]
        public int Capacity { get; set; }

        [Required]
        public required string Zone { get; set; }

        public string? Notes { get; set; }
    }
}



















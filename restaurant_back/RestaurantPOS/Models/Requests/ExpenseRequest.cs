using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class ExpenseRequest
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? EmployeeId { get; set; }

        public int? TagId { get; set; }
    }
}


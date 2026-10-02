using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace POS.Models.Requests
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

        /// <summary>Optional image or PDF attachment.</summary>
        public IFormFile? Attachment { get; set; }

        /// <summary>When true on update, clears the existing attachment if no new file is sent.</summary>
        public bool RemoveAttachment { get; set; }
    }
}

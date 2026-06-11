using System.ComponentModel.DataAnnotations;
using POS.Models;

namespace POS.Models.Requests
{
    public class EmployeeRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? JobTitle { get; set; }

        public decimal Salary { get; set; }

        public SalaryType SalaryType { get; set; }

        public int? TagId { get; set; }
    }
}

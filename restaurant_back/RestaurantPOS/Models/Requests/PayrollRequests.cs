using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class EmployeeAdvanceRequest
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public DateTime? Date { get; set; }

        public string? Notes { get; set; }
    }

    public class SalaryAdjustmentRequest
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public SalaryAdjustmentType Type { get; set; }

        public decimal Amount { get; set; }

        public decimal AbsenceDays { get; set; }

        public DateTime? Date { get; set; }

        public string? Notes { get; set; }
    }

    public class CreatePayrollRunRequest
    {
        [Required]
        public int Year { get; set; }

        [Required]
        public int Month { get; set; }

        public string? Notes { get; set; }
    }

    public class UpdatePayrollLineRequest
    {
        public decimal? WorkDays { get; set; }
        public decimal? BaseAmount { get; set; }
        public decimal? OvertimeAmount { get; set; }
        public decimal? DeductionAmount { get; set; }
        public decimal? AbsenceAmount { get; set; }
        public decimal? AdvanceDeducted { get; set; }
        public string? Notes { get; set; }
    }
}

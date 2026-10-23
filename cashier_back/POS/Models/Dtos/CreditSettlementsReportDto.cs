namespace POS.Models.Dtos
{
    public class CreditSettlementsReportDto
    {
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public List<EndOfDayPaymentDto> BreakdownByMethod { get; set; } = new();
        public List<EndOfDaySettlementDto> Items { get; set; } = new();
        public CreditSettlementsReportPeriodDto Period { get; set; } = new();
    }

    public class CreditSettlementsReportPeriodDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}

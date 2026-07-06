namespace POS.Models.Response
{
    public class CardPaymentStatusDto
    {
        public int TransactionId { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Message { get; set; }
        public string? AuthCode { get; set; }
        public string? RefNo { get; set; }
        public string? CardNo { get; set; }
        public bool IsTerminal { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "IQD";
        public string? DeviceName { get; set; }
    }
}

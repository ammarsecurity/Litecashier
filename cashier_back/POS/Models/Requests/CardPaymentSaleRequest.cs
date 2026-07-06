namespace POS.Models.Requests
{
    public class CardPaymentSaleRequest
    {
        public decimal Amount { get; set; }
        public decimal TipAmount { get; set; }
        public string CurrencyCode { get; set; } = "IQD";
        public int? PaymentDeviceId { get; set; }
    }
}

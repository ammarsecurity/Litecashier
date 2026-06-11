namespace POS.Models.Requests
{
    public class CustomerOrderRequest
    {
        public string?  OrderCode { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, BankTransfer, Credit
        public required List<CustomerOrderItemRequest>? CustomerOrderItem { get; set; }

        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? OrderSubTotal { get; set; }
        public decimal? OrderTotalAfterDiscount { get; set; }
    }
}

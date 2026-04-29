namespace POS.Models.Requests
{
    public class CustomerOrderRequest
    {
        public string?  OrderCode { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, BankTransfer, Credit
        public required List<CustomerOrderItemRequest>? CustomerOrderItem { get; set; }
    }
}

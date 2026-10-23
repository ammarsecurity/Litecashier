namespace POS.Models.Requests
{
    public class CustomerOrderRequest
    {
        public string?  OrderCode { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, BankTransfer, Credit

        /// <summary>When true, resolve line prices from Item.WholesalePrice.</summary>
        public bool IsWholesale { get; set; }

        public required List<CustomerOrderItemRequest>? CustomerOrderItem { get; set; }

        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? OrderSubTotal { get; set; }
        public decimal? OrderTotalAfterDiscount { get; set; }

        public bool IsCheckout { get; set; }

        public int? CardPaymentTransactionId { get; set; }

        public int? CreditCustomerId { get; set; }

        /// <summary>Warehouse to deduct stock from for this order.</summary>
        public int? WarehouseId { get; set; }

        /// <summary>Client UUID used to make AddOrder idempotent for offline sync.</summary>
        public Guid? ClientOrderId { get; set; }

        /// <summary>Original sale time on the cashier device. Mapped to InsertDate when valid.</summary>
        public DateTime? SoldAt { get; set; }

        /// <summary>Optional walk-in customer name on the sale (not required).</summary>
        public string? CustomerName { get; set; }

        /// <summary>Optional walk-in customer phone on the sale (not required).</summary>
        public string? CustomerPhone { get; set; }

        /// <summary>Optional walk-in customer address on the sale (not required).</summary>
        public string? CustomerAddress { get; set; }

        /// <summary>Optional order notes.</summary>
        public string? Notes { get; set; }
    }
}

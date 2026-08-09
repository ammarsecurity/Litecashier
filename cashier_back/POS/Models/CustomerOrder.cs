using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    public class CustomerOrder: BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string OrderCode { get; set; }

        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, BankTransfer, Credit

        /// <summary>When true, line prices were resolved using Item.WholesalePrice.</summary>
        public bool IsWholesale { get; set; }

        public List<CustomerOrderItem>? CustomerOrderItem { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public  User User { get; set; }

        public string? DiscountType { get; set; } // amount, percentage
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? OrderSubTotal { get; set; }
        public decimal? OrderTotalAfterDiscount { get; set; }

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Refunded

        /// <summary>دفع لاحق — حساب زبون مسجل.</summary>
        public int? CreditCustomerId { get; set; }

        [ForeignKey("CreditCustomerId")]
        public Customer? CreditCustomer { get; set; }

        /// <summary>طريقة التسديد الفعلية عند إغلاق فاتورة آجلة (Cash, Card, BankTransfer).</summary>
        [MaxLength(20)]
        public string? SettlementPaymentMethod { get; set; }

        public DateTime? SettledAt { get; set; }

        /// <summary>Warehouse stock is deducted from for this invoice.</summary>
        public int? WarehouseId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(WarehouseId))]
        public Warehouse? Warehouse { get; set; }
    }
}

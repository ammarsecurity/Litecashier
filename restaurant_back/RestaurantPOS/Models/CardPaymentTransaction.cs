using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class CardPaymentTransaction : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int PaymentDeviceId { get; set; }

        [ForeignKey(nameof(PaymentDeviceId))]
        public PaymentDevice? PaymentDevice { get; set; }

        public int? CustomerOrderId { get; set; }

        [ForeignKey(nameof(CustomerOrderId))]
        public CustomerOrder? CustomerOrder { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TipAmount { get; set; }

        [StringLength(10)]
        public string CurrencyCode { get; set; } = "IQD";

        /// <summary>Pending, Success, Failed</summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(20)]
        public string? ResultCode { get; set; }

        [StringLength(500)]
        public string? Message { get; set; }

        public string? RawResponse { get; set; }

        [StringLength(50)]
        public string? AuthCode { get; set; }

        [StringLength(100)]
        public string? RefNo { get; set; }

        [StringLength(50)]
        public string? CardNo { get; set; }

        [StringLength(30)]
        public string? CardType { get; set; }

        [StringLength(100)]
        public string? IssuerName { get; set; }

        [StringLength(100)]
        public string? AcquirerName { get; set; }

        [StringLength(50)]
        public string? TerminalId { get; set; }

        [StringLength(50)]
        public string? MerchantId { get; set; }

        [StringLength(200)]
        public string? MerchantName { get; set; }

        public long? VoucherNo { get; set; }

        public long? BatchNo { get; set; }

        [StringLength(30)]
        public string? TransTime { get; set; }

        [StringLength(50)]
        public string? TotalAmount { get; set; }

        public int InsertByUserId { get; set; }

        public int RequestedByUserId { get; set; }

        [ForeignKey(nameof(InsertByUserId))]
        public User? CommercialUser { get; set; }

        [ForeignKey(nameof(RequestedByUserId))]
        public User? RequestedByUser { get; set; }
    }
}

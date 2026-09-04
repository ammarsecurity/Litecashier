using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    public class User : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string PhoneNumber { get; set; }
        [JsonIgnore]
        public string Password { get; set; } = string.Empty;
        public required string Username { get; set; }
        public required string Role { get; set; }
        public int InsertByUserId { get; set; }
        public string? Logo { get; set; }
        public string? StoreName { get; set; }

        [StringLength(20)]
        public string? LoginCode { get; set; }

        [StringLength(2000)]
        public string? AllowedSectionsJson { get; set; }

        public bool CanUseOwnLoginCodeForSensitiveActions { get; set; }

        /// <summary>
        /// Default receipt printer for this POS (or Waiter) account.
        /// </summary>
        public int? DefaultPrinterId { get; set; }

        /// <summary>Invoice print format for the commercial account: Pos (thermal) or A4.</summary>
        [StringLength(10)]
        public string PrintInvoiceFormat { get; set; } = "Pos";

        /// <summary>Footer credit/rights line printed on all receipts (e.g. "برمجة وتصميم ...").</summary>
        [StringLength(200)]
        public string? FooterCreditText { get; set; }

        /// <summary>Footer support phone number printed on all receipts.</summary>
        [StringLength(30)]
        public string? FooterCreditPhone { get; set; }

        /// <summary>Watermark logo shown in the center of the POS cart.</summary>
        public string? CartWatermarkLogo { get; set; }

        /// <summary>Watermark opacity percent (5-80). Default 18.</summary>
        public int CartWatermarkOpacity { get; set; } = 18;

        /// <summary>Fallback image used when a catalog product has no photo.</summary>
        public string? DefaultProductImage { get; set; }

        /// <summary>Minimum public-menu order total. 0 means no minimum.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PublicMenuMinOrderAmount { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(DefaultPrinterId))]
        public Printer? DefaultPrinter { get; set; }

        [JsonIgnore]
        public List<Item>? Items { get; set; }
        [JsonIgnore]
        public List<CustomerOrder>? CustomerOrders { get; set; }
        [JsonIgnore]
        public List<Tag>? Tags { get; set; }
        [JsonIgnore]
        public List<CustomerOrderItem>? CustomerOrderItem { get; set; }
    }
}

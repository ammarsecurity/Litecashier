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

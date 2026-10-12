using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    public class CreditSettlement : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(CustomerOrder))]
        public int CustomerOrderId { get; set; }

        [JsonIgnore]
        public CustomerOrder? CustomerOrder { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = "Cash";

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(User))]
        public int InsertByUserId { get; set; }

        public User? User { get; set; }
    }
}

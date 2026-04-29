using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class CustomerOrder: BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string OrderCode { get; set; }

        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, BankTransfer, Credit

        public List<CustomerOrderItem>? CustomerOrderItem { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public  User User { get; set; }

    }
}

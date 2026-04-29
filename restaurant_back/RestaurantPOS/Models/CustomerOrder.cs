using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantPOS.Models.Restaurant;

namespace RestaurantPOS.Models
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

        // علاقات المطاعم
        public int? TableId { get; set; }

        [ForeignKey("TableId")]
        public Table? Table { get; set; }

        public int? ReservationId { get; set; }

        [ForeignKey("ReservationId")]
        public Reservation? Reservation { get; set; }

        public string OrderType { get; set; } = "DineIn"; // DineIn, Takeaway, Delivery

        public string? Notes { get; set; } // ملاحظات الطلب

        public string? PagerNumber { get; set; } // رقم جهاز النداء

        public string OrderStatus { get; set; } = "Pending"; // Pending, Processing, Ready, Completed, Cancelled

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Refunded

        public int? DailySequenceNumber { get; set; } // رقم تسلسلي يومي يبدأ من 1 كل يوم

        // Delivery fields
        public int? DeliveryDriverId { get; set; } // سائق التوصيل

        [ForeignKey("DeliveryDriverId")]
        public DeliveryDriver? DeliveryDriver { get; set; }

        public string? DeliveryStatus { get; set; } // Pending, InTransit, Delivered, Failed, Completed

        public string? DeliveryAddress { get; set; } // عنوان التوصيل

        public string? DeliveryPhoneNumber { get; set; } // رقم هاتف المستلم

        public string? DeliveryCustomerName { get; set; } // اسم المستلم

        public decimal? DeliveryFee { get; set; } // رسوم التوصيل

        public DateTime? DeliveryAssignedAt { get; set; } // وقت تعيين السائق

        public DateTime? DeliveryCompletedAt { get; set; } // وقت اكتمال التوصيل

        // علاقة many-to-many مع الطاولات
        public List<Restaurant.OrderTable>? OrderTables { get; set; }

    }
}

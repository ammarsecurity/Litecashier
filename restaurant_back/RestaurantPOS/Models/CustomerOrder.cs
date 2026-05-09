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

        public int? NumberOfGuests { get; set; } = 0;

        public string? Notes { get; set; } // ملاحظات الطلب

        public string? PagerNumber { get; set; } // رقم جهاز النداء

        // Order discount fields
        public string? DiscountType { get; set; } // amount, percentage
        public decimal? DiscountValue { get; set; } // raw input value
        public decimal? DiscountAmount { get; set; } // computed amount in currency
        public decimal? DiscountPercent { get; set; } // computed percent value
        public decimal? OrderSubTotal { get; set; } // total before discount
        public decimal? OrderTotalAfterDiscount { get; set; } // total after discount

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

        /// <summary>دفع لاحق — حساب موظف (أحد الخيارين مع CreditCustomerId).</summary>
        public int? CreditEmployeeId { get; set; }

        [ForeignKey("CreditEmployeeId")]
        public Employee? CreditEmployee { get; set; }

        /// <summary>دفع لاحق — حساب عميل مسجل.</summary>
        public int? CreditCustomerId { get; set; }

        [ForeignKey("CreditCustomerId")]
        public Customer? CreditCustomer { get; set; }

        // علاقة many-to-many مع الطاولات
        public List<Restaurant.OrderTable>? OrderTables { get; set; }

    }
}

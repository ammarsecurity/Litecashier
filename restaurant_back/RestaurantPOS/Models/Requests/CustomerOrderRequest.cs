namespace RestaurantPOS.Models.Requests
{
    public class CustomerOrderRequest
    {
        public string? OrderCode { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, BankTransfer, Credit
        public required List<CustomerOrderItemRequest>? CustomerOrderItem { get; set; }
        
        // Restaurant fields
        public int? TableId { get; set; } // للتوافق مع الكود الحالي
        public List<int>? TableIds { get; set; } // لدعم عدة طاولات
        public int? ReservationId { get; set; }
        public string OrderType { get; set; } = "DineIn"; // DineIn, Takeaway, Delivery
        public string? Notes { get; set; } // ملاحظات الطلب
        public string? PagerNumber { get; set; } // رقم جهاز النداء
        
        // Delivery fields
        public int? DeliveryDriverId { get; set; } // سائق التوصيل (اختياري - يمكن استخدام سائق موجود)
        public string? DeliveryStatus { get; set; } // Pending, InTransit, Delivered, Failed, Completed
        public string? DeliveryAddress { get; set; } // عنوان التوصيل
        public string? DeliveryPhoneNumber { get; set; } // رقم هاتف المستلم
        public string? DeliveryCustomerName { get; set; } // اسم المستلم
        public decimal? DeliveryFee { get; set; } // رسوم التوصيل
        
        // معلومات سائق جديد (إذا لم يتم اختيار سائق موجود)
        public string? NewDriverName { get; set; }
        public string? NewDriverPhone { get; set; }
        public string? NewDriverAddress { get; set; }
        public string? NewDriverVehicleType { get; set; }
        public string? NewDriverVehicleNumber { get; set; }
    }
}

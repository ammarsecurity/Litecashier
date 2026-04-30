using System.ComponentModel.DataAnnotations;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Restaurant;

namespace RestaurantPOS.Models.Dtos
{
    public class OrderDto
    {

        public int Id { get; set; }
        public  string? OrderCode { get; set; }
        public List<CustomerOrderItem>? CustomerOrderItem { get; set; }
        public decimal OrderPrice { get; set; }
        public int? ItemsCount { get; set;}
        public int? DailySequenceNumber { get; set; }
        public DateTime InsertDate { get; set;}
        public string? PaymentMethod { get; set; }
        public string? OrderType { get; set; }
        public string? OrderStatus { get; set; }
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public decimal? Total { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByUsername { get; set; }

        // Discount fields
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? OrderSubTotal { get; set; }
        public decimal? OrderTotalAfterDiscount { get; set; }
        
        // Tables relationship
        public List<TableDto>? Tables { get; set; }
        public string? MergedTableNumbers { get; set; } // مثل "1و3و5"
        
        // Delivery fields
        public int? DeliveryDriverId { get; set; }
        public DeliveryDriver? DeliveryDriver { get; set; }
        public string? DeliveryStatus { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryPhoneNumber { get; set; }
        public string? DeliveryCustomerName { get; set; }
        public decimal? DeliveryFee { get; set; }
    }
}

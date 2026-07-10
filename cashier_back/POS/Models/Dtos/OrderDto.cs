using POS.Models;

namespace POS.Models.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public  string? OrderCode { get; set; }
        public List<CustomerOrderItem>? CustomerOrderItem { get; set; }
        public decimal OrderPrice { get; set; }
        public int? ItemsCount { get; set;}
        public DateTime InsertDate { get; set;}
        public string? PaymentMethod { get; set; }
        public bool IsWholesale { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByUsername { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? OrderSubTotal { get; set; }
        public decimal? OrderTotalAfterDiscount { get; set; }
    }
}

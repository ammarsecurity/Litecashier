namespace RestaurantPOS.Models.Dtos
{
    public class OrdersSummaryDto
    {
        public int TotalOrders { get; set; }
        public decimal TotalSubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}

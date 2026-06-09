namespace RestaurantPOS.Models.Dtos
{
    public class TopSellingItemsSummaryDto
    {
        public int TotalQuantitySold { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalDistinctItems { get; set; }
        public int TotalOrders { get; set; }
    }
}

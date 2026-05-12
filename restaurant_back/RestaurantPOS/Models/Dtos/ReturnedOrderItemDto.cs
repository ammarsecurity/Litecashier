namespace RestaurantPOS.Models.Dtos
{
    public class ReturnedOrderItemDto
    {
        public int Id { get; set; }
        public int CustomerOrderId { get; set; }
        public int CustomerOrderItemId { get; set; }
        public int? TableId { get; set; }
        public string? TableNumber { get; set; }
        public string? MergedTableNumbers { get; set; }
        public string? OrderCode { get; set; }
        public string? OrderType { get; set; }
        public string? PaymentMethod { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int DeletedByUserId { get; set; }
        public string? DeletedByUsername { get; set; }
        public DateTime InsertDate { get; set; }
    }
}

namespace RestaurantPOS.Models.Requests
{
    public class TransferOrderItemToTableRequest
    {
        public int SourceOrderId { get; set; }
        public int SourceOrderItemId { get; set; }
        public int SourceTableId { get; set; }
        public int DestinationTableId { get; set; }
    }
}

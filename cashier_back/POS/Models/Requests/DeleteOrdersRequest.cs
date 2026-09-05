namespace POS.Models.Requests
{
    public class DeleteOrdersRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}

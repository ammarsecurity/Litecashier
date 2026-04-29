namespace RestaurantPOS.Models.Response
{
    public class GlobalResponse<T>
    {
        public T? Data { get; set; }
        public string? Message { get; set; }
        public bool? ErrorStatus { get; set; }

    }
}

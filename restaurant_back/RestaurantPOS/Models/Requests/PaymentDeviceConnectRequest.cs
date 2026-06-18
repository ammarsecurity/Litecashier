namespace RestaurantPOS.Models.Requests
{
    public class PaymentDeviceConnectRequest
    {
        public string? ComPort { get; set; }
        public string? WifiHost { get; set; }
        public int? WifiPort { get; set; }
        public string? WifiConfigJson { get; set; }
        public string? CloudConfigJson { get; set; }
    }
}

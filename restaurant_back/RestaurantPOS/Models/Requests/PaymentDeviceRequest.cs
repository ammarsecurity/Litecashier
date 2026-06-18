namespace RestaurantPOS.Models.Requests
{
    public class PaymentDeviceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "http://localhost:9092";
        public string ConnectionType { get; set; } = "Usb";
        public string? ComPort { get; set; }
        public string? WifiHost { get; set; }
        public int? WifiPort { get; set; }
        public string? WifiConfigJson { get; set; }
        public string? CloudConfigJson { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

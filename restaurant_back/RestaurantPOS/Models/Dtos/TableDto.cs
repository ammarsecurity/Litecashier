namespace RestaurantPOS.Models.Dtos
{
    public class TableDto
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Status { get; set; } = "Available";
        public string? Zone { get; set; }
        public string? Notes { get; set; }
    }
}


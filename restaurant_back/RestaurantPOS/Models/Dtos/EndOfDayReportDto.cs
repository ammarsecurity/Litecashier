namespace RestaurantPOS.Models.Dtos
{
    public class EndOfDayReportDto
    {
        public DateTime DayStart { get; set; }
        public DateTime DayEnd { get; set; }
        public EndOfDayTotalsDto Totals { get; set; } = new();
        public EndOfDayTableStatusDto TableStatus { get; set; } = new();
        public List<EndOfDayPaymentDto> PaymentBreakdown { get; set; } = new();
        public List<EndOfDayOrderTypeDto> OrdersByType { get; set; } = new();
        public List<EndOfDayTableInvoicesDto> InvoicesByTable { get; set; } = new();
        public List<EndOfDayTopItemDto> TopItems { get; set; } = new();
        public List<EndOfDayReturnedItemDto> ReturnedItems { get; set; } = new();
    }

    public class EndOfDayTotalsDto
    {
        public int OrdersCount { get; set; }
        public int ItemsCount { get; set; }
        public int ItemsQuantity { get; set; }
        public decimal GrossSales { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public decimal ReturnedAmount { get; set; }
        public int ReturnedCount { get; set; }
    }

    public class EndOfDayTableStatusDto
    {
        public int TotalTables { get; set; }
        public int AvailableTables { get; set; }
        public int OccupiedTables { get; set; }
        public int ReservedTables { get; set; }
        public int OutOfServiceTables { get; set; }
    }

    public class EndOfDayPaymentDto
    {
        public string Method { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal Amount { get; set; }
    }

    public class EndOfDayOrderTypeDto
    {
        public string OrderType { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class EndOfDayTableInvoicesDto
    {
        public int? TableId { get; set; }
        public string TableNumber { get; set; } = "-";
        public int InvoicesCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class EndOfDayTopItemDto
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal SalesAmount { get; set; }
    }

    public class EndOfDayReturnedItemDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? TableNumber { get; set; }
        public string? MergedTableNumbers { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? DeletedByUsername { get; set; }
        public DateTime InsertDate { get; set; }
    }
}

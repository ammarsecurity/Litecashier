namespace RestaurantPOS.Models.Response;

public class UpdateReservationStatusRequest
{
    public string Status { get; set; } = "";
}

public class TableReservationAvailabilityDto
{
    public int TableId { get; set; }
    public string TableNumber { get; set; } = "";
    public string? Zone { get; set; }
    public int Capacity { get; set; }
    public string TableStatus { get; set; } = "";
    public bool HasConflict { get; set; }
    public int? ReservationId { get; set; }
    public string? ReservationStatus { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? ReservationDateTime { get; set; }
    public bool IsAvailableForSlot { get; set; }
}

public class ReservationAvailabilityResponseDto
{
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public List<TableReservationAvailabilityDto> Tables { get; set; } = new();
}

public class ReservationSummaryStatsDto
{
    public int TodayCount { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int ReservedTablesCount { get; set; }
}

public class ReservationCustomerOptionDto
{
    public int? CustomerId { get; set; }
    public string Name { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Source { get; set; } = "";
}

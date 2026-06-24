namespace RestaurantPOS.Configuration;

public class ReservationSettingsOptions
{
    public const string SectionName = "ReservationSettings";

    public bool AutoCancelWhenDue { get; set; } = true;

    public int CheckIntervalMinutes { get; set; } = 1;
}

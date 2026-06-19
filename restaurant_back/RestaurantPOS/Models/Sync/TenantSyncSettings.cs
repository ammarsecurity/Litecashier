using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Sync;

public class TenantSyncSettings : BaseEntity
{
    [Key]
    public int Id { get; set; }

    public int CommercialUserId { get; set; }

    public bool AutoSyncEnabled { get; set; }

    public int IntervalMinutes { get; set; } = 10;
}

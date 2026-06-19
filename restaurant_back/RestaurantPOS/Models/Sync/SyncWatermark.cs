using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Sync;

public class SyncWatermark : BaseEntity
{
    [Key]
    public int Id { get; set; }

    public int CommercialUserId { get; set; }

    [MaxLength(128)]
    public string TableName { get; set; } = "";

    public DateTime LastSyncedUpdateDate { get; set; }
}

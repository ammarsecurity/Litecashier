using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Sync;

public class SyncFileWatermark : BaseEntity
{
    [Key]
    public int Id { get; set; }

    public int CommercialUserId { get; set; }

    [MaxLength(500)]
    public string RelativePath { get; set; } = "";

    public DateTime LastModifiedUtc { get; set; }

    public DateTime SyncedAt { get; set; }
}

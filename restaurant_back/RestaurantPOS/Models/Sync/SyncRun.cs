using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Sync;

public class SyncRun : BaseEntity
{
    [Key]
    public int Id { get; set; }

    public int CommercialUserId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Running";

    [MaxLength(20)]
    public string Trigger { get; set; } = "Manual";

    public int RecordsPushed { get; set; }

    public int FilesPushed { get; set; }

    [MaxLength(260)]
    public string? ArchiveFileName { get; set; }

    public long ArchiveSizeBytes { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
}

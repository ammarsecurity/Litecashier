namespace RestaurantPOS.Models.Response;

public class SyncStatusDto
{
    public bool SyncEnabled { get; set; }
    public bool FtpConnected { get; set; }
    public bool IsSyncInProgress { get; set; }
    public bool AutoSyncEnabled { get; set; }
    public int IntervalMinutes { get; set; }
    public DateTime? LastSuccessfulSyncAt { get; set; }
    public string? LastSyncStatus { get; set; }
    public string? LastSyncError { get; set; }
    public string? LastArchiveFileName { get; set; }
    public long LastArchiveSizeBytes { get; set; }
}

public class SyncPushResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? ArchiveFileName { get; set; }
    public long ArchiveSizeBytes { get; set; }
    public int RunId { get; set; }
}

public class SyncRunDto
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "";
    public string Trigger { get; set; } = "";
    public string? ArchiveFileName { get; set; }
    public long ArchiveSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SyncSettingsDto
{
    public bool AutoSyncEnabled { get; set; }
    public int IntervalMinutes { get; set; }
}

public class SyncConnectionTestDto
{
    public bool FtpOk { get; set; }
    public string? FtpMessage { get; set; }
}

public class UpdateSyncSettingsRequest
{
    public bool AutoSyncEnabled { get; set; }
    public int IntervalMinutes { get; set; } = 10;
}

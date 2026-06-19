namespace RestaurantPOS.Models.Response;

public class SyncStatusDto
{
    public bool SyncEnabled { get; set; }
    public bool RemoteDatabaseConnected { get; set; }
    public bool FtpConnected { get; set; }
    public bool IsSyncInProgress { get; set; }
    public bool AutoSyncEnabled { get; set; }
    public int IntervalMinutes { get; set; }
    public DateTime? LastSuccessfulSyncAt { get; set; }
    public string? LastSyncStatus { get; set; }
    public string? LastSyncError { get; set; }
    public int EstimatedPendingRecords { get; set; }
    public int LastRecordsPushed { get; set; }
    public int LastFilesPushed { get; set; }
}

public class SyncPushResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int RecordsPushed { get; set; }
    public int FilesPushed { get; set; }
    public int RunId { get; set; }
}

public class SyncRunDto
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "";
    public string Trigger { get; set; } = "";
    public int RecordsPushed { get; set; }
    public int FilesPushed { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SyncSettingsDto
{
    public bool AutoSyncEnabled { get; set; }
    public int IntervalMinutes { get; set; }
}

public class SyncConnectionTestDto
{
    public bool RemoteDatabaseOk { get; set; }
    public bool FtpOk { get; set; }
    public string? DatabaseMessage { get; set; }
    public string? FtpMessage { get; set; }
}

public class UpdateSyncSettingsRequest
{
    public bool AutoSyncEnabled { get; set; }
    public int IntervalMinutes { get; set; } = 10;
}

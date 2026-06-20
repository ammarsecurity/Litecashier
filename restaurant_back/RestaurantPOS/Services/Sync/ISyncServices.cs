using RestaurantPOS.Models.Response;

namespace RestaurantPOS.Services.Sync;

public interface IDatabaseSyncService
{
    bool IsSyncInProgress { get; }

    Task<SyncConnectionTestDto> TestConnectionsAsync(CancellationToken cancellationToken = default);

    Task<SyncStatusDto> GetStatusAsync(int commercialUserId, CancellationToken cancellationToken = default);

    Task<SyncPushResultDto> PushAsync(int commercialUserId, string trigger, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SyncRunDto>> GetHistoryAsync(int commercialUserId, int take = 30, CancellationToken cancellationToken = default);

    Task<int> ClearHistoryAsync(int commercialUserId, CancellationToken cancellationToken = default);

    Task<SyncSettingsDto> GetSettingsAsync(int commercialUserId, CancellationToken cancellationToken = default);

    Task<SyncSettingsDto> UpdateSettingsAsync(int commercialUserId, UpdateSyncSettingsRequest request, CancellationToken cancellationToken = default);
}

public class BackupUploadResult
{
    public string RemoteFileName { get; set; } = "";
    public long SizeBytes { get; set; }
}

public interface IFileSyncService
{
    Task<bool> TestFtpAsync(CancellationToken cancellationToken = default);

    Task<BackupUploadResult> UploadBackupArchiveAsync(
        string localZipPath,
        string remoteFileName,
        CancellationToken cancellationToken = default);
}

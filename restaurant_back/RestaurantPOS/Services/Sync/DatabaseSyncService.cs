using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestaurantPOS.Configuration;
using RestaurantPOS.Db;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Models.Sync;
using RestaurantPOS.Services;

namespace RestaurantPOS.Services.Sync;

public class DatabaseSyncService : IDatabaseSyncService
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private readonly DbConfig _db;
    private readonly IFileSyncService _fileSync;
    private readonly ISystemBackupService _systemBackupService;
    private readonly SyncSettingsOptions _options;
    private readonly ILogger<DatabaseSyncService> _logger;

    public DatabaseSyncService(
        DbConfig db,
        IFileSyncService fileSync,
        ISystemBackupService systemBackupService,
        IOptions<SyncSettingsOptions> options,
        ILogger<DatabaseSyncService> logger)
    {
        _db = db;
        _fileSync = fileSync;
        _systemBackupService = systemBackupService;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsSyncInProgress => SyncLock.CurrentCount == 0;

    public async Task<SyncConnectionTestDto> TestConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var result = new SyncConnectionTestDto();

        if (!_options.Ftp.Enabled)
        {
            result.FtpMessage = "FTP is disabled in configuration.";
        }
        else
        {
            try
            {
                result.FtpOk = await _fileSync.TestFtpAsync(cancellationToken);
                result.FtpMessage = result.FtpOk ? "OK" : "Connection failed";
            }
            catch (Exception ex)
            {
                result.FtpMessage = ex.Message;
            }
        }

        return result;
    }

    public async Task<SyncStatusDto> GetStatusAsync(int commercialUserId, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsEntityAsync(commercialUserId, cancellationToken);
        var lastRun = await _db.SyncRuns.AsNoTracking()
            .Where(r => r.CommercialUserId == commercialUserId && !r.IsDeleted)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lastSuccess = await _db.SyncRuns.AsNoTracking()
            .Where(r => r.CommercialUserId == commercialUserId && !r.IsDeleted && r.Status == SyncRunStatuses.Success)
            .OrderByDescending(r => r.FinishedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var ftpOk = !_options.Ftp.Enabled || await _fileSync.TestFtpAsync(cancellationToken);

        return new SyncStatusDto
        {
            SyncEnabled = _options.Enabled && _options.Ftp.Enabled,
            FtpConnected = ftpOk,
            IsSyncInProgress = IsSyncInProgress,
            AutoSyncEnabled = settings.AutoSyncEnabled,
            IntervalMinutes = settings.IntervalMinutes,
            LastSuccessfulSyncAt = lastSuccess?.FinishedAt,
            LastSyncStatus = lastRun?.Status,
            LastSyncError = lastRun?.Status == SyncRunStatuses.Failed ? lastRun.ErrorMessage : null,
            LastArchiveFileName = lastSuccess?.ArchiveFileName,
            LastArchiveSizeBytes = lastSuccess?.ArchiveSizeBytes ?? 0,
        };
    }

    public async Task<SyncPushResultDto> PushAsync(int commercialUserId, string trigger, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new SyncPushResultDto { Success = false, Message = "Sync is disabled in configuration." };
        }

        if (!_options.Ftp.Enabled)
        {
            return new SyncPushResultDto { Success = false, Message = "FTP is disabled in configuration." };
        }

        if (!await SyncLock.WaitAsync(0, cancellationToken))
        {
            return new SyncPushResultDto { Success = false, Message = "Sync already in progress." };
        }

        var run = new SyncRun
        {
            CommercialUserId = commercialUserId,
            StartedAt = DateTime.UtcNow,
            Status = SyncRunStatuses.Running,
            Trigger = trigger,
            InsertDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow,
            IsDeleted = false,
        };

        _db.SyncRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        var tempZipPath = Path.Combine(Path.GetTempPath(), $"litecashier-sync-{Guid.NewGuid():N}.zip");

        try
        {
            await using (var tempStream = new FileStream(
                             tempZipPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81920,
                             useAsync: true))
            {
                await _systemBackupService.WriteBackupArchiveAsync(tempStream, cancellationToken);
            }

            var archiveFileName = $"litecashier-backup-{commercialUserId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
            var upload = await _fileSync.UploadBackupArchiveAsync(tempZipPath, archiveFileName, cancellationToken);

            run.Status = SyncRunStatuses.Success;
            run.ArchiveFileName = upload.RemoteFileName;
            run.ArchiveSizeBytes = upload.SizeBytes;
            run.FinishedAt = DateTime.UtcNow;
            run.UpdateDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return new SyncPushResultDto
            {
                Success = true,
                Message = "Backup uploaded successfully.",
                ArchiveFileName = upload.RemoteFileName,
                ArchiveSizeBytes = upload.SizeBytes,
                RunId = run.Id,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZIP backup sync failed for commercial user {CommercialUserId}", commercialUserId);
            run.Status = SyncRunStatuses.Failed;
            run.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            run.FinishedAt = DateTime.UtcNow;
            run.UpdateDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return new SyncPushResultDto
            {
                Success = false,
                Message = ex.Message,
                RunId = run.Id,
            };
        }
        finally
        {
            SyncLock.Release();
            TryDeleteTempFile(tempZipPath);
        }
    }

    public async Task<IReadOnlyList<SyncRunDto>> GetHistoryAsync(int commercialUserId, int take = 30, CancellationToken cancellationToken = default)
    {
        return await _db.SyncRuns.AsNoTracking()
            .Where(r => r.CommercialUserId == commercialUserId && !r.IsDeleted)
            .OrderByDescending(r => r.StartedAt)
            .Take(take)
            .Select(r => new SyncRunDto
            {
                Id = r.Id,
                StartedAt = r.StartedAt,
                FinishedAt = r.FinishedAt,
                Status = r.Status,
                Trigger = r.Trigger,
                ArchiveFileName = r.ArchiveFileName,
                ArchiveSizeBytes = r.ArchiveSizeBytes,
                ErrorMessage = r.ErrorMessage,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ClearHistoryAsync(int commercialUserId, CancellationToken cancellationToken = default)
    {
        if (IsSyncInProgress)
        {
            throw new InvalidOperationException("syncInProgress");
        }

        var runs = await _db.SyncRuns
            .Where(r => r.CommercialUserId == commercialUserId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (runs.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var run in runs)
        {
            run.IsDeleted = true;
            run.UpdateDate = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return runs.Count;
    }

    public async Task<SyncSettingsDto> GetSettingsAsync(int commercialUserId, CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateSettingsEntityAsync(commercialUserId, cancellationToken);
        return MapSettings(entity);
    }

    public async Task<SyncSettingsDto> UpdateSettingsAsync(int commercialUserId, UpdateSyncSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateSettingsEntityAsync(commercialUserId, cancellationToken);
        entity.AutoSyncEnabled = request.AutoSyncEnabled;
        entity.IntervalMinutes = ClampInterval(request.IntervalMinutes);
        entity.UpdateDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapSettings(entity);
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private async Task<TenantSyncSettings> GetOrCreateSettingsEntityAsync(int commercialUserId, CancellationToken cancellationToken)
    {
        var entity = await _db.TenantSyncSettings
            .FirstOrDefaultAsync(s => s.CommercialUserId == commercialUserId, cancellationToken);

        if (entity != null)
        {
            if (entity.IsDeleted)
            {
                entity.IsDeleted = false;
                entity.AutoSyncEnabled = _options.AutoSyncEnabled;
                entity.IntervalMinutes = ClampInterval(_options.AutoSyncIntervalMinutes);
                entity.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return entity;
        }

        entity = new TenantSyncSettings
        {
            CommercialUserId = commercialUserId,
            AutoSyncEnabled = _options.AutoSyncEnabled,
            IntervalMinutes = ClampInterval(_options.AutoSyncIntervalMinutes),
            InsertDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow,
            IsDeleted = false,
        };
        _db.TenantSyncSettings.Add(entity);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex, "IX_SyncSettings_CommercialUserId"))
        {
            _db.Entry(entity).State = EntityState.Detached;
            return await _db.TenantSyncSettings
                .FirstAsync(s => s.CommercialUserId == commercialUserId, cancellationToken);
        }
    }

    private static bool IsDuplicateKeyException(Exception ex, string indexName)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                && current.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static SyncSettingsDto MapSettings(TenantSyncSettings entity) =>
        new()
        {
            AutoSyncEnabled = entity.AutoSyncEnabled,
            IntervalMinutes = entity.IntervalMinutes,
        };

    private static int ClampInterval(int minutes)
    {
        if (minutes <= 5) return 5;
        if (minutes <= 10) return 10;
        if (minutes <= 15) return 15;
        return 30;
    }
}

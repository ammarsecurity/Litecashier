using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySqlConnector;
using RestaurantPOS.Configuration;
using RestaurantPOS.Db;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Models.Sync;

namespace RestaurantPOS.Services.Sync;

public class DatabaseSyncService : IDatabaseSyncService
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private readonly DbConfig _db;
    private readonly IFileSyncService _fileSync;
    private readonly IConfiguration _configuration;
    private readonly SyncSettingsOptions _options;
    private readonly ILogger<DatabaseSyncService> _logger;
    private readonly Dictionary<string, IReadOnlyList<string>> _columnCache = new(StringComparer.OrdinalIgnoreCase);

    public DatabaseSyncService(
        DbConfig db,
        IFileSyncService fileSync,
        IConfiguration configuration,
        IOptions<SyncSettingsOptions> options,
        ILogger<DatabaseSyncService> logger)
    {
        _db = db;
        _fileSync = fileSync;
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsSyncInProgress => SyncLock.CurrentCount == 0;

    private string? RemoteConnectionString =>
        _configuration.GetConnectionString("SyncDatabase");

    private string LocalConnectionString =>
        _configuration.GetConnectionString("WebApiDatabase")
        ?? throw new InvalidOperationException("Connection string 'WebApiDatabase' not found.");

    public async Task<bool> TestRemoteDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var remote = RemoteConnectionString;
        if (string.IsNullOrWhiteSpace(remote))
        {
            return false;
        }

        try
        {
            await using var conn = new MySqlConnection(remote);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new MySqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote database connection test failed");
            return false;
        }
    }

    public async Task<SyncConnectionTestDto> TestConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var result = new SyncConnectionTestDto();

        if (string.IsNullOrWhiteSpace(RemoteConnectionString))
        {
            result.DatabaseMessage = "SyncDatabase connection string is not configured.";
        }
        else
        {
            try
            {
                result.RemoteDatabaseOk = await TestRemoteDatabaseAsync(cancellationToken);
                result.DatabaseMessage = result.RemoteDatabaseOk ? "OK" : "Connection failed";
            }
            catch (Exception ex)
            {
                result.DatabaseMessage = ex.Message;
            }
        }

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

        var remoteOk = await TestRemoteDatabaseAsync(cancellationToken);
        var ftpOk = !_options.Ftp.Enabled || await _fileSync.TestFtpAsync(cancellationToken);
        var pending = await EstimatePendingRecordsAsync(commercialUserId, cancellationToken);

        return new SyncStatusDto
        {
            SyncEnabled = _options.Enabled && !string.IsNullOrWhiteSpace(RemoteConnectionString),
            RemoteDatabaseConnected = remoteOk,
            FtpConnected = ftpOk,
            IsSyncInProgress = IsSyncInProgress,
            AutoSyncEnabled = settings.AutoSyncEnabled,
            IntervalMinutes = settings.IntervalMinutes,
            LastSuccessfulSyncAt = lastSuccess?.FinishedAt,
            LastSyncStatus = lastRun?.Status,
            LastSyncError = lastRun?.Status == SyncRunStatuses.Failed ? lastRun.ErrorMessage : null,
            EstimatedPendingRecords = pending,
            LastRecordsPushed = lastRun?.RecordsPushed ?? 0,
            LastFilesPushed = lastRun?.FilesPushed ?? 0,
        };
    }

    public async Task<SyncPushResultDto> PushAsync(int commercialUserId, string trigger, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new SyncPushResultDto { Success = false, Message = "Sync is disabled in configuration." };
        }

        var remoteCs = RemoteConnectionString;
        if (string.IsNullOrWhiteSpace(remoteCs))
        {
            return new SyncPushResultDto { Success = false, Message = "SyncDatabase connection string is not configured." };
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

        try
        {
            await using var localConn = new MySqlConnection(LocalConnectionString);
            await using var remoteConn = new MySqlConnection(remoteCs);
            await localConn.OpenAsync(cancellationToken);
            await remoteConn.OpenAsync(cancellationToken);

            var totalRecords = 0;
            foreach (var table in SyncTableRegistry.Tables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pushed = await PushTableAsync(
                    localConn,
                    remoteConn,
                    table,
                    commercialUserId,
                    cancellationToken);
                totalRecords += pushed;
            }

            await PushTableCurrentOrderLinksAsync(
                localConn,
                remoteConn,
                commercialUserId,
                cancellationToken);

            var filesPushed = 0;
            if (_options.Ftp.Enabled)
            {
                filesPushed = await _fileSync.PushImagesAsync(commercialUserId, cancellationToken);
            }

            run.Status = SyncRunStatuses.Success;
            run.RecordsPushed = totalRecords;
            run.FilesPushed = filesPushed;
            run.FinishedAt = DateTime.UtcNow;
            run.UpdateDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return new SyncPushResultDto
            {
                Success = true,
                Message = "Sync completed successfully.",
                RecordsPushed = totalRecords,
                FilesPushed = filesPushed,
                RunId = run.Id,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database sync failed for commercial user {CommercialUserId}", commercialUserId);
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
                RecordsPushed = r.RecordsPushed,
                FilesPushed = r.FilesPushed,
                ErrorMessage = r.ErrorMessage,
            })
            .ToListAsync(cancellationToken);
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

    private async Task<int> PushTableAsync(
        MySqlConnection localConn,
        MySqlConnection remoteConn,
        SyncTableDefinition table,
        int commercialUserId,
        CancellationToken cancellationToken)
    {
        var watermark = await _db.SyncWatermarks
            .FirstOrDefaultAsync(
                w => w.CommercialUserId == commercialUserId && w.TableName == table.TableName,
                cancellationToken);

        if (watermark is { IsDeleted: true })
        {
            watermark.IsDeleted = false;
            watermark.UpdateDate = DateTime.UtcNow;
        }

        var columns = await GetTableColumnsAsync(localConn, table.TableName, cancellationToken);
        if (columns.Count == 0)
        {
            _logger.LogDebug("Skipping sync for {TableName} — table not found on local database", table.TableName);
            return 0;
        }

        var remoteColumns = await GetTableColumnsAsync(remoteConn, table.TableName, cancellationToken);
        if (remoteColumns.Count == 0)
        {
            _logger.LogWarning("Skipping sync for {TableName} — table not found on remote database", table.TableName);
            return 0;
        }

        var selectSql = new StringBuilder();
        selectSql.Append("SELECT ");
        selectSql.Append(string.Join(", ", columns.Select(c => $"`{c}`")));
        selectSql.Append($" FROM `{table.TableName}` WHERE {table.WhereClause}");
        if (watermark != null)
        {
            selectSql.Append(" AND UpdateDate > @watermark");
        }
        selectSql.Append(" ORDER BY UpdateDate ASC, Id ASC");

        var totalPushed = 0;
        DateTime maxUpdateDate = watermark?.LastSyncedUpdateDate ?? DateTime.MinValue;
        var batchSize = Math.Max(50, _options.BatchSize);

        await using (var selectCmd = new MySqlCommand(selectSql.ToString(), localConn))
        {
            selectCmd.Parameters.AddWithValue("@commercialUserId", commercialUserId);
            if (watermark != null)
            {
                selectCmd.Parameters.AddWithValue("@watermark", watermark.LastSyncedUpdateDate);
            }

            await using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);
            var batch = new List<object?[]>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var values = new object?[reader.FieldCount];
                reader.GetValues(values);
                batch.Add(values);

                var updateDateIndex = -1;
                for (var ci = 0; ci < columns.Count; ci++)
                {
                    if (columns[ci].Equals("UpdateDate", StringComparison.OrdinalIgnoreCase))
                    {
                        updateDateIndex = ci;
                        break;
                    }
                }
                if (updateDateIndex >= 0 && values[updateDateIndex] is DateTime dt && dt > maxUpdateDate)
                {
                    maxUpdateDate = dt;
                }

                if (batch.Count >= batchSize)
                {
                    PrepareRowsForRemotePush(table, columns, batch);
                    totalPushed += await UpsertBatchAsync(remoteConn, table.TableName, columns, batch, cancellationToken);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                PrepareRowsForRemotePush(table, columns, batch);
                totalPushed += await UpsertBatchAsync(remoteConn, table.TableName, columns, batch, cancellationToken);
            }
        }

        if (totalPushed > 0 || watermark == null)
        {
            if (watermark == null)
            {
                watermark = new SyncWatermark
                {
                    CommercialUserId = commercialUserId,
                    TableName = table.TableName,
                    LastSyncedUpdateDate = maxUpdateDate == DateTime.MinValue ? DateTime.UtcNow : maxUpdateDate,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false,
                };
                _db.SyncWatermarks.Add(watermark);
            }
            else if (maxUpdateDate > watermark.LastSyncedUpdateDate)
            {
                watermark.LastSyncedUpdateDate = maxUpdateDate;
                watermark.UpdateDate = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return totalPushed;
    }

    /// <summary>
    /// Tables.CurrentOrderId references CustomerOrders, which sync later. Strip on upsert, restore after orders exist.
    /// </summary>
    private static void PrepareRowsForRemotePush(
        SyncTableDefinition table,
        IReadOnlyList<string> columns,
        List<object?[]> rows)
    {
        if (!table.TableName.Equals("Tables", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var currentOrderIdIndex = -1;
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].Equals("CurrentOrderId", StringComparison.OrdinalIgnoreCase))
            {
                currentOrderIdIndex = i;
                break;
            }
        }

        if (currentOrderIdIndex < 0)
        {
            return;
        }

        foreach (var row in rows)
        {
            row[currentOrderIdIndex] = DBNull.Value;
        }
    }

    private async Task PushTableCurrentOrderLinksAsync(
        MySqlConnection localConn,
        MySqlConnection remoteConn,
        int commercialUserId,
        CancellationToken cancellationToken)
    {
        const string selectSql = """
            SELECT Id, CurrentOrderId
            FROM `Tables`
            WHERE InsertByUserId = @commercialUserId
              AND CurrentOrderId IS NOT NULL
              AND IsDeleted = 0
            """;

        await using var selectCmd = new MySqlCommand(selectSql, localConn);
        selectCmd.Parameters.AddWithValue("@commercialUserId", commercialUserId);

        var links = new List<(int TableId, int OrderId)>();
        await using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                links.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }
        }

        if (links.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var (tableId, orderId) in links)
        {
            await using var checkCmd = new MySqlCommand(
                "SELECT 1 FROM `CustomerOrders` WHERE Id = @orderId LIMIT 1",
                remoteConn);
            checkCmd.Parameters.AddWithValue("@orderId", orderId);
            var exists = await checkCmd.ExecuteScalarAsync(cancellationToken);
            if (exists == null)
            {
                continue;
            }

            await using var updateCmd = new MySqlCommand(
                """
                UPDATE `Tables`
                SET `CurrentOrderId` = @orderId, `UpdateDate` = @updateDate
                WHERE Id = @tableId
                """,
                remoteConn);
            updateCmd.Parameters.AddWithValue("@orderId", orderId);
            updateCmd.Parameters.AddWithValue("@tableId", tableId);
            updateCmd.Parameters.AddWithValue("@updateDate", now);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task<int> UpsertBatchAsync(
        MySqlConnection remoteConn,
        string tableName,
        IReadOnlyList<string> columns,
        List<object?[]> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        var sb = new StringBuilder();
        sb.Append($"INSERT INTO `{tableName}` (");
        sb.Append(string.Join(", ", columns.Select(c => $"`{c}`")));
        sb.Append(") VALUES ");

        await using var cmd = new MySqlCommand { Connection = remoteConn };
        var valueGroups = new List<string>();
        var paramIndex = 0;

        foreach (var row in rows)
        {
            var placeholders = new List<string>();
            for (var i = 0; i < columns.Count; i++)
            {
                var paramName = $"@p{paramIndex++}";
                placeholders.Add(paramName);
                cmd.Parameters.AddWithValue(paramName, row[i] ?? DBNull.Value);
            }
            valueGroups.Add($"({string.Join(", ", placeholders)})");
        }

        sb.Append(string.Join(", ", valueGroups));
        sb.Append(" ON DUPLICATE KEY UPDATE ");
        sb.Append(string.Join(", ", columns
            .Where(c => !c.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(c => $"`{c}`=VALUES(`{c}`)")));

        cmd.CommandText = sb.ToString();
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows.Count;
    }

    private async Task<IReadOnlyList<string>> GetTableColumnsAsync(
        MySqlConnection conn,
        string tableName,
        CancellationToken cancellationToken)
    {
        if (_columnCache.TryGetValue(tableName, out var cached))
        {
            return cached;
        }

        const string sql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@tableName)
            ORDER BY ORDINAL_POSITION
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tableName", tableName);

        var columns = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        var list = columns.AsReadOnly();
        _columnCache[tableName] = list;
        return list;
    }

    private async Task<int> EstimatePendingRecordsAsync(int commercialUserId, CancellationToken cancellationToken)
    {
        var total = 0;
        await using var conn = new MySqlConnection(LocalConnectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var table in SyncTableRegistry.Tables)
        {
            var columns = await GetTableColumnsAsync(conn, table.TableName, cancellationToken);
            if (columns.Count == 0)
            {
                continue;
            }

            var watermark = await _db.SyncWatermarks.AsNoTracking()
                .FirstOrDefaultAsync(
                    w => w.CommercialUserId == commercialUserId && w.TableName == table.TableName && !w.IsDeleted,
                    cancellationToken);

            var sql = $"SELECT COUNT(*) FROM `{table.TableName}` WHERE {table.WhereClause}";
            if (watermark != null)
            {
                sql += " AND UpdateDate > @watermark";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@commercialUserId", commercialUserId);
            if (watermark != null)
            {
                cmd.Parameters.AddWithValue("@watermark", watermark.LastSyncedUpdateDate);
            }

            try
            {
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
                total += count;
            }
            catch (MySqlException ex)
            {
                _logger.LogWarning(ex, "Could not estimate pending records for {TableName}", table.TableName);
            }
        }

        return total;
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

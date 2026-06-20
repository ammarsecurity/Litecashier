using FluentFTP;
using Microsoft.Extensions.Options;
using RestaurantPOS.Configuration;

namespace RestaurantPOS.Services.Sync;

public class FileSyncService : IFileSyncService
{
    private const string BackupFilePrefix = "litecashier-backup-";

    private readonly IConfiguration _configuration;
    private readonly SyncSettingsOptions _options;
    private readonly ILogger<FileSyncService> _logger;

    public FileSyncService(
        IConfiguration configuration,
        IOptions<SyncSettingsOptions> options,
        ILogger<FileSyncService> logger)
    {
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> TestFtpAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Ftp.Enabled)
        {
            return false;
        }

        try
        {
            using var client = CreateFtpClient();
            await client.Connect(cancellationToken);
            var ok = client.IsConnected;
            await client.Disconnect(cancellationToken);
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FTP connection test failed");
            return false;
        }
    }

    public async Task<BackupUploadResult> UploadBackupArchiveAsync(
        string localZipPath,
        string remoteFileName,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Ftp.Enabled)
        {
            throw new InvalidOperationException("FTP is disabled in configuration.");
        }

        if (!File.Exists(localZipPath))
        {
            throw new FileNotFoundException("Backup archive not found.", localZipPath);
        }

        var sizeBytes = new FileInfo(localZipPath).Length;
        var safeName = SanitizeRemoteFileName(remoteFileName);
        var backupDir = NormalizeRemotePath(_options.Ftp.RemoteBackupPath);
        var tempRemotePath = CombineRemotePath(backupDir, $"{safeName}.uploading");
        var finalRemotePath = CombineRemotePath(backupDir, safeName);

        using var client = CreateFtpClient(forUpload: true);
        await client.Connect(cancellationToken);
        try
        {
            await EnsureRemoteDirectoryAsync(client, backupDir, cancellationToken);

            var status = await client.UploadFile(
                localZipPath,
                tempRemotePath,
                FtpRemoteExists.Overwrite,
                true,
                FtpVerify.None,
                null,
                cancellationToken);

            if (status != FtpStatus.Success)
            {
                throw new InvalidOperationException($"FTP upload failed with status {status}.");
            }

            if (await client.FileExists(finalRemotePath, cancellationToken))
            {
                await client.DeleteFile(finalRemotePath, cancellationToken);
            }

            await client.MoveFile(tempRemotePath, finalRemotePath, FtpRemoteExists.Overwrite, cancellationToken);

            await CleanupOldBackupsAsync(client, backupDir, cancellationToken);

            _logger.LogInformation(
                "Uploaded backup {FileName} ({SizeBytes} bytes) to FTP {RemotePath}",
                safeName,
                sizeBytes,
                finalRemotePath);

            return new BackupUploadResult
            {
                RemoteFileName = safeName,
                SizeBytes = sizeBytes,
            };
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.Disconnect(cancellationToken);
            }
        }
    }

    private async Task CleanupOldBackupsAsync(
        AsyncFtpClient client,
        string backupDir,
        CancellationToken cancellationToken)
    {
        var keep = _options.Ftp.KeepBackupCount;
        if (keep <= 0)
        {
            return;
        }

        var listing = await client.GetListing(backupDir, FtpListOption.ForceList, cancellationToken);
        var backups = listing
            .Where(item => item.Type == FtpObjectType.File
                           && item.Name.StartsWith(BackupFilePrefix, StringComparison.OrdinalIgnoreCase)
                           && item.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                           && !item.Name.EndsWith(".uploading", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Modified)
            .ThenByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var old in backups.Skip(keep))
        {
            try
            {
                await client.DeleteFile(old.FullName, cancellationToken);
                _logger.LogInformation("Removed old FTP backup {FileName}", old.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old FTP backup {FileName}", old.Name);
            }
        }
    }

    private AsyncFtpClient CreateFtpClient(bool forUpload = false)
    {
        var password = _options.Ftp.Password;
        if (string.IsNullOrEmpty(password))
        {
            password = _configuration["SyncSettings:Ftp:Password"] ?? "";
        }

        var host = _options.Ftp.Host;
        if (host.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            host = host["ftp://".Length..];
        }
        else if (host.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase))
        {
            host = host["ftps://".Length..];
        }

        var client = new AsyncFtpClient(host, _options.Ftp.Username, password, _options.Ftp.Port);
        client.Config.DataConnectionType = _options.Ftp.UsePassive
            ? FtpDataConnectionType.AutoPassive
            : FtpDataConnectionType.AutoActive;
        client.Config.ConnectTimeout = 15000;
        client.Config.ReadTimeout = forUpload ? 600000 : 30000;
        client.Config.DataConnectionConnectTimeout = 15000;
        client.Config.DataConnectionReadTimeout = forUpload ? 600000 : 30000;
        return client;
    }

    private static async Task EnsureRemoteDirectoryAsync(
        AsyncFtpClient client,
        string remotePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return;
        }

        var normalized = remotePath.Replace('\\', '/').TrimEnd('/');
        if (await client.DirectoryExists(normalized, cancellationToken))
        {
            return;
        }

        await client.CreateDirectory(normalized, true, cancellationToken);
    }

    private static string NormalizeRemotePath(string path)
    {
        return path.Replace('\\', '/').Trim().TrimEnd('/');
    }

    private static string CombineRemotePath(string basePath, string fileName)
    {
        var baseNorm = basePath.Replace('\\', '/').TrimEnd('/');
        var fileNorm = fileName.Replace('\\', '/').TrimStart('/');
        return string.IsNullOrEmpty(baseNorm) ? fileNorm : $"{baseNorm}/{fileNorm}";
    }

    private static string SanitizeRemoteFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Remote file name is required.", nameof(fileName));
        }

        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(ch, '_');
        }

        return name;
    }
}

using FluentFTP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestaurantPOS.Configuration;
using RestaurantPOS.Db;
using RestaurantPOS.Models.Sync;

namespace RestaurantPOS.Services.Sync;

public class FileSyncService : IFileSyncService
{
    private readonly DbConfig _db;
    private readonly IConfiguration _configuration;
    private readonly SyncSettingsOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileSyncService> _logger;

    public FileSyncService(
        DbConfig db,
        IConfiguration configuration,
        IOptions<SyncSettingsOptions> options,
        IWebHostEnvironment env,
        ILogger<FileSyncService> logger)
    {
        _db = db;
        _configuration = configuration;
        _options = options.Value;
        _env = env;
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

    public async Task<int> PushImagesAsync(int commercialUserId, CancellationToken cancellationToken = default)
    {
        if (!_options.Ftp.Enabled)
        {
            return 0;
        }

        var localRoot = ResolveImagesRoot();
        if (!Directory.Exists(localRoot))
        {
            return 0;
        }

        var relativePaths = await CollectImagePathsAsync(commercialUserId, localRoot, cancellationToken);
        if (relativePaths.Count == 0)
        {
            return 0;
        }

        var watermarks = await _db.SyncFileWatermarks
            .Where(w => w.CommercialUserId == commercialUserId && !w.IsDeleted)
            .ToDictionaryAsync(w => w.RelativePath, w => w, StringComparer.OrdinalIgnoreCase, cancellationToken);

        using var client = CreateFtpClient();
        await client.Connect(cancellationToken);
        await EnsureRemoteDirectoryAsync(client, _options.Ftp.RemoteImagesPath, cancellationToken);

        var pushed = 0;
        foreach (var relativePath in relativePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalized = relativePath.Replace('\\', '/').TrimStart('/');
            var localPath = Path.Combine(localRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(localPath))
            {
                continue;
            }

            var lastWrite = File.GetLastWriteTimeUtc(localPath);
            if (watermarks.TryGetValue(normalized, out var mark) && mark.LastModifiedUtc >= lastWrite)
            {
                continue;
            }

            var remotePath = CombineRemotePath(_options.Ftp.RemoteImagesPath, normalized);
            var remoteDir = GetRemoteDirectory(remotePath);
            if (!string.IsNullOrWhiteSpace(remoteDir))
            {
                await EnsureRemoteDirectoryAsync(client, remoteDir, cancellationToken);
            }

            var status = await client.UploadFile(
                localPath,
                remotePath,
                FtpRemoteExists.Overwrite,
                true,
                FtpVerify.None,
                null,
                cancellationToken);

            if (status != FtpStatus.Success)
            {
                _logger.LogWarning("FTP upload failed for {LocalPath} -> {RemotePath}: {Status}", localPath, remotePath, status);
                continue;
            }

            if (mark == null)
            {
                mark = await _db.SyncFileWatermarks.FirstOrDefaultAsync(
                    w => w.CommercialUserId == commercialUserId
                         && w.RelativePath == normalized
                         && w.IsDeleted,
                    cancellationToken);

                if (mark != null)
                {
                    mark.IsDeleted = false;
                    mark.LastModifiedUtc = lastWrite;
                    mark.SyncedAt = DateTime.UtcNow;
                    mark.UpdateDate = DateTime.UtcNow;
                }
                else
                {
                    mark = new SyncFileWatermark
                    {
                        CommercialUserId = commercialUserId,
                        RelativePath = normalized,
                        LastModifiedUtc = lastWrite,
                        SyncedAt = DateTime.UtcNow,
                        InsertDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow,
                        IsDeleted = false,
                    };
                    _db.SyncFileWatermarks.Add(mark);
                }
            }
            else
            {
                mark.LastModifiedUtc = lastWrite;
                mark.SyncedAt = DateTime.UtcNow;
                mark.UpdateDate = DateTime.UtcNow;
            }

            pushed++;
        }

        if (pushed > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        await client.Disconnect(cancellationToken);
        return pushed;
    }

    private AsyncFtpClient CreateFtpClient()
    {
        var password = _options.Ftp.Password;
        if (string.IsNullOrEmpty(password))
        {
            password = _configuration["SyncSettings:Ftp:Password"] ?? "";
        }

        var client = new AsyncFtpClient(_options.Ftp.Host, _options.Ftp.Username, password, _options.Ftp.Port);
        client.Config.DataConnectionType = _options.Ftp.UsePassive
            ? FtpDataConnectionType.AutoPassive
            : FtpDataConnectionType.AutoActive;
        client.Config.ConnectTimeout = 15000;
        client.Config.ReadTimeout = 30000;
        client.Config.DataConnectionConnectTimeout = 15000;
        client.Config.DataConnectionReadTimeout = 30000;
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

    private static string GetRemoteDirectory(string remoteFilePath)
    {
        var normalized = remoteFilePath.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        if (lastSlash <= 0)
        {
            return "";
        }

        return normalized[..lastSlash];
    }

    private async Task<HashSet<string>> CollectImagePathsAsync(int commercialUserId, string localRoot, CancellationToken cancellationToken)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var itemImages = await _db.Items.AsNoTracking()
            .Where(i => i.InsertByUserId == commercialUserId && !i.IsDeleted && i.Image != null && i.Image != "")
            .Select(i => i.Image!)
            .ToListAsync(cancellationToken);

        foreach (var img in itemImages)
        {
            AddImagePath(paths, localRoot, img);
        }

        var userLogos = await _db.Users.AsNoTracking()
            .Where(u => (u.Id == commercialUserId || u.InsertByUserId == commercialUserId) && !u.IsDeleted && u.Logo != null && u.Logo != "")
            .Select(u => u.Logo!)
            .ToListAsync(cancellationToken);

        foreach (var logo in userLogos)
        {
            AddImagePath(paths, localRoot, logo);
        }

        foreach (var file in Directory.EnumerateFiles(localRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(localRoot, file).Replace('\\', '/');
            paths.Add(relative);
        }

        return paths;
    }

    private static void AddImagePath(HashSet<string> paths, string localRoot, string value)
    {
        var trimmed = value.Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        trimmed = trimmed.TrimStart('/');
        if (trimmed.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["Images/".Length..];
        }

        var full = Path.Combine(localRoot, trimmed.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(full))
        {
            paths.Add(trimmed);
        }
    }

    private string ResolveImagesRoot()
    {
        var configured = _options.ImagesLocalPath;
        if (Path.IsPathRooted(configured))
        {
            return configured;
        }

        return Path.Combine(_env.ContentRootPath, configured);
    }

    private static string CombineRemotePath(string basePath, string relative)
    {
        var baseNorm = basePath.Replace('\\', '/').TrimEnd('/');
        var relNorm = relative.Replace('\\', '/').TrimStart('/');
        return $"{baseNorm}/{relNorm}";
    }
}

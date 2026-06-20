namespace RestaurantPOS.Configuration;

public class SyncSettingsOptions
{
    public const string SectionName = "SyncSettings";

    public bool Enabled { get; set; } = true;

    public bool AutoSyncEnabled { get; set; }

    public int AutoSyncIntervalMinutes { get; set; } = 10;

    public int BatchSize { get; set; } = 500;

    public string ImagesLocalPath { get; set; } = "wwwroot/Images";

    public SyncFtpOptions Ftp { get; set; } = new();
}

public class SyncFtpOptions
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "";

    public int Port { get; set; } = 21;

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    /// <summary>Legacy per-file image path; unused by ZIP backup sync.</summary>
    public string RemoteImagesPath { get; set; } = "";

    /// <summary>FTP folder for litecashier-backup-*.zip archives.</summary>
    public string RemoteBackupPath { get; set; } = "backups";

    /// <summary>Keep this many newest backup ZIPs on FTP (0 = unlimited).</summary>
    public int KeepBackupCount { get; set; } = 10;

    /// <summary>Passive mode — recommended for most shared hosting FTP.</summary>
    public bool UsePassive { get; set; } = true;
}

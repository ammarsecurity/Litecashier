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

    public string RemoteImagesPath { get; set; } = "";

    /// <summary>Passive mode — recommended for most shared hosting FTP.</summary>
    public bool UsePassive { get; set; } = true;
}

namespace RestaurantPOS.Logging;

public class ErrorLogSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>Relative to content root, e.g. wwwroot/logs</summary>
    public string LogDirectory { get; set; } = "wwwroot/logs";

    public int[] LogStatusCodes { get; set; } = { 404, 500 };

    public int MaxRequestBodyLength { get; set; } = 8192;

    public int MaxResponseBodyLength { get; set; } = 8192;

    /// <summary>IANA time zone for log timestamps (e.g. Asia/Baghdad).</summary>
    public string? TimeZoneId { get; set; }
}

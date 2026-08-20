namespace LicenseServer.Models;

public class LicenseKey
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    /// <summary>Cashier | Restaurant | Both</summary>
    public string Product { get; set; } = "Both";
    /// <summary>Days | Months | Lifetime</summary>
    public string DurationType { get; set; } = "Days";
    public int DurationValue { get; set; } = 2;
    public int MaxActivations { get; set; } = 1;
    public bool IsRevoked { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Activation> Activations { get; set; } = new();
}

public class Activation
{
    public int Id { get; set; }
    public int LicenseKeyId { get; set; }
    public LicenseKey? LicenseKey { get; set; }
    public string MachineId { get; set; } = "";
    public string Product { get; set; } = "";
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}

public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    /// <summary>Cashier | Restaurant | Both</summary>
    public string ProductScope { get; set; } = "Both";
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<AnnouncementDismissal> Dismissals { get; set; } = new();
}

public class AnnouncementDismissal
{
    public int Id { get; set; }
    public int AnnouncementId { get; set; }
    public Announcement? Announcement { get; set; }
    public string MachineId { get; set; } = "";
    public string Product { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DeviceControl
{
    public int Id { get; set; }
    public string MachineId { get; set; } = "";
    public string Product { get; set; } = "";
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public record CreateKeyRequest(
    string? Product,
    string DurationType,
    int DurationValue,
    int? MaxActivations,
    string? Notes);

public record ActivateRequest(string Code, string MachineId, string Product);
public record ValidateRequest(string Code, string MachineId, string Product);
public record RevokeRequest(string Code);

public record UpsertAnnouncementRequest(
    string? Title,
    string? Body,
    string? ImageUrl,
    string? LinkUrl,
    string? ProductScope,
    bool? IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int? SortOrder);

public record DismissAnnouncementRequest(string MachineId, string Product);
public record DevicePauseRequest(string MachineId, string Product, string? Reason);
public record DeviceResumeRequest(string MachineId, string Product);
public record DeviceSyncRequest(string Code, string MachineId, string Product);

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

public record CreateKeyRequest(
    string? Product,
    string DurationType,
    int DurationValue,
    int? MaxActivations,
    string? Notes);

public record ActivateRequest(string Code, string MachineId, string Product);
public record ValidateRequest(string Code, string MachineId, string Product);
public record RevokeRequest(string Code);

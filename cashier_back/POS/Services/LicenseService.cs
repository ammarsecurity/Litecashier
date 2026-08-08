using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POS.Services;

public class LicenseOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "";
    public string Product { get; set; } = "Cashier";
    /// <summary>Hours between online revalidation attempts.</summary>
    public int RevalidateHours { get; set; } = 24;
}

public class LocalLicenseState
{
    public string Code { get; set; } = "";
    public string Product { get; set; } = "";
    public string MachineId { get; set; } = "";
    public string DurationType { get; set; } = "";
    public int DurationValue { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsLifetime { get; set; }
    public DateTime? LastValidatedAt { get; set; }
}

public class LicenseStatusDto
{
    public bool EnforcementEnabled { get; set; }
    public bool IsActive { get; set; }
    public bool IsLifetime { get; set; }
    public string? Code { get; set; }
    public string? DurationType { get; set; }
    public int DurationValue { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DaysRemaining { get; set; }
    public string MachineId { get; set; } = "";
    public string Message { get; set; } = "";
}

public interface ILicenseService
{
    bool EnforcementEnabled { get; }
    string GetMachineId();
    LicenseStatusDto GetStatus();
    Task<LicenseStatusDto> ActivateAsync(string code, CancellationToken ct = default);
    Task<bool> EnsureLicensedAsync(CancellationToken ct = default);
}

public class LicenseService : ILicenseService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LicenseOptions _options;
    private readonly ILogger<LicenseService> _logger;
    private readonly object _gate = new();
    private LocalLicenseState? _cache;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LicenseService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LicenseService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = new LicenseOptions();
        configuration.GetSection("License").Bind(_options);
        if (string.IsNullOrWhiteSpace(_options.Product))
            _options.Product = "Cashier";
    }

    public bool EnforcementEnabled =>
        _options.Enabled && !string.IsNullOrWhiteSpace(_options.BaseUrl);

    private string DataDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Litecashier");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private string LicenseFilePath => Path.Combine(DataDir, $"license-{_options.Product.ToLowerInvariant()}.dat");
    private string MachineFilePath => Path.Combine(DataDir, "machine.id");

    public string GetMachineId()
    {
        try
        {
            if (File.Exists(MachineFilePath))
            {
                var existing = File.ReadAllText(MachineFilePath).Trim();
                if (!string.IsNullOrWhiteSpace(existing))
                    return existing;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed reading machine.id");
        }

        var raw = new StringBuilder();
        raw.Append(Environment.MachineName).Append('|');
        raw.Append(Environment.UserName).Append('|');
        try
        {
            raw.Append(Environment.OSVersion).Append('|');
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            foreach (var d in drives)
            {
                try { raw.Append(d.Name).Append(d.TotalSize).Append('|'); }
                catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw.ToString())))
            .ToLowerInvariant();
        var id = hash[..Math.Min(32, hash.Length)];

        try { File.WriteAllText(MachineFilePath, id); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed writing machine.id"); }

        return id;
    }

    private LocalLicenseState? LoadState()
    {
        lock (_gate)
        {
            if (_cache != null) return _cache;
            try
            {
                if (!File.Exists(LicenseFilePath)) return null;
                var b64 = File.ReadAllText(LicenseFilePath).Trim();
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                _cache = JsonSerializer.Deserialize<LocalLicenseState>(json, JsonOpts);
                return _cache;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed loading local license");
                return null;
            }
        }
    }

    private void SaveState(LocalLicenseState state)
    {
        lock (_gate)
        {
            _cache = state;
            var json = JsonSerializer.Serialize(state, JsonOpts);
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            File.WriteAllText(LicenseFilePath, b64);
        }
    }

    private static bool IsLocallyActive(LocalLicenseState? state)
    {
        if (state == null || string.IsNullOrWhiteSpace(state.Code)) return false;
        if (state.IsLifetime) return true;
        if (state.ExpiresAt == null) return true;
        return state.ExpiresAt > DateTime.UtcNow;
    }

    public LicenseStatusDto GetStatus()
    {
        var machineId = GetMachineId();
        if (!EnforcementEnabled)
        {
            return new LicenseStatusDto
            {
                EnforcementEnabled = false,
                IsActive = true,
                MachineId = machineId,
                Message = "licenseEnforcementDisabled"
            };
        }

        var state = LoadState();
        var active = IsLocallyActive(state);
        int? daysRemaining = null;
        if (state?.ExpiresAt != null)
        {
            daysRemaining = (int)Math.Ceiling((state.ExpiresAt.Value - DateTime.UtcNow).TotalDays);
            if (daysRemaining < 0) daysRemaining = 0;
        }

        return new LicenseStatusDto
        {
            EnforcementEnabled = true,
            IsActive = active,
            IsLifetime = state?.IsLifetime == true,
            Code = MaskCode(state?.Code),
            DurationType = state?.DurationType,
            DurationValue = state?.DurationValue ?? 0,
            ExpiresAt = state?.ExpiresAt,
            DaysRemaining = state?.IsLifetime == true ? null : daysRemaining,
            MachineId = machineId,
            Message = active ? "ok" : (state == null ? "notActivated" : "expired")
        };
    }

    private static string? MaskCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length < 8) return code;
        return code[..3] + "****" + code[^4..];
    }

    public async Task<LicenseStatusDto> ActivateAsync(string code, CancellationToken ct = default)
    {
        if (!EnforcementEnabled)
            return GetStatus();

        code = (code ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("codeRequired");

        var machineId = GetMachineId();
        var client = _httpClientFactory.CreateClient("LicenseServer");
        var payload = new { code, machineId, product = _options.Product };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("api/activate", payload, JsonOpts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "License activate network error");
            throw new InvalidOperationException("licenseServerUnreachable");
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = TryReadMessage(body) ?? "activationFailed";
            throw new InvalidOperationException(msg);
        }

        var remote = JsonSerializer.Deserialize<RemoteLicenseDto>(body, JsonOpts)
            ?? throw new InvalidOperationException("activationFailed");

        var state = new LocalLicenseState
        {
            Code = remote.Code ?? code,
            Product = remote.Product ?? _options.Product,
            MachineId = machineId,
            DurationType = remote.DurationType ?? "",
            DurationValue = remote.DurationValue,
            ActivatedAt = remote.ActivatedAt,
            ExpiresAt = remote.ExpiresAt,
            IsLifetime = remote.IsLifetime || remote.ExpiresAt == null,
            LastValidatedAt = DateTime.UtcNow
        };
        SaveState(state);
        return GetStatus();
    }

    public async Task<bool> EnsureLicensedAsync(CancellationToken ct = default)
    {
        if (!EnforcementEnabled) return true;

        var state = LoadState();
        if (!IsLocallyActive(state)) return false;

        var hours = Math.Max(1, _options.RevalidateHours);
        var needsOnline = state!.LastValidatedAt == null
            || state.LastValidatedAt < DateTime.UtcNow.AddHours(-hours);

        if (!needsOnline) return true;

        try
        {
            var client = _httpClientFactory.CreateClient("LicenseServer");
            var payload = new { code = state.Code, machineId = GetMachineId(), product = _options.Product };
            var response = await client.PostAsJsonAsync("api/validate", payload, JsonOpts, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                // Offline grace: keep local until ExpiresAt
                _logger.LogWarning("License validate HTTP {Status}", (int)response.StatusCode);
                return IsLocallyActive(state);
            }

            var remote = JsonSerializer.Deserialize<RemoteLicenseDto>(body, JsonOpts);
            if (remote == null) return IsLocallyActive(state);

            if (!remote.IsActive)
            {
                state.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
                state.IsLifetime = false;
                state.LastValidatedAt = DateTime.UtcNow;
                SaveState(state);
                return false;
            }

            state.ExpiresAt = remote.ExpiresAt;
            state.IsLifetime = remote.IsLifetime || remote.ExpiresAt == null;
            state.DurationType = remote.DurationType ?? state.DurationType;
            state.DurationValue = remote.DurationValue;
            state.LastValidatedAt = DateTime.UtcNow;
            SaveState(state);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "License revalidate failed; using local cache");
            return IsLocallyActive(state);
        }
    }

    private static string? TryReadMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m))
                return m.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    private sealed class RemoteLicenseDto
    {
        public bool IsActive { get; set; } = true;
        public string? Code { get; set; }
        public string? Product { get; set; }
        public string? DurationType { get; set; }
        public int DurationValue { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsLifetime { get; set; }
        public string? Message { get; set; }
    }
}

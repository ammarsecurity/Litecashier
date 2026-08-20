using System.Security.Cryptography;
using LicenseServer.Data;
using LicenseServer.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LicenseDb")
        ?? "Data Source=licenses.db"));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
    db.Database.EnsureCreated();
    await db.EnsureExtendedSchemaAsync();
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var adminKey = builder.Configuration["AdminApiKey"] ?? "change-me-admin-key";

bool IsAdmin(HttpRequest req) =>
    string.Equals(req.Headers["X-Admin-Key"].ToString(), adminKey, StringComparison.Ordinal);

static string NormalizeCode(string? code) =>
    (code ?? "").Trim().ToUpperInvariant().Replace(" ", "");

static string NormalizeProduct(string? product)
{
    var p = (product ?? "").Trim();
    if (string.Equals(p, "Cashier", StringComparison.OrdinalIgnoreCase)) return "Cashier";
    if (string.Equals(p, "Restaurant", StringComparison.OrdinalIgnoreCase)) return "Restaurant";
    if (string.Equals(p, "Both", StringComparison.OrdinalIgnoreCase)) return "Both";
    return p;
}

static bool ProductMatches(string keyProduct, string requestProduct)
{
    if (string.Equals(keyProduct, "Both", StringComparison.OrdinalIgnoreCase)) return true;
    return string.Equals(keyProduct, requestProduct, StringComparison.OrdinalIgnoreCase);
}

static bool ScopeMatches(string scope, string product) =>
    string.Equals(scope, "Both", StringComparison.OrdinalIgnoreCase)
    || string.Equals(scope, product, StringComparison.OrdinalIgnoreCase);

static string GenerateCode()
{
    static string Block()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(4);
        var chars = new char[4];
        for (var i = 0; i < 4; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }
    return $"LC-{Block()}-{Block()}-{Block()}";
}

static DateTime? ComputeExpiry(string durationType, int durationValue, DateTime fromUtc)
{
    return durationType.ToLowerInvariant() switch
    {
        "lifetime" => null,
        "months" => fromUtc.AddMonths(Math.Max(1, durationValue)),
        "days" => fromUtc.AddDays(Math.Max(1, durationValue)),
        _ => fromUtc.AddDays(Math.Max(1, durationValue))
    };
}

static object LicensePayload(LicenseKey key, Activation act) => new
{
    code = key.Code,
    product = act.Product,
    durationType = key.DurationType,
    durationValue = key.DurationValue,
    activatedAt = act.ActivatedAt,
    expiresAt = act.ExpiresAt,
    isLifetime = act.ExpiresAt == null,
    isActive = !key.IsRevoked && (act.ExpiresAt == null || act.ExpiresAt > DateTime.UtcNow)
};

static bool IsAnnouncementLive(Announcement a, DateTime now) =>
    a.IsActive
    && (a.StartsAt == null || a.StartsAt <= now)
    && (a.EndsAt == null || a.EndsAt >= now);

// --- Admin ---
app.MapPost("/api/admin/keys", async (HttpRequest req, CreateKeyRequest body, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var durationType = (body.DurationType ?? "Days").Trim();
    if (!new[] { "Days", "Months", "Lifetime" }.Contains(durationType, StringComparer.OrdinalIgnoreCase))
        return Results.BadRequest(new { message = "DurationType must be Days, Months, or Lifetime" });

    durationType = char.ToUpperInvariant(durationType[0]) + durationType[1..].ToLowerInvariant();
    if (durationType.Equals("Lifetime", StringComparison.OrdinalIgnoreCase))
        durationType = "Lifetime";

    var product = NormalizeProduct(body.Product ?? "Both");
    if (product is not ("Cashier" or "Restaurant" or "Both"))
        return Results.BadRequest(new { message = "Product must be Cashier, Restaurant, or Both" });

    string code;
    do { code = GenerateCode(); }
    while (await db.LicenseKeys.AnyAsync(k => k.Code == code));

    var key = new LicenseKey
    {
        Code = code,
        Product = product,
        DurationType = durationType,
        DurationValue = durationType == "Lifetime" ? 0 : Math.Max(1, body.DurationValue),
        MaxActivations = Math.Max(1, body.MaxActivations ?? 1),
        Notes = body.Notes,
        CreatedAt = DateTime.UtcNow
    };
    db.LicenseKeys.Add(key);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        key.Id,
        key.Code,
        key.Product,
        key.DurationType,
        key.DurationValue,
        key.MaxActivations,
        key.Notes,
        key.CreatedAt
    });
});

app.MapGet("/api/admin/keys", async (HttpRequest req, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var keys = await db.LicenseKeys
        .Include(k => k.Activations)
        .OrderByDescending(k => k.CreatedAt)
        .Select(k => new
        {
            k.Id,
            k.Code,
            k.Product,
            k.DurationType,
            k.DurationValue,
            k.MaxActivations,
            k.IsRevoked,
            k.Notes,
            k.CreatedAt,
            activationCount = k.Activations.Count,
            activations = k.Activations.Select(a => new
            {
                a.MachineId,
                a.Product,
                a.ActivatedAt,
                a.ExpiresAt,
                a.LastSeenAt
            })
        })
        .ToListAsync();

    return Results.Ok(keys);
});

app.MapPost("/api/admin/revoke", async (HttpRequest req, RevokeRequest body, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var code = NormalizeCode(body.Code);
    var key = await db.LicenseKeys.FirstOrDefaultAsync(k => k.Code == code);
    if (key == null) return Results.NotFound(new { message = "keyNotFound" });

    key.IsRevoked = true;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "revoked", code = key.Code });
});

// --- Admin announcements ---
app.MapGet("/api/admin/announcements", async (HttpRequest req, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var list = await db.Announcements
        .Include(a => a.Dismissals)
        .OrderBy(a => a.SortOrder)
        .ThenByDescending(a => a.CreatedAt)
        .Select(a => new
        {
            a.Id,
            a.Title,
            a.Body,
            a.ImageUrl,
            a.LinkUrl,
            a.ProductScope,
            a.IsActive,
            a.StartsAt,
            a.EndsAt,
            a.SortOrder,
            a.CreatedAt,
            dismissals = a.Dismissals.Select(d => new
            {
                d.Id,
                d.MachineId,
                d.Product,
                d.CreatedAt
            })
        })
        .ToListAsync();

    return Results.Ok(list);
});

app.MapPost("/api/admin/announcements", async (HttpRequest req, UpsertAnnouncementRequest body, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var title = (body.Title ?? "").Trim();
    if (string.IsNullOrWhiteSpace(title))
        return Results.BadRequest(new { message = "titleRequired" });

    var scope = NormalizeProduct(body.ProductScope ?? "Both");
    if (scope is not ("Cashier" or "Restaurant" or "Both"))
        return Results.BadRequest(new { message = "invalidProductScope" });

    var item = new Announcement
    {
        Title = title,
        Body = (body.Body ?? "").Trim(),
        ImageUrl = string.IsNullOrWhiteSpace(body.ImageUrl) ? null : body.ImageUrl.Trim(),
        LinkUrl = string.IsNullOrWhiteSpace(body.LinkUrl) ? null : body.LinkUrl.Trim(),
        ProductScope = scope,
        IsActive = body.IsActive ?? true,
        StartsAt = body.StartsAt,
        EndsAt = body.EndsAt,
        SortOrder = body.SortOrder ?? 0,
        CreatedAt = DateTime.UtcNow
    };
    db.Announcements.Add(item);
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

app.MapPut("/api/admin/announcements/{id:int}", async (HttpRequest req, int id, UpsertAnnouncementRequest body, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var item = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id);
    if (item == null) return Results.NotFound(new { message = "announcementNotFound" });

    if (!string.IsNullOrWhiteSpace(body.Title))
        item.Title = body.Title.Trim();
    if (body.Body != null)
        item.Body = body.Body.Trim();
    if (body.ImageUrl != null)
        item.ImageUrl = string.IsNullOrWhiteSpace(body.ImageUrl) ? null : body.ImageUrl.Trim();
    if (body.LinkUrl != null)
        item.LinkUrl = string.IsNullOrWhiteSpace(body.LinkUrl) ? null : body.LinkUrl.Trim();
    if (!string.IsNullOrWhiteSpace(body.ProductScope))
    {
        var scope = NormalizeProduct(body.ProductScope);
        if (scope is not ("Cashier" or "Restaurant" or "Both"))
            return Results.BadRequest(new { message = "invalidProductScope" });
        item.ProductScope = scope;
    }
    if (body.IsActive.HasValue) item.IsActive = body.IsActive.Value;
    item.StartsAt = body.StartsAt;
    item.EndsAt = body.EndsAt;
    if (body.SortOrder.HasValue) item.SortOrder = body.SortOrder.Value;

    await db.SaveChangesAsync();
    return Results.Ok(item);
});

app.MapDelete("/api/admin/announcements/{id:int}", async (HttpRequest req, int id, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var item = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id);
    if (item == null) return Results.NotFound(new { message = "announcementNotFound" });

    db.Announcements.Remove(item);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "deleted", id });
});

app.MapPost("/api/admin/announcements/{id:int}/dismiss", async (HttpRequest req, int id, DismissAnnouncementRequest body, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var machineId = (body.MachineId ?? "").Trim();
    var product = NormalizeProduct(body.Product);
    if (string.IsNullOrWhiteSpace(machineId) || product is not ("Cashier" or "Restaurant"))
        return Results.BadRequest(new { message = "invalidRequest" });

    var exists = await db.Announcements.AnyAsync(a => a.Id == id);
    if (!exists) return Results.NotFound(new { message = "announcementNotFound" });

    var dismissal = await db.AnnouncementDismissals
        .FirstOrDefaultAsync(d => d.AnnouncementId == id && d.MachineId == machineId && d.Product == product);
    if (dismissal == null)
    {
        dismissal = new AnnouncementDismissal
        {
            AnnouncementId = id,
            MachineId = machineId,
            Product = product,
            CreatedAt = DateTime.UtcNow
        };
        db.AnnouncementDismissals.Add(dismissal);
        await db.SaveChangesAsync();
    }

    return Results.Ok(new { message = "dismissed", dismissal.Id, dismissal.MachineId, dismissal.Product });
});

app.MapDelete("/api/admin/announcements/{id:int}/dismiss", async (HttpRequest req, int id, string machineId, string product, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    machineId = (machineId ?? "").Trim();
    product = NormalizeProduct(product);
    if (string.IsNullOrWhiteSpace(machineId) || product is not ("Cashier" or "Restaurant"))
        return Results.BadRequest(new { message = "invalidRequest" });

    var dismissal = await db.AnnouncementDismissals
        .FirstOrDefaultAsync(d => d.AnnouncementId == id && d.MachineId == machineId && d.Product == product);
    if (dismissal == null) return Results.NotFound(new { message = "dismissalNotFound" });

    db.AnnouncementDismissals.Remove(dismissal);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "undismissed" });
});

// --- Admin devices ---
app.MapGet("/api/admin/devices", async (HttpRequest req, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var controls = await db.DeviceControls.ToListAsync();
    var controlMap = controls.ToDictionary(
        c => $"{c.MachineId}|{c.Product}",
        c => c,
        StringComparer.OrdinalIgnoreCase);

    var activations = await db.Activations
        .Include(a => a.LicenseKey)
        .OrderByDescending(a => a.LastSeenAt)
        .ToListAsync();

    var devices = activations.Select(a =>
    {
        controlMap.TryGetValue($"{a.MachineId}|{a.Product}", out var ctrl);
        return new
        {
            a.MachineId,
            a.Product,
            licenseCode = a.LicenseKey?.Code,
            a.ActivatedAt,
            a.ExpiresAt,
            a.LastSeenAt,
            isPaused = ctrl?.IsPaused == true,
            pauseReason = ctrl?.PauseReason,
            controlUpdatedAt = ctrl?.UpdatedAt
        };
    });

    return Results.Ok(devices);
});

app.MapPost("/api/admin/devices/pause", async (HttpRequest req, DevicePauseRequest body, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var machineId = (body.MachineId ?? "").Trim();
    var product = NormalizeProduct(body.Product);
    if (string.IsNullOrWhiteSpace(machineId) || product is not ("Cashier" or "Restaurant"))
        return Results.BadRequest(new { message = "invalidRequest" });

    var ctrl = await db.DeviceControls
        .FirstOrDefaultAsync(c => c.MachineId == machineId && c.Product == product);
    if (ctrl == null)
    {
        ctrl = new DeviceControl { MachineId = machineId, Product = product };
        db.DeviceControls.Add(ctrl);
    }

    ctrl.IsPaused = true;
    ctrl.PauseReason = string.IsNullOrWhiteSpace(body.Reason) ? null : body.Reason.Trim();
    ctrl.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        ctrl.MachineId,
        ctrl.Product,
        ctrl.IsPaused,
        ctrl.PauseReason,
        ctrl.UpdatedAt
    });
});

app.MapPost("/api/admin/devices/resume", async (HttpRequest req, DeviceResumeRequest body, LicenseDbContext db) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();

    var machineId = (body.MachineId ?? "").Trim();
    var product = NormalizeProduct(body.Product);
    if (string.IsNullOrWhiteSpace(machineId) || product is not ("Cashier" or "Restaurant"))
        return Results.BadRequest(new { message = "invalidRequest" });

    var ctrl = await db.DeviceControls
        .FirstOrDefaultAsync(c => c.MachineId == machineId && c.Product == product);
    if (ctrl == null)
    {
        return Results.Ok(new { machineId, product, isPaused = false, pauseReason = (string?)null });
    }

    ctrl.IsPaused = false;
    ctrl.PauseReason = null;
    ctrl.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        ctrl.MachineId,
        ctrl.Product,
        ctrl.IsPaused,
        ctrl.PauseReason,
        ctrl.UpdatedAt
    });
});

// --- Device sync (pull) ---
app.MapPost("/api/device/sync", async (DeviceSyncRequest body, LicenseDbContext db) =>
{
    var code = NormalizeCode(body.Code);
    var machineId = (body.MachineId ?? "").Trim();
    var product = NormalizeProduct(body.Product);

    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(machineId)
        || product is not ("Cashier" or "Restaurant"))
        return Results.BadRequest(new { message = "invalidRequest" });

    var key = await db.LicenseKeys.Include(k => k.Activations)
        .FirstOrDefaultAsync(k => k.Code == code);

    if (key == null) return Results.NotFound(new { message = "invalidCode" });
    if (key.IsRevoked) return Results.BadRequest(new { message = "codeRevoked" });
    if (!ProductMatches(key.Product, product))
        return Results.BadRequest(new { message = "codeNotValidForProduct" });

    var act = key.Activations.FirstOrDefault(a =>
        a.MachineId == machineId &&
        string.Equals(a.Product, product, StringComparison.OrdinalIgnoreCase));

    if (act == null)
        return Results.BadRequest(new { message = "notActivatedOnThisMachine" });

    act.LastSeenAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var now = DateTime.UtcNow;
    var dismissedIds = await db.AnnouncementDismissals
        .Where(d => d.MachineId == machineId && d.Product == product)
        .Select(d => d.AnnouncementId)
        .ToListAsync();

    var announcements = await db.Announcements
        .Where(a => a.IsActive)
        .OrderBy(a => a.SortOrder)
        .ThenByDescending(a => a.CreatedAt)
        .ToListAsync();

    var visible = announcements
        .Where(a => ScopeMatches(a.ProductScope, product))
        .Where(a => IsAnnouncementLive(a, now))
        .Where(a => !dismissedIds.Contains(a.Id))
        .Select(a => new
        {
            a.Id,
            a.Title,
            a.Body,
            a.ImageUrl,
            a.LinkUrl,
            a.SortOrder
        })
        .ToList();

    var ctrl = await db.DeviceControls
        .FirstOrDefaultAsync(c => c.MachineId == machineId && c.Product == product);

    return Results.Ok(new
    {
        serverTime = now,
        isPaused = ctrl?.IsPaused == true,
        pauseReason = ctrl?.PauseReason,
        announcements = visible
    });
});

// --- Public activation / validate ---
app.MapPost("/api/activate", async (ActivateRequest body, LicenseDbContext db) =>
{
    var code = NormalizeCode(body.Code);
    var machineId = (body.MachineId ?? "").Trim();
    var product = NormalizeProduct(body.Product);

    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(machineId) || string.IsNullOrWhiteSpace(product))
        return Results.BadRequest(new { message = "invalidRequest" });

    if (product is not ("Cashier" or "Restaurant"))
        return Results.BadRequest(new { message = "productMustBeCashierOrRestaurant" });

    var key = await db.LicenseKeys.Include(k => k.Activations)
        .FirstOrDefaultAsync(k => k.Code == code);

    if (key == null) return Results.NotFound(new { message = "invalidCode" });
    if (key.IsRevoked) return Results.BadRequest(new { message = "codeRevoked" });
    if (!ProductMatches(key.Product, product))
        return Results.BadRequest(new { message = "codeNotValidForProduct" });

    var existing = key.Activations.FirstOrDefault(a =>
        a.MachineId == machineId &&
        string.Equals(a.Product, product, StringComparison.OrdinalIgnoreCase));

    if (existing != null)
    {
        var now = DateTime.UtcNow;
        existing.ExpiresAt = ComputeExpiry(key.DurationType, key.DurationValue, now);
        existing.ActivatedAt = now;
        existing.LastSeenAt = now;
        await db.SaveChangesAsync();
        return Results.Ok(LicensePayload(key, existing));
    }

    var distinctMachines = key.Activations.Select(a => a.MachineId).Distinct().Count();
    if (distinctMachines >= key.MaxActivations)
        return Results.BadRequest(new { message = "maxActivationsReached" });

    var activation = new Activation
    {
        LicenseKeyId = key.Id,
        MachineId = machineId,
        Product = product,
        ActivatedAt = DateTime.UtcNow,
        ExpiresAt = ComputeExpiry(key.DurationType, key.DurationValue, DateTime.UtcNow),
        LastSeenAt = DateTime.UtcNow
    };
    db.Activations.Add(activation);
    await db.SaveChangesAsync();

    return Results.Ok(LicensePayload(key, activation));
});

app.MapPost("/api/validate", async (ValidateRequest body, LicenseDbContext db) =>
{
    var code = NormalizeCode(body.Code);
    var machineId = (body.MachineId ?? "").Trim();
    var product = NormalizeProduct(body.Product);

    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(machineId))
        return Results.BadRequest(new { message = "invalidRequest" });

    var key = await db.LicenseKeys.Include(k => k.Activations)
        .FirstOrDefaultAsync(k => k.Code == code);

    if (key == null) return Results.NotFound(new { message = "invalidCode" });
    if (key.IsRevoked) return Results.Ok(new { isActive = false, message = "codeRevoked" });
    if (!ProductMatches(key.Product, product))
        return Results.Ok(new { isActive = false, message = "codeNotValidForProduct" });

    var act = key.Activations.FirstOrDefault(a =>
        a.MachineId == machineId &&
        string.Equals(a.Product, product, StringComparison.OrdinalIgnoreCase));

    if (act == null)
        return Results.Ok(new { isActive = false, message = "notActivatedOnThisMachine" });

    act.LastSeenAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var ctrl = await db.DeviceControls
        .FirstOrDefaultAsync(c => c.MachineId == machineId && c.Product == product);

    var active = act.ExpiresAt == null || act.ExpiresAt > DateTime.UtcNow;
    return Results.Ok(new
    {
        isActive = active,
        code = key.Code,
        product = act.Product,
        durationType = key.DurationType,
        durationValue = key.DurationValue,
        activatedAt = act.ActivatedAt,
        expiresAt = act.ExpiresAt,
        isLifetime = act.ExpiresAt == null,
        isPaused = ctrl?.IsPaused == true,
        pauseReason = ctrl?.PauseReason,
        message = active ? "ok" : "expired"
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/admin/ping", (HttpRequest req) =>
{
    if (!IsAdmin(req)) return Results.Unauthorized();
    return Results.Ok(new { ok = true });
});

app.Run();

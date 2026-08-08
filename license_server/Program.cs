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
        // Re-activate / refresh same machine: extend from now using key duration
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

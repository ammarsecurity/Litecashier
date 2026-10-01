using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Models;

namespace POS.Services;

public static class TenantHostHelper
{
    /// <summary>
    /// Normalize a domain/subdomain input or request host to a comparable host string.
    /// Returns null for empty/invalid values.
    /// </summary>
    public static string? NormalizeDomain(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var s = raw.Trim().ToLowerInvariant();
        if (s.StartsWith("https://", StringComparison.Ordinal))
            s = s[8..];
        else if (s.StartsWith("http://", StringComparison.Ordinal))
            s = s[7..];

        var slash = s.IndexOf('/');
        if (slash >= 0) s = s[..slash];
        var q = s.IndexOf('?');
        if (q >= 0) s = s[..q];
        var hash = s.IndexOf('#');
        if (hash >= 0) s = s[..hash];

        s = s.Trim().TrimEnd('.');
        if (s.EndsWith(":443", StringComparison.Ordinal))
            s = s[..^4];
        else if (s.EndsWith(":80", StringComparison.Ordinal))
            s = s[..^3];

        s = s.Trim();
        if (string.IsNullOrWhiteSpace(s) || s.Contains(' ') || s.Length > 255)
            return null;

        return s;
    }

    public static string GetRequestHost(HttpContext httpContext)
    {
        var forwarded = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            return NormalizeDomain(first) ?? string.Empty;
        }

        return NormalizeDomain(httpContext.Request.Host.Value) ?? string.Empty;
    }

    public static int? ResolveCommercialOwnerId(User user)
    {
        if (user.Role == "Admin") return null;
        if (user.Role == "Commercial") return user.Id;
        if (user.InsertByUserId > 0) return user.InsertByUserId;
        return null;
    }

    /// <summary>
    /// Whether <paramref name="user"/> may authenticate / call APIs on <paramref name="requestHost"/>.
    /// Admin is always allowed. Commercials with a Domain may only use that host;
    /// hosts claimed by a Commercial reject other tenants.
    /// </summary>
    public static async Task<bool> IsHostAllowedForUserAsync(
        DbConfig db,
        User user,
        string? requestHost,
        CancellationToken cancellationToken = default)
    {
        if (user.Role == "Admin") return true;

        var host = NormalizeDomain(requestHost) ?? string.Empty;
        var ownerId = ResolveCommercialOwnerId(user);
        if (ownerId == null) return true;

        User? owner;
        if (user.Role == "Commercial" && user.Id == ownerId.Value)
            owner = user;
        else
        {
            owner = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Id == ownerId.Value && !u.IsDeleted,
                    cancellationToken);
        }

        if (owner == null || owner.Role != "Commercial")
            return true;

        var ownerDomain = NormalizeDomain(owner.Domain);
        if (!string.IsNullOrEmpty(host))
        {
            var claimant = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => !u.IsDeleted
                         && u.Role == "Commercial"
                         && u.Domain == host,
                    cancellationToken);

            if (claimant != null)
                return claimant.Id == owner.Id;
        }

        // Unclaimed host (or empty): only tenants without a bound domain.
        return string.IsNullOrEmpty(ownerDomain);
    }
}

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Services;

namespace POS.Middleware;

/// <summary>
/// After JWT auth: reject Commercial/staff tokens when the request host
/// does not match their assigned Domain (if any).
/// </summary>
public class CommercialDomainMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly PathString[] ExemptPrefixes =
    [
        "/Auth/Login",
        "/Auth/LoginByCode",
        "/Auth/RegisterUser",
        "/License",
        "/PublicMenu",
        "/swagger",
        "/hubs",
        "/orderHub"
    ];

    public CommercialDomainMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DbConfig db)
    {
        var path = context.Request.Path;
        if (ExemptPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var idRaw = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idRaw, out var userId) || userId <= 0)
        {
            await _next(context);
            return;
        }

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
        {
            await _next(context);
            return;
        }

        var host = TenantHostHelper.GetRequestHost(context);
        var allowed = await TenantHostHelper.IsHostAllowedForUserAsync(
            db,
            user,
            host,
            context.RequestAborted);

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                errorStatus = true,
                message = "domainAccessDenied"
            });
            return;
        }

        await _next(context);
    }
}

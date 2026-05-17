using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using System.Security.Claims;

namespace RestaurantPOS.Authorization;

/// <summary>
/// Allows users with traditional roles OR Manager with any of the given sections in AllowedSectionsJson.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthorizeSectionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string[] SectionKeys { get; }
    public string Roles { get; set; } = "";

    public AuthorizeSectionAttribute(params string[] sectionKeys)
    {
        SectionKeys = sectionKeys?.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray() ?? Array.Empty<string>();
        if (SectionKeys.Length == 0)
            throw new ArgumentException("At least one section key is required.", nameof(sectionKeys));
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<DbConfig>();
        var dbUser = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (dbUser == null)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!SectionPermissionService.UserCanAccessAnySection(dbUser, SectionKeys, Roles))
        {
            context.Result = new ForbidResult();
        }
    }
}

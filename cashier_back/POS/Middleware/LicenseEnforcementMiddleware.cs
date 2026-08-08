using POS.Services;

namespace POS.Middleware;

public class LicenseEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] ProtectedApiPrefixes =
    [
        "/Admin", "/Auth", "/Inventory", "/Printers", "/TagPrinters", "/Customers",
        "/Employees", "/Expenses", "/ExpenseCategories", "/CreditAccounts",
        "/PaymentDevices", "/CardPayments", "/PayrollRuns", "/Payroll",
        "/SalaryAdjustments", "/EmployeeAdvances", "/AuditLog", "/Users",
        "/Items", "/Orders", "/Reports"
    ];

    private static readonly PathString[] ExemptPrefixes =
    [
        "/Auth/Login",
        "/Auth/LoginByCode",
        "/License"
    ];

    public LicenseEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
    {
        if (!licenseService.EnforcementEnabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        if (!path.HasValue)
        {
            await _next(context);
            return;
        }

        if (ExemptPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var isProtectedApi = ProtectedApiPrefixes.Any(p =>
            path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        if (!isProtectedApi)
        {
            await _next(context);
            return;
        }

        var ok = await licenseService.EnsureLicensedAsync(context.RequestAborted);
        if (!ok)
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "licenseExpired",
                status = licenseService.GetStatus()
            });
            return;
        }

        await _next(context);
    }
}

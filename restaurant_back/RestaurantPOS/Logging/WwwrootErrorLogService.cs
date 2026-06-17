using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;

namespace RestaurantPOS.Logging;

public class WwwrootErrorLogService : IWwwrootErrorLogService
{
    private readonly ErrorLogSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<WwwrootErrorLogService> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private TimeZoneInfo? _timeZone;

    public WwwrootErrorLogService(
        IOptions<ErrorLogSettings> settings,
        IWebHostEnvironment environment,
        ILogger<WwwrootErrorLogService> logger,
        IConfiguration configuration)
    {
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
        _timeZone = ResolveTimeZone(_settings.TimeZoneId ?? configuration["BusinessSettings:TimeZoneId"]);
    }

    public async Task LogHttpErrorAsync(
        HttpContext context,
        int statusCode,
        Exception? exception = null,
        string? requestBody = null,
        string? responseBody = null)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        if (!_settings.LogStatusCodes.Contains(statusCode))
        {
            return;
        }

        try
        {
            var logDirectory = Path.Combine(_environment.ContentRootPath, _settings.LogDirectory);
            Directory.CreateDirectory(logDirectory);

            var timestamp = GetLocalTimestamp();
            var fileName = $"errors-{timestamp:yyyy-MM-dd}.log";
            var filePath = Path.Combine(logDirectory, fileName);

            var entry = BuildLogEntry(context, statusCode, exception, requestBody, responseBody, timestamp);

            await _writeLock.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(filePath, entry, Encoding.UTF8);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write error log to wwwroot");
        }
    }

    private string BuildLogEntry(
        HttpContext context,
        int statusCode,
        Exception? exception,
        string? requestBody,
        string? responseBody,
        DateTimeOffset timestamp)
    {
        var request = context.Request;
        var sb = new StringBuilder();

        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"Timestamp   : {timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}");
        sb.AppendLine($"StatusCode  : {statusCode}");
        sb.AppendLine($"Method      : {request.Method}");
        sb.AppendLine($"Path        : {request.Path}{request.QueryString}");
        sb.AppendLine($"Scheme      : {request.Scheme}");
        sb.AppendLine($"Host        : {request.Host}");
        sb.AppendLine($"RemoteIp    : {context.Connection.RemoteIpAddress}");
        sb.AppendLine($"UserAgent   : {request.Headers.UserAgent}");
        sb.AppendLine($"TraceId     : {context.TraceIdentifier}");

        var userInfo = DescribeUser(context.User);
        if (!string.IsNullOrEmpty(userInfo))
        {
            sb.AppendLine($"User        : {userInfo}");
        }

        if (!string.IsNullOrWhiteSpace(requestBody))
        {
            sb.AppendLine("RequestBody :");
            sb.AppendLine(Truncate(requestBody, _settings.MaxRequestBodyLength));
        }

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            sb.AppendLine("ResponseBody:");
            sb.AppendLine(Truncate(responseBody, _settings.MaxResponseBodyLength));
        }

        if (exception != null)
        {
            sb.AppendLine("Exception   :");
            sb.AppendLine(exception.GetType().FullName);
            sb.AppendLine($"Message     : {exception.Message}");

            if (exception.InnerException != null)
            {
                sb.AppendLine($"InnerType   : {exception.InnerException.GetType().FullName}");
                sb.AppendLine($"InnerMessage: {exception.InnerException.Message}");
            }

            sb.AppendLine("StackTrace  :");
            sb.AppendLine(exception.StackTrace ?? "(no stack trace)");
        }

        sb.AppendLine(new string('=', 80));
        sb.AppendLine();

        return sb.ToString();
    }

    private static string? DescribeUser(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("unique_name")?.Value
            ?? user.Identity?.Name;
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(id))
        {
            parts.Add($"Id={id}");
        }
        if (!string.IsNullOrWhiteSpace(name))
        {
            parts.Add($"Name={name}");
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            parts.Add($"Role={role}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "Authenticated";
    }

    private DateTimeOffset GetLocalTimestamp()
    {
        var utc = DateTimeOffset.UtcNow;
        if (_timeZone == null)
        {
            return utc.ToLocalTime();
        }

        var local = TimeZoneInfo.ConvertTime(utc.UtcDateTime, _timeZone);
        return new DateTimeOffset(local, _timeZone.GetUtcOffset(local));
    }

    private static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch
        {
            return null;
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + $"... [truncated, total {value.Length} chars]";
    }
}

using System.Text;
using Microsoft.Extensions.Options;
using RestaurantPOS.Logging;

namespace RestaurantPOS.Middleware;

public class HttpErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ErrorLogSettings _settings;
    private readonly HashSet<int> _statusCodesToLog;

    public HttpErrorLoggingMiddleware(RequestDelegate next, IOptions<ErrorLogSettings> settings)
    {
        _next = next;
        _settings = settings.Value;
        _statusCodesToLog = _settings.LogStatusCodes?.ToHashSet() ?? new HashSet<int> { 404, 500 };
    }

    public async Task InvokeAsync(HttpContext context, IWwwrootErrorLogService errorLogService)
    {
        if (!_settings.Enabled || !ShouldInspectRequest(context.Request))
        {
            await _next(context);
            return;
        }

        string? requestBody = null;
        if (HasRequestBody(context.Request))
        {
            requestBody = await ReadRequestBodyAsync(context.Request);
        }

        var originalResponseBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        var exceptionLogged = false;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exceptionLogged = true;
            await errorLogService.LogHttpErrorAsync(
                context,
                500,
                ex,
                requestBody,
                responseBody: null);
            throw;
        }
        finally
        {
            responseBuffer.Seek(0, SeekOrigin.Begin);
            var responseBody = await ReadStreamAsStringAsync(responseBuffer, _settings.MaxResponseBodyLength);
            responseBuffer.Seek(0, SeekOrigin.Begin);
            await responseBuffer.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            if (!exceptionLogged && _statusCodesToLog.Contains(context.Response.StatusCode))
            {
                await errorLogService.LogHttpErrorAsync(
                    context,
                    context.Response.StatusCode,
                    exception: null,
                    requestBody,
                    responseBody);
            }
        }
    }

    private static bool ShouldInspectRequest(HttpRequest request)
    {
        var path = request.Path.Value ?? string.Empty;

        if (path.StartsWith("/static", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Images", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/orderHub", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(extension)
            && !string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool HasRequestBody(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        return request.ContentLength.GetValueOrDefault() > 0
            || request.Headers.ContainsKey("Content-Length");
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

        if (request.Body.CanSeek)
        {
            request.Body.Seek(0, SeekOrigin.Begin);
        }

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        if (request.Body.CanSeek)
        {
            request.Body.Seek(0, SeekOrigin.Begin);
        }

        return body;
    }

    private static async Task<string> ReadStreamAsStringAsync(Stream stream, int maxLength)
    {
        if (stream.Length == 0)
        {
            return string.Empty;
        }

        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var text = await reader.ReadToEndAsync();

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength] + $"... [truncated, total {text.Length} chars]";
    }
}

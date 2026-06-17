namespace RestaurantPOS.Logging;

public interface IWwwrootErrorLogService
{
    Task LogHttpErrorAsync(
        HttpContext context,
        int statusCode,
        Exception? exception = null,
        string? requestBody = null,
        string? responseBody = null);
}

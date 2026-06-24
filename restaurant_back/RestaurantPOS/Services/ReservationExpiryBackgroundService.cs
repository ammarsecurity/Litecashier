using Microsoft.Extensions.Options;
using RestaurantPOS.Configuration;

namespace RestaurantPOS.Services;

public class ReservationExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ReservationSettingsOptions> _settings;
    private readonly ILogger<ReservationExpiryBackgroundService> _logger;

    public ReservationExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ReservationSettingsOptions> settings,
        ILogger<ReservationExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Max(1, _settings.Value.CheckIntervalMinutes);
        var delay = TimeSpan.FromMinutes(intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_settings.Value.AutoCancelWhenDue)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var expiry = scope.ServiceProvider.GetRequiredService<IReservationExpiryService>();
                    var expired = await expiry.ExpireAllOverdueAsync(stoppingToken);
                    if (expired > 0)
                    {
                        _logger.LogInformation("Auto-cancelled {Count} overdue reservation(s)", expired);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reservation expiry background cycle failed");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;

namespace RestaurantPOS.Services.Sync;

public class DatabaseSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseSyncBackgroundService> _logger;

    public DatabaseSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAutoSyncCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto sync background cycle failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task RunAutoSyncCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbConfig>();
        var sync = scope.ServiceProvider.GetRequiredService<IDatabaseSyncService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!configuration.GetValue("SyncSettings:Enabled", true))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("SyncDatabase")))
        {
            return;
        }

        var tenants = await db.TenantSyncSettings.AsNoTracking()
            .Where(s => s.AutoSyncEnabled && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var tenant in tenants)
        {
            if (sync.IsSyncInProgress)
            {
                break;
            }

            var lastRun = await db.SyncRuns.AsNoTracking()
                .Where(r => r.CommercialUserId == tenant.CommercialUserId && !r.IsDeleted)
                .OrderByDescending(r => r.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var interval = TimeSpan.FromMinutes(Math.Max(5, tenant.IntervalMinutes));
            if (lastRun != null && lastRun.StartedAt > DateTime.UtcNow.Subtract(interval))
            {
                continue;
            }

            _logger.LogInformation("Starting auto sync for commercial user {CommercialUserId}", tenant.CommercialUserId);
            await sync.PushAsync(tenant.CommercialUserId, SyncTriggers.Auto, cancellationToken);
        }
    }
}

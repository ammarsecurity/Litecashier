using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Restaurant;
using RestaurantPOS.Models.Sync;

namespace RestaurantPOS.Services;

public interface ICommercialTenantDeleteService
{
    Task SoftDeleteCommercialTenantAsync(int commercialUserId, CancellationToken cancellationToken = default);

    Task PurgeAllExceptPrimaryAdminAsync(CancellationToken cancellationToken = default);
}

public class CommercialTenantDeleteService : ICommercialTenantDeleteService
{
    private const int PrimaryAdminUserId = 1;

    private readonly DbConfig _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CommercialTenantDeleteService> _logger;

    public CommercialTenantDeleteService(
        DbConfig db,
        IWebHostEnvironment env,
        ILogger<CommercialTenantDeleteService> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task PurgeAllExceptPrimaryAdminAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var nonAdminUserIds = await _db.Users
            .Where(u => u.Id != PrimaryAdminUserId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await PurgeAllSystemDataCoreAsync(now, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        await PurgeUploadedImagesAsync(cancellationToken);

        _logger.LogWarning(
            "System data purge completed. Primary admin preserved (user {AdminId}). Removed {UserCount} other user accounts.",
            PrimaryAdminUserId,
            nonAdminUserIds.Count);
    }

    private async Task PurgeAllSystemDataCoreAsync(DateTime now, CancellationToken cancellationToken)
    {
        await SafeNullTableCurrentOrdersAsync(cancellationToken);

        await SafeMarkDeletedAsync(
            _db.CustomerOrderItems.Where(x => !x.IsDeleted),
            now, nameof(CustomerOrderItem), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.OrderTables.Where(x => !x.IsDeleted),
            now, nameof(OrderTable), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.ReturnedOrderItems.Where(x => !x.IsDeleted),
            now, nameof(ReturnedOrderItem), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.CardPaymentTransactions.Where(x => !x.IsDeleted),
            now, nameof(CardPaymentTransaction), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.CustomerOrders.Where(x => !x.IsDeleted),
            now, nameof(CustomerOrder), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.TagPrinters.Where(x => !x.IsDeleted),
            now, nameof(TagPrinter), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.TableLayoutPlacements.Where(x => !x.IsDeleted),
            now, nameof(TableLayoutPlacement), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Tags.Where(x => !x.IsDeleted),
            now, nameof(Tag), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Items.Where(x => !x.IsDeleted),
            now, nameof(Item), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Tables.Where(x => !x.IsDeleted),
            now, nameof(Table), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.RestaurantLayoutSettings.Where(x => !x.IsDeleted),
            now, nameof(RestaurantLayoutSettings), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Reservations.Where(x => !x.IsDeleted),
            now, nameof(Reservation), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Printers.Where(x => !x.IsDeleted),
            now, nameof(Printer), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.DeliveryDrivers.Where(x => !x.IsDeleted),
            now, nameof(DeliveryDriver), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Expenses.Where(x => !x.IsDeleted),
            now, nameof(Expense), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.ExpenseCategories.Where(x => !x.IsDeleted),
            now, nameof(ExpenseCategory), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Employees.Where(x => !x.IsDeleted),
            now, nameof(Employee), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.StockMovements.Where(x => !x.IsDeleted),
            now, nameof(StockMovement), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Suppliers.Where(x => !x.IsDeleted),
            now, nameof(Supplier), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Customers.Where(x => !x.IsDeleted),
            now, nameof(Customer), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.PaymentDevices.Where(x => !x.IsDeleted),
            now, nameof(PaymentDevice), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.AuditLogs.Where(x => !x.IsDeleted),
            now, nameof(AuditLog), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.SyncRuns.Where(x => !x.IsDeleted),
            now, nameof(SyncRun), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.SyncWatermarks.Where(x => !x.IsDeleted),
            now, nameof(SyncWatermark), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.SyncFileWatermarks.Where(x => !x.IsDeleted),
            now, nameof(SyncFileWatermark), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.TenantSyncSettings.Where(x => !x.IsDeleted),
            now, nameof(TenantSyncSettings), cancellationToken);

        await SafeMarkDeletedAsync(
            _db.Users.Where(x => !x.IsDeleted && x.Id != PrimaryAdminUserId),
            now, nameof(User), cancellationToken);

        var nonAdminUserIds = await _db.Users
            .Where(u => u.Id != PrimaryAdminUserId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        await ClearTenantUserLoginCodesAsync(nonAdminUserIds, cancellationToken);
    }

    private async Task SafeNullTableCurrentOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.Tables
                .Where(t => t.CurrentOrderId != null && !t.IsDeleted)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(t => t.CurrentOrderId, (int?)null),
                    cancellationToken);
        }
        catch (Exception ex) when (IsExecuteUpdateNotSupported(ex) || IsMissingTableOrUnknown(ex))
        {
            var tables = await _db.Tables
                .Where(t => t.CurrentOrderId != null && !t.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var table in tables)
            {
                table.CurrentOrderId = null;
            }

            if (tables.Count > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private Task PurgeUploadedImagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var imagesPath = Path.Combine(_env.WebRootPath ?? string.Empty, "Images");
        if (!Directory.Exists(imagesPath))
        {
            return Task.CompletedTask;
        }

        foreach (var file in Directory.EnumerateFiles(imagesPath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete image file {FilePath}", file);
            }
        }

        return Task.CompletedTask;
    }

    public async Task SoftDeleteCommercialTenantAsync(int commercialUserId, CancellationToken cancellationToken = default)
    {
        var tenantUserIds = await _db.Users
            .Where(u => u.Id == commercialUserId || u.InsertByUserId == commercialUserId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (tenantUserIds.Count == 0)
        {
            return;
        }

        var orderIds = await SafeQueryIdsAsync(
            () => _db.CustomerOrders
                .Where(o => !o.IsDeleted && tenantUserIds.Contains(o.InsertByUserId))
                .Select(o => o.Id)
                .ToListAsync(cancellationToken),
            "CustomerOrders",
            cancellationToken);

        var tableIds = await SafeQueryIdsAsync(
            () => _db.Tables
                .Where(t => !t.IsDeleted && tenantUserIds.Contains(t.InsertByUserId))
                .Select(t => t.Id)
                .ToListAsync(cancellationToken),
            "Tables",
            cancellationToken);

        var now = DateTime.UtcNow;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await SoftDeleteCommercialTenantCoreAsync(
                    commercialUserId,
                    tenantUserIds,
                    orderIds,
                    tableIds,
                    now,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        _logger.LogInformation(
            "Soft-deleted commercial tenant {CommercialUserId} with {UserCount} users, {OrderCount} orders",
            commercialUserId,
            tenantUserIds.Count,
            orderIds.Count);
    }

    private async Task SoftDeleteCommercialTenantCoreAsync(
        int commercialUserId,
        List<int> tenantUserIds,
        List<int> orderIds,
        List<int> tableIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        try
        {
            await SafeMarkDeletedAsync(
                _db.CustomerOrderItems.Where(x => !x.IsDeleted && (tenantUserIds.Contains(x.InsertByUserId) || orderIds.Contains(x.CustomerOrderId))),
                now, nameof(CustomerOrderItem), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.OrderTables.Where(x => !x.IsDeleted && (tenantUserIds.Contains(x.InsertByUserId) || orderIds.Contains(x.OrderId))),
                now, nameof(OrderTable), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.ReturnedOrderItems.Where(x => !x.IsDeleted && (tenantUserIds.Contains(x.InsertByUserId) || orderIds.Contains(x.CustomerOrderId))),
                now, nameof(ReturnedOrderItem), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.CardPaymentTransactions.Where(x => !x.IsDeleted && (x.InsertByUserId == commercialUserId || tenantUserIds.Contains(x.InsertByUserId))),
                now, nameof(CardPaymentTransaction), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.CustomerOrders.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(CustomerOrder), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.TagPrinters.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(TagPrinter), cancellationToken);

            if (tableIds.Count > 0)
            {
                await SafeMarkDeletedAsync(
                    _db.TableLayoutPlacements.Where(x => !x.IsDeleted && tableIds.Contains(x.TableId)),
                    now, nameof(TableLayoutPlacement), cancellationToken);
            }

            await SafeMarkDeletedAsync(
                _db.Tags.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Tag), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Items.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Item), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Tables.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Table), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.RestaurantLayoutSettings.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(RestaurantLayoutSettings), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Reservations.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Reservation), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Printers.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Printer), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.DeliveryDrivers.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(DeliveryDriver), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Expenses.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Expense), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.ExpenseCategories.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(ExpenseCategory), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Employees.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Employee), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.StockMovements.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(StockMovement), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Suppliers.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Supplier), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Customers.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(Customer), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.PaymentDevices.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.InsertByUserId)),
                now, nameof(PaymentDevice), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.AuditLogs.Where(x => !x.IsDeleted && x.CommercialUserId == commercialUserId),
                now, nameof(AuditLog), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.SyncRuns.Where(x => !x.IsDeleted && x.CommercialUserId == commercialUserId),
                now, nameof(SyncRun), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.SyncWatermarks.Where(x => !x.IsDeleted && x.CommercialUserId == commercialUserId),
                now, nameof(SyncWatermark), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.SyncFileWatermarks.Where(x => !x.IsDeleted && x.CommercialUserId == commercialUserId),
                now, nameof(SyncFileWatermark), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.TenantSyncSettings.Where(x => !x.IsDeleted && x.CommercialUserId == commercialUserId),
                now, nameof(TenantSyncSettings), cancellationToken);

            await SafeMarkDeletedAsync(
                _db.Users.Where(x => !x.IsDeleted && tenantUserIds.Contains(x.Id)),
                now, nameof(User), cancellationToken);

            await ClearTenantUserLoginCodesAsync(tenantUserIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Commercial tenant soft-delete failed for {CommercialUserId}", commercialUserId);
            throw;
        }
    }

    private async Task ClearTenantUserLoginCodesAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        try
        {
            await _db.Users
                .Where(u => userIds.Contains(u.Id) && u.LoginCode != null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(u => u.LoginCode, (string?)null),
                    cancellationToken);
        }
        catch (Exception ex) when (IsExecuteUpdateNotSupported(ex) || IsMissingTableOrUnknown(ex))
        {
            var users = await _db.Users
                .Where(u => userIds.Contains(u.Id) && u.LoginCode != null)
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                user.LoginCode = null;
            }

            if (users.Count > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task<List<int>> SafeQueryIdsAsync(
        Func<Task<List<int>>> query,
        string entityName,
        CancellationToken cancellationToken)
    {
        try
        {
            return await query();
        }
        catch (Exception ex) when (IsMissingTableOrUnknown(ex))
        {
            _logger.LogWarning(ex, "Skipping id query for {Entity} (table may not exist)", entityName);
            return [];
        }
    }

    private async Task SafeMarkDeletedAsync<TEntity>(
        IQueryable<TEntity> query,
        DateTime now,
        string entityName,
        CancellationToken cancellationToken)
        where TEntity : BaseEntity
    {
        try
        {
            var count = await query.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.IsDeleted, true)
                    .SetProperty(e => e.UpdateDate, now),
                cancellationToken);

            if (count > 0)
            {
                _logger.LogDebug("Soft-deleted {Count} {Entity} rows", count, entityName);
            }
        }
        catch (Exception ex) when (IsMissingTableOrUnknown(ex))
        {
            _logger.LogWarning(ex, "ExecuteUpdate skipped for {Entity}, trying load/update fallback", entityName);
            await FallbackMarkDeletedAsync(query, now, entityName, cancellationToken);
        }
        catch (Exception ex) when (IsExecuteUpdateNotSupported(ex))
        {
            _logger.LogWarning(ex, "ExecuteUpdate not supported for {Entity}, using load/update fallback", entityName);
            await FallbackMarkDeletedAsync(query, now, entityName, cancellationToken);
        }
    }

    private async Task FallbackMarkDeletedAsync<TEntity>(
        IQueryable<TEntity> query,
        DateTime now,
        string entityName,
        CancellationToken cancellationToken)
        where TEntity : BaseEntity
    {
        try
        {
            var rows = await query.ToListAsync(cancellationToken);
            if (rows.Count == 0)
            {
                return;
            }

            foreach (var row in rows)
            {
                row.IsDeleted = true;
                row.UpdateDate = now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Soft-deleted {Count} {Entity} rows via fallback", rows.Count, entityName);
        }
        catch (Exception ex) when (IsMissingTableOrUnknown(ex))
        {
            _logger.LogWarning(ex, "Fallback soft-delete skipped for {Entity} (table may not exist)", entityName);
        }
    }

    private static bool IsMissingTableOrUnknown(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is MySqlException mysql)
            {
                // 1146 = table doesn't exist, 1054 = unknown column
                if (mysql.Number is 1146 or 1054)
                {
                    return true;
                }
            }

            var message = current.Message;
            if (message.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Unknown column", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Unknown table", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExecuteUpdateNotSupported(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("ExecuteUpdate", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("ExecuteUpdateAsync", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

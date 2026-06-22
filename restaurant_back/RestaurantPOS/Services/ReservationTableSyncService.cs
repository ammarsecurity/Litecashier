using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using RestaurantPOS.Hubs;
using RestaurantPOS.Models.Restaurant;

namespace RestaurantPOS.Services;

public interface IReservationTableSyncService
{
    Task SyncTableForReservationAsync(
        Reservation reservation,
        string? previousStatus,
        int? previousTableId,
        int commercialUserId,
        int actingUserId);

    Task ReleaseTableAsync(int tableId, int commercialUserId, int actingUserId, string reason);

    Task ReconcileAllTablesAsync(int commercialUserId, int actingUserId);
}

public class ReservationTableSyncService : IReservationTableSyncService
{
    private static readonly HashSet<string> ActiveReservationStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Confirmed", "Seated",
    };

    private readonly DbConfig _db;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<ReservationTableSyncService> _logger;

    public ReservationTableSyncService(
        DbConfig db,
        IHubContext<OrderHub> hubContext,
        ILogger<ReservationTableSyncService> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SyncTableForReservationAsync(
        Reservation reservation,
        string? previousStatus,
        int? previousTableId,
        int commercialUserId,
        int actingUserId)
    {
        if (previousTableId.HasValue && previousTableId != reservation.TableId)
        {
            await ReleaseTableAsync(previousTableId.Value, commercialUserId, actingUserId, "reservation_table_changed");
        }

        if (!reservation.TableId.HasValue)
        {
            return;
        }

        var status = reservation.Status ?? "Pending";
        var table = await _db.Tables
            .FirstOrDefaultAsync(t =>
                t.Id == reservation.TableId.Value
                && !t.IsDeleted
                && t.InsertByUserId == commercialUserId);

        if (table == null)
        {
            return;
        }

        if (string.Equals(status, "Seated", StringComparison.OrdinalIgnoreCase))
        {
            var hasActiveOrder = table.CurrentOrderId.HasValue
                || await TableHasActiveDineInOrderAsync(table.Id, commercialUserId);

            if (hasActiveOrder)
            {
                await SetTableStatusAsync(table, "Occupied", commercialUserId, actingUserId, "reservation_seated");
            }
            else if (!string.Equals(table.Status, "OutOfService", StringComparison.OrdinalIgnoreCase))
            {
                await SetTableStatusAsync(table, "Reserved", commercialUserId, actingUserId, "reservation_seated_no_order");
            }

            return;
        }

        if (string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(table.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
            {
                var reason = string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase)
                    ? "reservation_confirmed"
                    : "reservation_pending";
                await SetTableStatusAsync(table, "Reserved", commercialUserId, actingUserId, reason);
            }

            return;
        }

        if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            await TryReleaseTableAsync(table, commercialUserId, actingUserId, "reservation_closed");
        }
    }

    public async Task ReleaseTableAsync(int tableId, int commercialUserId, int actingUserId, string reason)
    {
        var table = await _db.Tables
            .FirstOrDefaultAsync(t =>
                t.Id == tableId
                && !t.IsDeleted
                && t.InsertByUserId == commercialUserId);

        if (table == null)
        {
            return;
        }

        await TryReleaseTableAsync(table, commercialUserId, actingUserId, reason);
    }

    public async Task ReconcileAllTablesAsync(int commercialUserId, int actingUserId)
    {
        var tables = await _db.Tables
            .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId)
            .ToListAsync();

        var activeReservations = await _db.Reservations
            .Where(r => !r.IsDeleted
                && r.InsertByUserId == commercialUserId
                && r.TableId != null
                && (r.Status == "Pending" || r.Status == "Confirmed" || r.Status == "Seated"))
            .ToListAsync();

        foreach (var table in tables)
        {
            var forTable = activeReservations
                .Where(r => r.TableId == table.Id)
                .OrderBy(r => r.ReservationDateTime)
                .ToList();

            if (forTable.Any(r => string.Equals(r.Status, "Seated", StringComparison.OrdinalIgnoreCase)))
            {
                var hasActiveOrder = table.CurrentOrderId.HasValue
                    || await TableHasActiveDineInOrderAsync(table.Id, commercialUserId);

                if (!string.Equals(table.Status, "OutOfService", StringComparison.OrdinalIgnoreCase))
                {
                    var targetStatus = hasActiveOrder ? "Occupied" : "Reserved";
                    var reason = hasActiveOrder ? "reconcile_seated" : "reconcile_seated_no_order";
                    await SetTableStatusAsync(table, targetStatus, commercialUserId, actingUserId, reason);
                }

                continue;
            }

            if (forTable.Any(r =>
                    string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.Equals(table.Status, "Occupied", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(table.Status, "OutOfService", StringComparison.OrdinalIgnoreCase))
                {
                    await SetTableStatusAsync(table, "Reserved", commercialUserId, actingUserId, "reconcile_reserved");
                }

                continue;
            }

            await TryReleaseTableAsync(table, commercialUserId, actingUserId, "reconcile_release");
        }

        foreach (var table in tables)
        {
            if (!string.Equals(table.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (table.CurrentOrderId.HasValue || await TableHasActiveDineInOrderAsync(table.Id, commercialUserId))
            {
                continue;
            }

            var forTable = activeReservations
                .Where(r => r.TableId == table.Id)
                .ToList();

            if (forTable.Any(r =>
                    string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r.Status, "Seated", StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.Equals(table.Status, "Reserved", StringComparison.OrdinalIgnoreCase))
                {
                    await SetTableStatusAsync(table, "Reserved", commercialUserId, actingUserId, "reconcile_stale_occupied");
                }

                continue;
            }

            await SetTableStatusAsync(table, "Available", commercialUserId, actingUserId, "reconcile_stale_occupied_release");
        }
    }

    private async Task<bool> TableHasActiveDineInOrderAsync(int tableId, int commercialUserId)
    {
        var table = await _db.Tables
            .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);

        if (table?.CurrentOrderId is int currentOrderId)
        {
            var fromCurrent = await _db.CustomerOrders.AnyAsync(o =>
                o.Id == currentOrderId
                && !o.IsDeleted
                && o.OrderType == "DineIn"
                && o.PaymentStatus != "Paid");

            if (fromCurrent)
            {
                return true;
            }
        }

        var orderIdsFromLinks = await _db.OrderTables
            .Where(ot => ot.TableId == tableId && !ot.IsDeleted)
            .Select(ot => ot.OrderId)
            .Distinct()
            .ToListAsync();

        if (orderIdsFromLinks.Count > 0)
        {
            var fromLinks = await _db.CustomerOrders.AnyAsync(o =>
                orderIdsFromLinks.Contains(o.Id)
                && !o.IsDeleted
                && o.OrderType == "DineIn"
                && o.PaymentStatus != "Paid");

            if (fromLinks)
            {
                return true;
            }
        }

        return await _db.CustomerOrders.AnyAsync(o =>
            !o.IsDeleted
            && o.OrderType == "DineIn"
            && o.PaymentStatus != "Paid"
            && o.TableId == tableId
            && o.InsertByUserId == commercialUserId);
    }

    private async Task TryReleaseTableAsync(Table table, int commercialUserId, int actingUserId, string reason)
    {
        if (table.CurrentOrderId.HasValue || await TableHasActiveDineInOrderAsync(table.Id, commercialUserId))
        {
            return;
        }

        var hasOtherActive = await _db.Reservations.AnyAsync(r =>
            !r.IsDeleted
            && r.InsertByUserId == commercialUserId
            && r.TableId == table.Id
            && (r.Status == "Pending" || r.Status == "Confirmed" || r.Status == "Seated"));

        if (hasOtherActive)
        {
            return;
        }

        await SetTableStatusAsync(table, "Available", commercialUserId, actingUserId, reason);
    }

    private async Task SetTableStatusAsync(
        Table table,
        string newStatus,
        int commercialUserId,
        int actingUserId,
        string reason)
    {
        if (string.Equals(table.Status, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var oldStatus = table.Status;
        table.Status = newStatus;

        if (string.Equals(newStatus, "Available", StringComparison.OrdinalIgnoreCase))
        {
            table.CurrentOrderId = null;
        }

        _db.Tables.Update(table);
        await _db.SaveChangesAsync();

        await _db.LogAuditAsync(
            "Update",
            "Table",
            table.Id,
            $"طاولة {table.TableNumber}",
            actingUserId,
            commercialUserId,
            new { Status = oldStatus },
            new { Status = newStatus, Reason = reason },
            $"مزامنة حجز: {table.TableNumber} {oldStatus} → {newStatus}");

        try
        {
            await _hubContext.Clients.All.SendAsync("TableUpdated", new
            {
                TableId = table.Id,
                Status = table.Status,
                TableNumber = table.TableNumber,
                Zone = table.Zone,
                CurrentOrderId = table.CurrentOrderId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TableUpdated SignalR failed for table {TableId}", table.Id);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Restaurant;

namespace RestaurantPOS.Services;

public interface ITableOrderSyncService
{
    Task SyncTablesWithUnpaidOrdersAsync(int commercialUserId, IList<Table> tables, CancellationToken cancellationToken = default);
}

public class TableOrderSyncService : ITableOrderSyncService
{
    private readonly DbConfig _db;
    private readonly ILogger<TableOrderSyncService> _logger;

    public TableOrderSyncService(DbConfig db, ILogger<TableOrderSyncService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SyncTablesWithUnpaidOrdersAsync(
        int commercialUserId,
        IList<Table> tables,
        CancellationToken cancellationToken = default)
    {
        if (tables.Count == 0)
        {
            return;
        }

        var tableIds = tables.Select(t => t.Id).ToHashSet();
        var unpaidOrderByTable = await BuildUnpaidOrderMapAsync(commercialUserId, tableIds, cancellationToken);
        if (unpaidOrderByTable.Count == 0)
        {
            return;
        }

        var changed = false;
        foreach (var table in tables)
        {
            if (!unpaidOrderByTable.TryGetValue(table.Id, out var orderId))
            {
                continue;
            }

            if (table.CurrentOrderId == orderId
                && string.Equals(table.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            table.CurrentOrderId = orderId;
            table.Status = "Occupied";
            table.UpdateDate = DateTime.UtcNow;
            _db.Tables.Update(table);
            changed = true;
        }

        if (changed)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Repaired {Count} table(s) with unpaid DineIn orders for commercial user {CommercialUserId}",
                unpaidOrderByTable.Count,
                commercialUserId);
        }
    }

    private async Task<Dictionary<int, int>> BuildUnpaidOrderMapAsync(
        int commercialUserId,
        HashSet<int> tableIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, (int OrderId, DateTime SortDate)>();

        var linked = await (
            from ot in _db.OrderTables.AsNoTracking()
            join o in _db.CustomerOrders.AsNoTracking() on ot.OrderId equals o.Id
            where !ot.IsDeleted
                  && !o.IsDeleted
                  && o.OrderType == "DineIn"
                  && o.PaymentStatus != "Paid"
                  && o.InsertByUserId == commercialUserId
                  && tableIds.Contains(ot.TableId)
            select new { ot.TableId, o.Id, o.InsertDate, o.UpdateDate }
        ).ToListAsync(cancellationToken);

        foreach (var row in linked)
        {
            Consider(map, row.TableId, row.Id, row.UpdateDate > row.InsertDate ? row.UpdateDate : row.InsertDate);
        }

        var legacy = await _db.CustomerOrders.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.OrderType == "DineIn"
                        && o.PaymentStatus != "Paid"
                        && o.InsertByUserId == commercialUserId
                        && o.TableId.HasValue
                        && tableIds.Contains(o.TableId.Value))
            .Select(o => new { TableId = o.TableId!.Value, o.Id, o.InsertDate, o.UpdateDate })
            .ToListAsync(cancellationToken);

        foreach (var row in legacy)
        {
            Consider(map, row.TableId, row.Id, row.UpdateDate > row.InsertDate ? row.UpdateDate : row.InsertDate);
        }

        return map.ToDictionary(x => x.Key, x => x.Value.OrderId);
    }

    private static void Consider(
        Dictionary<int, (int OrderId, DateTime SortDate)> map,
        int tableId,
        int orderId,
        DateTime sortDate)
    {
        if (!map.TryGetValue(tableId, out var existing) || sortDate >= existing.SortDate)
        {
            map[tableId] = (orderId, sortDate);
        }
    }
}

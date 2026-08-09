using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Models;
using POS.Models.Dtos;

namespace POS.Services;

public class WarehouseStockService : IWarehouseStockService
{
    private readonly DbConfig _db;

    public WarehouseStockService(DbConfig db)
    {
        _db = db;
    }

    public async Task<Warehouse> EnsureDefaultWarehouseAsync(int commercialUserId, CancellationToken ct = default)
    {
        var existing = await _db.Warehouses
            .FirstOrDefaultAsync(w =>
                !w.IsDeleted &&
                w.InsertByUserId == commercialUserId &&
                w.IsDefault, ct);

        if (existing != null)
            return existing;

        var any = await _db.Warehouses
            .Where(w => !w.IsDeleted && w.InsertByUserId == commercialUserId)
            .OrderBy(w => w.Id)
            .FirstOrDefaultAsync(ct);

        if (any != null)
        {
            any.IsDefault = true;
            any.IsActive = true;
            await _db.SaveChangesAsync(ct);
            return any;
        }

        var created = new Warehouse
        {
            Name = "المخزن الرئيسي",
            IsDefault = true,
            IsActive = true,
            InsertByUserId = commercialUserId
        };
        _db.Warehouses.Add(created);
        await _db.SaveChangesAsync(ct);
        return created;
    }

    public Task<Warehouse?> GetActiveWarehouseAsync(int commercialUserId, int warehouseId, CancellationToken ct = default)
    {
        return _db.Warehouses.FirstOrDefaultAsync(w =>
            !w.IsDeleted &&
            w.IsActive &&
            w.Id == warehouseId &&
            w.InsertByUserId == commercialUserId, ct);
    }

    public async Task<int> GetStockAsync(int itemId, int warehouseId, CancellationToken ct = default)
    {
        var row = await _db.ItemWarehouseStocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.ItemId == itemId && s.WarehouseId == warehouseId, ct);
        return row?.Quantity ?? 0;
    }

    public async Task<Dictionary<int, int>> GetStocksForItemsAsync(IEnumerable<int> itemIds, int warehouseId, CancellationToken ct = default)
    {
        var ids = itemIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, int>();

        var rows = await _db.ItemWarehouseStocks
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.WarehouseId == warehouseId && ids.Contains(s.ItemId))
            .Select(s => new { s.ItemId, s.Quantity })
            .ToListAsync(ct);

        var map = ids.ToDictionary(id => id, _ => 0);
        foreach (var row in rows)
            map[row.ItemId] = row.Quantity;
        return map;
    }

    public async Task<List<WarehouseStockDto>> GetItemStockBreakdownAsync(int itemId, int commercialUserId, CancellationToken ct = default)
    {
        await EnsureDefaultWarehouseAsync(commercialUserId, ct);

        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => !w.IsDeleted && w.InsertByUserId == commercialUserId)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .ToListAsync(ct);

        var stocks = await _db.ItemWarehouseStocks
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.ItemId == itemId)
            .ToDictionaryAsync(
                s => s.WarehouseId,
                s => new { s.Quantity, s.LowStockAlertQuantity },
                ct);

        return warehouses.Select(w =>
        {
            stocks.TryGetValue(w.Id, out var row);
            return new WarehouseStockDto
            {
                WarehouseId = w.Id,
                WarehouseName = w.Name,
                Quantity = row?.Quantity ?? 0,
                LowStockAlertQuantity = row?.LowStockAlertQuantity,
                IsDefault = w.IsDefault
            };
        }).ToList();
    }

    public async Task SetItemStocksAsync(
        int itemId,
        int commercialUserId,
        IReadOnlyList<WarehouseStockInputDto>? stocks,
        int? fallbackTotalQuantity,
        CancellationToken ct = default)
    {
        var defaultWh = await EnsureDefaultWarehouseAsync(commercialUserId, ct);
        var warehouseIds = await _db.Warehouses
            .Where(w => !w.IsDeleted && w.InsertByUserId == commercialUserId)
            .Select(w => w.Id)
            .ToListAsync(ct);

        var inputs = (stocks ?? Array.Empty<WarehouseStockInputDto>())
            .Where(s => warehouseIds.Contains(s.WarehouseId))
            .GroupBy(s => s.WarehouseId)
            .Select(g =>
            {
                var last = g.Last();
                return new WarehouseStockInputDto
                {
                    WarehouseId = g.Key,
                    Quantity = Math.Max(0, g.Sum(x => x.Quantity)),
                    LowStockAlertQuantity = last.LowStockAlertQuantity
                };
            })
            .ToList();

        if (inputs.Count == 0)
        {
            inputs.Add(new WarehouseStockInputDto
            {
                WarehouseId = defaultWh.Id,
                Quantity = Math.Max(0, fallbackTotalQuantity ?? 0),
                LowStockAlertQuantity = null
            });
        }

        var existing = await _db.ItemWarehouseStocks
            .Where(s => !s.IsDeleted && s.ItemId == itemId)
            .ToListAsync(ct);

        foreach (var input in inputs)
        {
            var row = existing.FirstOrDefault(s => s.WarehouseId == input.WarehouseId);
            if (row == null)
            {
                _db.ItemWarehouseStocks.Add(new ItemWarehouseStock
                {
                    ItemId = itemId,
                    WarehouseId = input.WarehouseId,
                    Quantity = input.Quantity,
                    LowStockAlertQuantity = input.LowStockAlertQuantity
                });
            }
            else
            {
                row.Quantity = input.Quantity;
                row.LowStockAlertQuantity = input.LowStockAlertQuantity;
            }
        }

        foreach (var row in existing.Where(e => !inputs.Any(i => i.WarehouseId == e.WarehouseId)))
        {
            row.Quantity = 0;
            row.LowStockAlertQuantity = null;
        }

        await _db.SaveChangesAsync(ct);
        await RecalculateItemTotalAsync(itemId, ct);

        // Keep item-level alert as the lowest enabled warehouse threshold (legacy / list badges).
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted, ct);
        if (item != null)
        {
            var enabled = inputs
                .Where(i => i.LowStockAlertQuantity.HasValue)
                .Select(i => i.LowStockAlertQuantity!.Value)
                .ToList();
            item.LowStockAlertQuantity = enabled.Count > 0 ? enabled.Min() : null;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeductAsync(int itemId, int warehouseId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0) return;
        var row = await GetOrCreateStockRowAsync(itemId, warehouseId, ct);
        if (row.Quantity < quantity)
            throw new InvalidOperationException($"insufficientInventory|{itemId}|{row.Quantity}|{quantity}");
        row.Quantity -= quantity;
        await _db.SaveChangesAsync(ct);
        await RecalculateItemTotalAsync(itemId, ct);
    }

    public async Task AddAsync(int itemId, int warehouseId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0) return;
        var row = await GetOrCreateStockRowAsync(itemId, warehouseId, ct);
        row.Quantity += quantity;
        await _db.SaveChangesAsync(ct);
        await RecalculateItemTotalAsync(itemId, ct);
    }

    public async Task TransferAsync(
        int itemId,
        int commercialUserId,
        int fromWarehouseId,
        int toWarehouseId,
        int quantity,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("invalidTransferQuantity");
        if (fromWarehouseId == toWarehouseId)
            throw new InvalidOperationException("sameWarehouseTransfer");

        var from = await GetActiveWarehouseAsync(commercialUserId, fromWarehouseId, ct)
            ?? throw new InvalidOperationException("invalidWarehouse");
        var to = await GetActiveWarehouseAsync(commercialUserId, toWarehouseId, ct)
            ?? throw new InvalidOperationException("invalidWarehouse");

        var itemOk = await _db.Items.AnyAsync(i =>
            !i.IsDeleted && i.Id == itemId && i.InsertByUserId == commercialUserId, ct);
        if (!itemOk)
            throw new InvalidOperationException("itemNotFound");

        var fromRow = await GetOrCreateStockRowAsync(itemId, from.Id, ct);
        if (fromRow.Quantity < quantity)
            throw new InvalidOperationException($"insufficientInventory|{itemId}|{fromRow.Quantity}|{quantity}");

        var toRow = await GetOrCreateStockRowAsync(itemId, to.Id, ct);
        fromRow.Quantity -= quantity;
        toRow.Quantity += quantity;
        await _db.SaveChangesAsync(ct);
        await RecalculateItemTotalAsync(itemId, ct);
    }

    public async Task RecalculateItemTotalAsync(int itemId, CancellationToken ct = default)
    {
        var total = await _db.ItemWarehouseStocks
            .Where(s => !s.IsDeleted && s.ItemId == itemId)
            .SumAsync(s => (int?)s.Quantity, ct) ?? 0;

        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted, ct);
        if (item == null) return;
        item.Quantity = total;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<ItemWarehouseStock> GetOrCreateStockRowAsync(int itemId, int warehouseId, CancellationToken ct)
    {
        var row = await _db.ItemWarehouseStocks
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.ItemId == itemId && s.WarehouseId == warehouseId, ct);
        if (row != null)
            return row;

        row = new ItemWarehouseStock
        {
            ItemId = itemId,
            WarehouseId = warehouseId,
            Quantity = 0
        };
        _db.ItemWarehouseStocks.Add(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }
}

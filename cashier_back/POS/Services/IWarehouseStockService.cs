using POS.Models;
using POS.Models.Dtos;

namespace POS.Services;

public interface IWarehouseStockService
{
    Task<Warehouse> EnsureDefaultWarehouseAsync(int commercialUserId, CancellationToken ct = default);
    Task<Warehouse?> GetActiveWarehouseAsync(int commercialUserId, int warehouseId, CancellationToken ct = default);
    Task<int> GetStockAsync(int itemId, int warehouseId, CancellationToken ct = default);
    Task<Dictionary<int, int>> GetStocksForItemsAsync(IEnumerable<int> itemIds, int warehouseId, CancellationToken ct = default);
    Task<List<WarehouseStockDto>> GetItemStockBreakdownAsync(int itemId, int commercialUserId, CancellationToken ct = default);
    Task SetItemStocksAsync(int itemId, int commercialUserId, IReadOnlyList<WarehouseStockInputDto>? stocks, int? fallbackTotalQuantity, CancellationToken ct = default);
    Task DeductAsync(int itemId, int warehouseId, int quantity, CancellationToken ct = default);
    Task AddAsync(int itemId, int warehouseId, int quantity, CancellationToken ct = default);
    Task TransferAsync(int itemId, int commercialUserId, int fromWarehouseId, int toWarehouseId, int quantity, CancellationToken ct = default);
    Task RecalculateItemTotalAsync(int itemId, CancellationToken ct = default);
}

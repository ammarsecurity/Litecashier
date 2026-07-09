using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Models.Dtos;

namespace POS.Services
{
    public interface ICommercialCatalogClearService
    {
        Task<CatalogClearResultDto> ClearCatalogAsync(int commercialUserId);
    }

    public class CommercialCatalogClearService : ICommercialCatalogClearService
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<CommercialCatalogClearService> _logger;

        public CommercialCatalogClearService(DbConfig dbConfig, ILogger<CommercialCatalogClearService> logger)
        {
            _dbConfig = dbConfig;
            _logger = logger;
        }

        public async Task<CatalogClearResultDto> ClearCatalogAsync(int commercialUserId)
        {
            var result = new CatalogClearResultDto();
            var now = DateTime.UtcNow;

            var tenantUserIds = await _dbConfig.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted &&
                    (u.Id == commercialUserId || u.InsertByUserId == commercialUserId))
                .Select(u => u.Id)
                .ToListAsync();

            if (tenantUserIds.Count == 0)
            {
                tenantUserIds.Add(commercialUserId);
            }

            await using var transaction = await _dbConfig.Database.BeginTransactionAsync();

            try
            {
                var orderIds = await _dbConfig.CustomerOrders
                    .Where(o => !o.IsDeleted && tenantUserIds.Contains(o.InsertByUserId))
                    .Select(o => o.Id)
                    .ToListAsync();

                if (orderIds.Count > 0)
                {
                    result.OrderItemsCleared = await _dbConfig.CustomerOrderItems
                        .Where(oi => !oi.IsDeleted && orderIds.Contains(oi.CustomerOrderId))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.IsDeleted, true)
                            .SetProperty(x => x.UpdateDate, now));

                    result.CardPaymentsCleared = await _dbConfig.CardPaymentTransactions
                        .Where(t => !t.IsDeleted &&
                            t.InsertByUserId == commercialUserId &&
                            t.CustomerOrderId != null &&
                            orderIds.Contains(t.CustomerOrderId.Value))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.IsDeleted, true)
                            .SetProperty(x => x.CustomerOrderId, (int?)null)
                            .SetProperty(x => x.UpdateDate, now));
                }

                result.OrdersCleared = await _dbConfig.CustomerOrders
                    .Where(o => !o.IsDeleted && tenantUserIds.Contains(o.InsertByUserId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateDate, now));

                result.ItemsCleared = await _dbConfig.Items
                    .Where(i => !i.IsDeleted && tenantUserIds.Contains(i.InsertByUserId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateDate, now));

                result.TagsCleared = await _dbConfig.Tags
                    .Where(t => !t.IsDeleted && tenantUserIds.Contains(t.InsertByUserId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateDate, now));

                result.TagPrintersCleared = await _dbConfig.TagPrinters
                    .Where(tp => !tp.IsDeleted && tenantUserIds.Contains(tp.InsertByUserId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateDate, now));

                result.StockMovementsCleared = await _dbConfig.StockMovements
                    .Where(sm => !sm.IsDeleted && tenantUserIds.Contains(sm.InsertByUserId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateDate, now));

                result.SuppliersCleared = await _dbConfig.Suppliers
                    .Where(s => !s.IsDeleted && tenantUserIds.Contains(s.InsertByUserId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateDate, now));

                await transaction.CommitAsync();

                _logger.LogWarning(
                    "Catalog cleared for commercial user {CommercialUserId}: tags={Tags}, items={Items}, orders={Orders}, orderItems={OrderItems}, cardPayments={CardPayments}, stockMovements={StockMovements}, suppliers={Suppliers}, tagPrinters={TagPrinters}",
                    commercialUserId,
                    result.TagsCleared,
                    result.ItemsCleared,
                    result.OrdersCleared,
                    result.OrderItemsCleared,
                    result.CardPaymentsCleared,
                    result.StockMovementsCleared,
                    result.SuppliersCleared,
                    result.TagPrintersCleared);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to clear catalog for commercial user {CommercialUserId}", commercialUserId);
                throw;
            }
        }
    }
}

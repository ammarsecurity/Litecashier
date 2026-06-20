using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Response;

namespace RestaurantPOS.Services;

public interface ICreditAccountService
{
    Task<CreditAccountsOverviewDto> GetOverviewAsync(int commercialUserId, CancellationToken cancellationToken = default);

    Task<CreditAccountDetailDto?> GetAccountDetailAsync(
        int commercialUserId,
        string accountType,
        int accountId,
        string? statusFilter,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? ErrorMessage, SettleCreditOrderResultDto? Result)> SettleOrderAsync(
        int commercialUserId,
        int actingUserId,
        SettleCreditOrderRequest request,
        CancellationToken cancellationToken = default);
}

public class CreditAccountService : ICreditAccountService
{
    private static readonly HashSet<string> ValidSettlementMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cash", "Card", "BankTransfer",
    };

    private readonly DbConfig _db;
    private readonly ILogger<CreditAccountService> _logger;

    public CreditAccountService(DbConfig db, ILogger<CreditAccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CreditAccountsOverviewDto> GetOverviewAsync(int commercialUserId, CancellationToken cancellationToken = default)
    {
        var orders = await LoadCreditOrdersQuery(commercialUserId)
            .Include(o => o.CreditCustomer)
            .Include(o => o.CreditEmployee)
            .Include(o => o.CustomerOrderItem)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var customers = BuildCustomerSummaries(orders);
        var employees = BuildEmployeeSummaries(orders);

        return new CreditAccountsOverviewDto
        {
            TotalPendingDebt = customers.Sum(c => c.PendingAmount) + employees.Sum(e => e.PendingAmount),
            TotalPaidAmount = customers.Sum(c => c.PaidAmount) + employees.Sum(e => e.PaidAmount),
            AccountsWithPendingDebt = customers.Count(c => c.PendingAmount > 0) + employees.Count(e => e.PendingAmount > 0),
            Customers = customers.OrderByDescending(c => c.PendingAmount).ThenBy(c => c.Name).ToList(),
            Employees = employees.OrderByDescending(e => e.PendingAmount).ThenBy(e => e.Name).ToList(),
        };
    }

    public async Task<CreditAccountDetailDto?> GetAccountDetailAsync(
        int commercialUserId,
        string accountType,
        int accountId,
        string? statusFilter,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeAccountType(accountType, out var normalizedType))
        {
            return null;
        }

        var ordersQuery = LoadCreditOrdersQuery(commercialUserId)
            .Include(o => o.CustomerOrderItem)
            .AsNoTracking();

        ordersQuery = normalizedType == "Customer"
            ? ordersQuery.Where(o => o.CreditCustomerId == accountId)
            : ordersQuery.Where(o => o.CreditEmployeeId == accountId);

        var orders = await ordersQuery
            .OrderByDescending(o => o.InsertDate)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            var exists = normalizedType == "Customer"
                ? await _db.Customers.AsNoTracking().AnyAsync(
                    c => c.Id == accountId && c.InsertByUserId == commercialUserId && !c.IsDeleted,
                    cancellationToken)
                : await _db.Employees.AsNoTracking().AnyAsync(
                    e => e.Id == accountId && e.InsertByUserId == commercialUserId && !e.IsDeleted,
                    cancellationToken);

            if (!exists)
            {
                return null;
            }
        }

        var summary = normalizedType == "Customer"
            ? BuildCustomerSummaries(orders).FirstOrDefault() ?? await BuildEmptyCustomerSummaryAsync(accountId, commercialUserId, cancellationToken)
            : BuildEmployeeSummaries(orders).FirstOrDefault() ?? await BuildEmptyEmployeeSummaryAsync(accountId, commercialUserId, cancellationToken);

        var filter = (statusFilter ?? "all").Trim().ToLowerInvariant();
        IEnumerable<CustomerOrder> filtered = orders;
        if (filter == "pending")
        {
            filtered = orders.Where(o => IsPending(o));
        }
        else if (filter == "paid")
        {
            filtered = orders.Where(o => IsPaid(o));
        }

        return new CreditAccountDetailDto
        {
            Summary = summary,
            Orders = filtered.Select(MapOrder).ToList(),
        };
    }

    public async Task<(bool Ok, string? ErrorMessage, SettleCreditOrderResultDto? Result)> SettleOrderAsync(
        int commercialUserId,
        int actingUserId,
        SettleCreditOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var settlementMethod = (request.SettlementPaymentMethod ?? "Cash").Trim();
        if (!ValidSettlementMethods.Contains(settlementMethod))
        {
            return (false, "invalidSettlementPaymentMethod", null);
        }

        var order = await _db.CustomerOrders
            .FirstOrDefaultAsync(
                o => o.Id == request.OrderId
                     && !o.IsDeleted
                     && o.InsertByUserId == commercialUserId,
                cancellationToken);

        if (order == null)
        {
            return (false, "orderNotFound", null);
        }

        if (!string.Equals(order.PaymentMethod, "Credit", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "orderNotCredit", null);
        }

        if (!IsPending(order))
        {
            return (false, "orderAlreadySettled", null);
        }

        var settledAt = DateTime.UtcNow;
        order.PaymentStatus = "Paid";
        order.OrderStatus = "Completed";
        order.SettlementPaymentMethod = settlementMethod;
        order.SettledAt = settledAt;
        order.UpdateDate = settledAt;

        await _db.SaveChangesAsync(cancellationToken);

        await _db.LogAuditAsync(
            "Settle",
            "CustomerOrder",
            order.Id,
            order.OrderCode,
            actingUserId,
            commercialUserId,
            description: $"Credit order settled via {settlementMethod}",
            newValues: new
            {
                order.PaymentStatus,
                order.SettlementPaymentMethod,
                order.SettledAt,
            });

        _logger.LogInformation(
            "Credit order {OrderId} settled for commercial user {CommercialUserId} via {Method}",
            order.Id,
            commercialUserId,
            settlementMethod);

        return (true, null, new SettleCreditOrderResultDto
        {
            OrderId = order.Id,
            PaymentStatus = order.PaymentStatus,
            SettlementPaymentMethod = order.SettlementPaymentMethod,
            SettledAt = order.SettledAt,
        });
    }

    private IQueryable<CustomerOrder> LoadCreditOrdersQuery(int commercialUserId) =>
        _db.CustomerOrders.Where(o =>
            !o.IsDeleted
            && o.InsertByUserId == commercialUserId
            && o.PaymentMethod == "Credit");

    private static List<CreditAccountSummaryDto> BuildCustomerSummaries(IEnumerable<CustomerOrder> orders) =>
        orders
            .Where(o => o.CreditCustomerId.HasValue)
            .GroupBy(o => o.CreditCustomerId!.Value)
            .Select(g =>
            {
                var first = g.First();
                var name = first.CreditCustomer?.Name ?? $"Customer #{g.Key}";
                var phone = first.CreditCustomer?.PhoneNumber;
                return BuildSummary("Customer", g.Key, name, phone, g);
            })
            .ToList();

    private static List<CreditAccountSummaryDto> BuildEmployeeSummaries(IEnumerable<CustomerOrder> orders) =>
        orders
            .Where(o => o.CreditEmployeeId.HasValue)
            .GroupBy(o => o.CreditEmployeeId!.Value)
            .Select(g =>
            {
                var first = g.First();
                var name = first.CreditEmployee?.Name ?? $"Employee #{g.Key}";
                var phone = first.CreditEmployee?.PhoneNumber;
                return BuildSummary("Employee", g.Key, name, phone, g);
            })
            .ToList();

    private static CreditAccountSummaryDto BuildSummary(
        string accountType,
        int accountId,
        string name,
        string? phone,
        IEnumerable<CustomerOrder> orders)
    {
        var list = orders.ToList();
        var pending = list.Where(IsPending).ToList();
        var paid = list.Where(IsPaid).ToList();

        return new CreditAccountSummaryDto
        {
            AccountType = accountType,
            AccountId = accountId,
            Name = name,
            Phone = phone,
            TotalCharged = list.Sum(GetOrderAmount),
            PaidAmount = paid.Sum(GetOrderAmount),
            PendingAmount = pending.Sum(GetOrderAmount),
            PendingOrderCount = pending.Count,
            TotalOrderCount = list.Count,
        };
    }

    private async Task<CreditAccountSummaryDto> BuildEmptyCustomerSummaryAsync(
        int accountId,
        int commercialUserId,
        CancellationToken cancellationToken)
    {
        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == accountId && c.InsertByUserId == commercialUserId && !c.IsDeleted, cancellationToken);

        return new CreditAccountSummaryDto
        {
            AccountType = "Customer",
            AccountId = accountId,
            Name = customer?.Name ?? $"Customer #{accountId}",
            Phone = customer?.PhoneNumber,
        };
    }

    private async Task<CreditAccountSummaryDto> BuildEmptyEmployeeSummaryAsync(
        int accountId,
        int commercialUserId,
        CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == accountId && e.InsertByUserId == commercialUserId && !e.IsDeleted, cancellationToken);

        return new CreditAccountSummaryDto
        {
            AccountType = "Employee",
            AccountId = accountId,
            Name = employee?.Name ?? $"Employee #{accountId}",
            Phone = employee?.PhoneNumber,
        };
    }

    private static CreditAccountOrderDto MapOrder(CustomerOrder o) =>
        new()
        {
            OrderId = o.Id,
            OrderCode = o.OrderCode,
            InsertDate = o.InsertDate,
            Amount = GetOrderAmount(o),
            PaymentStatus = o.PaymentStatus,
            SettlementPaymentMethod = o.SettlementPaymentMethod,
            SettledAt = o.SettledAt,
            OrderType = o.OrderType,
            OrderStatus = o.OrderStatus,
        };

    private static decimal GetOrderAmount(CustomerOrder o)
    {
        if (o.OrderTotalAfterDiscount.HasValue)
        {
            return o.OrderTotalAfterDiscount.Value;
        }

        if (o.OrderSubTotal.HasValue)
        {
            return o.OrderSubTotal.Value;
        }

        if (o.CustomerOrderItem == null || o.CustomerOrderItem.Count == 0)
        {
            return 0;
        }

        return o.CustomerOrderItem
            .Where(i => !i.IsDeleted)
            .Sum(i => i.SellingPrice * i.Quantity);
    }

    private static bool IsPending(CustomerOrder o) =>
        !string.Equals(o.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(o.PaymentStatus, "Refunded", StringComparison.OrdinalIgnoreCase);

    private static bool IsPaid(CustomerOrder o) =>
        string.Equals(o.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);

    private static bool TryNormalizeAccountType(string accountType, out string normalized)
    {
        normalized = accountType.Trim();
        if (normalized.Equals("customer", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Customer";
            return true;
        }

        if (normalized.Equals("employee", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Employee";
            return true;
        }

        return false;
    }
}

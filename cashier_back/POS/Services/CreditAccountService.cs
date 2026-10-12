using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Models;
using POS.Models.Response;

namespace POS.Services;

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

    Task<(bool Ok, string? ErrorMessage, CreditAccountOrderDto? Result)> CreateOrderAsync(
        int commercialUserId,
        int actingUserId,
        CreateCreditOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? ErrorMessage, CreditAccountOrderDto? Result)> UpdateOrderAsync(
        int commercialUserId,
        int actingUserId,
        int orderId,
        UpdateCreditOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? ErrorMessage)> DeleteOrderAsync(
        int commercialUserId,
        int actingUserId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? ErrorMessage)> DeleteAccountAsync(
        int commercialUserId,
        int actingUserId,
        int accountId,
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
            .Include(o => o.CustomerOrderItem)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var customers = BuildCustomerSummaries(orders);

        return new CreditAccountsOverviewDto
        {
            TotalPendingDebt = customers.Sum(c => c.PendingAmount),
            TotalPaidAmount = customers.Sum(c => c.PaidAmount),
            AccountsWithPendingDebt = customers.Count(c => c.PendingAmount > 0),
            Customers = customers.OrderByDescending(c => c.PendingAmount).ThenBy(c => c.Name).ToList(),
        };
    }

    public async Task<CreditAccountDetailDto?> GetAccountDetailAsync(
        int commercialUserId,
        string accountType,
        int accountId,
        string? statusFilter,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeAccountType(accountType, out _))
        {
            return null;
        }

        var ordersQuery = LoadCreditOrdersQuery(commercialUserId)
            .Where(o => o.CreditCustomerId == accountId)
            .Include(o => o.CustomerOrderItem)
            .Include(o => o.CreditSettlements!.Where(s => !s.IsDeleted))
            .AsNoTracking();

        var orders = await ordersQuery
            .OrderByDescending(o => o.InsertDate)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            var exists = await _db.Customers.AsNoTracking().AnyAsync(
                c => c.Id == accountId && c.InsertByUserId == commercialUserId && !c.IsDeleted,
                cancellationToken);

            if (!exists)
            {
                return null;
            }
        }

        var summary = BuildCustomerSummaries(orders).FirstOrDefault()
            ?? await BuildEmptyCustomerSummaryAsync(accountId, commercialUserId, cancellationToken);

        var filter = (statusFilter ?? "all").Trim().ToLowerInvariant();
        IEnumerable<CustomerOrder> filtered = orders;
        if (filter == "pending")
        {
            filtered = orders.Where(IsPending);
        }
        else if (filter == "paid")
        {
            filtered = orders.Where(IsPaid);
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

        var order = await LoadCreditOrdersQuery(commercialUserId)
            .Include(o => o.CustomerOrderItem)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

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

        var total = GetOrderAmount(order);
        var remaining = Math.Max(0m, Round2(total - order.SettledAmount));
        if (remaining <= 0)
        {
            return (false, "orderAlreadySettled", null);
        }

        var apply = request.Amount.HasValue && request.Amount.Value > 0
            ? Round2(request.Amount.Value)
            : remaining;

        if (apply <= 0)
        {
            return (false, "invalidSettlementAmount", null);
        }

        if (apply > remaining)
        {
            return (false, "settlementAmountExceedsRemaining", null);
        }

        var settledAt = DateTime.UtcNow;
        order.SettledAmount = Round2(order.SettledAmount + apply);
        order.SettlementPaymentMethod = settlementMethod;
        order.UpdateDate = settledAt;

        var fullyPaid = order.SettledAmount >= total;
        if (fullyPaid)
        {
            order.SettledAmount = total;
            order.PaymentStatus = "Paid";
            order.SettledAt = settledAt;
        }
        else
        {
            order.PaymentStatus = "Pending";
            order.SettledAt = null;
        }

        _db.CreditSettlements.Add(new CreditSettlement
        {
            CustomerOrderId = order.Id,
            Amount = apply,
            PaymentMethod = settlementMethod,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            InsertByUserId = actingUserId,
            InsertDate = settledAt,
            UpdateDate = settledAt,
            IsDeleted = false,
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _db.LogAuditAsync(
            "Settle",
            "CustomerOrder",
            order.Id,
            order.OrderCode,
            actingUserId,
            commercialUserId,
            description: $"Credit settlement {apply} via {settlementMethod}",
            newValues: new
            {
                order.PaymentStatus,
                order.SettlementPaymentMethod,
                order.SettledAmount,
                order.SettledAt,
                AppliedAmount = apply,
            });

        _logger.LogInformation(
            "Credit order {OrderId} settled {Amount} for commercial user {CommercialUserId} via {Method}",
            order.Id,
            apply,
            commercialUserId,
            settlementMethod);

        return (true, null, new SettleCreditOrderResultDto
        {
            OrderId = order.Id,
            PaymentStatus = order.PaymentStatus,
            SettlementPaymentMethod = order.SettlementPaymentMethod,
            SettledAt = order.SettledAt,
            SettledAmount = order.SettledAmount,
            RemainingAmount = Math.Max(0m, Round2(total - order.SettledAmount)),
            AppliedAmount = apply,
        });
    }

    public async Task<(bool Ok, string? ErrorMessage, CreditAccountOrderDto? Result)> CreateOrderAsync(
        int commercialUserId,
        int actingUserId,
        CreateCreditOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var amount = Round2(request.Amount);
        if (amount <= 0)
        {
            return (false, "invalidCreditAmount", null);
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(
            c => c.Id == request.CustomerId && c.InsertByUserId == commercialUserId && !c.IsDeleted,
            cancellationToken);
        if (customer == null)
        {
            return (false, "customerNotFound", null);
        }

        var now = DateTime.UtcNow;
        var order = new CustomerOrder
        {
            OrderCode = await GenerateCreditOrderCodeAsync(commercialUserId, cancellationToken),
            PaymentMethod = "Credit",
            PaymentStatus = "Pending",
            CreditCustomerId = customer.Id,
            OrderSubTotal = amount,
            OrderTotalAfterDiscount = amount,
            SettledAmount = 0,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            InsertByUserId = actingUserId,
            InsertDate = now,
            UpdateDate = now,
            IsDeleted = false,
            OrderSource = "Pos",
            CustomerName = customer.Name,
            CustomerPhone = customer.PhoneNumber,
        };

        _db.CustomerOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.LogAuditAsync(
            "Create",
            "CustomerOrder",
            order.Id,
            order.OrderCode,
            actingUserId,
            commercialUserId,
            description: "Manual credit debt created",
            newValues: new { order.CreditCustomerId, amount, order.Notes });

        return (true, null, MapOrder(order));
    }

    public async Task<(bool Ok, string? ErrorMessage, CreditAccountOrderDto? Result)> UpdateOrderAsync(
        int commercialUserId,
        int actingUserId,
        int orderId,
        UpdateCreditOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadCreditOrdersQuery(commercialUserId)
            .Include(o => o.CustomerOrderItem)
            .Include(o => o.CreditSettlements!.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return (false, "orderNotFound", null);
        }

        if (!IsPending(order))
        {
            return (false, "cannotEditSettledCreditOrder", null);
        }

        if (request.CustomerId.HasValue && request.CustomerId.Value != order.CreditCustomerId)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(
                c => c.Id == request.CustomerId.Value && c.InsertByUserId == commercialUserId && !c.IsDeleted,
                cancellationToken);
            if (customer == null)
            {
                return (false, "customerNotFound", null);
            }

            order.CreditCustomerId = customer.Id;
            order.CustomerName = customer.Name;
            order.CustomerPhone = customer.PhoneNumber;
        }

        if (request.Amount.HasValue)
        {
            var amount = Round2(request.Amount.Value);
            if (amount <= 0)
            {
                return (false, "invalidCreditAmount", null);
            }

            if (amount < order.SettledAmount)
            {
                return (false, "creditAmountBelowSettled", null);
            }

            order.OrderSubTotal = amount;
            order.OrderTotalAfterDiscount = amount;
            if (order.SettledAmount >= amount && amount > 0)
            {
                order.PaymentStatus = "Paid";
                order.SettledAt = DateTime.UtcNow;
                order.SettledAmount = amount;
            }
        }

        if (request.Notes != null)
        {
            order.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        }

        order.UpdateDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _db.LogAuditAsync(
            "Update",
            "CustomerOrder",
            order.Id,
            order.OrderCode,
            actingUserId,
            commercialUserId,
            description: "Manual credit debt updated",
            newValues: new
            {
                order.CreditCustomerId,
                Amount = GetOrderAmount(order),
                order.Notes,
                order.SettledAmount,
            });

        return (true, null, MapOrder(order));
    }

    public async Task<(bool Ok, string? ErrorMessage)> DeleteOrderAsync(
        int commercialUserId,
        int actingUserId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadCreditOrdersQuery(commercialUserId)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return (false, "orderNotFound");
        }

        if (!IsPending(order) || order.SettledAmount > 0)
        {
            return (false, "cannotDeleteSettledCreditOrder");
        }

        order.IsDeleted = true;
        order.UpdateDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _db.LogAuditAsync(
            "Delete",
            "CustomerOrder",
            order.Id,
            order.OrderCode,
            actingUserId,
            commercialUserId,
            description: "Manual credit debt deleted");

        return (true, null);
    }

    public async Task<(bool Ok, string? ErrorMessage)> DeleteAccountAsync(
        int commercialUserId,
        int actingUserId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(
            c => c.Id == accountId && c.InsertByUserId == commercialUserId && !c.IsDeleted,
            cancellationToken);

        var orders = await LoadCreditOrdersQuery(commercialUserId)
            .Where(o => o.CreditCustomerId == accountId)
            .ToListAsync(cancellationToken);

        if (customer == null && orders.Count == 0)
        {
            return (false, "accountNotFound");
        }

        var now = DateTime.UtcNow;
        foreach (var order in orders)
        {
            order.IsDeleted = true;
            order.UpdateDate = now;
        }

        if (customer != null)
        {
            customer.IsDeleted = true;
            customer.UpdateDate = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _db.LogAuditAsync(
            "Delete",
            "CreditAccount",
            accountId,
            customer?.Name ?? $"Customer #{accountId}",
            actingUserId,
            commercialUserId,
            description: $"Credit account deleted with {orders.Count} order(s)");

        return (true, null);
    }

    private async Task<string> GenerateCreditOrderCodeAsync(int commercialUserId, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 8; i++)
        {
            var code = $"CR-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
            var exists = await _db.CustomerOrders.AnyAsync(
                o => o.OrderCode == code && !o.IsDeleted,
                cancellationToken);
            if (!exists) return code;
        }

        return $"CR-{Guid.NewGuid():N}"[..16];
    }

    private IQueryable<CustomerOrder> LoadCreditOrdersQuery(int commercialUserId) =>
        _db.CustomerOrders
            .Include(o => o.User)
            .Where(o =>
                !o.IsDeleted
                && o.PaymentMethod == "Credit"
                && (o.InsertByUserId == commercialUserId
                    || (o.User != null && o.User.InsertByUserId == commercialUserId)));

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

    private static CreditAccountSummaryDto BuildSummary(
        string accountType,
        int accountId,
        string name,
        string? phone,
        IEnumerable<CustomerOrder> orders)
    {
        var list = orders.ToList();
        var paidAmount = Round2(list.Sum(o => Math.Min(GetOrderAmount(o), Math.Max(0m, o.SettledAmount))));
        var pendingAmount = Round2(list.Sum(o => Math.Max(0m, GetOrderAmount(o) - Math.Max(0m, o.SettledAmount))));
        var pendingCount = list.Count(IsPending);

        return new CreditAccountSummaryDto
        {
            AccountType = accountType,
            AccountId = accountId,
            Name = name,
            Phone = phone,
            TotalCharged = Round2(list.Sum(GetOrderAmount)),
            PaidAmount = paidAmount,
            PendingAmount = pendingAmount,
            PendingOrderCount = pendingCount,
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

    private static CreditAccountOrderDto MapOrder(CustomerOrder o)
    {
        var amount = GetOrderAmount(o);
        var settled = Math.Max(0m, o.SettledAmount);
        return new CreditAccountOrderDto
        {
            OrderId = o.Id,
            OrderCode = o.OrderCode,
            InsertDate = o.InsertDate,
            Amount = amount,
            SettledAmount = settled,
            RemainingAmount = Math.Max(0m, Round2(amount - settled)),
            PaymentStatus = o.PaymentStatus,
            SettlementPaymentMethod = o.SettlementPaymentMethod,
            SettledAt = o.SettledAt,
            Notes = o.Notes,
            Settlements = (o.CreditSettlements ?? new List<CreditSettlement>())
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.InsertDate)
                .Select(s => new CreditSettlementDto
                {
                    Id = s.Id,
                    Amount = s.Amount,
                    PaymentMethod = s.PaymentMethod,
                    Notes = s.Notes,
                    InsertDate = s.InsertDate,
                })
                .ToList(),
        };
    }

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
        && !string.Equals(o.PaymentStatus, "Refunded", StringComparison.OrdinalIgnoreCase)
        && GetOrderAmount(o) > Math.Max(0m, o.SettledAmount);

    private static bool IsPaid(CustomerOrder o) =>
        string.Equals(o.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
        || (GetOrderAmount(o) > 0 && Math.Max(0m, o.SettledAmount) >= GetOrderAmount(o));

    private static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static bool TryNormalizeAccountType(string accountType, out string normalized)
    {
        normalized = accountType.Trim();
        if (normalized.Equals("customer", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Customer";
            return true;
        }

        return false;
    }
}

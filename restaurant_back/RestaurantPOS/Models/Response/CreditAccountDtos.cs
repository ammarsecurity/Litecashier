namespace RestaurantPOS.Models.Response;

public class CreditAccountsOverviewDto
{
    public decimal TotalPendingDebt { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public int AccountsWithPendingDebt { get; set; }
    public List<CreditAccountSummaryDto> Customers { get; set; } = new();
    public List<CreditAccountSummaryDto> Employees { get; set; } = new();
}

public class CreditAccountSummaryDto
{
    public string AccountType { get; set; } = "";
    public int AccountId { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public decimal TotalCharged { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public int PendingOrderCount { get; set; }
    public int TotalOrderCount { get; set; }
}

public class CreditAccountDetailDto
{
    public CreditAccountSummaryDto Summary { get; set; } = new();
    public List<CreditAccountOrderDto> Orders { get; set; } = new();
}

public class CreditAccountOrderDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public DateTime InsertDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string? SettlementPaymentMethod { get; set; }
    public DateTime? SettledAt { get; set; }
    public string OrderType { get; set; } = "";
    public string OrderStatus { get; set; } = "";
}

public class SettleCreditOrderRequest
{
    public int OrderId { get; set; }
    public string SettlementPaymentMethod { get; set; } = "Cash";
}

public class SettleCreditOrderResultDto
{
    public int OrderId { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string? SettlementPaymentMethod { get; set; }
    public DateTime? SettledAt { get; set; }
}

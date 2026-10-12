namespace POS.Models.Response;

public class CreditAccountsOverviewDto
{
    public decimal TotalPendingDebt { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public int AccountsWithPendingDebt { get; set; }
    public List<CreditAccountSummaryDto> Customers { get; set; } = new();
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
    public decimal SettledAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string? SettlementPaymentMethod { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? Notes { get; set; }
    public List<CreditSettlementDto> Settlements { get; set; } = new();
}

public class CreditSettlementDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime InsertDate { get; set; }
}

public class SettleCreditOrderRequest
{
    public int OrderId { get; set; }
    public string SettlementPaymentMethod { get; set; } = "Cash";
    /// <summary>Partial or full amount. Null/0 means settle remaining balance in full.</summary>
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }
}

public class SettleCreditOrderResultDto
{
    public int OrderId { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string? SettlementPaymentMethod { get; set; }
    public DateTime? SettledAt { get; set; }
    public decimal SettledAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal AppliedAmount { get; set; }
}

public class CreateCreditOrderRequest
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCreditOrderRequest
{
    public int? CustomerId { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }
}

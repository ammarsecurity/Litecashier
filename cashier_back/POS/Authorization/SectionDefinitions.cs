namespace POS.Authorization;

/// <summary>
/// Section keys align with cashier_front navigation item.name values.
/// </summary>
public static class SectionDefinitions
{
    public const string ManagerRole = "Manager";

    public static readonly IReadOnlyList<string> AssignableSectionKeys = new[]
    {
        "pos",
        "category",
        "items",
        "priceReader",
        "reports",
        "endOfDayReport",
        "expenses",
        "inventory",
        "printServer",
        "paymentDevices",
        "cardPayments",
        "employees",
        "customers",
        "deferredPayments",
        "stockAlerts",
        "stockReturns",
        "auditLog",
        "users",
        "dashboard",
    };

    private static readonly HashSet<string> AssignableSet =
        new(AssignableSectionKeys, StringComparer.OrdinalIgnoreCase);

    public static bool IsAssignable(string? key) =>
        !string.IsNullOrWhiteSpace(key) && AssignableSet.Contains(key.Trim());

    public static IReadOnlyList<string> NormalizeAssignable(IEnumerable<string>? keys)
    {
        if (keys == null) return Array.Empty<string>();
        return keys
            .Select(k => (k ?? "").Trim())
            .Where(k => k.Length > 0 && AssignableSet.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

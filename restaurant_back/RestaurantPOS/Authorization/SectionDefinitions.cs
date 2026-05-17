namespace RestaurantPOS.Authorization;

/// <summary>
/// Section keys align with restaurant_front/src/navigation/navItems.js item.name values.
/// </summary>
public static class SectionDefinitions
{
    public const string ManagerRole = "Manager";

    public static readonly IReadOnlyList<string> AssignableSectionKeys = new[]
    {
        "category",
        "items",
        "tables",
        "reservations",
        "reports",
        "endOfDayReport",
        "publicOrders",
        "orderQueue",
        "expenses",
        "inventory",
        "printServer",
        "deliveryDrivers",
        "employees",
        "customers",
        "auditLog",
        "printTemplates",
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

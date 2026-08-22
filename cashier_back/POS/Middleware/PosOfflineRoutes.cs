namespace POS.Middleware;

/// <summary>
/// POS catalog/sale endpoints that must never wait on the license server (offline-first).
/// </summary>
internal static class PosOfflineRoutes
{
    private static readonly HashSet<string> AdminActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "GetItems",
        "GetItemsByCode",
        "GetTags",
        "AddOrder",
        "CommercialUserInfo",
        "UpdateOrder",
    };

    public static bool IsLocalOnlyPath(PathString path)
    {
        if (!path.HasValue || string.IsNullOrEmpty(path.Value))
            return false;

        var p = path.Value;

        if (p.StartsWith("/Warehouses/ForPos", StringComparison.OrdinalIgnoreCase))
            return true;

        if (p.StartsWith("/Printers", StringComparison.OrdinalIgnoreCase))
            return true;

        if (p.StartsWith("/Customers", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!p.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase))
            return false;

        var segments = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return false;

        return AdminActions.Contains(segments[1]);
    }
}

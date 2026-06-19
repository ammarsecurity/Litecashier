namespace RestaurantPOS.Services.Sync;

public static class SyncTriggers
{
    public const string Manual = "Manual";
    public const string Auto = "Auto";
}

public static class SyncRunStatuses
{
    public const string Running = "Running";
    public const string Success = "Success";
    public const string Failed = "Failed";
}

public sealed class SyncTableDefinition
{
    public required string TableName { get; init; }
    public required string WhereClause { get; init; }
    public int Order { get; init; }
}

public static class SyncTableRegistry
{
    public static readonly IReadOnlyList<SyncTableDefinition> Tables = new List<SyncTableDefinition>
    {
        new() { Order = 1, TableName = "Users", WhereClause = "(Id = @commercialUserId OR InsertByUserId = @commercialUserId)" },
        new() { Order = 2, TableName = "Tags", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 3, TableName = "Items", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 4, TableName = "Printers", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 5, TableName = "TagPrinters", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 6, TableName = "Tables", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 7, TableName = "RestaurantLayoutSettings", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 8, TableName = "TableLayoutPlacements", WhereClause = "TableId IN (SELECT Id FROM `Tables` WHERE InsertByUserId = @commercialUserId)" },
        new() { Order = 9, TableName = "Customers", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 10, TableName = "Employees", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 11, TableName = "ExpenseCategories", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 12, TableName = "Expenses", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 13, TableName = "Suppliers", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 14, TableName = "StockMovements", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 15, TableName = "DeliveryDrivers", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 16, TableName = "CustomerOrders", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 17, TableName = "CustomerOrderItems", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 18, TableName = "OrderTables", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 19, TableName = "PaymentDevices", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 20, TableName = "CardPaymentTransactions", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 21, TableName = "ReturnedOrderItems", WhereClause = "InsertByUserId = @commercialUserId" },
        new() { Order = 22, TableName = "Reservations", WhereClause = "InsertByUserId = @commercialUserId" },
    }.OrderBy(t => t.Order).ToList();
}

using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace RestaurantPOS.Db;

/// <summary>
/// Ensures MySQL database exists and baselines EF history when an existing schema
/// has tables but no __EFMigrationsHistory (common after reinstall / manual import).
/// </summary>
public static class DatabaseBootstrap
{
    public static void EnsureDatabaseAndMigrate(DbConfig db, string? connectionString, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'WebApiDatabase' is missing.");

        EnsureDatabaseExists(connectionString, logger);
        BaselineIfSchemaExistsWithoutHistory(db, logger);

        logger.LogInformation("Applying EF Core migrations...");
        db.Database.Migrate();
        logger.LogInformation("Database migrations completed.");
    }

    private static void EnsureDatabaseExists(string connectionString, ILogger logger)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Connection string must include Database=...");

        builder.Database = "";
        using var connection = new MySqlConnection(builder.ConnectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"CREATE DATABASE IF NOT EXISTS `{EscapeIdent(databaseName)}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        cmd.ExecuteNonQuery();
        logger.LogInformation("Ensured database exists: {Database}", databaseName);
    }

    private static void BaselineIfSchemaExistsWithoutHistory(DbConfig db, ILogger logger)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            connection.Open();

        try
        {
            if (!TableExists(connection, "Users") && !TableExists(connection, "users"))
            {
                logger.LogInformation("Empty (or new) schema — migrations will create tables.");
                return;
            }

            var historyExists = TableExists(connection, "__EFMigrationsHistory");
            var appliedCount = historyExists ? CountRows(connection, "__EFMigrationsHistory") : 0;
            if (appliedCount > 0)
            {
                logger.LogInformation("EF migration history present ({Count} rows).", appliedCount);
                return;
            }

            var migrations = db.Database.GetMigrations().ToList();
            if (migrations.Count == 0)
                return;

            logger.LogWarning(
                "Schema already has Users but no EF history. Baselining {Count} migration(s) so startup can continue.",
                migrations.Count);

            if (!historyExists)
            {
                using var create = connection.CreateCommand();
                create.CommandText =
                    """
                    CREATE TABLE `__EFMigrationsHistory` (
                        `MigrationId` varchar(150) NOT NULL,
                        `ProductVersion` varchar(32) NOT NULL,
                        PRIMARY KEY (`MigrationId`)
                    ) CHARACTER SET utf8mb4;
                    """;
                create.ExecuteNonQuery();
            }

            const string productVersion = "8.0.11";
            foreach (var id in migrations)
            {
                using var insert = connection.CreateCommand();
                insert.CommandText =
                    "INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES (@id, @ver);";
                var pId = insert.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = id;
                insert.Parameters.Add(pId);
                var pVer = insert.CreateParameter();
                pVer.ParameterName = "@ver";
                pVer.Value = productVersion;
                insert.Parameters.Add(pVer);
                insert.ExecuteNonQuery();
            }
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }

    private static bool TableExists(System.Data.Common.DbConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*) FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @name;
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@name";
        p.Value = tableName;
        cmd.Parameters.Add(p);
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }

    private static int CountRows(System.Data.Common.DbConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM `{EscapeIdent(tableName)}`;";
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    private static string EscapeIdent(string name) => name.Replace("`", "``", StringComparison.Ordinal);
}

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using MySqlConnector;

namespace RestaurantPOS.Services;

public class SystemBackupService : ISystemBackupService
{
    private const string ManifestEntryName = "manifest.json";
    private const string DatabaseEntryName = "database.sql";
    private const string ImagesFolderPrefix = "images/";

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SystemBackupService> _logger;

    public SystemBackupService(
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<SystemBackupService> logger)
    {
        _configuration = configuration;
        _env = env;
        _logger = logger;
    }

    public async Task WriteBackupArchiveAsync(Stream outputStream, CancellationToken cancellationToken = default)
    {
        EnsureMySqlProvider();

        var connectionString = GetLocalConnectionString();
        var builder = new MySqlConnectionStringBuilder(connectionString);

        using var zip = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true);

        var manifest = new BackupManifest
        {
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            Database = builder.Database,
            Provider = "mysql",
        };

        await WriteManifestAsync(zip, manifest, cancellationToken);

        var sqlEntry = zip.CreateEntry(DatabaseEntryName, CompressionLevel.Optimal);
        await using (var sqlStream = sqlEntry.Open())
        await using (var writer = new StreamWriter(sqlStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            await WriteDatabaseDumpAsync(writer, connectionString, cancellationToken);
        }

        AddImagesToArchive(zip);
    }

    public async Task RestoreFromArchiveAsync(Stream archiveStream, CancellationToken cancellationToken = default)
    {
        EnsureMySqlProvider();

        var tempRoot = Path.Combine(Path.GetTempPath(), $"litecashier-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            archiveStream.Position = 0;
            using (var zip = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    var destinationPath = Path.Combine(
                        tempRoot,
                        entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                    var destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    await using var entryStream = entry.Open();
                    await using var fileStream = File.Create(destinationPath);
                    await entryStream.CopyToAsync(fileStream, cancellationToken);
                }
            }

            var sqlPath = Path.Combine(tempRoot, DatabaseEntryName);
            if (!File.Exists(sqlPath))
            {
                throw new InvalidOperationException("backupSqlMissing");
            }

            var connectionString = GetLocalConnectionString();
            var sql = await File.ReadAllTextAsync(sqlPath, Encoding.UTF8, cancellationToken);

            await using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            await ExecuteSqlScriptAsync(conn, sql, cancellationToken);

            RestoreImagesFromFolder(Path.Combine(tempRoot, "images"));

            _logger.LogWarning("System backup restored from archive");
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete temp restore folder {TempRoot}", tempRoot);
            }
        }
    }

    private async Task WriteDatabaseDumpAsync(
        StreamWriter writer,
        string connectionString,
        CancellationToken cancellationToken)
    {
        await writer.WriteLineAsync("-- Litecashier system backup");
        await writer.WriteLineAsync($"-- Generated (UTC): {DateTime.UtcNow:O}");
        await writer.WriteLineAsync("SET NAMES utf8mb4;");
        await writer.WriteLineAsync("SET FOREIGN_KEY_CHECKS=0;");
        await writer.WriteLineAsync("SET UNIQUE_CHECKS=0;");
        await writer.WriteLineAsync("SET SQL_MODE='NO_AUTO_VALUE_ON_ZERO';");
        await writer.WriteLineAsync();

        await using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var tables = new List<string>();
        await using (var tablesCmd = new MySqlCommand(
                         """
                         SELECT TABLE_NAME
                         FROM INFORMATION_SCHEMA.TABLES
                         WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'
                         ORDER BY TABLE_NAME
                         """,
                         conn))
        await using (var reader = await tablesCmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(reader.GetString(0));
            }
        }

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await writer.WriteLineAsync($"DROP TABLE IF EXISTS `{table}`;");

            {
                await using var createCmd = new MySqlCommand($"SHOW CREATE TABLE `{table}`", conn);
                await using var createReader = await createCmd.ExecuteReaderAsync(cancellationToken);
                if (await createReader.ReadAsync(cancellationToken))
                {
                    var createSql = createReader.GetString(1);
                    await writer.WriteLineAsync($"{createSql};");
                }
            }

            await writer.WriteLineAsync();
            await WriteTableDataAsync(conn, writer, table, cancellationToken);
            await writer.WriteLineAsync();
        }

        await writer.WriteLineAsync("SET FOREIGN_KEY_CHECKS=1;");
        await writer.WriteLineAsync("SET UNIQUE_CHECKS=1;");
    }

    private static async Task WriteTableDataAsync(
        MySqlConnection conn,
        StreamWriter writer,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var fieldCount = reader.FieldCount;
        if (fieldCount == 0)
        {
            return;
        }

        var columns = new string[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            columns[i] = $"`{reader.GetName(i)}`";
        }

        var columnList = string.Join(", ", columns);
        var batch = new List<string>(capacity: 50);

        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new string[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                values[i] = EscapeSqlValue(reader.GetValue(i));
            }

            batch.Add($"({string.Join(", ", values)})");

            if (batch.Count >= 50)
            {
                await writer.WriteLineAsync(
                    $"INSERT INTO `{tableName}` ({columnList}) VALUES{Environment.NewLine}{string.Join($",{Environment.NewLine}", batch)};");
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await writer.WriteLineAsync(
                $"INSERT INTO `{tableName}` ({columnList}) VALUES{Environment.NewLine}{string.Join($",{Environment.NewLine}", batch)};");
        }
    }

    private void AddImagesToArchive(ZipArchive zip)
    {
        var imagesPath = ResolveImagesPath();
        if (!Directory.Exists(imagesPath))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(imagesPath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(imagesPath, file).Replace('\\', '/');
            zip.CreateEntryFromFile(file, ImagesFolderPrefix + relative, CompressionLevel.Optimal);
        }
    }

    private void RestoreImagesFromFolder(string extractedImagesPath)
    {
        var imagesPath = ResolveImagesPath();
        Directory.CreateDirectory(imagesPath);

        foreach (var file in Directory.EnumerateFiles(imagesPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete image {FilePath} before restore", file);
            }
        }

        if (!Directory.Exists(extractedImagesPath))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(extractedImagesPath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(extractedImagesPath, file);
            var destination = Path.Combine(imagesPath, relative);
            var destinationDir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            File.Copy(file, destination, overwrite: true);
        }
    }

    private static async Task WriteManifestAsync(
        ZipArchive zip,
        BackupManifest manifest,
        CancellationToken cancellationToken)
    {
        var entry = zip.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await JsonSerializer.SerializeAsync(stream, manifest, cancellationToken: cancellationToken);
    }

    private string ResolveImagesPath()
    {
        var configured = _configuration["SyncSettings:ImagesLocalPath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(_env.ContentRootPath, configured);
        }

        return Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "Images");
    }

    private string GetLocalConnectionString()
    {
        return _configuration.GetConnectionString("WebApiDatabase")
            ?? throw new InvalidOperationException("Connection string 'WebApiDatabase' not found.");
    }

    private void EnsureMySqlProvider()
    {
        var provider = _configuration["DatabaseSettings:Provider"]?.ToLowerInvariant();
        if (provider != "mysql")
        {
            throw new InvalidOperationException("backupMySqlOnly");
        }
    }

    private static async Task ExecuteSqlScriptAsync(
        MySqlConnection conn,
        string sql,
        CancellationToken cancellationToken)
    {
        var statementBuilder = new StringBuilder();
        var inSingleQuote = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            if (c == '\'' && !IsLineCommentAt(sql, i))
            {
                if (inSingleQuote && i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    statementBuilder.Append("''");
                    i++;
                    continue;
                }

                inSingleQuote = !inSingleQuote;
                statementBuilder.Append(c);
                continue;
            }

            if (!inSingleQuote && c == ';')
            {
                var statement = statementBuilder.ToString().Trim();
                statementBuilder.Clear();

                if (ShouldExecuteStatement(statement))
                {
                    await using var cmd = new MySqlCommand(statement, conn)
                    {
                        CommandTimeout = 0,
                    };
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                continue;
            }

            statementBuilder.Append(c);
        }

        var trailing = statementBuilder.ToString().Trim();
        if (ShouldExecuteStatement(trailing))
        {
            await using var cmd = new MySqlCommand(trailing, conn)
            {
                CommandTimeout = 0,
            };
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static bool ShouldExecuteStatement(string statement)
    {
        if (string.IsNullOrWhiteSpace(statement))
        {
            return false;
        }

        var trimmed = statement.TrimStart();
        return !trimmed.StartsWith("--", StringComparison.Ordinal);
    }

    private static bool IsLineCommentAt(string sql, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (sql[i] == '\n')
            {
                break;
            }

            if (!char.IsWhiteSpace(sql[i]))
            {
                return false;
            }
        }

        for (var i = index - 1; i >= 1; i--)
        {
            if (sql[i] == '\n')
            {
                break;
            }

            if (sql[i] == '-' && sql[i - 1] == '-')
            {
                return true;
            }
        }

        return false;
    }

    private static string EscapeSqlValue(object? value)
    {
        if (value is null or DBNull)
        {
            return "NULL";
        }

        return value switch
        {
            bool b => b ? "1" : "0",
            byte or sbyte or short or ushort or int or uint or long or ulong
                or decimal or double or float =>
                Convert.ToString(value, CultureInfo.InvariantCulture)!,
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.ffffff}'",
            DateTimeOffset dto => $"'{dto.UtcDateTime:yyyy-MM-dd HH:mm:ss.ffffff}'",
            byte[] bytes => "0x" + Convert.ToHexString(bytes),
            _ => "'" + (value.ToString() ?? string.Empty)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("'", "''", StringComparison.Ordinal)
                .Replace("\0", "\\0", StringComparison.Ordinal) + "'",
        };
    }

    private sealed class BackupManifest
    {
        public int Version { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string? Database { get; set; }

        public string? Provider { get; set; }
    }
}

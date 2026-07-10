using System.Diagnostics;
using System.Text;
using MySqlConnector;

namespace POS.Services
{
    public interface IDatabaseBackupService
    {
        Task<(byte[] Content, string FileName)> CreateBackupAsync(CancellationToken cancellationToken = default);
    }

    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<DatabaseBackupService> _logger;

        public DatabaseBackupService(
            IConfiguration configuration,
            IHostEnvironment environment,
            ILogger<DatabaseBackupService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task<(byte[] Content, string FileName)> CreateBackupAsync(CancellationToken cancellationToken = default)
        {
            var connectionString = _configuration.GetConnectionString("WebApiDatabase")
                ?? throw new InvalidOperationException("connectionStringMissing");

            var csb = new MySqlConnectionStringBuilder(connectionString);
            var database = string.IsNullOrWhiteSpace(csb.Database) ? "possuper" : csb.Database;
            var host = string.IsNullOrWhiteSpace(csb.Server) ? "127.0.0.1" : csb.Server;
            var port = csb.Port == 0 ? 3306u : csb.Port;
            var user = string.IsNullOrWhiteSpace(csb.UserID) ? "root" : csb.UserID;
            var password = csb.Password ?? string.Empty;

            var mysqldumpPath = ResolveMysqldumpPath();
            if (mysqldumpPath == null)
            {
                throw new FileNotFoundException("mysqldumpNotFound");
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"litecashier-backup-{Guid.NewGuid():N}.sql");
            try
            {
                var args = new StringBuilder();
                args.Append($"--host={EscapeArg(host)} ");
                args.Append($"--port={port} ");
                args.Append($"--user={EscapeArg(user)} ");
                // Empty password still needs the flag for non-interactive dump.
                args.Append($"--password={EscapeArg(password)} ");
                args.Append("--single-transaction --routines --triggers --default-character-set=utf8mb4 ");
                args.Append($"--result-file={EscapeArg(tempFile)} ");
                args.Append($"--databases {EscapeArg(database)}");

                _logger.LogInformation(
                    "Starting database backup via {Mysqldump} for database {Database} on {Host}:{Port}",
                    mysqldumpPath,
                    database,
                    host,
                    port);

                var startInfo = new ProcessStartInfo
                {
                    FileName = mysqldumpPath,
                    Arguments = args.ToString(),
                    WorkingDirectory = Path.GetDirectoryName(mysqldumpPath) ?? _environment.ContentRootPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = new Process { StartInfo = startInfo };
                var stderr = new StringBuilder();
                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        stderr.AppendLine(e.Data);
                };

                if (!process.Start())
                {
                    throw new InvalidOperationException("backupProcessFailed");
                }

                process.BeginErrorReadLine();
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    _logger.LogError(
                        "mysqldump exited with code {ExitCode}. stderr: {Stderr}",
                        process.ExitCode,
                        stderr.ToString());
                    throw new InvalidOperationException("backupFailed");
                }

                if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
                {
                    throw new InvalidOperationException("backupEmpty");
                }

                var bytes = await File.ReadAllBytesAsync(tempFile, cancellationToken).ConfigureAwait(false);
                var fileName = $"litecashier-backup-{DateTime.Now:yyyyMMdd-HHmmss}.sql";
                return (bytes, fileName);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch
                {
                    // ignore cleanup failures
                }
            }
        }

        private string? ResolveMysqldumpPath()
        {
            var configured = _configuration["DatabaseSettings:MysqldumpPath"];
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            {
                return Path.GetFullPath(configured);
            }

            var candidates = new List<string>();

            // Installer layout: {app}\POS\  →  {app}\mariadb\bin\mysqldump.exe
            var contentRoot = _environment.ContentRootPath;
            candidates.Add(Path.GetFullPath(Path.Combine(contentRoot, "..", "mariadb", "bin", "mysqldump.exe")));
            candidates.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "mariadb", "bin", "mysqldump.exe")));
            candidates.Add(Path.Combine(contentRoot, "mariadb", "bin", "mysqldump.exe"));

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                candidates.Add(Path.Combine(programFiles, "Litecashier", "mariadb", "bin", "mysqldump.exe"));
            }

            // Common XAMPP locations
            candidates.Add(@"C:\xampp\mysql\bin\mysqldump.exe");
            candidates.Add(@"C:\XAMPP\mysql\bin\mysqldump.exe");
            var driveRoot = Path.GetPathRoot(Environment.SystemDirectory);
            if (!string.IsNullOrWhiteSpace(driveRoot))
            {
                candidates.Add(Path.Combine(driveRoot, "xampp", "mysql", "bin", "mysqldump.exe"));
            }

            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                candidates.Add(Path.Combine(programFiles, "MySQL", "MySQL Server 8.0", "bin", "mysqldump.exe"));
                candidates.Add(Path.Combine(programFiles, "MySQL", "MySQL Server 8.4", "bin", "mysqldump.exe"));
                candidates.Add(Path.Combine(programFiles, "MariaDB 11.4", "bin", "mysqldump.exe"));
            }

            foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Last resort: rely on PATH (dev machines with MariaDB/MySQL client tools)
            return FindExecutableOnPath("mysqldump");
        }

        private static string? FindExecutableOnPath(string fileName)
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim(), fileName);
                    if (File.Exists(candidate))
                        return candidate;

                    if (OperatingSystem.IsWindows())
                    {
                        var exe = Path.Combine(dir.Trim(), fileName + ".exe");
                        if (File.Exists(exe))
                            return exe;
                    }
                }
                catch
                {
                    // ignore invalid PATH entries
                }
            }

            return null;
        }

        private static string EscapeArg(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            if (value.IndexOfAny(new[] { ' ', '"', '\\' }) < 0)
                return value;

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}

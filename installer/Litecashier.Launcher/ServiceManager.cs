using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace Litecashier.Launcher;

internal static class ServiceManager
{
    private const string AppUrl = "http://localhost:5189/";
    private const string PosHealthUrl = "http://127.0.0.1:5189/";
    private const string PrintServerHealthUrl = "http://127.0.0.1:5000/swagger/index.html";

    private static readonly string InstallDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private static readonly string DataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Litecashier");
    private static readonly string MariaDbDir = Path.Combine(InstallDir, "mariadb");
    private static readonly string MariaDbBin = Path.Combine(MariaDbDir, "bin");
    private static readonly string MariaDbData = Path.Combine(DataRoot, "mariadb", "data");
    private static readonly string MariaDbIni = Path.Combine(DataRoot, "mariadb", "my.ini");
    private static readonly string LegacyMariaDbData = Path.Combine(MariaDbDir, "data");
    private static readonly string PosExe = Path.Combine(InstallDir, "POS", "POS.exe");
    private static readonly string PrintServerExe = Path.Combine(InstallDir, "PrintServer", "PrintServer.exe");
    private static readonly string LogsDir = Path.Combine(DataRoot, "Logs");

    public static string LogsDirectory => LogsDir;

    public static async Task EnsureRunningAsync(Action<string> setStatus)
    {
        Directory.CreateDirectory(LogsDir);
        Directory.CreateDirectory(Path.GetDirectoryName(MariaDbData)!);
        WriteStartupLog("Launcher started.");
        WriteStartupLog("Checking if services are already running...");

        var posHealthy = await IsEndpointHealthyAsync(PosHealthUrl).ConfigureAwait(false);
        WriteStartupLog($"POS health check: {posHealthy}");
        var printHealthy = await IsEndpointHealthyAsync(PrintServerHealthUrl).ConfigureAwait(false);
        WriteStartupLog($"PrintServer health check: {printHealthy}");

        if (posHealthy && printHealthy)
        {
            WriteStartupLog("Services already running.");
            return;
        }

        setStatus("جاري تشغيل قاعدة البيانات...");
        await EnsureMariaDbAsync(setStatus).ConfigureAwait(false);

        if (!await IsEndpointHealthyAsync(PrintServerHealthUrl).ConfigureAwait(false))
        {
            setStatus("جاري تشغيل خادم الطباعة...");
            StartBackgroundProcess(PrintServerExe, Path.Combine(InstallDir, "PrintServer"), "printserver.log");
            await WaitForHealthyAsync(PrintServerHealthUrl, TimeSpan.FromSeconds(90), setStatus, "خادم الطباعة").ConfigureAwait(false);
        }

        if (!await IsEndpointHealthyAsync(PosHealthUrl).ConfigureAwait(false))
        {
            setStatus("جاري تشغيل النظام...");
            StartBackgroundProcess(PosExe, Path.Combine(InstallDir, "POS"), "pos.log", "Production");
            await WaitForHealthyAsync(PosHealthUrl, TimeSpan.FromSeconds(180), setStatus, "النظام").ConfigureAwait(false);
        }

        WriteStartupLog("All services are ready.");
    }

    public static void OpenBrowser()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = AppUrl,
            UseShellExecute = true
        });
    }

    public static string BuildErrorMessage(Exception ex)
    {
        var builder = new StringBuilder();
        builder.AppendLine(ex.Message);
        builder.AppendLine();
        builder.AppendLine($"مجلد السجلات: {LogsDir}");

        AppendLogTail(builder, "startup.log", 20);
        AppendLogTail(builder, "mariadb-init.log", 20);
        AppendLogTail(builder, "mariadb.log", 20);
        AppendLogTail(builder, "pos.log", 20);
        AppendLogTail(builder, "printserver.log", 20);

        return builder.ToString().Trim();
    }

    private static async Task EnsureMariaDbAsync(Action<string> setStatus)
    {
        if (await IsPortOpenAsync(3306).ConfigureAwait(false))
        {
            WriteStartupLog("Port 3306 is already open.");
            return;
        }

        if (!File.Exists(Path.Combine(MariaDbBin, "mysqld.exe")))
        {
            throw new InvalidOperationException("ملفات MariaDB غير موجودة. أعد تثبيت البرنامج.");
        }

        MigrateLegacyMariaDbData();
        EnsureMariaDbConfig();

        if (!IsMariaDbInitialized())
        {
            PrepareMariaDbDataDirectory();
            setStatus("جاري تهيئة قاعدة البيانات لأول مرة...");
            WriteStartupLog("Initializing MariaDB data directory.");

            var initResult = RunMariaDbProcess(
                "--initialize-insecure",
                TimeSpan.FromMinutes(5),
                "mariadb-init.log");

            if (initResult.ExitCode != 0)
            {
                var details = string.IsNullOrWhiteSpace(initResult.LastOutput)
                    ? $"رمز الخطأ: {initResult.ExitCode}"
                    : initResult.LastOutput.Trim();

                throw new InvalidOperationException(
                    $"فشل تهيئة قاعدة البيانات (رمز {initResult.ExitCode}).{Environment.NewLine}{details}{Environment.NewLine}{Environment.NewLine}راجع mariadb-init.log في مجلد السجلات.");
            }
        }

        if (!await IsPortOpenAsync(3306).ConfigureAwait(false))
        {
            WriteStartupLog("Starting mysqld.");
            StartBackgroundProcess(
                Path.Combine(MariaDbBin, "mysqld.exe"),
                MariaDbDir,
                "mariadb.log",
                extraMariaDbArgs: "--standalone");

            await WaitForPortAsync(3306, TimeSpan.FromSeconds(90), setStatus).ConfigureAwait(false);
        }
    }

    private static void MigrateLegacyMariaDbData()
    {
        if (IsMariaDbInitialized() || !Directory.Exists(LegacyMariaDbData))
        {
            return;
        }

        if (!Directory.EnumerateFileSystemEntries(LegacyMariaDbData).Any())
        {
            return;
        }

        WriteStartupLog($"Migrating legacy MariaDB data from {LegacyMariaDbData}");
        Directory.CreateDirectory(MariaDbData);
        CopyDirectory(LegacyMariaDbData, MariaDbData);
    }

    private static bool IsMariaDbInitialized()
    {
        if (!Directory.Exists(MariaDbData))
        {
            return false;
        }

        return File.Exists(Path.Combine(MariaDbData, "ibdata1"))
            || Directory.Exists(Path.Combine(MariaDbData, "mysql"))
            || File.Exists(Path.Combine(MariaDbData, "aria_log_control"));
    }

    private static void PrepareMariaDbDataDirectory()
    {
        if (IsMariaDbInitialized())
        {
            return;
        }

        if (Directory.Exists(MariaDbData))
        {
            WriteStartupLog("Removing incomplete MariaDB data directory before initialize.");
            try
            {
                Directory.Delete(MariaDbData, recursive: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"تعذر تنظيف مجلد قاعدة البيانات التالف. احذف المجلد يدوياً ثم أعد المحاولة:{Environment.NewLine}{MariaDbData}",
                    ex);
            }
        }

        Directory.CreateDirectory(MariaDbData);
        Directory.CreateDirectory(Path.Combine(DataRoot, "mariadb", "tmp"));
    }

    private sealed class ProcessResult
    {
        public int ExitCode { get; init; }
        public string LastOutput { get; init; } = string.Empty;
    }

    private static void EnsureMariaDbConfig()
    {
        var configDir = Path.GetDirectoryName(MariaDbIni)!;
        Directory.CreateDirectory(configDir);
        Directory.CreateDirectory(Path.Combine(DataRoot, "mariadb", "tmp"));

        var basedir = ToIniPath(MariaDbDir);
        var datadir = ToIniPath(MariaDbData);
        var tmpdir = ToIniPath(Path.Combine(DataRoot, "mariadb", "tmp"));
        var content = string.Join(Environment.NewLine, new[]
        {
            "[mysqld]",
            $"basedir={basedir}",
            $"datadir={datadir}",
            $"tmpdir={tmpdir}",
            "port=3306",
            "bind-address=127.0.0.1",
            "character-set-server=utf8mb4",
            "collation-server=utf8mb4_unicode_ci",
            "skip-name-resolve=1",
            string.Empty,
            "[client]",
            "port=3306",
            "default-character-set=utf8mb4"
        });

        File.WriteAllText(MariaDbIni, content, Utf8WithoutBom);
        WriteStartupLog($"Wrote MariaDB config: {MariaDbIni}");
    }

    private static string ToIniPath(string path) => $"\"{path.Replace('\\', '/')}\"";

    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static void StartBackgroundProcess(string exePath, string workingDir, string logFileName, string? environment = null, string? extraMariaDbArgs = null)
    {
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"الملف غير موجود: {exePath}");
        }

        var logPath = Path.Combine(LogsDir, logFileName);
        var logStream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        var logWriter = new StreamWriter(logStream, Encoding.UTF8) { AutoFlush = true };

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (!string.IsNullOrWhiteSpace(extraMariaDbArgs))
        {
            ConfigureMariaDbArguments(startInfo, extraMariaDbArgs);
            logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting {Path.GetFileName(exePath)} {string.Join(' ', startInfo.ArgumentList)}");
        }
        else
        {
            logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting {Path.GetFileName(exePath)}");
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) logWriter.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) logWriter.WriteLine(e.Data); };
        process.Exited += (_, _) => logWriter.Dispose();
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        WriteStartupLog($"Started background process {Path.GetFileName(exePath)} (PID {process.Id}).");
    }

    private static ProcessResult RunMariaDbProcess(string extraArgument, TimeSpan timeout, string logFileName)
    {
        var logPath = Path.Combine(LogsDir, logFileName);
        using var logWriter = new StreamWriter(logPath, false, Utf8WithoutBom);

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(MariaDbBin, "mysqld.exe"),
            WorkingDirectory = MariaDbDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        ConfigureMariaDbArguments(startInfo, extraArgument);

        logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}");

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            output.AppendLine(e.Data);
            logWriter.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            output.AppendLine(e.Data);
            logWriter.WriteLine(e.Data);
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("تعذر تشغيل عملية تهيئة قاعدة البيانات.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException("انتهت مهلة تهيئة قاعدة البيانات.");
        }

        WriteStartupLog($"Process {Path.GetFileName(startInfo.FileName)} exited with code {process.ExitCode}.");
        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            LastOutput = output.ToString()
        };
    }

    private static void ConfigureMariaDbArguments(ProcessStartInfo startInfo, string extraArgument)
    {
        startInfo.ArgumentList.Clear();
        startInfo.ArgumentList.Add($"--defaults-file={MariaDbIni}");
        startInfo.ArgumentList.Add("--console");

        foreach (var part in extraArgument.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            startInfo.ArgumentList.Add(part);
        }
    }

    private static async Task WaitForHealthyAsync(string url, TimeSpan timeout, Action<string> setStatus, string serviceName)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await IsEndpointHealthyAsync(url).ConfigureAwait(false))
            {
                return;
            }

            setStatus($"بانتظار {serviceName}...");
            await Task.Delay(1000).ConfigureAwait(false);
        }

        throw new TimeoutException($"انتهت مهلة انتظار {serviceName}. راجع مجلد السجلات:\n{LogsDir}");
    }

    private static async Task WaitForPortAsync(int port, TimeSpan timeout, Action<string> setStatus)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await IsPortOpenAsync(port).ConfigureAwait(false))
            {
                return;
            }

            setStatus("بانتظار قاعدة البيانات...");
            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException($"انتهت مهلة انتظار قاعدة البيانات. راجع mariadb.log في:\n{LogsDir}");
    }

    private static async Task<bool> IsEndpointHealthyAsync(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync(url).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsPortOpenAsync(int port)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await client.ConnectAsync("127.0.0.1", port, cts.Token).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteStartupLog(string message)
    {
        try
        {
            Directory.CreateDirectory(LogsDir);
            File.AppendAllText(
                Path.Combine(LogsDir, "startup.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch
        {
            // ignore logging failures
        }
    }

    private static void AppendLogTail(StringBuilder builder, string fileName, int maxLines)
    {
        var path = Path.Combine(LogsDir, fileName);
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var lines = File.ReadLines(path).TakeLast(maxLines).ToArray();
            if (lines.Length == 0)
            {
                return;
            }

            builder.AppendLine();
            builder.AppendLine($"--- {fileName} ---");
            foreach (var line in lines)
            {
                builder.AppendLine(line);
            }
        }
        catch
        {
            // ignore
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourceDir, destinationDir, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var target = file.Replace(sourceDir, destinationDir, StringComparison.OrdinalIgnoreCase);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}

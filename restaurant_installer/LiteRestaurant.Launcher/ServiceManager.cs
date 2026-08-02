using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace LiteRestaurant.Launcher;

internal static class ServiceManager
{
    private const string AppUrl = "http://localhost:5189/";
    private const string ApiHealthUrl = "http://127.0.0.1:5189/";
    private const string PrintServerHealthUrl = "http://127.0.0.1:5000/swagger/index.html";

    private static readonly string InstallDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private static readonly string DataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "LiteRestaurant");
    private static readonly string ApiExe = Path.Combine(InstallDir, "RestaurantPOS", "RestaurantPOS.exe");
    private static readonly string PrintServerExe = Path.Combine(InstallDir, "PrintServer", "PrintServer.exe");
    private static readonly string LogsDir = Path.Combine(DataRoot, "Logs");

    public static string LogsDirectory => LogsDir;

    public static async Task EnsureRunningAsync(Action<string> setStatus)
    {
        Directory.CreateDirectory(LogsDir);
        WriteStartupLog("Launcher started.");
        WriteStartupLog("Database is external/manual (localhost / pos / root / password set). Launcher will not start MySQL/MariaDB.");
        WriteStartupLog("Checking if services are already running...");

        var apiHealthy = await IsEndpointHealthyAsync(ApiHealthUrl).ConfigureAwait(false);
        WriteStartupLog($"RestaurantPOS health check: {apiHealthy}");
        var printHealthy = await IsEndpointHealthyAsync(PrintServerHealthUrl).ConfigureAwait(false);
        WriteStartupLog($"PrintServer health check: {printHealthy}");

        if (apiHealthy && printHealthy)
        {
            WriteStartupLog("Services already running.");
            return;
        }

        if (!await IsEndpointHealthyAsync(PrintServerHealthUrl).ConfigureAwait(false))
        {
            setStatus("جاري تشغيل خادم الطباعة...");
            StartBackgroundProcess(PrintServerExe, Path.Combine(InstallDir, "PrintServer"), "printserver.log");
            await WaitForHealthyAsync(PrintServerHealthUrl, TimeSpan.FromSeconds(90), setStatus, "خادم الطباعة").ConfigureAwait(false);
        }

        if (!await IsEndpointHealthyAsync(ApiHealthUrl).ConfigureAwait(false))
        {
            setStatus("جاري تشغيل نظام المطاعم...");
            StartBackgroundProcess(ApiExe, Path.Combine(InstallDir, "RestaurantPOS"), "restaurantpos.log", "Production");
            await WaitForHealthyAsync(ApiHealthUrl, TimeSpan.FromSeconds(180), setStatus, "النظام").ConfigureAwait(false);
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
        builder.AppendLine("تأكد أن MySQL/XAMPP يعمل وأن قاعدة البيانات pos موجودة (مستخدم root وكلمة المرور المضبوطة في الإعدادات).");

        AppendLogTail(builder, "startup.log", 20);
        AppendLogTail(builder, "restaurantpos.log", 20);
        AppendLogTail(builder, "printserver.log", 20);

        return builder.ToString().Trim();
    }

    private static void StartBackgroundProcess(string exePath, string workingDir, string logFileName, string? environment = null)
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

        logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting {Path.GetFileName(exePath)}");

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
}

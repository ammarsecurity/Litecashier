using System.Runtime.Versioning;

namespace PrintServer.Services;

/// <summary>
/// Renders receipt HTML with CSS and prints via WebView2 on an STA thread with a WinForms message pump.
/// </summary>
[SupportedOSPlatform("windows")]
public static class HtmlWebViewPrinter
{
    public static bool TryPrint(string html, string? printerName, int timeoutMs = 90000)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(html))
            return false;

        bool? success = null;
        using var done = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var form = new ReceiptPrintForm(html, printerName, result =>
                {
                    success = result;
                    if (!done.IsSet)
                        done.Set();
                });

                Application.Run(form);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebView2 STA thread error: {ex.Message}");
                success = false;
                done.Set();
            }
        })
        {
            IsBackground = false
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!done.Wait(timeoutMs))
        {
            Console.WriteLine("ERROR: WebView2 print timed out (message pump)");
            return false;
        }

        return success == true;
    }
}

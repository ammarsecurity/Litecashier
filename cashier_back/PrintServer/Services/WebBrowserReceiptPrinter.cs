using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace PrintServer.Services;

/// <summary>
/// Fallback HTML print using the legacy WebBrowser control (requires STA + message pump).
/// </summary>
[SupportedOSPlatform("windows")]
public static class WebBrowserReceiptPrinter
{
    public static bool TryPrint(string html, string? printerName, int timeoutMs = 60000)
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

                var form = new WebBrowserPrintForm(html, printerName, result =>
                {
                    success = result;
                    if (!done.IsSet)
                        done.Set();
                });

                Application.Run(form);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebBrowser print thread error: {ex.Message}");
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
            Console.WriteLine("ERROR: WebBrowser print timed out");
            return false;
        }

        return success == true;
    }

    private sealed class WebBrowserPrintForm : Form
    {
        private readonly string _html;
        private readonly string? _printerName;
        private readonly Action<bool> _onComplete;
        private WebBrowser? _browser;
        private bool _completed;

        public WebBrowserPrintForm(string html, string? printerName, Action<bool> onComplete)
        {
            _html = html;
            _printerName = printerName;
            _onComplete = onComplete;

            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Size = new Size(400, 700);
            Opacity = 0;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-2000, -2000);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                _browser = new WebBrowser
                {
                    Dock = DockStyle.Fill,
                    ScriptErrorsSuppressed = true
                };
                Controls.Add(_browser);
                _browser.DocumentCompleted += OnDocumentCompleted;
                _browser.DocumentText = _html;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebBrowser init error: {ex.Message}");
                Finish(false);
            }
        }

        private void OnDocumentCompleted(object? sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (_completed || _browser == null || _browser.Url == null || !e.Url.Equals(_browser.Url))
                return;

            _completed = true;

            try
            {
                if (!string.IsNullOrWhiteSpace(_printerName))
                {
                    var printDoc = new PrintDocument();
                    printDoc.PrinterSettings.PrinterName = _printerName;
                    if (!printDoc.PrinterSettings.IsValid)
                    {
                        Console.WriteLine($"WebBrowser: printer not valid: {_printerName}");
                        Finish(false);
                        return;
                    }
                }

                _browser.Print();
                Console.WriteLine("WebBrowser Print() sent to spooler");
                Finish(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebBrowser Print failed: {ex.Message}");
                Finish(false);
            }
        }

        private void Finish(bool success)
        {
            try
            {
                _onComplete(success);
            }
            finally
            {
                BeginInvoke(() =>
                {
                    Close();
                    Application.ExitThread();
                });
            }
        }
    }
}

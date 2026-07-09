using System.Runtime.Versioning;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PrintServer.Services;

[SupportedOSPlatform("windows")]
internal sealed class ReceiptPrintForm : Form
{
    private readonly string _html;
    private readonly string? _printerName;
    private readonly Action<bool> _onComplete;
    private WebView2? _webView;
    private bool _printStarted;

    public ReceiptPrintForm(string html, string? printerName, Action<bool> onComplete)
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

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        try
        {
            _webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_webView);

            var userDataFolder = Path.Combine(
                Path.GetTempPath(),
                "LitecashierPrintServer",
                "WebView2");

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);

            await _webView.EnsureCoreWebView2Async(env);

            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            var tempFile = Path.Combine(Path.GetTempPath(), $"receipt_{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(tempFile, _html, System.Text.Encoding.UTF8);

            var fileUri = new Uri(tempFile).AbsoluteUri;
            Console.WriteLine($"WebView2 navigating to {fileUri}");
            _webView.CoreWebView2.Navigate(fileUri);

            _ = Task.Run(async () =>
            {
                await Task.Delay(90000);
                if (!_printStarted)
                {
                    Console.WriteLine("ERROR: WebView2 navigation/print timed out waiting for page");
                    Finish(false);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReceiptPrintForm init error: {ex.Message}");
            Finish(false);
        }
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_printStarted)
            return;

        if (!e.IsSuccess)
        {
            Console.WriteLine($"WebView2 navigation failed: {e.WebErrorStatus}");
            Finish(false);
            return;
        }

        _printStarted = true;

        try
        {
            await Task.Delay(800);

            var settings = _webView!.CoreWebView2.Environment.CreatePrintSettings();
            if (!string.IsNullOrWhiteSpace(_printerName))
                settings.PrinterName = _printerName;

            settings.ShouldPrintBackgrounds = true;
            settings.ShouldPrintHeaderAndFooter = false;
                // Margins in inches — inset from non-printable edges on 80mm thermal
                settings.MarginTop = 0.08;
                settings.MarginBottom = 0.08;
                settings.MarginLeft = 0.16;
                settings.MarginRight = 0.12;
                settings.PageWidth = 2.83;
                settings.PageHeight = 11;

            Console.WriteLine($"WebView2 printing to: {_printerName ?? "(default)"}");
            await _webView.CoreWebView2.PrintAsync(settings);
            Console.WriteLine("WebView2 PrintAsync completed");
            Finish(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebView2 PrintAsync failed: {ex.Message}");
            Finish(false);
        }
    }

    private void Finish(bool success)
    {
        if (IsDisposed)
            return;

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

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        _webView?.Dispose();
    }
}

using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PrintServer.Models;

namespace PrintServer.Services;

public class PrintService
{
    private readonly ConfigurationService _configService;

    public PrintService(ConfigurationService configService)
    {
        _configService = configService;
    }

    public bool PrintHtmlContent(string htmlContent, string? printerName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                Console.WriteLine("ERROR: HTML content is empty or null");
                return false;
            }

            var config = _configService.GetConfig();
            if (config.Type != "windows")
                return false;

            htmlContent = ReceiptPrintStyles.EnsureFullDocument(htmlContent);
            Console.WriteLine($"Prepared receipt HTML document. Length: {htmlContent.Length} characters");

            if (OperatingSystem.IsWindows())
            {
                Console.WriteLine("Attempting WebView2 styled HTML print...");
                if (HtmlWebViewPrinter.TryPrint(htmlContent, printerName))
                {
                    Console.WriteLine("HTML printed successfully via WebView2");
                    return true;
                }

                Console.WriteLine("WebView2 print failed, trying legacy WebBrowser...");
                if (WebBrowserReceiptPrinter.TryPrint(htmlContent, printerName))
                {
                    Console.WriteLine("HTML printed successfully via WebBrowser");
                    return true;
                }

                Console.WriteLine("WebBrowser print failed, trying Edge print...");
                if (PrintHtmlViaEdge(htmlContent, printerName))
                {
                    Console.WriteLine("HTML printed successfully via Edge");
                    return true;
                }
            }

            Console.WriteLine("ERROR: Could not print styled receipt HTML. Install WebView2 Runtime if missing.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error printing HTML content: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private bool PrintHtmlViaEdge(string htmlContent, string? printerName = null)
    {
        try
        {
            var edgePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
            };

            var edgeExe = edgePaths.FirstOrDefault(File.Exists);
            if (edgeExe == null)
            {
                Console.WriteLine("Edge not found for HTML print fallback");
                return false;
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"receipt_{Guid.NewGuid():N}.html");
            File.WriteAllText(tempFile, htmlContent, Encoding.UTF8);

            var args = $"--headless --disable-gpu --no-pdf-header-footer --print \"{tempFile}\"";
            if (!string.IsNullOrWhiteSpace(printerName))
                args += $" --printer=\"{printerName}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = edgeExe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            if (!process.WaitForExit(60000))
            {
                try { process.Kill(true); } catch { }
                Console.WriteLine("Edge headless print timed out");
                return false;
            }

            Task.Run(async () =>
            {
                await Task.Delay(8000);
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            });

            var ok = process.ExitCode == 0;
            if (!ok)
                Console.WriteLine($"Edge print exit code: {process.ExitCode}");

            return ok;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Edge HTML print error: {ex.Message}");
            return false;
        }
    }

    public bool PrintWithWindows(string textContent, string? printerName = null)
    {
        Console.WriteLine($"PrintWithWindows called. Text content length: {textContent?.Length ?? 0}");
        Console.WriteLine($"Printer name parameter: {printerName}");

        if (string.IsNullOrWhiteSpace(textContent))
        {
            Console.WriteLine("ERROR: Text content is empty!");
            return false;
        }

        try
        {
            var config = _configService.GetConfig();
            var targetPrinter = printerName ?? config.WindowsPrinterName;

            if (string.IsNullOrEmpty(targetPrinter))
            {
                try
                {
                    targetPrinter = GetDefaultPrinter();
                    Console.WriteLine($"Got default printer: {targetPrinter}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Error getting default printer: {ex.Message}");
                    var printers = GetAvailablePrinters();
                    if (printers.Any())
                    {
                        targetPrinter = printers.First();
                        Console.WriteLine($"Using first available printer: {targetPrinter}");
                    }
                    else
                    {
                        Console.WriteLine("ERROR: No printers found on system");
                        return false;
                    }
                }
            }

            if (string.IsNullOrEmpty(targetPrinter))
            {
                Console.WriteLine("ERROR: No printer name available");
                return false;
            }

            Console.WriteLine($"Attempting to print to: {targetPrinter}");

            // Verify printer exists
            var availablePrinters = GetAvailablePrinters();
            if (!availablePrinters.Contains(targetPrinter))
            {
                Console.WriteLine($"ERROR: Printer '{targetPrinter}' not found in available printers");
                return false;
            }

            Console.WriteLine($"Printer '{targetPrinter}' found in available printers");

            // Fix Arabic text
            textContent = FixArabicText(textContent);

            // Convert text to bytes with proper encoding
            byte[]? textBytes = null;
            var hasArabic = textContent.Any(c => c >= '\u0600' && c <= '\u06FF');
            Console.WriteLine($"Text contains Arabic: {hasArabic}");

            // Try multiple encoding strategies for Arabic text
            if (hasArabic)
            {
                // Strategy 1: Try Windows-1256 (most common for Arabic thermal printers)
                try
                {
                    var encoding = Encoding.GetEncoding("windows-1256");
                    textBytes = encoding.GetBytes(textContent);
                    Console.WriteLine("Successfully encoded Arabic text with Windows-1256");
                    config.EscPosEncoding = 17; // Windows-1256
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed Windows-1256 encoding: {ex.Message}");
                }

                // Strategy 2: Try CP1256 (alternative Arabic encoding)
                if (textBytes == null)
                {
                    try
                    {
                        var encoding = Encoding.GetEncoding("cp1256");
                        textBytes = encoding.GetBytes(textContent);
                        Console.WriteLine("Successfully encoded Arabic text with CP1256");
                        config.EscPosEncoding = 17; // Windows-1256
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed CP1256 encoding: {ex.Message}");
                    }
                }

                // Strategy 3: Try UTF-8 (some modern printers support this)
                if (textBytes == null)
                {
                    try
                    {
                        textBytes = Encoding.UTF8.GetBytes(textContent);
                        Console.WriteLine("Successfully encoded Arabic text with UTF-8");
                        config.EscPosEncoding = 16; // UTF-8
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed UTF-8 encoding: {ex.Message}");
                    }
                }

                // Strategy 4: Last resort - use configured encoding
                if (textBytes == null)
                {
                    try
                    {
                        textBytes = Encoding.GetEncoding(config.Encoding).GetBytes(textContent);
                        Console.WriteLine($"Using configured encoding: {config.Encoding}");
                    }
                    catch
                    {
                        // Final fallback
                        textBytes = Encoding.UTF8.GetBytes(textContent);
                        Console.WriteLine("Final fallback: Using UTF-8");
                    }
                }
            }
            else
            {
                // For non-Arabic text, use configured encoding
                try
                {
                    textBytes = Encoding.GetEncoding(config.Encoding).GetBytes(textContent);
                    Console.WriteLine($"Successfully encoded with configured encoding: {config.Encoding}");
                }
                catch
                {
                    textBytes = Encoding.UTF8.GetBytes(textContent);
                    Console.WriteLine("Fallback: Using UTF-8 encoding");
                }
            }

            // Ensure textBytes is not null (final safety check)
            if (textBytes == null)
            {
                textBytes = Encoding.UTF8.GetBytes(textContent);
                Console.WriteLine("Final safety: Using UTF-8 encoding");
            }

            // Add ESC/POS commands if enabled
            byte[] finalPrintData;
            if (config.UseEscPosCommands)
            {
                var escPosInit = new byte[] { 0x1B, 0x40 }; // ESC @ - Initialize printer
                
                // Set character code table based on encoding
                byte encodingCode = (byte)config.EscPosEncoding;
                var escPosEncoding = new byte[] { 0x1B, 0x74, encodingCode }; // ESC t [code]
                Console.WriteLine($"Using ESC/POS encoding code: {encodingCode}");
                
                // For UTF-8, some printers need additional command
                var utf8Mode = encodingCode == 16 ? new byte[] { 0x1B, 0x25, 0x01 } : new byte[] { }; // ESC % 1 - Enable UTF-8
                
                // DO NOT use ESC R 8 for Arabic - it may cause issues with some printers
                // Most printers handle Arabic correctly with just the encoding command
                
                var escPosLeft = new byte[] { 0x1B, 0x61, 0x00 }; // ESC a 0 - Left align
                var cutCommand = new byte[] { 0x0A, 0x0A, 0x0A, 0x1D, 0x56, 0x41, 0x03 }; // GS V A 3 - Full cut

                var combined = new List<byte>();
                combined.AddRange(escPosInit);
                combined.AddRange(escPosEncoding);
                if (encodingCode == 16) // UTF-8
                {
                    combined.AddRange(utf8Mode);
                }
                combined.AddRange(escPosLeft);
                combined.AddRange(textBytes);
                combined.AddRange(cutCommand);
                finalPrintData = combined.ToArray();
            }
            else
            {
                var combined = new List<byte>();
                combined.AddRange(textBytes);
                combined.AddRange(new byte[] { 0x0A, 0x0A, 0x0A });
                finalPrintData = combined.ToArray();
            }

            Console.WriteLine($"Starting print job. Data length: {finalPrintData.Length} bytes");
            Console.WriteLine($"Use ESC/POS: {config.UseEscPosCommands}");

            // For Arabic text with thermal printers, try both methods
            // Some thermal printers don't support Arabic via RAW printing properly
            if (hasArabic)
            {
                Console.WriteLine("Arabic text detected - trying PrintDocument method first for better font support");
                var docSuccess = PrintWithDocument(textContent, targetPrinter);
                if (docSuccess)
                {
                    return true;
                }
                Console.WriteLine("PrintDocument method failed, trying RAW printing...");
            }

            // Try Raw printing (for thermal printers)
            var success = RawPrinterHelper.SendBytesToPrinter(targetPrinter, finalPrintData);
            
            if (success)
            {
                Console.WriteLine("SUCCESS: Print job sent successfully via RAW");
                // Small delay to allow print job to process
                Thread.Sleep(500);
                return true;
            }
            else
            {
                Console.WriteLine("ERROR: Both RAW and PrintDocument methods failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Windows print error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return false;
        }
    }

    public string HtmlToText(string htmlContent)
    {
        try
        {
            // Remove script and style tags
            htmlContent = Regex.Replace(htmlContent, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlContent = Regex.Replace(htmlContent, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Process tables
            var tablePattern = @"<table[^>]*>(.*?)</table>";
            var tables = Regex.Matches(htmlContent, tablePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            foreach (Match tableMatch in tables.Cast<Match>().Reverse())
            {
                var tableHtml = tableMatch.Groups[1].Value;
                var tableRows = new List<string>();

                // Extract header row
                var headerMatch = Regex.Match(tableHtml, @"<thead[^>]*>(.*?)</thead>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (headerMatch.Success)
                {
                    var headerHtml = headerMatch.Groups[1].Value;
                    var headerCells = Regex.Matches(headerHtml, @"<th[^>]*>(.*?)</th>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (headerCells.Count > 0)
                    {
                        var cleanHeaderCells = headerCells.Cast<Match>()
                            .Select(m => Regex.Replace(m.Groups[1].Value, @"<[^>]+>", "").Trim())
                            .ToList();
                        tableRows.Add(string.Join(" | ", cleanHeaderCells));
                        tableRows.Add(new string('-', 48));
                    }
                }

                // Extract body rows
                var tbodyMatch = Regex.Match(tableHtml, @"<tbody[^>]*>(.*?)</tbody>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (tbodyMatch.Success)
                {
                    var tbodyHtml = tbodyMatch.Groups[1].Value;
                    var rowMatches = Regex.Matches(tbodyHtml, @"<tr[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    foreach (Match rowMatch in rowMatches.Cast<Match>())
                    {
                        var rowHtml = rowMatch.Groups[1].Value;
                        var cells = Regex.Matches(rowHtml, @"<td[^>]*>(.*?)</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        if (cells.Count > 0)
                        {
                            var cellTexts = cells.Cast<Match>()
                                .Select(m => Regex.Replace(m.Groups[1].Value, @"<[^>]+>", "").Trim())
                                .ToList();

                            if (cellTexts.Count >= 4)
                            {
                                var itemName = (cellTexts[0].Length > 18 ? cellTexts[0].Substring(0, 18) : cellTexts[0]).PadRight(18);
                                var qty = (cellTexts[1].Length > 6 ? cellTexts[1].Substring(0, 6) : cellTexts[1]).PadLeft(6);
                                var price = (cellTexts[2].Length > 10 ? cellTexts[2].Substring(0, 10) : cellTexts[2]).PadLeft(10);
                                var total = (cellTexts[3].Length > 10 ? cellTexts[3].Substring(0, 10) : cellTexts[3]).PadLeft(10);
                                tableRows.Add($"{itemName} | {qty} | {price} | {total}");
                            }
                            else
                            {
                                tableRows.Add(string.Join(" | ", cellTexts));
                            }
                        }
                    }
                }

                var tableText = string.Join("\n", tableRows);
                htmlContent = htmlContent.Substring(0, tableMatch.Index) + "\n" + tableText + "\n" + htmlContent.Substring(tableMatch.Index + tableMatch.Length);
            }

            // Convert common HTML elements to text
            htmlContent = Regex.Replace(htmlContent, @"<h[1-6][^>]*>(.*?)</h[1-6]>", "$1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlContent = Regex.Replace(htmlContent, @"<img[^>]*>", "", RegexOptions.IgnoreCase);
            htmlContent = Regex.Replace(htmlContent, @"<p[^>]*>", "\n", RegexOptions.IgnoreCase);
            htmlContent = Regex.Replace(htmlContent, @"</p>", "\n", RegexOptions.IgnoreCase);
            htmlContent = Regex.Replace(htmlContent, @"<div[^>]*class=""bill-divider""[^>]*>", "\n" + new string('-', 48) + "\n", RegexOptions.IgnoreCase);
            htmlContent = Regex.Replace(htmlContent, @"<div[^>]*>", "\n", RegexOptions.IgnoreCase);
            htmlContent = Regex.Replace(htmlContent, @"</div>", "\n", RegexOptions.IgnoreCase);
            htmlContent = Regex.Replace(htmlContent, @"<span[^>]*class=""bill-info-label""[^>]*>(.*?)</span>", "$1:", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlContent = Regex.Replace(htmlContent, @"<span[^>]*class=""bill-info-value""[^>]*>(.*?)</span>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlContent = Regex.Replace(htmlContent, @"<br[^>]*>", "\n", RegexOptions.IgnoreCase);
            htmlContent = Regex.Replace(htmlContent, @"<strong[^>]*>(.*?)</strong>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlContent = Regex.Replace(htmlContent, @"<b[^>]*>(.*?)</b>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlContent = Regex.Replace(htmlContent, @"<h2[^>]*>(.*?)</h2>", "\n$1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlContent = Regex.Replace(htmlContent, @"<h3[^>]*>(.*?)</h3>", "\n$1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlContent = Regex.Replace(htmlContent, @"<[^>]+>", "");
            htmlContent = System.Net.WebUtility.HtmlDecode(htmlContent);

            // Clean up whitespace
            var lines = htmlContent.Split('\n');
            var cleanedLines = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    cleanedLines.Add(trimmed);
                }
                else if (cleanedLines.Count > 0 && !string.IsNullOrEmpty(cleanedLines.Last()))
                {
                    cleanedLines.Add("");
                }
            }

            // Remove multiple consecutive empty lines
            var finalLines = new List<string>();
            int emptyCount = 0;
            foreach (var line in cleanedLines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    finalLines.Add(line);
                    emptyCount = 0;
                }
                else
                {
                    emptyCount++;
                    if (emptyCount <= 2)
                    {
                        finalLines.Add("");
                    }
                }
            }

            var textContent = string.Join("\n", finalLines);
            Console.WriteLine($"HTML converted to text successfully. Length: {textContent.Length} chars");
            Console.WriteLine($"First 300 chars: {textContent.Substring(0, Math.Min(300, textContent.Length))}");
            return textContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting HTML to text: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return "";
        }
    }

    private string FixArabicText(string text)
    {
        try
        {
            var hasArabic = text.Any(c => c >= '\u0600' && c <= '\u06FF');
            if (!hasArabic)
            {
                return text;
            }

            // Remove problematic characters
            text = text.Replace("\u0640", ""); // Tatweel (kashida)
            text = text.Replace("\u200C", ""); // Zero-width non-joiner
            text = text.Replace("\u200D", ""); // Zero-width joiner

            return text;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error fixing Arabic text: {ex.Message}");
            return text;
        }
    }

    [SupportedOSPlatform("windows")]
    public List<string> GetAvailablePrinters()
    {
        var printers = new List<string>();
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            printers.Add(printer);
        }
        return printers;
    }

    [SupportedOSPlatform("windows")]
    public string GetDefaultPrinter()
    {
        var printDoc = new PrintDocument();
        return printDoc.PrinterSettings.PrinterName;
    }

    [SupportedOSPlatform("windows")]
    private bool PrintWithDocument(string textContent, string printerName)
    {
        try
        {
            Console.WriteLine("Attempting to print using PrintDocument with Arabic font support...");
            
            var printDoc = new PrintDocument
            {
                PrinterSettings = { PrinterName = printerName }
            };

            var lines = textContent.Split('\n');
            var printData = lines;
            var printSuccess = false;

            printDoc.PrintPage += (sender, e) =>
            {
                var graphics = e.Graphics!;
                
                // Try to use a font that supports Arabic
                Font? font = null;
                try
                {
                    // Try Arabic fonts first
                    string[] arabicFonts = { "Arial", "Tahoma", "Times New Roman", "Courier New" };
                    foreach (var fontName in arabicFonts)
                    {
                        try
                        {
                            font = new Font(fontName, 9, FontStyle.Regular);
                            break;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    
                    if (font == null)
                    {
                        font = new Font("Courier New", 9);
                    }
                }
                catch
                {
                    font = new Font("Courier New", 9);
                }

                float y = 0;
                float lineHeight = font.GetHeight(graphics);
                float pageHeight = e.MarginBounds.Height;

                foreach (var line in printData)
                {
                    if (y + lineHeight > pageHeight)
                    {
                        e.HasMorePages = true;
                        return;
                    }

                    graphics.DrawString(line, font, Brushes.Black, 0, y);
                    y += lineHeight;
                }

                e.HasMorePages = false;
                printSuccess = true;
            };

            printDoc.Print();
            
            if (printSuccess)
            {
                Console.WriteLine("SUCCESS: Print job sent successfully via PrintDocument");
                Thread.Sleep(500);
                return true;
            }
            else
            {
                Console.WriteLine("ERROR: PrintDocument print failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: PrintDocument error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return false;
        }
    }

    public string FormatReceipt(PrintRequest data)
    {
        // Generate modern HTML receipt with logo and QR code
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html dir='rtl' lang='ar'>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='UTF-8'>");
        html.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        html.AppendLine("<title>فاتورة</title>");
        html.AppendLine(ReceiptPrintStyles.CairoFontLink);
        html.AppendLine("<style>");
        html.AppendLine("* { margin: 0; padding: 0; box-sizing: border-box; }");
        html.AppendLine("body { font-family: 'Cairo', 'Arial', sans-serif; direction: rtl; font-size: 12px; line-height: 1.6; color: #000; background: #fff; padding: 10mm; width: 80mm; margin: 0 auto; }");
        html.AppendLine(".receipt-container { width: 100%; max-width: 80mm; margin: 0 auto; }");
        html.AppendLine(".receipt-header { text-align: center; margin-bottom: 15px; padding-bottom: 15px; border-bottom: 2px dashed #333; }");
        html.AppendLine(".receipt-logo { max-width: 80px; height: auto; margin-bottom: 10px; display: block; margin-left: auto; margin-right: auto; }");
        html.AppendLine(".receipt-title { font-size: 24px; font-weight: 800; margin: 10px 0; color: #1a1a1a; letter-spacing: 1px; }");
        html.AppendLine(".store-info { text-align: center; margin-bottom: 15px; }");
        html.AppendLine(".store-name { font-size: 18px; font-weight: 700; margin: 5px 0; color: #000; }");
        html.AppendLine(".store-details { font-size: 11px; color: #666; margin: 3px 0; }");
        html.AppendLine(".receipt-divider { border: none; border-top: 1px dashed #ccc; margin: 15px 0; }");
        html.AppendLine(".order-info { margin: 15px 0; font-size: 11px; }");
        html.AppendLine(".info-row { display: flex; justify-content: space-between; margin-bottom: 8px; padding: 4px 0; }");
        html.AppendLine(".info-label { font-weight: 600; color: #333; }");
        html.AppendLine(".info-value { font-weight: 400; color: #000; }");
        html.AppendLine(".items-table { width: 100%; border-collapse: collapse; margin: 15px 0; font-size: 11px; }");
        html.AppendLine(".items-table thead { background: #f5f5f5; border-bottom: 2px solid #333; }");
        html.AppendLine(".items-table th { padding: 8px 4px; text-align: right; font-weight: 700; font-size: 11px; }");
        html.AppendLine(".items-table td { padding: 6px 4px; text-align: right; border-bottom: 1px dotted #ddd; }");
        html.AppendLine(".items-table .col-item { width: 45%; text-align: right; }");
        html.AppendLine(".items-table .col-qty { width: 15%; text-align: center; }");
        html.AppendLine(".items-table .col-price { width: 20%; text-align: left; }");
        html.AppendLine(".items-table .col-total { width: 20%; text-align: left; font-weight: 600; }");
        html.AppendLine(".totals-section { margin-top: 15px; padding-top: 15px; border-top: 2px solid #333; font-size: 12px; }");
        html.AppendLine(".total-row { display: flex; justify-content: space-between; margin-bottom: 8px; padding: 4px 0; }");
        html.AppendLine(".total-label { font-weight: 600; }");
        html.AppendLine(".total-value { font-weight: 400; }");
        html.AppendLine(".total-final { margin-top: 10px; padding-top: 10px; border-top: 1px dashed #333; font-size: 16px; font-weight: 800; }");
        html.AppendLine(".payment-section { margin-top: 15px; padding-top: 15px; border-top: 1px dashed #ccc; font-size: 12px; }");
        html.AppendLine(".qr-section { text-align: center; margin: 20px 0; padding: 15px 0; border-top: 1px dashed #ccc; border-bottom: 1px dashed #ccc; }");
        html.AppendLine(".qr-code { max-width: 120px; height: auto; margin: 10px auto; display: block; }");
        html.AppendLine(".receipt-footer { text-align: center; margin-top: 20px; padding-top: 15px; border-top: 2px dashed #333; font-size: 12px; }");
        html.AppendLine(".footer-text { font-weight: 600; margin: 5px 0; }");
        html.AppendLine("@media print { body { padding: 0; } .receipt-container { width: 80mm; } }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class='receipt-container'>");
        
        // Header with Logo
        html.AppendLine("<div class='receipt-header'>");
        if (!string.IsNullOrEmpty(data.Logo))
        {
            html.AppendLine($"<img src='{data.Logo}' alt='Logo' class='receipt-logo' />");
        }
        html.AppendLine("<h1 class='receipt-title'>فاتورة</h1>");
        html.AppendLine("</div>");
        
        // Store Info
        html.AppendLine("<div class='store-info'>");
        if (!string.IsNullOrEmpty(data.StoreName))
        {
            html.AppendLine($"<div class='store-name'>{EscapeHtml(data.StoreName)}</div>");
        }
        if (!string.IsNullOrEmpty(data.StoreAddress))
        {
            html.AppendLine($"<div class='store-details'>{EscapeHtml(data.StoreAddress)}</div>");
        }
        if (!string.IsNullOrEmpty(data.StorePhone))
        {
            html.AppendLine($"<div class='store-details'>Tel: {EscapeHtml(data.StorePhone)}</div>");
        }
        html.AppendLine("</div>");
        
        html.AppendLine("<hr class='receipt-divider' />");
        
        // Order Info
        html.AppendLine("<div class='order-info'>");
        if (!string.IsNullOrEmpty(data.OrderCode))
        {
            html.AppendLine($"<div class='info-row'><span class='info-label'>رقم الفاتورة:</span><span class='info-value'>{EscapeHtml(data.OrderCode)}</span></div>");
        }
        if (!string.IsNullOrEmpty(data.Date))
        {
            html.AppendLine($"<div class='info-row'><span class='info-label'>التاريخ:</span><span class='info-value'>{EscapeHtml(data.Date)}</span></div>");
        }
        if (!string.IsNullOrEmpty(data.Time))
        {
            html.AppendLine($"<div class='info-row'><span class='info-label'>الوقت:</span><span class='info-value'>{EscapeHtml(data.Time)}</span></div>");
        }
        if (!string.IsNullOrEmpty(data.TableNumber))
        {
            html.AppendLine($"<div class='info-row'><span class='info-label'>الطاولة:</span><span class='info-value'>{EscapeHtml(data.TableNumber)}</span></div>");
        }
        if (!string.IsNullOrEmpty(data.EmployeeName))
        {
            html.AppendLine($"<div class='info-row'><span class='info-label'>الموظف:</span><span class='info-value'>{EscapeHtml(data.EmployeeName)}</span></div>");
        }
        html.AppendLine("</div>");
        
        html.AppendLine("<hr class='receipt-divider' />");
        
        // Items Table
        html.AppendLine("<table class='items-table'>");
        html.AppendLine("<thead>");
        html.AppendLine("<tr>");
        html.AppendLine("<th class='col-item'>المنتج</th>");
        html.AppendLine("<th class='col-qty'>الكمية</th>");
        html.AppendLine("<th class='col-price'>السعر</th>");
        html.AppendLine("<th class='col-total'>الإجمالي</th>");
        html.AppendLine("</tr>");
        html.AppendLine("</thead>");
        html.AppendLine("<tbody>");
        
        if (data.Items != null)
        {
            foreach (var item in data.Items)
            {
                var itemTotal = item.Quantity * item.Price;
                if (item.Discount.HasValue && item.Discount.Value > 0)
                {
                    itemTotal = item.Quantity * (item.Price - item.Discount.Value);
                }
                
                html.AppendLine("<tr>");
                html.AppendLine($"<td class='col-item'>{EscapeHtml(item.Name ?? "")}</td>");
                html.AppendLine($"<td class='col-qty'>{item.Quantity}</td>");
                html.AppendLine($"<td class='col-price'>{item.Price:F2}</td>");
                html.AppendLine($"<td class='col-total'>{itemTotal:F2}</td>");
                html.AppendLine("</tr>");
                
                if (item.Discount.HasValue && item.Discount.Value > 0)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td colspan='4' style='font-size: 10px; color: #666; padding-right: 20px;'>خصم: {item.Discount.Value:F2}</td>");
                    html.AppendLine("</tr>");
                }
            }
        }
        
        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
        
        html.AppendLine("<hr class='receipt-divider' />");
        
        // Totals
        html.AppendLine("<div class='totals-section'>");
        if (!string.IsNullOrEmpty(data.Subtotal))
        {
            html.AppendLine($"<div class='total-row'><span class='total-label'>المجموع الفرعي:</span><span class='total-value'>{EscapeHtml(data.Subtotal)}</span></div>");
        }
        if (!string.IsNullOrEmpty(data.Discount) && data.Discount != "0")
        {
            html.AppendLine($"<div class='total-row'><span class='total-label'>الخصم:</span><span class='total-value'>{EscapeHtml(data.Discount)}</span></div>");
        }
        if (!string.IsNullOrEmpty(data.Tax) && data.Tax != "0")
        {
            html.AppendLine($"<div class='total-row'><span class='total-label'>الضريبة:</span><span class='total-value'>{EscapeHtml(data.Tax)}</span></div>");
        }
        if (!string.IsNullOrEmpty(data.Total))
        {
            html.AppendLine($"<div class='total-row total-final'><span class='total-label'>الإجمالي:</span><span class='total-value'>{EscapeHtml(data.Total)}</span></div>");
        }
        html.AppendLine("</div>");
        
        // Payment Method
        if (!string.IsNullOrEmpty(data.PaymentMethod))
        {
            html.AppendLine("<div class='payment-section'>");
            html.AppendLine($"<div class='info-row'><span class='info-label'>طريقة الدفع:</span><span class='info-value'>{EscapeHtml(data.PaymentMethod)}</span></div>");
            html.AppendLine("</div>");
        }
        
        // QR Code
        if (!string.IsNullOrEmpty(data.QrCode))
        {
            html.AppendLine("<div class='qr-section'>");
            html.AppendLine($"<img src='{data.QrCode}' alt='QR Code' class='qr-code' />");
            html.AppendLine("</div>");
        }
        
        // Footer
        html.AppendLine("<div class='receipt-footer'>");
        html.AppendLine("<div class='footer-text'>شكراً لزيارتكم</div>");
        html.AppendLine("<div class='footer-text receipt-credit'>نظام لايت كاشير - برمجة وتصميم عمار الاصفر</div>");
        html.AppendLine("<div class='footer-text receipt-credit-phone'>07830200030</div>");
        html.AppendLine("</div>");
        
        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }
    
    private string EscapeHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        
        return System.Net.WebUtility.HtmlEncode(text);
    }

    private string CenterText(string text, int width)
    {
        if (text.Length >= width)
        {
            return text.Substring(0, width);
        }
        int padding = (width - text.Length) / 2;
        return new string(' ', padding) + text + new string(' ', width - text.Length - padding);
    }
}


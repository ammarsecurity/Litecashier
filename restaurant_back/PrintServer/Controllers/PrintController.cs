using Microsoft.AspNetCore.Mvc;
using PrintServer.Models;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("[controller]")]
public class PrintController : ControllerBase
{
    private readonly PrintService _printService;
    private readonly ConfigurationService _configService;
    private readonly ILogger<PrintController> _logger;

    public PrintController(PrintService printService, ConfigurationService configService, ILogger<PrintController> logger)
    {
        _printService = printService;
        _configService = configService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PrintReceipt([FromBody] object? requestData)
    {
        try
        {
            _logger.LogInformation("Received print request");
            _logger.LogInformation($"Request data type: {requestData?.GetType().Name}");
            _logger.LogInformation($"Request data: {System.Text.Json.JsonSerializer.Serialize(requestData)}");

            if (requestData == null)
            {
                _logger.LogWarning("Print request data is null");
                return BadRequest(new { error = "No data provided" });
            }

            // Deserialize to dynamic first to handle both camelCase and PascalCase
            var jsonString = System.Text.Json.JsonSerializer.Serialize(requestData);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };
            options.Converters.Add(new Models.FlexibleDecimalJsonConverter());
            options.Converters.Add(new Models.FlexibleNullableDecimalJsonConverter());
            
            var data = System.Text.Json.JsonSerializer.Deserialize<PrintRequest>(jsonString, options);
            
            if (data == null)
            {
                _logger.LogWarning("Failed to deserialize print request");
                return BadRequest(new { error = "Invalid request data format" });
            }

            var printerName = data.PrinterName;
            var printerType = data.PrinterType ?? "windows";

            _logger.LogInformation($"Printer Name: {printerName}");
            _logger.LogInformation($"Printer Type: {printerType}");
            _logger.LogInformation($"Has HTML Content: {!string.IsNullOrWhiteSpace(data.HtmlContent)}");
            _logger.LogInformation($"HTML Content Length: {data.HtmlContent?.Length ?? 0}");

            var htmlContent = data.HtmlContent;

            bool success;

            if (!string.IsNullOrWhiteSpace(htmlContent))
            {
                _logger.LogInformation("Using HTML content for printing");
                success = _printService.PrintHtmlContent(htmlContent, printerName);
            }
            else
            {
                _logger.LogInformation("No HTML content, formatting receipt from JSON data as HTML");
                var receiptHtml = ReceiptPrintStyles.EnsureFullDocument(_printService.FormatReceipt(data));
                _logger.LogInformation($"Formatted receipt HTML length: {receiptHtml.Length} characters");
                success = _printService.PrintHtmlContent(receiptHtml, printerName);
            }

            if (success)
            {
                _logger.LogInformation("PRINT SUCCESS - Job sent to printer queue");
                return Ok(new
                {
                    success = true,
                    message = "Receipt printed successfully",
                    note = "Print job sent to printer queue. If nothing printed, check printer status and console logs."
                });
            }
            else
            {
                _logger.LogWarning("PRINT FAILED - PrintService returned false");
                return StatusCode(500, new
                {
                    error = "Print failed",
                    message = "Could not send to printer",
                    details = new[] { "Check console for detailed error messages", "Verify printer is online and properly configured" }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in print_receipt");
            _logger.LogError($"Exception details: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = ex.Message
            });
        }
    }

    [HttpPost("html")]
    public IActionResult PrintHtml([FromBody] PrintRequest? data)
    {
        try
        {
            _logger.LogInformation("Received HTML print request");

            if (data == null || string.IsNullOrWhiteSpace(data.HtmlContent))
            {
                return BadRequest(new { error = "No HTML content provided" });
            }

            var htmlContent = data.HtmlContent;
            _logger.LogInformation($"HTML content length: {htmlContent.Length} characters");

            var printerName = data.PrinterName;
            var printerType = data.PrinterType ?? "windows";

            var success = _printService.PrintHtmlContent(htmlContent, printerName);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "HTML content printed successfully"
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    error = "Print failed",
                    message = "Could not print HTML content. Check console for details."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in print_html");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = ex.Message
            });
        }
    }
}


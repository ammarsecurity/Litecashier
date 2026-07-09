using Microsoft.AspNetCore.Mvc;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("[controller]")]
public class PrinterController : ControllerBase
{
    private readonly PrintService _printService;
    private readonly ILogger<PrinterController> _logger;

    public PrinterController(PrintService printService, ILogger<PrinterController> logger)
    {
        _printService = printService;
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult CheckPrinterStatus([FromQuery] string? name, [FromQuery] string? type = "windows")
    {
        try
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest(new
                {
                    error = "Printer name is required",
                    message = "Please provide printer name as query parameter"
                });
            }

            var printers = _printService.GetAvailablePrinters();

            // Support both "windows" and "usb" types (USB printers in Windows are accessed via Windows API)
            if (type == "windows" || type == "usb")
            {
                if (!printers.Contains(name))
                {
                    return Ok(new
                    {
                        name = name,
                        type = type,
                        online = false,
                        available = false,
                        error = "Printer not found"
                    });
                }

                // Try to check printer status using Windows API
                try
                {
                    // Check if printer is actually accessible using PrintDocument
                    var printDoc = new System.Drawing.Printing.PrintDocument
                    {
                        PrinterSettings = { PrinterName = name }
                    };

                    // If we can create PrintDocument and it's valid, printer is available
                    var isAvailable = printDoc.PrinterSettings.IsValid;
                    var isOnline = isAvailable; // Assume online if available
                    
                    // Try to get more detailed status if possible
                    try
                    {
                        // Check if printer name matches (basic validation)
                        var isValid = printDoc.PrinterSettings.PrinterName == name;
                        isOnline = isValid && isAvailable;
                    }
                    catch
                    {
                        // If we can't get detailed status, use basic availability
                    }
                    
                    return Ok(new
                    {
                        name = name,
                        type = type,
                        online = isOnline,
                        available = isAvailable,
                        status_code = isOnline ? 0 : 1
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Cannot access printer {name}");
                    return Ok(new
                    {
                        name = name,
                        type = type,
                        online = false,
                        available = false,
                        error = $"Cannot access printer: {ex.Message}"
                    });
                }
            }
            else
            {
                return Ok(new
                {
                    name = name,
                    type = type,
                    online = false,
                    available = false,
                    error = $"Printer type {type} not supported for status check"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking printer status");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = ex.Message
            });
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("[controller]")]
public class PrintersController : ControllerBase
{
    private readonly PrintService _printService;
    private readonly ILogger<PrintersController> _logger;

    public PrintersController(PrintService printService, ILogger<PrintersController> logger)
    {
        _printService = printService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult ListPrinters()
    {
        try
        {
            var printers = _printService.GetAvailablePrinters();
            string? defaultPrinter = null;
            try
            {
                defaultPrinter = _printService.GetDefaultPrinter();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting default printer");
            }

            var printerList = printers.Select(p => new
            {
                name = p,
                type = "windows"
            }).ToList();

            return Ok(new
            {
                printers = printerList,
                @default = defaultPrinter
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing printers");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = ex.Message
            });
        }
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
            var status = new
            {
                name = name,
                type = type,
                online = false,
                available = false,
                error = (string?)null
            };

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

                // Try to check printer status
                try
                {
                    // For now, we'll just check if printer exists
                    // In a full implementation, you might want to check actual printer status
                    return Ok(new
                    {
                        name = name,
                        type = type,
                        online = true,
                        available = true,
                        status_code = 0
                    });
                }
                catch (Exception ex)
                {
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


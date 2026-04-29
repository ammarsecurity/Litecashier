using Microsoft.AspNetCore.Mvc;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ConfigurationService _configService;
    private readonly PrintService _printService;

    public HealthController(ConfigurationService configService, PrintService printService)
    {
        _configService = configService;
        _printService = printService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var config = _configService.GetConfig();
        var printers = _printService.GetAvailablePrinters();
        string? defaultPrinter = null;
        try
        {
            defaultPrinter = _printService.GetDefaultPrinter();
        }
        catch
        {
            // Ignore
        }

        return Ok(new
        {
            status = "ok",
            printer = new
            {
                available = printers.Any(),
                type = config.Type
            },
            windows_default_printer = defaultPrinter,
            config = new
            {
                printer_type = config.Type,
                windows_printer_name = config.WindowsPrinterName
            }
        });
    }
}


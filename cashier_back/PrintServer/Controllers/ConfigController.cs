using Microsoft.AspNetCore.Mvc;
using PrintServer.Models;
using PrintServer.Services;

namespace PrintServer.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigController : ControllerBase
{
    private readonly ConfigurationService _configService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(ConfigurationService configService, ILogger<ConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetConfig()
    {
        try
        {
            var config = _configService.GetConfig();
            return Ok(new
            {
                config = new
                {
                    type = config.Type,
                    windows_printer_name = config.WindowsPrinterName,
                    use_esc_pos_commands = config.UseEscPosCommands,
                    encoding = config.Encoding,
                    esc_pos_encoding = config.EscPosEncoding,
                    serial_port = config.SerialPort,
                    network_host = config.NetworkHost,
                    network_port = config.NetworkPort,
                    usb_vendor_id = config.UsbVendorId,
                    usb_product_id = config.UsbProductId,
                    file_path = config.FilePath,
                    server_port = 5000,
                    server_host = "localhost"
                },
                libraries = new
                {
                    win32_available = true, // Always true on Windows
                    espos_available = false // ESC/POS library not implemented in C# version
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = ex.Message
            });
        }
    }

    [HttpPut]
    [HttpPost]
    public IActionResult UpdateConfig([FromBody] Dictionary<string, object>? data)
    {
        try
        {
            if (data == null)
            {
                return BadRequest(new
                {
                    error = "No data provided",
                    message = "Please provide configuration data in the request body"
                });
            }

            var config = _configService.GetConfig();
            var changes = new List<string>();

            // Update allowed keys
            if (data.ContainsKey("type") && data["type"] is string typeStr)
            {
                if (new[] { "usb", "serial", "network", "file", "windows" }.Contains(typeStr))
                {
                    config.Type = typeStr;
                    changes.Add("type");
                }
                else
                {
                    return BadRequest(new
                    {
                        error = "Invalid printer type",
                        message = "Printer type must be one of: usb, serial, network, file, windows"
                    });
                }
            }

            if (data.ContainsKey("windows_printer_name"))
            {
                var value = data["windows_printer_name"];
                config.WindowsPrinterName = value?.ToString() == "null" || string.IsNullOrEmpty(value?.ToString()) ? null : value?.ToString();
                changes.Add("windows_printer_name");
            }

            if (data.ContainsKey("use_esc_pos_commands"))
            {
                if (bool.TryParse(data["use_esc_pos_commands"]?.ToString(), out var useEscPos))
                {
                    config.UseEscPosCommands = useEscPos;
                    changes.Add("use_esc_pos_commands");
                }
            }

            if (data.ContainsKey("encoding") && data["encoding"] is string encodingStr)
            {
                if (new[] { "utf-8", "windows-1256", "cp1256", "latin1" }.Contains(encodingStr))
                {
                    config.Encoding = encodingStr;
                    changes.Add("encoding");
                }
                else
                {
                    return BadRequest(new
                    {
                        error = "Invalid encoding",
                        message = "Encoding must be one of: utf-8, windows-1256, cp1256, latin1"
                    });
                }
            }

            if (data.ContainsKey("esc_pos_encoding"))
            {
                if (int.TryParse(data["esc_pos_encoding"]?.ToString(), out var escPosEncoding))
                {
                    config.EscPosEncoding = escPosEncoding;
                    changes.Add("esc_pos_encoding");
                }
                else
                {
                    return BadRequest(new
                    {
                        error = "Invalid ESC/POS encoding",
                        message = "ESC/POS encoding must be a number (16=UTF-8, 17=Windows-1256, 0=PC437)"
                    });
                }
            }

            if (data.ContainsKey("serial_port") && data["serial_port"] is string serialPort)
            {
                config.SerialPort = serialPort;
                changes.Add("serial_port");
            }

            if (data.ContainsKey("network_host") && data["network_host"] is string networkHost)
            {
                config.NetworkHost = networkHost;
                changes.Add("network_host");
            }

            if (data.ContainsKey("network_port"))
            {
                if (int.TryParse(data["network_port"]?.ToString(), out var networkPort))
                {
                    config.NetworkPort = networkPort;
                    changes.Add("network_port");
                }
            }

            if (data.ContainsKey("usb_vendor_id"))
            {
                if (int.TryParse(data["usb_vendor_id"]?.ToString(), out var usbVendorId))
                {
                    config.UsbVendorId = usbVendorId;
                    changes.Add("usb_vendor_id");
                }
            }

            if (data.ContainsKey("usb_product_id"))
            {
                if (int.TryParse(data["usb_product_id"]?.ToString(), out var usbProductId))
                {
                    config.UsbProductId = usbProductId;
                    changes.Add("usb_product_id");
                }
            }

            if (data.ContainsKey("file_path") && data["file_path"] is string filePath)
            {
                config.FilePath = filePath;
                changes.Add("file_path");
            }

            if (changes.Count == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "No changes detected",
                    config = config
                });
            }

            _configService.UpdateConfig(config);

            return Ok(new
            {
                success = true,
                message = $"Configuration updated successfully. Changes: {string.Join(", ", changes)}",
                config = config,
                changes = changes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = ex.Message
            });
        }
    }

    [HttpPut("printer")]
    [HttpPost("printer")]
    public IActionResult SetDefaultPrinter([FromBody] Dictionary<string, string>? data)
    {
        try
        {
            if (data == null || !data.ContainsKey("printer_name"))
            {
                return BadRequest(new
                {
                    error = "Printer name is required",
                    message = "Please provide printer_name in the request body"
                });
            }

            var printerName = data["printer_name"];
            var printService = HttpContext.RequestServices.GetRequiredService<PrintService>();
            var printers = printService.GetAvailablePrinters();

            if (!printers.Contains(printerName))
            {
                return NotFound(new
                {
                    error = "Printer not found",
                    message = $"Printer \"{printerName}\" does not exist",
                    available_printers = printers
                });
            }

            var config = _configService.GetConfig();
            config.WindowsPrinterName = printerName;
            _configService.UpdateConfig(config);

            return Ok(new
            {
                success = true,
                message = $"Default printer set to \"{printerName}\"",
                printer_name = printerName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default printer");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = ex.Message
            });
        }
    }
}



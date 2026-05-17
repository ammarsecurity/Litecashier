using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Response;
using System.Security.Claims;
using System.Text.Json;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class PrintersController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<PrintersController> _logger;
        private readonly IConfiguration _configuration;

        public PrintersController(ILogger<PrintersController> logger, DbConfig dbConfig, IConfiguration configuration)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _configuration = configuration;
        }

        // Helper method to get Commercial User ID
        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            
            if (user != null && user.Role == "Commercial")
            {
                return userId;
            }
            
            return user?.InsertByUserId ?? userId;
        }

        // GET: api/Printers
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS,Waiter")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<Printer>>>> GetPrinters()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var printers = await _dbConfig.Printers
                    .Where(p => !p.IsDeleted && p.InsertByUserId == commercialUserId)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<Printer>>
                {
                    Data = printers,
                    ErrorStatus = false,
                    Message = "تم جلب الطابعات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printers");
                return StatusCode(500, new GlobalResponse<List<Printer>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الطابعات: {ex.Message}"
                });
            }
        }

        // GET: api/Printers/{id}
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<Printer>>> GetPrinter(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var printer = await _dbConfig.Printers
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.InsertByUserId == commercialUserId);

                if (printer == null)
                {
                    return NotFound(new GlobalResponse<Printer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطابعة غير موجودة"
                    });
                }

                return Ok(new GlobalResponse<Printer>
                {
                    Data = printer,
                    ErrorStatus = false,
                    Message = "تم جلب الطابعة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printer {PrinterId}", id);
                return StatusCode(500, new GlobalResponse<Printer>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الطابعة: {ex.Message}"
                });
            }
        }

        // GET: api/Printers/category/{category}
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpGet("category/{category}")]
        public async Task<ActionResult<GlobalResponse<List<Printer>>>> GetPrintersByCategory(string category)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var printers = await _dbConfig.Printers
                    .Where(p => !p.IsDeleted 
                        && p.InsertByUserId == commercialUserId
                        && p.PrintCategory == category
                        && p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<Printer>>
                {
                    Data = printers,
                    ErrorStatus = false,
                    Message = "تم جلب الطابعات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printers by category {Category}", category);
                return StatusCode(500, new GlobalResponse<List<Printer>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الطابعات: {ex.Message}"
                });
            }
        }

        // POST: api/Printers
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<Printer>>> AddPrinter([FromBody] PrinterRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var commercialUserId = GetCommercialUserId();

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<Printer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم الطابعة مطلوب"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PrinterName))
                {
                    return BadRequest(new GlobalResponse<Printer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم الطابعة في النظام مطلوب"
                    });
                }

                // If setting this printer as main, unset other main printers for this user
                if (request.IsMain == true)
                {
                    var existingMainPrinters = await _dbConfig.Printers
                        .Where(p => !p.IsDeleted && p.InsertByUserId == commercialUserId && p.IsMain)
                        .ToListAsync();
                    
                    foreach (var mainPrinter in existingMainPrinters)
                    {
                        mainPrinter.IsMain = false;
                        _dbConfig.Printers.Update(mainPrinter);
                    }
                }

                var printer = new Printer
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    PrinterName = request.PrinterName.Trim(),
                    PrinterType = request.PrinterType ?? "windows",
                    PrintCategory = request.PrintCategory?.Trim(),
                    Configuration = request.Configuration?.Trim(),
                    IsActive = request.IsActive ?? true,
                    IsMain = request.IsMain ?? false,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _dbConfig.Printers.Add(printer);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<Printer>
                {
                    Data = printer,
                    ErrorStatus = false,
                    Message = "تم إضافة الطابعة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding printer: {Exception}", ex);
                _logger.LogError(ex, "Inner exception: {InnerException}", ex.InnerException?.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                
                return StatusCode(500, new GlobalResponse<Printer>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة الطابعة: {errorMessage}"
                });
            }
        }

        // PUT: api/Printers/{id}
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<Printer>>> UpdatePrinter(int id, [FromBody] PrinterRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var printer = await _dbConfig.Printers
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.InsertByUserId == commercialUserId);

                if (printer == null)
                {
                    return NotFound(new GlobalResponse<Printer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطابعة غير موجودة"
                    });
                }

                // Store old values for audit log
                var oldValues = new
                {
                    Name = printer.Name,
                    Description = printer.Description,
                    PrinterName = printer.PrinterName,
                    PrinterType = printer.PrinterType,
                    PrintCategory = printer.PrintCategory,
                    IsActive = printer.IsActive,
                    IsMain = printer.IsMain
                };

                printer.Name = request.Name;
                printer.Description = request.Description;
                printer.PrinterName = request.PrinterName;
                printer.PrinterType = request.PrinterType ?? printer.PrinterType;
                printer.PrintCategory = request.PrintCategory;
                printer.Configuration = request.Configuration;
                if (request.IsActive.HasValue)
                {
                    printer.IsActive = request.IsActive.Value;
                }
                
                // Handle IsMain - if setting to true, unset other main printers
                if (request.IsMain.HasValue)
                {
                    if (request.IsMain.Value)
                    {
                        // Unset other main printers for this user
                        var existingMainPrinters = await _dbConfig.Printers
                            .Where(p => !p.IsDeleted && p.InsertByUserId == commercialUserId && p.IsMain && p.Id != id)
                            .ToListAsync();
                        
                        foreach (var mainPrinter in existingMainPrinters)
                        {
                            mainPrinter.IsMain = false;
                            _dbConfig.Printers.Update(mainPrinter);
                        }
                    }
                    printer.IsMain = request.IsMain.Value;
                }

                // Store new values for audit log
                var newValues = new
                {
                    Name = printer.Name,
                    Description = printer.Description,
                    PrinterName = printer.PrinterName,
                    PrinterType = printer.PrinterType,
                    PrintCategory = printer.PrintCategory,
                    IsActive = printer.IsActive,
                    IsMain = printer.IsMain
                };

                _dbConfig.Printers.Update(printer);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Update",
                    "Printer",
                    printer.Id,
                    printer.Name,
                    userId,
                    commercialUserId,
                    oldValues,
                    newValues,
                    $"تم تعديل الطابعة: {printer.Name}"
                );

                return Ok(new GlobalResponse<Printer>
                {
                    Data = printer,
                    ErrorStatus = false,
                    Message = "تم تحديث الطابعة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating printer {PrinterId}", id);
                return StatusCode(500, new GlobalResponse<Printer>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث الطابعة: {ex.Message}"
                });
            }
        }

        // DELETE: api/Printers/{id}
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeletePrinter(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var printer = await _dbConfig.Printers
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.InsertByUserId == commercialUserId);

                if (printer == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطابعة غير موجودة"
                    });
                }

                var printerName = printer.Name;
                printer.IsDeleted = true;
                _dbConfig.Printers.Update(printer);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Delete",
                    "Printer",
                    printer.Id,
                    printerName,
                    userId,
                    commercialUserId,
                    null,
                    null,
                    $"تم حذف الطابعة: {printerName}"
                );

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف الطابعة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting printer {PrinterId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف الطابعة: {ex.Message}"
                });
            }
        }

        // POST: api/Printers/{id}/print
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS,Waiter")]
        [HttpPost("{id}/print")]
        public async Task<ActionResult<GlobalResponse<object>>> Print(int id, [FromBody] PrintRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var printer = await _dbConfig.Printers
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.InsertByUserId == commercialUserId && p.IsActive);

                if (printer == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطابعة غير موجودة أو غير مفعلة"
                    });
                }

                // Queue print on Print Server (WebView2 can take 30–60s; avoid blocking API / axios timeout → "canceled")
                var printServerUrl = _configuration["PrintServer:Url"] ?? "http://localhost:5000";
                var copies = request.Copies ?? 1;
                var printerName = printer.PrinterName;
                var printerType = printer.PrinterType;
                var htmlContent = request.HtmlContent;
                var jsonData = request.JsonData;
                object? configuration = printer.Configuration != null
                    ? JsonSerializer.Deserialize<object>(printer.Configuration)
                    : null;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var httpClient = new HttpClient
                        {
                            Timeout = TimeSpan.FromMinutes(3)
                        };

                        var printData = new
                        {
                            printerName,
                            printerType,
                            htmlContent,
                            jsonData,
                            configuration
                        };

                        for (var i = 0; i < copies; i++)
                        {
                            var response = await httpClient.PostAsJsonAsync($"{printServerUrl}/print", printData);
                            if (!response.IsSuccessStatusCode)
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                _logger.LogError(
                                    "Print server error for printer {PrinterId} copy {Copy}: {Error}",
                                    id, i + 1, errorContent);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background print failed for printer {PrinterId}", id);
                    }
                });

                return Ok(new GlobalResponse<object>
                {
                    Data = new { Copies = copies, Queued = true },
                    ErrorStatus = false,
                    Message = copies > 1
                        ? $"تم إرسال {copies} أوامر طباعة إلى الطابعة"
                        : "تم إرسال أمر الطباعة إلى الطابعة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing to printer {PrinterId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء الطباعة: {ex.Message}"
                });
            }
        }

        // GET: /Printers/{id}/status
        [HttpGet("{id}/status")]
        public async Task<ActionResult<GlobalResponse<PrinterStatusResponse>>> GetPrinterStatus(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var printer = await _dbConfig.Printers
                    .FirstOrDefaultAsync(p => p.Id == id && p.InsertByUserId == commercialUserId && !p.IsDeleted);

                if (printer == null)
                {
                    return NotFound(new GlobalResponse<PrinterStatusResponse>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Printer not found"
                    });
                }

                // Check printer status from print server
                var printServerUrl = _configuration["PrintServer:Url"] ?? "http://localhost:5000";
                
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(5);
                        var response = await httpClient.GetAsync($"{printServerUrl}/printer/status?name={Uri.EscapeDataString(printer.PrinterName)}&type={Uri.EscapeDataString(printer.PrinterType)}");

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var statusData = JsonSerializer.Deserialize<JsonElement>(content);

                            var statusResponse = new PrinterStatusResponse
                            {
                                PrinterId = printer.Id,
                                PrinterName = printer.PrinterName,
                                Online = statusData.TryGetProperty("online", out var onlineProp) && onlineProp.GetBoolean(),
                                Available = statusData.TryGetProperty("available", out var availableProp) && availableProp.GetBoolean(),
                                Error = statusData.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : null
                            };

                            return Ok(new GlobalResponse<PrinterStatusResponse>
                            {
                                Data = statusResponse,
                                ErrorStatus = false,
                                Message = "Printer status retrieved successfully"
                            });
                        }
                        else
                        {
                            return Ok(new GlobalResponse<PrinterStatusResponse>
                            {
                                Data = new PrinterStatusResponse
                                {
                                    PrinterId = printer.Id,
                                    PrinterName = printer.PrinterName,
                                    Online = false,
                                    Available = false,
                                    Error = $"Print server returned status code: {response.StatusCode}"
                                },
                                ErrorStatus = false,
                                Message = "Printer status retrieved"
                            });
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    return Ok(new GlobalResponse<PrinterStatusResponse>
                    {
                        Data = new PrinterStatusResponse
                        {
                            PrinterId = printer.Id,
                            PrinterName = printer.PrinterName,
                            Online = false,
                            Available = false,
                            Error = "Print server timeout - printer may be offline"
                        },
                        ErrorStatus = false,
                        Message = "Printer status retrieved"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking printer status from print server");
                    return Ok(new GlobalResponse<PrinterStatusResponse>
                    {
                        Data = new PrinterStatusResponse
                        {
                            PrinterId = printer.Id,
                            PrinterName = printer.PrinterName,
                            Online = false,
                            Available = false,
                            Error = $"Cannot connect to print server: {ex.Message}"
                        },
                        ErrorStatus = false,
                        Message = "Printer status retrieved"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printer status");
                return StatusCode(500, new GlobalResponse<PrinterStatusResponse>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "Error getting printer status"
                });
            }
        }
    }

    public class PrinterStatusResponse
    {
        public int PrinterId { get; set; }
        public string PrinterName { get; set; } = string.Empty;
        public bool Online { get; set; }
        public bool Available { get; set; }
        public string? Error { get; set; }
    }

    // Request DTOs
    public class PrinterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string PrinterName { get; set; } = string.Empty;
        public string? PrinterType { get; set; }
        public string? PrintCategory { get; set; }
        public string? Configuration { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsMain { get; set; }
    }

    public class PrintRequest
    {
        public string? HtmlContent { get; set; }
        public object? JsonData { get; set; }
        public int? Copies { get; set; } = 1;
    }
}


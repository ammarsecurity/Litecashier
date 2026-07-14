using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Authorization;
using POS.Db;
using POS.Models;
using POS.Models.Requests;
using POS.Models.Response;
using System.Security.Claims;
using System.Text.Json;

namespace POS.Controllers
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

        // GET: api/Printers/user-assignments
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpGet("user-assignments")]
        public async Task<ActionResult<GlobalResponse<List<UserPrinterAssignmentDto>>>> GetUserPrinterAssignments()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var assignableRoles = new[] { "POS", "Waiter" };

                var users = await _dbConfig.Users
                    .AsNoTracking()
                    .Where(u => !u.IsDeleted
                        && u.InsertByUserId == commercialUserId
                        && assignableRoles.Contains(u.Role))
                    .OrderBy(u => u.Name)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Username,
                        u.Role,
                        u.DefaultPrinterId
                    })
                    .ToListAsync();

                var printerIds = users
                    .Where(u => u.DefaultPrinterId != null)
                    .Select(u => u.DefaultPrinterId!.Value)
                    .Distinct()
                    .ToList();

                var printerNames = printerIds.Count == 0
                    ? new Dictionary<int, string>()
                    : await _dbConfig.Printers
                        .AsNoTracking()
                        .Where(p => printerIds.Contains(p.Id) && !p.IsDeleted && p.InsertByUserId == commercialUserId)
                        .ToDictionaryAsync(p => p.Id, p => p.Name);

                var data = users.Select(u => new UserPrinterAssignmentDto
                {
                    UserId = u.Id,
                    Name = u.Name,
                    Username = u.Username,
                    Role = u.Role,
                    DefaultPrinterId = u.DefaultPrinterId,
                    DefaultPrinterName = u.DefaultPrinterId != null && printerNames.TryGetValue(u.DefaultPrinterId.Value, out var n)
                        ? n
                        : null
                }).ToList();

                return Ok(new GlobalResponse<List<UserPrinterAssignmentDto>>
                {
                    Data = data,
                    ErrorStatus = false,
                    Message = "تم جلب تخصيصات الطابعات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user printer assignments");
                return StatusCode(500, new GlobalResponse<List<UserPrinterAssignmentDto>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب التخصيصات: {ex.Message}"
                });
            }
        }

        // PUT: api/Printers/user-assignments/{userId}
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpPut("user-assignments/{userId:int}")]
        public async Task<ActionResult<GlobalResponse<UserPrinterAssignmentDto>>> SetUserPrinterAssignment(
            int userId,
            [FromBody] SetUserPrinterAssignmentRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var assignableRoles = new[] { "POS", "Waiter" };

                var targetUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.InsertByUserId == commercialUserId);

                if (targetUser == null || !assignableRoles.Contains(targetUser.Role))
                {
                    return NotFound(new GlobalResponse<UserPrinterAssignmentDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "حساب نقطة البيع غير موجود"
                    });
                }

                string? printerName = null;
                if (request.PrinterId == null)
                {
                    targetUser.DefaultPrinterId = null;
                }
                else
                {
                    var printer = await _dbConfig.Printers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.Id == request.PrinterId.Value
                            && !p.IsDeleted
                            && p.InsertByUserId == commercialUserId);

                    if (printer == null)
                    {
                        return BadRequest(new GlobalResponse<UserPrinterAssignmentDto>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "الطابعة غير موجودة أو لا تتبع هذا الحساب"
                        });
                    }

                    targetUser.DefaultPrinterId = printer.Id;
                    printerName = printer.Name;
                }

                _dbConfig.Users.Update(targetUser);
                await _dbConfig.SaveChangesAsync();

                var actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Update",
                    "UserPrinterAssignment",
                    targetUser.Id,
                    targetUser.Name,
                    actorId,
                    commercialUserId,
                    null,
                    null,
                    request.PrinterId == null
                        ? $"تم إلغاء تخصيص الطابعة لحساب: {targetUser.Name}"
                        : $"تم تخصيص الطابعة ({printerName}) لحساب: {targetUser.Name}"
                );

                return Ok(new GlobalResponse<UserPrinterAssignmentDto>
                {
                    Data = new UserPrinterAssignmentDto
                    {
                        UserId = targetUser.Id,
                        Name = targetUser.Name,
                        Username = targetUser.Username,
                        Role = targetUser.Role,
                        DefaultPrinterId = targetUser.DefaultPrinterId,
                        DefaultPrinterName = printerName
                    },
                    ErrorStatus = false,
                    Message = "تم حفظ تخصيص الطابعة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user printer assignment for user {UserId}", userId);
                return StatusCode(500, new GlobalResponse<UserPrinterAssignmentDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حفظ التخصيص: {ex.Message}"
                });
            }
        }

        // GET: api/Printers/my-default
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS,Waiter")]
        [HttpGet("my-default")]
        public async Task<ActionResult<GlobalResponse<MyDefaultPrinterDto>>> GetMyDefaultPrinter()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var commercialUserId = GetCommercialUserId();

                var user = await _dbConfig.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

                if (user == null)
                {
                    return Unauthorized(new GlobalResponse<MyDefaultPrinterDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المستخدم غير موجود"
                    });
                }

                if (user.DefaultPrinterId == null)
                {
                    return Ok(new GlobalResponse<MyDefaultPrinterDto>
                    {
                        Data = new MyDefaultPrinterDto { PrinterId = null },
                        ErrorStatus = false,
                        Message = "لا توجد طابعة افتراضية لهذا الحساب"
                    });
                }

                var printer = await _dbConfig.Printers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.Id == user.DefaultPrinterId.Value
                        && !p.IsDeleted
                        && p.IsActive
                        && p.InsertByUserId == commercialUserId);

                return Ok(new GlobalResponse<MyDefaultPrinterDto>
                {
                    Data = new MyDefaultPrinterDto
                    {
                        PrinterId = printer?.Id,
                        PrinterName = printer?.Name
                    },
                    ErrorStatus = false,
                    Message = printer == null
                        ? "الطابعة المخصصة غير متاحة"
                        : "تم جلب الطابعة الافتراضية"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my default printer");
                return StatusCode(500, new GlobalResponse<MyDefaultPrinterDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الطابعة الافتراضية: {ex.Message}"
                });
            }
        }

        // GET: api/Printers
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS")]
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
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS")]
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
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS")]
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
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS")]
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
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS")]
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
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS")]
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

                var assignedUsers = await _dbConfig.Users
                    .Where(u => u.DefaultPrinterId == id)
                    .ToListAsync();
                foreach (var assignedUser in assignedUsers)
                {
                    assignedUser.DefaultPrinterId = null;
                }

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
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS")]
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
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS")]
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

    public class UserPrinterAssignmentDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? DefaultPrinterId { get; set; }
        public string? DefaultPrinterName { get; set; }
    }

    public class SetUserPrinterAssignmentRequest
    {
        public int? PrinterId { get; set; }
    }

    public class MyDefaultPrinterDto
    {
        public int? PrinterId { get; set; }
        public string? PrinterName { get; set; }
    }
}


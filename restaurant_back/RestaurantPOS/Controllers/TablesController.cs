using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Hubs;
using RestaurantPOS.Models.Requests.Restaurant;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Models.Restaurant;
using System.IO;
using System.Security.Claims;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class TablesController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<TablesController> _logger;
        private readonly IMapper _mapper;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IConfiguration _configuration;

        public TablesController(
            ILogger<TablesController> logger,
            DbConfig dbConfig,
            IMapper mapper,
            IHubContext<OrderHub> hubContext,
            IConfiguration configuration)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
            _hubContext = hubContext;
            _configuration = configuration;
        }

        // Helper method to get Commercial User ID
        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            
            // If user is Commercial, return their own ID
            // Otherwise, return their InsertByUserId (which should be the Commercial user)
            if (user != null && user.Role == "Commercial")
            {
                return userId;
            }
            
            return user?.InsertByUserId ?? userId;
        }

        // GET: api/Tables
        [AuthorizeSection("tables", Roles = "Commercial,POS,Admin,Waiter")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<PagedList<Table>>>> GetTables(
            int pageNumber = 0,
            int pageSize = 10,
            string? status = null,
            string? zone = null,
            string? search = null)
        {
             var commercialUserId = GetCommercialUserId();
            
            var query = _dbConfig.Tables
                .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(zone))
            {
                query = query.Where(t => t.Zone == zone);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => 
                    t.TableNumber.Contains(search) || 
                    (t.Notes != null && t.Notes.Contains(search)));
            }

            // حسب المنطقة ثم رقم الطاولة كعدد طبيعي (نص): تجنب الترتيب النصي الخام (1،10،2)
            query = query
                .OrderBy(t => t.Zone ?? "")
                .ThenBy(t => t.TableNumber.Length)
                .ThenBy(t => t.TableNumber);

            pageNumber = Math.Max(0, pageNumber);
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 500) pageSize = 500;

            var totalItems = await query.CountAsync();
            var tables = await query.ToListAsync();

            var pagedResult = new PagedList<Table>(tables, totalItems, pageNumber, pageSize);

            return Ok(new GlobalResponse<PagedList<Table>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "تم جلب الطاولات بنجاح"
            });
        }

        // GET: api/Tables/{id}
        [AuthorizeSection("tables", Roles = "Commercial,POS,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<Table>>> GetTable(int id)
        {
            var commercialUserId = GetCommercialUserId();
            
            var table = await _dbConfig.Tables
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (table == null)
            {
                return NotFound(new GlobalResponse<Table>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "الطاولة غير موجودة"
                });
            }

            return Ok(new GlobalResponse<Table>
            {
                Data = table,
                ErrorStatus = false,
                Message = "تم جلب الطاولة بنجاح"
            });
        }

        // POST: api/Tables
        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<Table>>> AddTable(TableRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            
            // Check if table number already exists for this Commercial
            var existingTable = await _dbConfig.Tables
                .FirstOrDefaultAsync(t => t.TableNumber == request.TableNumber && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (existingTable != null)
            {
                return BadRequest(new GlobalResponse<Table>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "رقم الطاولة موجود مسبقاً"
                });
            }

            var table = _mapper.Map<Table>(request);
            table.InsertByUserId = commercialUserId;
            _dbConfig.Tables.Add(table);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Table>
            {
                Data = table,
                ErrorStatus = false,
                Message = "تم إضافة الطاولة بنجاح"
            });
        }

        // PUT: api/Tables/{id}")
        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<Table>>> UpdateTable(int id, TableRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            
            var table = await _dbConfig.Tables
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (table == null)
            {
                return NotFound(new GlobalResponse<Table>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "الطاولة غير موجودة"
                });
            }

            // Check if table number already exists (excluding current table) for this Commercial
            var existingTable = await _dbConfig.Tables
                .FirstOrDefaultAsync(t => t.TableNumber == request.TableNumber && t.Id != id && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (existingTable != null)
            {
                return BadRequest(new GlobalResponse<Table>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "رقم الطاولة موجود مسبقاً"
                });
            }

            // Store old values for audit log
            var oldValues = new
            {
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                Zone = table.Zone,
                Status = table.Status
            };

            _mapper.Map(request, table);
            _dbConfig.Tables.Update(table);
            await _dbConfig.SaveChangesAsync();

            // Store new values for audit log
            var newValues = new
            {
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                Zone = table.Zone,
                Status = table.Status
            };

            // Log audit
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _dbConfig.LogAuditAsync(
                "Update",
                "Table",
                table.Id,
                $"طاولة {table.TableNumber}",
                userId,
                commercialUserId,
                oldValues,
                newValues,
                $"تم تعديل الطاولة: {table.TableNumber}"
            );

            // Send SignalR notification for table update
            try
            {
                await _hubContext.Clients.All.SendAsync("TableUpdated", new
                {
                    TableId = table.Id,
                    Status = table.Status,
                    TableNumber = table.TableNumber,
                    Zone = table.Zone
                });
                _logger.LogInformation("SignalR notification sent for TableUpdated: TableId={TableId}, Status={Status}", table.Id, table.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification for TableUpdated");
            }

            return Ok(new GlobalResponse<Table>
            {
                Data = table,
                ErrorStatus = false,
                Message = "تم تحديث الطاولة بنجاح"
            });
        }

        // PUT: api/Tables/{id}/status
        [AuthorizeSection("tables", Roles = "Commercial,POS,Admin")]
        [HttpPut("{id}/status")]
        public async Task<ActionResult<GlobalResponse<Table>>> UpdateTableStatus(int id, [FromBody] string status)
        {
            var commercialUserId = GetCommercialUserId();
            
            var table = await _dbConfig.Tables
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (table == null)
            {
                return NotFound(new GlobalResponse<Table>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "الطاولة غير موجودة"
                });
            }

            var validStatuses = new[] { "Available", "Occupied", "Reserved", "OutOfService" };
            if (!validStatuses.Contains(status))
            {
                return BadRequest(new GlobalResponse<Table>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حالة غير صحيحة"
                });
            }

            // Store old status for audit log
            var oldStatus = table.Status;
            table.Status = status;
            
            // إذا أصبحت الطاولة متاحة، قم بإزالة ربطها بالطلب الحالي
            if (status == "Available")
            {
                table.CurrentOrderId = null;
            }

            _dbConfig.Tables.Update(table);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _dbConfig.LogAuditAsync(
                "Update",
                "Table",
                table.Id,
                $"طاولة {table.TableNumber}",
                userId,
                commercialUserId,
                new { Status = oldStatus },
                new { Status = status },
                $"تم تعديل حالة الطاولة {table.TableNumber}: {oldStatus} → {status}"
            );

            // Send SignalR notification for table status update
            try
            {
                await _hubContext.Clients.All.SendAsync("TableUpdated", new
                {
                    TableId = table.Id,
                    Status = table.Status,
                    TableNumber = table.TableNumber,
                    Zone = table.Zone
                });
                _logger.LogInformation("SignalR notification sent for TableUpdated: TableId={TableId}, Status={Status}", table.Id, table.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification for TableUpdated");
            }

            return Ok(new GlobalResponse<Table>
            {
                Data = table,
                ErrorStatus = false,
                Message = "تم تحديث حالة الطاولة بنجاح"
            });
        }

        // POST: api/Tables/bulk
        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpPost("bulk")]
        public async Task<ActionResult<GlobalResponse<List<Table>>>> AddTablesBulk([FromBody] BulkTableRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            
            if (request.NumberOfTables <= 0 || request.Capacity <= 0)
            {
                return BadRequest(new GlobalResponse<List<Table>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "عدد الطاولات والسعة يجب أن تكون أكبر من صفر"
                });
            }

            var tables = new List<Table>();
            var existingTableNumbers = await _dbConfig.Tables
                .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId)
                .Select(t => t.TableNumber)
                .ToListAsync();

            // Find the highest table number to start from
            int startNumber = 1;
            if (existingTableNumbers.Any())
            {
                var numbers = existingTableNumbers
                    .Where(n => int.TryParse(n, out _))
                    .Select(n => int.Parse(n))
                    .ToList();
                if (numbers.Any())
                {
                    startNumber = numbers.Max() + 1;
                }
            }

            for (int i = 0; i < request.NumberOfTables; i++)
            {
                var tableNumber = (startNumber + i).ToString();
                
                var table = new Table
                {
                    TableNumber = tableNumber,
                    Capacity = request.Capacity,
                    Zone = request.Zone,
                    Status = "Available",
                    Notes = request.Notes,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                tables.Add(table);
            }

            _dbConfig.Tables.AddRange(tables);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<List<Table>>
            {
                Data = tables,
                ErrorStatus = false,
                Message = $"تم إضافة {request.NumberOfTables} طاولة بنجاح"
            });
        }

        // DELETE: api/Tables/{id}
        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteTable(int id)
        {
            var commercialUserId = GetCommercialUserId();
            
            var table = await _dbConfig.Tables
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (table == null)
            {
                return NotFound(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "الطاولة غير موجودة"
                });
            }

            // Store table name for audit log
            var tableName = $"طاولة {table.TableNumber}";
            
            // Soft delete
            table.IsDeleted = true;
            _dbConfig.Tables.Update(table);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _dbConfig.LogAuditAsync(
                "Delete",
                "Table",
                table.Id,
                tableName,
                userId,
                commercialUserId,
                null,
                null,
                $"تم حذف الطاولة: {table.TableNumber}"
            );

            return Ok(new GlobalResponse<int>
            {
                Data = id,
                ErrorStatus = false,
                Message = "تم حذف الطاولة بنجاح"
            });
        }

        /// <summary>حذف عدة طاولات (إخفاء منطقي) حسب المعرفات.</summary>
        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpPost("bulk-delete")]
        public async Task<ActionResult<GlobalResponse<int>>> BulkDeleteTables([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "لم يتم تحديد أي طاولات"
                });
            }

            var commercialUserId = GetCommercialUserId();
            var distinctIds = ids.Distinct().ToList();

            var tables = await _dbConfig.Tables
                .Where(t => distinctIds.Contains(t.Id) && !t.IsDeleted && t.InsertByUserId == commercialUserId)
                .ToListAsync();

            if (tables.Count == 0)
            {
                return NotFound(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "لم يُعثر على طاولات للحذف"
                });
            }

            foreach (var table in tables)
            {
                table.IsDeleted = true;
                _dbConfig.Tables.Update(table);
            }

            await _dbConfig.SaveChangesAsync();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            foreach (var table in tables)
            {
                await _dbConfig.LogAuditAsync(
                    "Delete",
                    "Table",
                    table.Id,
                    $"طاولة {table.TableNumber}",
                    userId,
                    commercialUserId,
                    null,
                    null,
                    $"تم حذف الطاولة (دفعة): {table.TableNumber}"
                );
            }

            return Ok(new GlobalResponse<int>
            {
                Data = tables.Count,
                ErrorStatus = false,
                Message = $"تم حذف {tables.Count} طاولة بنجاح"
            });
        }

        /// <summary>حذف جميع الطاولات للمستخدم التجاري؛ يمكن تقييد الحالة بنفس فلتر القائمة.</summary>
        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpPost("delete-all")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteAllTables([FromQuery] string? status = null)
        {
            var commercialUserId = GetCommercialUserId();

            var query = _dbConfig.Tables
                .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var tables = await query.ToListAsync();

            if (tables.Count == 0)
            {
                return Ok(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = false,
                    Message = "لا توجد طاولات للحذف"
                });
            }

            foreach (var table in tables)
            {
                table.IsDeleted = true;
                _dbConfig.Tables.Update(table);
            }

            await _dbConfig.SaveChangesAsync();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            foreach (var table in tables)
            {
                await _dbConfig.LogAuditAsync(
                    "Delete",
                    "Table",
                    table.Id,
                    $"طاولة {table.TableNumber}",
                    userId,
                    commercialUserId,
                    null,
                    null,
                    $"تم حذف الطاولة (مسح الكل): {table.TableNumber}"
                );
            }

            return Ok(new GlobalResponse<int>
            {
                Data = tables.Count,
                ErrorStatus = false,
                Message = $"تم حذف {tables.Count} طاولة بنجاح"
            });
        }

        // --- مخطط الأرضية (عدة مخططات حسب PlanKey ≈ المنطقة / الطابق) ---

        private static string NormalizeFloorPlanKey(string? planKey) =>
            string.IsNullOrWhiteSpace(planKey) ? "" : planKey.Trim();

        [AuthorizeSection("tables", Roles = "Commercial,POS,Admin,Waiter")]
        [HttpGet("floor-plan")]
        public async Task<ActionResult<GlobalResponse<object>>> GetFloorPlan([FromQuery] string? planKey = null)
        {
            var commercialUserId = GetCommercialUserId();
            var pk = NormalizeFloorPlanKey(planKey);

            var tables = await _dbConfig.Tables
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId)
                .OrderBy(t => t.Zone ?? "")
                .ThenBy(t => t.TableNumber.Length)
                .ThenBy(t => t.TableNumber)
                .ToListAsync();

            var tableIds = tables.Select(t => t.Id).ToList();

            var placementsForPlan = await _dbConfig.TableLayoutPlacements
                .AsNoTracking()
                .Where(p => tableIds.Contains(p.TableId) && p.PlanKey == pk && !p.IsDeleted)
                .ToDictionaryAsync(p => p.TableId);

            var settings = await _dbConfig.RestaurantLayoutSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InsertByUserId == commercialUserId && !x.IsDeleted && x.PlanKey == pk);

            var imageBase = (_configuration["ApiSettings:ImageBaseUrl"] ?? "").TrimEnd('/');

            object? settingsDto = null;
            if (settings != null)
            {
                string? fullImageUrl = null;
                if (!string.IsNullOrEmpty(settings.FloorPlanImageFileName))
                {
                    fullImageUrl = string.IsNullOrEmpty(imageBase)
                        ? settings.FloorPlanImageFileName
                        : $"{imageBase}/{settings.FloorPlanImageFileName}";
                }

                settingsDto = new
                {
                    planKey = pk,
                    floorPlanImageUrl = fullImageUrl,
                    floorPlanImageFileName = settings.FloorPlanImageFileName,
                    backgroundColor = settings.BackgroundColor,
                    zonesJson = settings.ZonesJson,
                    tableChipSizePx = settings.TableChipSizePx
                };
            }

            var tableRows = tables.Select(t =>
            {
                double? lx = null, ly = null;
                if (placementsForPlan.TryGetValue(t.Id, out var pl))
                {
                    lx = pl.LayoutPosX;
                    ly = pl.LayoutPosY;
                }
                else if (pk == "")
                {
                    lx = t.LayoutPosX;
                    ly = t.LayoutPosY;
                }

                return new
                {
                    t.Id,
                    t.TableNumber,
                    t.Capacity,
                    t.Status,
                    t.Zone,
                    layoutPosX = lx,
                    layoutPosY = ly,
                    t.Notes,
                    t.CurrentOrderId,
                    t.InsertByUserId,
                    t.InsertDate,
                    t.UpdateDate,
                };
            }).ToList();

            // مواقع تظهر فقط إن وُجدت لها طاولة: إما Zone على الطاولة أو سجل موضع على ذلك المخطط (PlanKey).
            // لا نُرجع مفاتيح من RestaurantLayoutSettings وحدها (مخطط بدون أي طاولة مرتبطة).
            var keySet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var t in tables)
                keySet.Add(NormalizeFloorPlanKey(t.Zone));

            if (tableIds.Count > 0)
            {
                var placementPlanKeys = await _dbConfig.TableLayoutPlacements
                    .AsNoTracking()
                    .Where(p => tableIds.Contains(p.TableId) && !p.IsDeleted)
                    .Select(p => p.PlanKey)
                    .Distinct()
                    .ToListAsync();
                foreach (var k in placementPlanKeys)
                    keySet.Add(NormalizeFloorPlanKey(k));
            }

            var availablePlanKeys = keySet.OrderBy(x => x, StringComparer.Ordinal).ToList();

            if (availablePlanKeys.Count == 0)
                availablePlanKeys.Add("");
            return Ok(new GlobalResponse<object>
            {
                Data = new
                {
                    planKey = pk,
                    availablePlanKeys,
                    settings = settingsDto,
                    tables = tableRows,
                },
                ErrorStatus = false,
                Message = "ok"
            });
        }

        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpPut("floor-plan/settings")]
        public async Task<ActionResult<GlobalResponse<object>>> UpdateFloorPlanSettings(
            [FromBody] RestaurantLayoutSettingsUpdateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "بيانات غير صالحة"
                });
            }

            var commercialUserId = GetCommercialUserId();
            var pk = NormalizeFloorPlanKey(request.PlanKey);
            var settings = await _dbConfig.RestaurantLayoutSettings
                .FirstOrDefaultAsync(x => x.InsertByUserId == commercialUserId && !x.IsDeleted && x.PlanKey == pk);

            if (settings == null)
            {
                settings = new RestaurantLayoutSettings
                {
                    InsertByUserId = commercialUserId,
                    PlanKey = pk,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };
                _dbConfig.RestaurantLayoutSettings.Add(settings);
            }

            if (request.BackgroundColor != null)
                settings.BackgroundColor = string.IsNullOrWhiteSpace(request.BackgroundColor) ? null : request.BackgroundColor.Trim();

            if (request.ZonesJson != null)
                settings.ZonesJson = string.IsNullOrWhiteSpace(request.ZonesJson) ? null : request.ZonesJson;

            if (request.TableChipSizePx.HasValue)
                settings.TableChipSizePx = ClampFloorPlanChipSize(request.TableChipSizePx.Value);

            if (request.ClearFloorPlanImage)
            {
                settings.FloorPlanImageFileName = null;
            }

            settings.UpdateDate = DateTime.UtcNow;
            await _dbConfig.SaveChangesAsync();

            var imageBase = (_configuration["ApiSettings:ImageBaseUrl"] ?? "").TrimEnd('/');
            string? fullImageUrl = null;
            if (!string.IsNullOrEmpty(settings.FloorPlanImageFileName) && !string.IsNullOrEmpty(imageBase))
                fullImageUrl = $"{imageBase}/{settings.FloorPlanImageFileName}";

            return Ok(new GlobalResponse<object>
            {
                Data = new
                {
                    planKey = pk,
                    floorPlanImageUrl = fullImageUrl,
                    floorPlanImageFileName = settings.FloorPlanImageFileName,
                    backgroundColor = settings.BackgroundColor,
                    zonesJson = settings.ZonesJson,
                    tableChipSizePx = settings.TableChipSizePx
                },
                ErrorStatus = false,
                Message = "تم حفظ إعدادات المخطط"
            });
        }

        private static int ClampFloorPlanChipSize(int sizePx) =>
            Math.Max(32, Math.Min(96, sizePx));

        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpPost("floor-plan/image")]
        public async Task<ActionResult<GlobalResponse<object>>> UploadFloorPlanImage(
            [FromForm] IFormFile? file,
            [FromForm] string? planKey = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "لا يوجد ملف"
                });
            }

            try
            {
                var fileName = await UploadLayoutImageAsync(file);
                var commercialUserId = GetCommercialUserId();
                var pk = NormalizeFloorPlanKey(planKey);
                var settings = await _dbConfig.RestaurantLayoutSettings
                    .FirstOrDefaultAsync(x => x.InsertByUserId == commercialUserId && !x.IsDeleted && x.PlanKey == pk);

                if (settings == null)
                {
                    settings = new RestaurantLayoutSettings
                    {
                        InsertByUserId = commercialUserId,
                        PlanKey = pk,
                        InsertDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow,
                        IsDeleted = false,
                        FloorPlanImageFileName = fileName
                    };
                    _dbConfig.RestaurantLayoutSettings.Add(settings);
                }
                else
                {
                    settings.FloorPlanImageFileName = fileName;
                    settings.UpdateDate = DateTime.UtcNow;
                    _dbConfig.RestaurantLayoutSettings.Update(settings);
                }

                await _dbConfig.SaveChangesAsync();

                var imageBase = (_configuration["ApiSettings:ImageBaseUrl"] ?? "").TrimEnd('/');
                var fullImageUrl = string.IsNullOrEmpty(imageBase) ? fileName : $"{imageBase}/{fileName}";

                return Ok(new GlobalResponse<object>
                {
                    Data = new { planKey = pk, floorPlanImageUrl = fullImageUrl, floorPlanImageFileName = fileName },
                    ErrorStatus = false,
                    Message = "تم رفع صورة المخطط"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadFloorPlanImage");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("tables", Roles = "Commercial,Admin")]
        [HttpPost("floor-plan/positions")]
        public async Task<ActionResult<GlobalResponse<int>>> UpdateFloorPlanPositions(
            [FromQuery] string? planKey,
            [FromBody] List<TableLayoutPositionDto>? positions)
        {
            if (positions == null || positions.Count == 0)
            {
                return BadRequest(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "لا توجد مواضع"
                });
            }

            var pk = NormalizeFloorPlanKey(planKey);
            var commercialUserId = GetCommercialUserId();
            var ids = positions.Select(p => p.TableId).Distinct().ToList();

            var tables = await _dbConfig.Tables
                .Where(t => ids.Contains(t.Id) && !t.IsDeleted && t.InsertByUserId == commercialUserId)
                .ToListAsync();

            if (tables.Count == 0)
            {
                return NotFound(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "لم تُعثر على طاولات"
                });
            }

            var existingPlacements = await _dbConfig.TableLayoutPlacements
                .Where(p => ids.Contains(p.TableId) && p.PlanKey == pk && !p.IsDeleted)
                .ToListAsync();
            var placementByTable = existingPlacements.ToDictionary(p => p.TableId);

            var touched = 0;
            foreach (var dto in positions)
            {
                if (dto.LayoutPosX < 0 || dto.LayoutPosX > 1 || dto.LayoutPosY < 0 || dto.LayoutPosY > 1)
                {
                    return BadRequest(new GlobalResponse<int>
                    {
                        Data = 0,
                        ErrorStatus = true,
                        Message = "الإحداثيات يجب أن تكون بين 0 و 1"
                    });
                }

                var table = tables.FirstOrDefault(t => t.Id == dto.TableId);
                if (table == null) continue;

                placementByTable.TryGetValue(table.Id, out var placement);

                if (placement == null)
                {
                    var newPl = new TableLayoutPlacement
                    {
                        TableId = table.Id,
                        PlanKey = pk,
                        LayoutPosX = dto.LayoutPosX,
                        LayoutPosY = dto.LayoutPosY,
                        InsertDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _dbConfig.TableLayoutPlacements.Add(newPl);
                    placementByTable[table.Id] = newPl;
                }
                else
                {
                    placement.LayoutPosX = dto.LayoutPosX;
                    placement.LayoutPosY = dto.LayoutPosY;
                    placement.UpdateDate = DateTime.UtcNow;
                    _dbConfig.TableLayoutPlacements.Update(placement);
                }

                if (dto.Zone != null)
                    table.Zone = string.IsNullOrWhiteSpace(dto.Zone) ? null : dto.Zone.Trim();
                table.UpdateDate = DateTime.UtcNow;
                _dbConfig.Tables.Update(table);
                touched++;
            }

            await _dbConfig.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("FloorPlanUpdated", new { CommercialUserId = commercialUserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR FloorPlanUpdated");
            }

            return Ok(new GlobalResponse<int>
            {
                Data = touched,
                ErrorStatus = false,
                Message = "تم حفظ مواضع الطاولات"
            });
        }

        private static async Task<string> UploadLayoutImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                throw new ArgumentException("ملف الصورة فارغ");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var valid = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName);
            if (string.IsNullOrEmpty(ext) || !valid.Contains(ext.ToLowerInvariant()))
                throw new ArgumentException("امتداد غير مدعوم");

            var unique = "floorplan_" + Guid.NewGuid().ToString("N") + ext;
            var filePath = Path.Combine(path, unique);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return unique;
        }
    }
}


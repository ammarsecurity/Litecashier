using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using RestaurantPOS.Hubs;
using RestaurantPOS.Models.Requests.Restaurant;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Models.Restaurant;
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

        public TablesController(ILogger<TablesController> logger, DbConfig dbConfig, IMapper mapper, IHubContext<OrderHub> hubContext)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
            _hubContext = hubContext;
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
        [Authorize(Roles = "Commercial,POS,Admin,TablesManager,Waiter")]
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

            // Order by table number
            query = query.OrderBy(t => t.TableNumber);

            var totalItems = await query.CountAsync();
            var tables = await query.ToListAsync();

            var pagedResult = new PagedList<Table>(tables, totalItems, pageNumber, 10000);

            return Ok(new GlobalResponse<PagedList<Table>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "تم جلب الطاولات بنجاح"
            });
        }

        // GET: api/Tables/{id}
        [Authorize(Roles = "Commercial,POS,Admin,TablesManager")]
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
        [Authorize(Roles = "Commercial,Admin,TablesManager")]
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
        [Authorize(Roles = "Commercial,Admin,TablesManager")]
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
        [Authorize(Roles = "Commercial,POS,Admin,TablesManager")]
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
        [Authorize(Roles = "Commercial,Admin,TablesManager")]
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
        [Authorize(Roles = "Commercial,Admin,TablesManager")]
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
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Response;
using System.Security.Claims;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Commercial")]
    [EnableCors("CorsPolicy")]
    public class AuditLogController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(DbConfig dbConfig, ILogger<AuditLogController> logger)
        {
            _dbConfig = dbConfig;
            _logger = logger;
        }

        // GET: api/AuditLog
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<PagedList<AuditLog>>>> GetAuditLogs(
            int pageNumber = 0,
            int pageSize = 20,
            string? action = null,
            string? entityType = null,
            int? entityId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    return BadRequest(new GlobalResponse<PagedList<AuditLog>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                // Get commercial user ID
                var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;

                // Build query
                var query = _dbConfig.AuditLogs
                    .Include(x => x.User)
                    .Include(x => x.CommercialUser)
                    .Where(x => x.CommercialUserId == commercialUserId && !x.IsDeleted)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(x => x.Action == action);
                }

                if (!string.IsNullOrEmpty(entityType))
                {
                    query = query.Where(x => x.EntityType == entityType);
                }

                if (entityId.HasValue)
                {
                    query = query.Where(x => x.EntityId == entityId.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(x => x.InsertDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(x => x.InsertDate <= endDate.Value);
                }

                // Get total count
                var totalItems = await query.CountAsync();

                // Apply pagination and ordering
                var auditLogs = await query
                    .OrderByDescending(x => x.InsertDate)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var pagedResult = new PagedList<AuditLog>(auditLogs, totalItems, pageNumber, pageSize);

                return Ok(new GlobalResponse<PagedList<AuditLog>>
                {
                    Data = pagedResult,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return StatusCode(500, new GlobalResponse<PagedList<AuditLog>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء جلب سجل العمليات"
                });
            }
        }

        // GET: api/AuditLog/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<AuditLog>>> GetAuditLog(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    return BadRequest(new GlobalResponse<AuditLog>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;

                var auditLog = await _dbConfig.AuditLogs
                    .Include(x => x.User)
                    .Include(x => x.CommercialUser)
                    .FirstOrDefaultAsync(x => x.Id == id && x.CommercialUserId == commercialUserId && !x.IsDeleted);

                if (auditLog == null)
                {
                    return NotFound(new GlobalResponse<AuditLog>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Audit log not found"
                    });
                }

                return Ok(new GlobalResponse<AuditLog>
                {
                    Data = auditLog,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit log");
                return StatusCode(500, new GlobalResponse<AuditLog>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء جلب سجل العملية"
                });
            }
        }

        // GET: api/AuditLog/EntityTypes
        [HttpGet("EntityTypes")]
        public async Task<ActionResult<GlobalResponse<List<string>>>> GetEntityTypes()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    return BadRequest(new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;

                var entityTypes = await _dbConfig.AuditLogs
                    .Where(x => x.CommercialUserId == commercialUserId && !x.IsDeleted)
                    .Select(x => x.EntityType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<string>>
                {
                    Data = entityTypes,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity types");
                return StatusCode(500, new GlobalResponse<List<string>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء جلب أنواع الكيانات"
                });
            }
        }
    }
}


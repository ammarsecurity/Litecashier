using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Response;
using System.Security.Claims;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class EmployeeAdvancesController : ControllerBase
    {
        private readonly DbConfig _db;
        private readonly ILogger<EmployeeAdvancesController> _logger;

        public EmployeeAdvancesController(DbConfig db, ILogger<EmployeeAdvancesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _db.Users.FirstOrDefault(x => x.Id == userId);
            if (user != null && user.Role == "Commercial") return userId;
            var commercialId = user?.InsertByUserId ?? userId;
            return commercialId == 0 ? userId : commercialId;
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<EmployeeAdvance>>>> GetAdvances(
            int? employeeId = null,
            bool openOnly = false)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var query = _db.EmployeeAdvances
                    .Include(a => a.Employee)
                    .Where(a => !a.IsDeleted && a.InsertByUserId == commercialUserId);

                if (employeeId.HasValue)
                    query = query.Where(a => a.EmployeeId == employeeId.Value);
                if (openOnly)
                    query = query.Where(a => !a.IsClosed && a.RemainingAmount > 0);

                var list = await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.Id).ToListAsync();
                return Ok(new GlobalResponse<List<EmployeeAdvance>>
                {
                    Data = list,
                    ErrorStatus = false,
                    Message = "تم جلب السلف بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting advances");
                return StatusCode(500, new GlobalResponse<List<EmployeeAdvance>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب السلف: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpGet("balances")]
        public async Task<ActionResult<GlobalResponse<object>>> GetBalances()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var employees = await _db.Employees
                    .Where(e => !e.IsDeleted && e.InsertByUserId == commercialUserId)
                    .OrderBy(e => e.Name)
                    .ToListAsync();

                var advances = await _db.EmployeeAdvances
                    .Where(a => !a.IsDeleted && a.InsertByUserId == commercialUserId && !a.IsClosed && a.RemainingAmount > 0)
                    .ToListAsync();

                var rows = employees.Select(e => new
                {
                    employeeId = e.Id,
                    employeeName = e.Name,
                    jobTitle = e.JobTitle,
                    salary = e.Salary,
                    salaryType = e.SalaryType,
                    isActive = e.IsActive,
                    openAdvanceBalance = advances.Where(a => a.EmployeeId == e.Id).Sum(a => a.RemainingAmount)
                }).ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = new
                    {
                        totalOpenAdvances = rows.Sum(r => r.openAdvanceBalance),
                        employees = rows
                    },
                    ErrorStatus = false,
                    Message = "تم جلب أرصدة السلف بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting advance balances");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الأرصدة: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<EmployeeAdvance>>> Create([FromBody] EmployeeAdvanceRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                if (request.Amount <= 0)
                {
                    return BadRequest(new GlobalResponse<EmployeeAdvance>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "مبلغ السلفة يجب أن يكون أكبر من صفر"
                    });
                }

                var employee = await _db.Employees.FirstOrDefaultAsync(e =>
                    e.Id == request.EmployeeId && !e.IsDeleted && e.InsertByUserId == commercialUserId);
                if (employee == null)
                {
                    return BadRequest(new GlobalResponse<EmployeeAdvance>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الموظف غير موجود"
                    });
                }

                var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
                var advance = new EmployeeAdvance
                {
                    EmployeeId = employee.Id,
                    Amount = amount,
                    RemainingAmount = amount,
                    Date = (request.Date ?? DateTime.UtcNow).Date,
                    Notes = request.Notes?.Trim(),
                    IsClosed = false,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };
                _db.EmployeeAdvances.Add(advance);
                await _db.SaveChangesAsync();

                var created = await _db.EmployeeAdvances.Include(a => a.Employee)
                    .FirstAsync(a => a.Id == advance.Id);

                return Ok(new GlobalResponse<EmployeeAdvance>
                {
                    Data = created,
                    ErrorStatus = false,
                    Message = "تم تسجيل السلفة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating advance");
                return StatusCode(500, new GlobalResponse<EmployeeAdvance>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تسجيل السلفة: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost("{id}/close")]
        public async Task<ActionResult<GlobalResponse<EmployeeAdvance>>> Close(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var advance = await _db.EmployeeAdvances
                    .Include(a => a.Employee)
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.InsertByUserId == commercialUserId);
                if (advance == null)
                {
                    return NotFound(new GlobalResponse<EmployeeAdvance>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "السلفة غير موجودة"
                    });
                }

                advance.IsClosed = true;
                advance.RemainingAmount = 0;
                advance.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new GlobalResponse<EmployeeAdvance>
                {
                    Data = advance,
                    ErrorStatus = false,
                    Message = "تم إغلاق السلفة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing advance {Id}", id);
                return StatusCode(500, new GlobalResponse<EmployeeAdvance>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إغلاق السلفة: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> Delete(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var advance = await _db.EmployeeAdvances
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.InsertByUserId == commercialUserId);
                if (advance == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "السلفة غير موجودة"
                    });
                }

                if (advance.RemainingAmount < advance.Amount - 0.001m)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لا يمكن حذف سلفة تم خصم جزء منها؛ أغلقها يدوياً إن لزم"
                    });
                }

                advance.IsDeleted = true;
                advance.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف السلفة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting advance {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف السلفة: {ex.Message}"
                });
            }
        }
    }
}

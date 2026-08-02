using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Services;
using System.Security.Claims;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class SalaryAdjustmentsController : ControllerBase
    {
        private readonly DbConfig _db;
        private readonly ILogger<SalaryAdjustmentsController> _logger;

        public SalaryAdjustmentsController(DbConfig db, ILogger<SalaryAdjustmentsController> logger)
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
        public async Task<ActionResult<GlobalResponse<List<SalaryAdjustment>>>> Get(
            int? employeeId = null,
            SalaryAdjustmentType? type = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var query = _db.SalaryAdjustments
                    .Include(a => a.Employee)
                    .Where(a => !a.IsDeleted && a.InsertByUserId == commercialUserId);

                if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
                if (type.HasValue) query = query.Where(a => a.Type == type.Value);
                if (startDate.HasValue) query = query.Where(a => a.Date.Date >= startDate.Value.Date);
                if (endDate.HasValue) query = query.Where(a => a.Date.Date <= endDate.Value.Date);

                var list = await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.Id).ToListAsync();
                return Ok(new GlobalResponse<List<SalaryAdjustment>>
                {
                    Data = list,
                    ErrorStatus = false,
                    Message = "تم جلب التعديلات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting salary adjustments");
                return StatusCode(500, new GlobalResponse<List<SalaryAdjustment>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب التعديلات: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<SalaryAdjustment>>> Create([FromBody] SalaryAdjustmentRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var employee = await _db.Employees.FirstOrDefaultAsync(e =>
                    e.Id == request.EmployeeId && !e.IsDeleted && e.InsertByUserId == commercialUserId);
                if (employee == null)
                {
                    return BadRequest(new GlobalResponse<SalaryAdjustment>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الموظف غير موجود"
                    });
                }

                if (request.Type == SalaryAdjustmentType.Absence)
                {
                    if (request.AbsenceDays <= 0 && request.Amount <= 0)
                    {
                        return BadRequest(new GlobalResponse<SalaryAdjustment>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "حدد أيام الغياب أو مبلغ الخصم"
                        });
                    }
                }
                else if (request.Amount <= 0)
                {
                    return BadRequest(new GlobalResponse<SalaryAdjustment>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المبلغ يجب أن يكون أكبر من صفر"
                    });
                }

                var amount = PayrollService.Round2(request.Amount);
                if (request.Type == SalaryAdjustmentType.Absence && amount <= 0 && request.AbsenceDays > 0)
                {
                    var (y, m) = (DateTime.UtcNow.Year, DateTime.UtcNow.Month);
                    var d = request.Date ?? DateTime.UtcNow;
                    y = d.Year;
                    m = d.Month;
                    amount = PayrollService.Round2(
                        PayrollService.DailyRateForPeriod(employee, y, m) * request.AbsenceDays);
                }

                var adj = new SalaryAdjustment
                {
                    EmployeeId = employee.Id,
                    Type = request.Type,
                    Amount = amount,
                    AbsenceDays = request.Type == SalaryAdjustmentType.Absence ? request.AbsenceDays : 0,
                    Date = (request.Date ?? DateTime.UtcNow).Date,
                    Notes = request.Notes?.Trim(),
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };
                _db.SalaryAdjustments.Add(adj);
                await _db.SaveChangesAsync();

                var created = await _db.SalaryAdjustments.Include(a => a.Employee)
                    .FirstAsync(a => a.Id == adj.Id);

                return Ok(new GlobalResponse<SalaryAdjustment>
                {
                    Data = created,
                    ErrorStatus = false,
                    Message = "تم إضافة التعديل بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating salary adjustment");
                return StatusCode(500, new GlobalResponse<SalaryAdjustment>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة التعديل: {ex.Message}"
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
                var adj = await _db.SalaryAdjustments
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.InsertByUserId == commercialUserId);
                if (adj == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "التعديل غير موجود"
                    });
                }

                adj.IsDeleted = true;
                adj.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف التعديل"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting salary adjustment {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف التعديل: {ex.Message}"
                });
            }
        }
    }
}

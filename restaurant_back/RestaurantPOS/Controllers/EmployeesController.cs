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

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class EmployeesController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(ILogger<EmployeesController> logger, DbConfig dbConfig)
        {
            _logger = logger;
            _dbConfig = dbConfig;
        }

        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user != null && user.Role == "Commercial")
            {
                return userId;
            }

            var commercialId = user?.InsertByUserId ?? userId;
            // Ensure we never use 0 (no User with Id=0 exists); use current user id instead (e.g. Admin)
            if (commercialId == 0)
            {
                commercialId = userId;
            }
            return commercialId;
        }

        [AuthorizeSection("employees", Roles = "Commercial,Admin,POS")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<Employee>>>> GetEmployees()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                var employees = await _dbConfig.Employees
                    .Include(e => e.Tag)
                    .Where(e => !e.IsDeleted && e.InsertByUserId == commercialUserId)
                    .OrderBy(e => e.Name)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<Employee>>
                {
                    Data = employees,
                    ErrorStatus = false,
                    Message = "تم جلب قائمة الموظفين بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees");
                return StatusCode(500, new GlobalResponse<List<Employee>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب قائمة الموظفين: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("employees", Roles = "Commercial,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<Employee>>> GetEmployee(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                var employee = await _dbConfig.Employees
                    .Include(e => e.Tag)
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.InsertByUserId == commercialUserId);

                if (employee == null)
                {
                    return NotFound(new GlobalResponse<Employee>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الموظف غير موجود"
                    });
                }

                return Ok(new GlobalResponse<Employee>
                {
                    Data = employee,
                    ErrorStatus = false,
                    Message = "تم جلب بيانات الموظف بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee {EmployeeId}", id);
                return StatusCode(500, new GlobalResponse<Employee>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب بيانات الموظف: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("employees", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<Employee>>> AddEmployee([FromBody] EmployeeRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<Employee>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم الموظف مطلوب"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new GlobalResponse<Employee>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رقم الهاتف مطلوب"
                    });
                }

                if (request.TagId.HasValue)
                {
                    var tagExists = await _dbConfig.Tags
                        .AnyAsync(t => t.Id == request.TagId.Value && !t.IsDeleted &&
                            (t.InsertByUserId == commercialUserId || t.IsForAll));
                    if (!tagExists)
                    {
                        return BadRequest(new GlobalResponse<Employee>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "القسم المحدد غير صالح"
                        });
                    }
                }

                var employee = new Employee
                {
                    Name = request.Name.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Address = request.Address?.Trim(),
                    JobTitle = request.JobTitle?.Trim(),
                    Salary = request.Salary,
                    SalaryType = request.SalaryType,
                    IsActive = request.IsActive ?? true,
                    HireDate = request.HireDate,
                    TagId = request.TagId,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _dbConfig.Employees.Add(employee);
                await _dbConfig.SaveChangesAsync();

                var added = await _dbConfig.Employees
                    .Include(e => e.Tag)
                    .FirstOrDefaultAsync(e => e.Id == employee.Id);

                return Ok(new GlobalResponse<Employee>
                {
                    Data = added ?? employee,
                    ErrorStatus = false,
                    Message = "تم إضافة الموظف بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee");
                return StatusCode(500, new GlobalResponse<Employee>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة الموظف: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("employees", Roles = "Commercial,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<Employee>>> UpdateEmployee(int id, [FromBody] EmployeeRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                var employee = await _dbConfig.Employees
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.InsertByUserId == commercialUserId);

                if (employee == null)
                {
                    return NotFound(new GlobalResponse<Employee>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الموظف غير موجود"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<Employee>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم الموظف مطلوب"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new GlobalResponse<Employee>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رقم الهاتف مطلوب"
                    });
                }

                if (request.TagId.HasValue)
                {
                    var tagExists = await _dbConfig.Tags
                        .AnyAsync(t => t.Id == request.TagId.Value && !t.IsDeleted &&
                            (t.InsertByUserId == commercialUserId || t.IsForAll));
                    if (!tagExists)
                    {
                        return BadRequest(new GlobalResponse<Employee>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "القسم المحدد غير صالح"
                        });
                    }
                }

                var oldValues = new
                {
                    employee.Name,
                    employee.PhoneNumber,
                    employee.Address,
                    employee.JobTitle,
                    employee.Salary,
                    employee.SalaryType,
                    employee.IsActive,
                    employee.HireDate,
                    employee.TagId
                };

                employee.Name = request.Name.Trim();
                employee.PhoneNumber = request.PhoneNumber.Trim();
                employee.Address = request.Address?.Trim();
                employee.JobTitle = request.JobTitle?.Trim();
                employee.Salary = request.Salary;
                employee.SalaryType = request.SalaryType;
                if (request.IsActive.HasValue) employee.IsActive = request.IsActive.Value;
                employee.HireDate = request.HireDate;
                employee.TagId = request.TagId;
                employee.UpdateDate = DateTime.UtcNow;

                var newValues = new
                {
                    employee.Name,
                    employee.PhoneNumber,
                    employee.Address,
                    employee.JobTitle,
                    employee.Salary,
                    employee.SalaryType,
                    employee.IsActive,
                    employee.HireDate,
                    employee.TagId
                };

                _dbConfig.Employees.Update(employee);
                await _dbConfig.SaveChangesAsync();

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Update",
                    "Employee",
                    employee.Id,
                    employee.Name,
                    userId,
                    commercialUserId,
                    oldValues,
                    newValues,
                    $"تم تعديل بيانات الموظف: {employee.Name}"
                );

                var updated = await _dbConfig.Employees.Include(e => e.Tag).FirstOrDefaultAsync(e => e.Id == id);
                return Ok(new GlobalResponse<Employee>
                {
                    Data = updated ?? employee,
                    ErrorStatus = false,
                    Message = "تم تحديث بيانات الموظف بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
                return StatusCode(500, new GlobalResponse<Employee>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث بيانات الموظف: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("employees", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteEmployee(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                var employee = await _dbConfig.Employees
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.InsertByUserId == commercialUserId);

                if (employee == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الموظف غير موجود"
                    });
                }

                var employeeName = employee.Name;
                employee.IsDeleted = true;
                employee.UpdateDate = DateTime.UtcNow;
                _dbConfig.Employees.Update(employee);
                await _dbConfig.SaveChangesAsync();

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Delete",
                    "Employee",
                    employee.Id,
                    employeeName,
                    userId,
                    commercialUserId,
                    null,
                    null,
                    $"تم حذف الموظف: {employeeName}"
                );

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف الموظف بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف الموظف: {ex.Message}"
                });
            }
        }
    }
}

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
using System.Text;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class ExpensesController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<ExpensesController> _logger;

        public ExpensesController(ILogger<ExpensesController> logger, DbConfig dbConfig)
        {
            _logger = logger;
            _dbConfig = dbConfig;
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

        // GET: api/Expenses
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<PagedList<Expense>>>> GetExpenses(
            int pageNumber = 0,
            int pageSize = 10,
            string? search = null,
            string? category = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var query = _dbConfig.Expenses
                    .Include(e => e.Employee)
                    .Include(e => e.Tag)
                    .Where(e => !e.IsDeleted && e.InsertByUserId == commercialUserId);

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(e => e.Description != null && e.Description.Contains(search));
                }

                // Category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(e => e.Category == category);
                }

                // Date filters
                if (startDate.HasValue)
                {
                    query = query.Where(e => e.Date.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.Date.Date <= endDate.Value.Date);
                }

                // Amount filters
                if (minAmount.HasValue)
                {
                    query = query.Where(e => e.Amount >= minAmount.Value);
                }

                if (maxAmount.HasValue)
                {
                    query = query.Where(e => e.Amount <= maxAmount.Value);
                }

                var totalItems = await query.CountAsync();
                
                var allExpenses = await query
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.InsertDate)
                    .ToListAsync();

                var pagedList = new PagedList<Expense>(allExpenses, totalItems, pageNumber, pageSize);

                return Ok(new GlobalResponse<PagedList<Expense>>
                {
                    Data = pagedList,
                    ErrorStatus = false,
                    Message = "تم جلب قائمة الصرفيات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expenses");
                return StatusCode(500, new GlobalResponse<PagedList<Expense>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب قائمة الصرفيات: {ex.Message}"
                });
            }
        }

        // GET: api/Expenses/{id}
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<Expense>>> GetExpense(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var expense = await _dbConfig.Expenses
                    .Include(e => e.Employee)
                    .Include(e => e.Tag)
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.InsertByUserId == commercialUserId);

                if (expense == null)
                {
                    return NotFound(new GlobalResponse<Expense>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الصرفية غير موجودة"
                    });
                }

                return Ok(new GlobalResponse<Expense>
                {
                    Data = expense,
                    ErrorStatus = false,
                    Message = "تم جلب بيانات الصرفية بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense {ExpenseId}", id);
                return StatusCode(500, new GlobalResponse<Expense>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب بيانات الصرفية: {ex.Message}"
                });
            }
        }

        // POST: api/Expenses
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<Expense>>> AddExpense([FromBody] ExpenseRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                if (request.Amount <= 0)
                {
                    return BadRequest(new GlobalResponse<Expense>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المبلغ يجب أن يكون أكبر من الصفر"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Category))
                {
                    return BadRequest(new GlobalResponse<Expense>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفئة مطلوبة"
                    });
                }

                if (request.EmployeeId.HasValue)
                {
                    var employeeExists = await _dbConfig.Employees
                        .AnyAsync(emp => emp.Id == request.EmployeeId.Value && !emp.IsDeleted && emp.InsertByUserId == commercialUserId);
                    if (!employeeExists)
                    {
                        return BadRequest(new GlobalResponse<Expense>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "الموظف المحدد غير صالح"
                        });
                    }
                }

                if (request.TagId.HasValue)
                {
                    var tagExists = await _dbConfig.Tags
                        .AnyAsync(t => t.Id == request.TagId.Value && !t.IsDeleted && (t.InsertByUserId == commercialUserId || t.IsForAll));
                    if (!tagExists)
                    {
                        return BadRequest(new GlobalResponse<Expense>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "القسم (Tag) المحدد غير صالح"
                        });
                    }
                }

                var expense = new Expense
                {
                    Amount = request.Amount,
                    Date = request.Date,
                    Category = request.Category.Trim(),
                    Description = request.Description?.Trim(),
                    EmployeeId = request.EmployeeId,
                    TagId = request.TagId,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _dbConfig.Expenses.Add(expense);
                await _dbConfig.SaveChangesAsync();

                var added = await _dbConfig.Expenses.Include(e => e.Employee).Include(e => e.Tag).FirstOrDefaultAsync(e => e.Id == expense.Id);
                return Ok(new GlobalResponse<Expense>
                {
                    Data = added ?? expense,
                    ErrorStatus = false,
                    Message = "تم إضافة الصرفية بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding expense");
                return StatusCode(500, new GlobalResponse<Expense>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة الصرفية: {ex.Message}"
                });
            }
        }

        // PUT: api/Expenses/{id}
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<Expense>>> UpdateExpense(int id, [FromBody] ExpenseRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var expense = await _dbConfig.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.InsertByUserId == commercialUserId);

                if (expense == null)
                {
                    return NotFound(new GlobalResponse<Expense>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الصرفية غير موجودة"
                    });
                }

                if (request.Amount <= 0)
                {
                    return BadRequest(new GlobalResponse<Expense>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المبلغ يجب أن يكون أكبر من الصفر"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Category))
                {
                    return BadRequest(new GlobalResponse<Expense>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفئة مطلوبة"
                    });
                }

                if (request.EmployeeId.HasValue)
                {
                    var employeeExists = await _dbConfig.Employees
                        .AnyAsync(emp => emp.Id == request.EmployeeId.Value && !emp.IsDeleted && emp.InsertByUserId == commercialUserId);
                    if (!employeeExists)
                    {
                        return BadRequest(new GlobalResponse<Expense>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "الموظف المحدد غير صالح"
                        });
                    }
                }

                if (request.TagId.HasValue)
                {
                    var tagExists = await _dbConfig.Tags
                        .AnyAsync(t => t.Id == request.TagId.Value && !t.IsDeleted && (t.InsertByUserId == commercialUserId || t.IsForAll));
                    if (!tagExists)
                    {
                        return BadRequest(new GlobalResponse<Expense>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "القسم (Tag) المحدد غير صالح"
                        });
                    }
                }

                // Store old values for audit log
                var oldValues = new
                {
                    Amount = expense.Amount,
                    Date = expense.Date,
                    Category = expense.Category,
                    Description = expense.Description,
                    EmployeeId = expense.EmployeeId,
                    TagId = expense.TagId
                };

                expense.Amount = request.Amount;
                expense.Date = request.Date;
                expense.Category = request.Category.Trim();
                expense.Description = request.Description?.Trim();
                expense.EmployeeId = request.EmployeeId;
                expense.TagId = request.TagId;
                expense.UpdateDate = DateTime.UtcNow;

                // Store new values for audit log
                var newValues = new
                {
                    Amount = expense.Amount,
                    Date = expense.Date,
                    Category = expense.Category,
                    Description = expense.Description,
                    EmployeeId = expense.EmployeeId,
                    TagId = expense.TagId
                };

                _dbConfig.Expenses.Update(expense);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Update",
                    "Expense",
                    expense.Id,
                    $"صرفية {expense.Category}",
                    userId,
                    commercialUserId,
                    oldValues,
                    newValues,
                    $"تم تعديل الصرفية: {expense.Category} - {expense.Amount}"
                );

                var updated = await _dbConfig.Expenses.Include(e => e.Employee).Include(e => e.Tag).FirstOrDefaultAsync(e => e.Id == id);
                return Ok(new GlobalResponse<Expense>
                {
                    Data = updated ?? expense,
                    ErrorStatus = false,
                    Message = "تم تحديث الصرفية بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense {ExpenseId}", id);
                return StatusCode(500, new GlobalResponse<Expense>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث الصرفية: {ex.Message}"
                });
            }
        }

        // DELETE: api/Expenses/{id}
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteExpense(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var expense = await _dbConfig.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.InsertByUserId == commercialUserId);

                if (expense == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الصرفية غير موجودة"
                    });
                }

                var expenseName = $"صرفية {expense.Category} - {expense.Amount}";
                expense.IsDeleted = true;
                expense.UpdateDate = DateTime.UtcNow;
                _dbConfig.Expenses.Update(expense);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Delete",
                    "Expense",
                    expense.Id,
                    expenseName,
                    userId,
                    commercialUserId,
                    null,
                    null,
                    $"تم حذف الصرفية: {expense.Category} - {expense.Amount}"
                );

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف الصرفية بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense {ExpenseId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف الصرفية: {ex.Message}"
                });
            }
        }

        // GET: api/Expenses/Statistics
        [AuthorizeSection("expenses", "reports", Roles = "Commercial,Admin")]
        [HttpGet("Statistics")]
        public async Task<ActionResult<GlobalResponse<object>>> GetExpensesStatistics(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var query = _dbConfig.Expenses
                    .Where(e => !e.IsDeleted && e.InsertByUserId == commercialUserId);

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.Date.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.Date.Date <= endDate.Value.Date);
                }

                var expenses = await query.ToListAsync();

                var totalExpenses = expenses.Sum(e => e.Amount);
                var totalCount = expenses.Count;

                // Statistics by category
                var expensesByCategory = expenses
                    .GroupBy(e => e.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        TotalAmount = g.Sum(e => e.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();

                // This month expenses
                var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var thisMonthExpenses = expenses
                    .Where(e => e.Date.Date >= thisMonthStart.Date)
                    .Sum(e => e.Amount);

                // This week expenses
                var thisWeekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
                var thisWeekExpenses = expenses
                    .Where(e => e.Date.Date >= thisWeekStart.Date)
                    .Sum(e => e.Amount);

                // Top category
                var topCategory = expensesByCategory.FirstOrDefault();

                var statistics = new
                {
                    TotalExpenses = totalExpenses,
                    TotalCount = totalCount,
                    ThisMonthExpenses = thisMonthExpenses,
                    ThisWeekExpenses = thisWeekExpenses,
                    TopCategory = topCategory?.Category ?? "",
                    TopCategoryAmount = topCategory?.TotalAmount ?? 0,
                    ExpensesByCategory = expensesByCategory
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = statistics,
                    ErrorStatus = false,
                    Message = "تم جلب إحصائيات الصرفيات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expenses statistics");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب إحصائيات الصرفيات: {ex.Message}"
                });
            }
        }

        // GET: api/Expenses/Export
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpGet("Export")]
        public async Task<ActionResult> ExportExpenses(
            string? category = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string format = "csv")
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var query = _dbConfig.Expenses
                    .Where(e => !e.IsDeleted && e.InsertByUserId == commercialUserId);

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(e => e.Category == category);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.Date.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.Date.Date <= endDate.Value.Date);
                }

                var expenses = await query
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.InsertDate)
                    .ToListAsync();

                if (format.ToLower() == "csv")
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("التاريخ,الفئة,المبلغ,الوصف");
                    
                    foreach (var expense in expenses)
                    {
                        var date = expense.Date.ToString("yyyy-MM-dd");
                        var expenseCategory = expense.Category;
                        var amount = expense.Amount.ToString("F2");
                        var description = expense.Description?.Replace(",", ";") ?? "";
                        csv.AppendLine($"{date},{expenseCategory},{amount},{description}");
                    }

                    var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                    return File(bytes, "text/csv", $"expenses_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                }
                else
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "صيغة التصدير غير مدعومة. استخدم CSV"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting expenses");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تصدير الصرفيات: {ex.Message}"
                });
            }
        }
    }
}


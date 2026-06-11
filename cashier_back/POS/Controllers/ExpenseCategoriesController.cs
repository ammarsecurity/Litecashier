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

namespace POS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class ExpenseCategoriesController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<ExpenseCategoriesController> _logger;

        public ExpenseCategoriesController(ILogger<ExpenseCategoriesController> logger, DbConfig dbConfig)
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

        // GET: api/ExpenseCategories
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<ExpenseCategory>>>> GetExpenseCategories()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var categories = await _dbConfig.ExpenseCategories
                    .Where(c => !c.IsDeleted && c.InsertByUserId == commercialUserId)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<ExpenseCategory>>
                {
                    Data = categories,
                    ErrorStatus = false,
                    Message = "تم جلب قائمة الفئات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense categories");
                return StatusCode(500, new GlobalResponse<List<ExpenseCategory>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب قائمة الفئات: {ex.Message}"
                });
            }
        }

        // GET: api/ExpenseCategories/{id}
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<ExpenseCategory>>> GetExpenseCategory(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var category = await _dbConfig.ExpenseCategories
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.InsertByUserId == commercialUserId);

                if (category == null)
                {
                    return NotFound(new GlobalResponse<ExpenseCategory>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفئة غير موجودة"
                    });
                }

                return Ok(new GlobalResponse<ExpenseCategory>
                {
                    Data = category,
                    ErrorStatus = false,
                    Message = "تم جلب بيانات الفئة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense category {CategoryId}", id);
                return StatusCode(500, new GlobalResponse<ExpenseCategory>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب بيانات الفئة: {ex.Message}"
                });
            }
        }

        // POST: api/ExpenseCategories
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<ExpenseCategory>>> AddExpenseCategory([FromBody] ExpenseCategoryRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<ExpenseCategory>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم الفئة مطلوب"
                    });
                }

                // Check if category name already exists
                var existingCategory = await _dbConfig.ExpenseCategories
                    .FirstOrDefaultAsync(c => !c.IsDeleted && 
                        c.InsertByUserId == commercialUserId && 
                        c.Name.Trim().ToLower() == request.Name.Trim().ToLower());

                if (existingCategory != null)
                {
                    return BadRequest(new GlobalResponse<ExpenseCategory>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفئة موجودة بالفعل"
                    });
                }

                var category = new ExpenseCategory
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Color = request.Color?.Trim(),
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _dbConfig.ExpenseCategories.Add(category);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<ExpenseCategory>
                {
                    Data = category,
                    ErrorStatus = false,
                    Message = "تم إضافة الفئة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding expense category");
                return StatusCode(500, new GlobalResponse<ExpenseCategory>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة الفئة: {ex.Message}"
                });
            }
        }

        // PUT: api/ExpenseCategories/{id}
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<ExpenseCategory>>> UpdateExpenseCategory(int id, [FromBody] ExpenseCategoryRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var category = await _dbConfig.ExpenseCategories
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.InsertByUserId == commercialUserId);

                if (category == null)
                {
                    return NotFound(new GlobalResponse<ExpenseCategory>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفئة غير موجودة"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<ExpenseCategory>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم الفئة مطلوب"
                    });
                }

                // Check if category name already exists (excluding current category)
                var existingCategory = await _dbConfig.ExpenseCategories
                    .FirstOrDefaultAsync(c => !c.IsDeleted && 
                        c.Id != id &&
                        c.InsertByUserId == commercialUserId && 
                        c.Name.Trim().ToLower() == request.Name.Trim().ToLower());

                if (existingCategory != null)
                {
                    return BadRequest(new GlobalResponse<ExpenseCategory>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفئة موجودة بالفعل"
                    });
                }

                // Store old values for audit log
                var oldValues = new
                {
                    Name = category.Name,
                    Description = category.Description,
                    Color = category.Color
                };

                category.Name = request.Name.Trim();
                category.Description = request.Description?.Trim();
                category.Color = request.Color?.Trim();
                category.UpdateDate = DateTime.UtcNow;

                // Store new values for audit log
                var newValues = new
                {
                    Name = category.Name,
                    Description = category.Description,
                    Color = category.Color
                };

                _dbConfig.ExpenseCategories.Update(category);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Update",
                    "ExpenseCategory",
                    category.Id,
                    category.Name,
                    userId,
                    commercialUserId,
                    oldValues,
                    newValues,
                    $"تم تعديل فئة المصروفات: {category.Name}"
                );

                return Ok(new GlobalResponse<ExpenseCategory>
                {
                    Data = category,
                    ErrorStatus = false,
                    Message = "تم تحديث الفئة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense category {CategoryId}", id);
                return StatusCode(500, new GlobalResponse<ExpenseCategory>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث الفئة: {ex.Message}"
                });
            }
        }

        // DELETE: api/ExpenseCategories/{id}
        [AuthorizeSection("expenses", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteExpenseCategory(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var category = await _dbConfig.ExpenseCategories
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.InsertByUserId == commercialUserId);

                if (category == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفئة غير موجودة"
                    });
                }

                // Check if category is used in any expenses
                var expensesCount = await _dbConfig.Expenses
                    .CountAsync(e => !e.IsDeleted && 
                        e.InsertByUserId == commercialUserId && 
                        e.Category == category.Name);

                if (expensesCount > 0)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = $"لا يمكن حذف الفئة لأنها مستخدمة في {expensesCount} صرفية"
                    });
                }

                var categoryName = category.Name;
                category.IsDeleted = true;
                category.UpdateDate = DateTime.UtcNow;
                _dbConfig.ExpenseCategories.Update(category);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Delete",
                    "ExpenseCategory",
                    category.Id,
                    categoryName,
                    userId,
                    commercialUserId,
                    null,
                    null,
                    $"تم حذف فئة المصروفات: {categoryName}"
                );

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف الفئة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense category {CategoryId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف الفئة: {ex.Message}"
                });
            }
        }
    }
}


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
    public class CustomersController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ILogger<CustomersController> logger, DbConfig dbConfig)
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
            if (commercialId == 0)
            {
                commercialId = userId;
            }
            return commercialId;
        }

        [AuthorizeSection("customers", Roles = "Commercial,Admin,POS")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<Customer>>>> GetCustomers([FromQuery] string? search = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                var query = _dbConfig.Customers.AsQueryable()
                    .Where(c => !c.IsDeleted && c.InsertByUserId == commercialUserId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.Trim().ToLower();
                    query = query.Where(c =>
                        c.Name.ToLower().Contains(q) ||
                        c.PhoneNumber.ToLower().Contains(q));
                }

                var list = await query
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<Customer>>
                {
                    Data = list,
                    ErrorStatus = false,
                    Message = "تم جلب قائمة العملاء بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return StatusCode(500, new GlobalResponse<List<Customer>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب قائمة العملاء: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("customers", Roles = "Commercial,Admin,POS")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<Customer>>> GetCustomer(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                var customer = await _dbConfig.Customers
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.InsertByUserId == commercialUserId);

                if (customer == null)
                {
                    return NotFound(new GlobalResponse<Customer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "العميل غير موجود"
                    });
                }

                return Ok(new GlobalResponse<Customer>
                {
                    Data = customer,
                    ErrorStatus = false,
                    Message = "تم جلب بيانات العميل بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {CustomerId}", id);
                return StatusCode(500, new GlobalResponse<Customer>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب بيانات العميل: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("customers", Roles = "Commercial,Admin,POS")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<Customer>>> AddCustomer([FromBody] CustomerRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<Customer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم العميل مطلوب"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new GlobalResponse<Customer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رقم الهاتف مطلوب"
                    });
                }

                var phone = request.PhoneNumber.Trim();
                var duplicate = await _dbConfig.Customers.AnyAsync(c =>
                    !c.IsDeleted &&
                    c.InsertByUserId == commercialUserId &&
                    c.PhoneNumber == phone);

                if (duplicate)
                {
                    return BadRequest(new GlobalResponse<Customer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يوجد عميل بنفس رقم الهاتف"
                    });
                }

                var customer = new Customer
                {
                    Name = request.Name.Trim(),
                    PhoneNumber = phone,
                    Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    IsActive = request.IsActive ?? true,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _dbConfig.Customers.Add(customer);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<Customer>
                {
                    Data = customer,
                    ErrorStatus = false,
                    Message = "تم إضافة العميل بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer");
                return StatusCode(500, new GlobalResponse<Customer>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة العميل: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("customers", Roles = "Commercial,Admin,POS")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<Customer>>> UpdateCustomer(int id, [FromBody] CustomerRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                var customer = await _dbConfig.Customers
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.InsertByUserId == commercialUserId);

                if (customer == null)
                {
                    return NotFound(new GlobalResponse<Customer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "العميل غير موجود"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<Customer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم العميل مطلوب"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new GlobalResponse<Customer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رقم الهاتف مطلوب"
                    });
                }

                var phone = request.PhoneNumber.Trim();
                var phoneTaken = await _dbConfig.Customers.AnyAsync(c =>
                    !c.IsDeleted &&
                    c.InsertByUserId == commercialUserId &&
                    c.Id != id &&
                    c.PhoneNumber == phone);

                if (phoneTaken)
                {
                    return BadRequest(new GlobalResponse<Customer>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يوجد عميل آخر بنفس رقم الهاتف"
                    });
                }

                customer.Name = request.Name.Trim();
                customer.PhoneNumber = phone;
                customer.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
                customer.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
                if (request.IsActive.HasValue)
                {
                    customer.IsActive = request.IsActive.Value;
                }
                customer.UpdateDate = DateTime.UtcNow;

                _dbConfig.Customers.Update(customer);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<Customer>
                {
                    Data = customer,
                    ErrorStatus = false,
                    Message = "تم تحديث بيانات العميل بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", id);
                return StatusCode(500, new GlobalResponse<Customer>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث بيانات العميل: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("customers", Roles = "Commercial,Admin,POS")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteCustomer(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                var customer = await _dbConfig.Customers
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.InsertByUserId == commercialUserId);

                if (customer == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "العميل غير موجود"
                    });
                }

                customer.IsDeleted = true;
                customer.UpdateDate = DateTime.UtcNow;
                _dbConfig.Customers.Update(customer);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف العميل بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف العميل: {ex.Message}"
                });
            }
        }
    }
}

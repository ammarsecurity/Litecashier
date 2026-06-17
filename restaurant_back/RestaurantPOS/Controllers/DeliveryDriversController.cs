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
    public class DeliveryDriversController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<DeliveryDriversController> _logger;

        public DeliveryDriversController(ILogger<DeliveryDriversController> logger, DbConfig dbConfig)
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

        private static decimal ResolveOrderSalesAmount(CustomerOrder order)
        {
            if (order.OrderTotalAfterDiscount.HasValue)
                return order.OrderTotalAfterDiscount.Value;
            if (order.OrderSubTotal.HasValue)
                return order.OrderSubTotal.Value;
            return order.CustomerOrderItem?
                .Where(item => !item.IsDeleted)
                .Sum(item => item.SellingPrice * item.Quantity) ?? 0;
        }

        // GET: api/DeliveryDrivers
        [AuthorizeSection("deliveryDrivers", "reports", Roles = "Commercial,Admin,POS,Waiter")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<DeliveryDriver>>>> GetDeliveryDrivers()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var drivers = await _dbConfig.DeliveryDrivers
                    .Where(d => !d.IsDeleted && d.InsertByUserId == commercialUserId)
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<DeliveryDriver>>
                {
                    Data = drivers,
                    ErrorStatus = false,
                    Message = "تم جلب قائمة السائقين بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery drivers");
                return StatusCode(500, new GlobalResponse<List<DeliveryDriver>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب قائمة السائقين: {ex.Message}"
                });
            }
        }

        // GET: api/DeliveryDrivers/{id}
        [AuthorizeSection("deliveryDrivers", Roles = "Commercial,Admin,POS,Waiter")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<DeliveryDriver>>> GetDeliveryDriver(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var driver = await _dbConfig.DeliveryDrivers
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.InsertByUserId == commercialUserId);

                if (driver == null)
                {
                    return NotFound(new GlobalResponse<DeliveryDriver>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "السائق غير موجود"
                    });
                }

                return Ok(new GlobalResponse<DeliveryDriver>
                {
                    Data = driver,
                    ErrorStatus = false,
                    Message = "تم جلب بيانات السائق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery driver {DriverId}", id);
                return StatusCode(500, new GlobalResponse<DeliveryDriver>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب بيانات السائق: {ex.Message}"
                });
            }
        }

        // POST: api/DeliveryDrivers
        [AuthorizeSection("deliveryDrivers", Roles = "Commercial,Admin,POS,Waiter")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<DeliveryDriver>>> AddDeliveryDriver([FromBody] DeliveryDriverRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<DeliveryDriver>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم السائق مطلوب"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new GlobalResponse<DeliveryDriver>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رقم الهاتف مطلوب"
                    });
                }

                var driver = new DeliveryDriver
                {
                    Name = request.Name.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Address = request.Address?.Trim(),
                    VehicleType = request.VehicleType?.Trim(),
                    VehicleNumber = request.VehicleNumber?.Trim(),
                    Notes = request.Notes?.Trim(),
                    IsActive = request.IsActive ?? true,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _dbConfig.DeliveryDrivers.Add(driver);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<DeliveryDriver>
                {
                    Data = driver,
                    ErrorStatus = false,
                    Message = "تم إضافة السائق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding delivery driver: {Exception}", ex);
                return StatusCode(500, new GlobalResponse<DeliveryDriver>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة السائق: {ex.Message}"
                });
            }
        }

        // PUT: api/DeliveryDrivers/{id}
        [AuthorizeSection("deliveryDrivers", Roles = "Commercial,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<DeliveryDriver>>> UpdateDeliveryDriver(int id, [FromBody] DeliveryDriverRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var driver = await _dbConfig.DeliveryDrivers
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.InsertByUserId == commercialUserId);

                if (driver == null)
                {
                    return NotFound(new GlobalResponse<DeliveryDriver>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "السائق غير موجود"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new GlobalResponse<DeliveryDriver>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم السائق مطلوب"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new GlobalResponse<DeliveryDriver>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رقم الهاتف مطلوب"
                    });
                }

                // Store old values for audit log
                var oldValues = new
                {
                    Name = driver.Name,
                    PhoneNumber = driver.PhoneNumber,
                    Address = driver.Address,
                    VehicleType = driver.VehicleType,
                    VehicleNumber = driver.VehicleNumber,
                    Notes = driver.Notes,
                    IsActive = driver.IsActive
                };

                driver.Name = request.Name.Trim();
                driver.PhoneNumber = request.PhoneNumber.Trim();
                driver.Address = request.Address?.Trim();
                driver.VehicleType = request.VehicleType?.Trim();
                driver.VehicleNumber = request.VehicleNumber?.Trim();
                driver.Notes = request.Notes?.Trim();
                if (request.IsActive.HasValue)
                {
                    driver.IsActive = request.IsActive.Value;
                }
                driver.UpdateDate = DateTime.UtcNow;

                // Store new values for audit log
                var newValues = new
                {
                    Name = driver.Name,
                    PhoneNumber = driver.PhoneNumber,
                    Address = driver.Address,
                    VehicleType = driver.VehicleType,
                    VehicleNumber = driver.VehicleNumber,
                    Notes = driver.Notes,
                    IsActive = driver.IsActive
                };

                _dbConfig.DeliveryDrivers.Update(driver);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Update",
                    "DeliveryDriver",
                    driver.Id,
                    driver.Name,
                    userId,
                    commercialUserId,
                    oldValues,
                    newValues,
                    $"تم تعديل بيانات السائق: {driver.Name}"
                );

                return Ok(new GlobalResponse<DeliveryDriver>
                {
                    Data = driver,
                    ErrorStatus = false,
                    Message = "تم تحديث بيانات السائق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating delivery driver {DriverId}", id);
                return StatusCode(500, new GlobalResponse<DeliveryDriver>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث بيانات السائق: {ex.Message}"
                });
            }
        }

        // DELETE: api/DeliveryDrivers/{id}
        [AuthorizeSection("deliveryDrivers", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteDeliveryDriver(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var driver = await _dbConfig.DeliveryDrivers
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.InsertByUserId == commercialUserId);

                if (driver == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "السائق غير موجود"
                    });
                }

                var driverName = driver.Name;
                driver.IsDeleted = true;
                driver.UpdateDate = DateTime.UtcNow;
                _dbConfig.DeliveryDrivers.Update(driver);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Delete",
                    "DeliveryDriver",
                    driver.Id,
                    driverName,
                    userId,
                    commercialUserId,
                    null,
                    null,
                    $"تم حذف السائق: {driverName}"
                );

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف السائق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting delivery driver {DriverId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف السائق: {ex.Message}"
                });
            }
        }

        // GET: api/DeliveryDrivers/{id}/Statistics
        [AuthorizeSection("deliveryDrivers", Roles = "Commercial,Admin")]
        [HttpGet("{id}/Statistics")]
        public async Task<ActionResult<GlobalResponse<object>>> GetDriverStatistics(int id, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var driver = await _dbConfig.DeliveryDrivers
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.InsertByUserId == commercialUserId);

                if (driver == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "السائق غير موجود"
                    });
                }

                var ordersQuery = _dbConfig.CustomerOrders
                    .Include(o => o.CustomerOrderItem)
                    .Where(o => o.DeliveryDriverId == id 
                        && !o.IsDeleted 
                        && o.InsertByUserId == commercialUserId);

                if (startDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.InsertDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.InsertDate.Date <= endDate.Value.Date);
                }

                var orders = await ordersQuery.ToListAsync();

                var totalOrders = orders.Count;
                // Count delivered orders: DeliveryStatus is Delivered/Completed OR OrderStatus is Completed
                var deliveredOrders = orders.Count(o => 
                    o.DeliveryStatus == "Delivered" || 
                    o.DeliveryStatus == "Completed" ||
                    o.OrderStatus == "Completed");
                // Count pending orders: DeliveryStatus is Pending/InTransit AND OrderStatus is not Completed
                var pendingOrders = orders.Count(o => 
                    (o.DeliveryStatus == "Pending" || o.DeliveryStatus == "InTransit") &&
                    o.OrderStatus != "Completed");
                var failedOrders = orders.Count(o => o.DeliveryStatus == "Failed");
                var completedOrders = orders.Count(o => 
                    o.DeliveryStatus == "Completed" || 
                    o.OrderStatus == "Completed");

                var totalAmount = orders
                    .Where(o => o.DeliveryStatus == "Delivered" || 
                               o.DeliveryStatus == "Completed" ||
                               o.OrderStatus == "Completed")
                    .Sum(ResolveOrderSalesAmount);

                var paidAmount = orders
                    .Where(o => (o.DeliveryStatus == "Delivered" || 
                                o.DeliveryStatus == "Completed" ||
                                o.OrderStatus == "Completed") 
                        && o.PaymentStatus == "Paid")
                    .Sum(ResolveOrderSalesAmount);

                var remainingAmount = totalAmount - paidAmount;

                var statistics = new
                {
                    DriverId = driver.Id,
                    DriverName = driver.Name,
                    TotalOrders = totalOrders,
                    DeliveredOrders = deliveredOrders,
                    PendingOrders = pendingOrders,
                    FailedOrders = failedOrders,
                    CompletedOrders = completedOrders,
                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    RemainingAmount = remainingAmount
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = statistics,
                    ErrorStatus = false,
                    Message = "تم جلب إحصائيات السائق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting driver statistics {DriverId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب إحصائيات السائق: {ex.Message}"
                });
            }
        }

        // GET: api/DeliveryDrivers/Statistics/All
        [AuthorizeSection("deliveryDrivers", "reports", Roles = "Commercial,Admin")]
        [HttpGet("Statistics/All")]
        public async Task<ActionResult<GlobalResponse<object>>> GetAllDriversStatistics()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var drivers = await _dbConfig.DeliveryDrivers
                    .Where(d => !d.IsDeleted && d.InsertByUserId == commercialUserId)
                    .ToListAsync();

                var allOrders = await _dbConfig.CustomerOrders
                    .Include(o => o.CustomerOrderItem)
                    .Where(o => o.DeliveryDriverId.HasValue 
                        && !o.IsDeleted 
                        && o.InsertByUserId == commercialUserId)
                    .ToListAsync();

                var statistics = drivers.Select(driver =>
                {
                    var driverOrders = allOrders.Where(o => o.DeliveryDriverId == driver.Id).ToList();
                    
                    var totalOrders = driverOrders.Count;
                    // Count delivered orders: DeliveryStatus is Delivered/Completed OR OrderStatus is Completed
                    var deliveredOrders = driverOrders.Count(o => 
                        o.DeliveryStatus == "Delivered" || 
                        o.DeliveryStatus == "Completed" ||
                        o.OrderStatus == "Completed");
                    // Count pending orders: DeliveryStatus is Pending/InTransit AND OrderStatus is not Completed
                    var pendingOrders = driverOrders.Count(o => 
                        (o.DeliveryStatus == "Pending" || o.DeliveryStatus == "InTransit") &&
                        o.OrderStatus != "Completed");
                    var failedOrders = driverOrders.Count(o => o.DeliveryStatus == "Failed");
                    var completedOrders = driverOrders.Count(o => 
                        o.DeliveryStatus == "Completed" || 
                        o.OrderStatus == "Completed");

                    var totalAmount = driverOrders
                        .Where(o => o.DeliveryStatus == "Delivered" || 
                                   o.DeliveryStatus == "Completed" ||
                                   o.OrderStatus == "Completed")
                        .Sum(ResolveOrderSalesAmount);

                    var paidAmount = driverOrders
                        .Where(o => (o.DeliveryStatus == "Delivered" || 
                                    o.DeliveryStatus == "Completed" ||
                                    o.OrderStatus == "Completed") 
                            && o.PaymentStatus == "Paid")
                        .Sum(ResolveOrderSalesAmount);

                    var remainingAmount = totalAmount - paidAmount;

                    return new
                    {
                        DriverId = driver.Id,
                        DriverName = driver.Name,
                        PhoneNumber = driver.PhoneNumber,
                        IsActive = driver.IsActive,
                        TotalOrders = totalOrders,
                        DeliveredOrders = deliveredOrders,
                        PendingOrders = pendingOrders,
                        FailedOrders = failedOrders,
                        CompletedOrders = completedOrders,
                        TotalAmount = totalAmount,
                        PaidAmount = paidAmount,
                        RemainingAmount = remainingAmount
                    };
                }).ToList();

                var overallStats = new
                {
                    TotalDrivers = drivers.Count,
                    ActiveDrivers = drivers.Count(d => d.IsActive),
                    TotalOrders = allOrders.Count,
                    // Count delivered orders: DeliveryStatus is Delivered/Completed OR OrderStatus is Completed
                    DeliveredOrders = allOrders.Count(o => 
                        o.DeliveryStatus == "Delivered" || 
                        o.DeliveryStatus == "Completed" ||
                        o.OrderStatus == "Completed"),
                    // Count pending orders: DeliveryStatus is Pending/InTransit AND OrderStatus is not Completed
                    PendingOrders = allOrders.Count(o => 
                        (o.DeliveryStatus == "Pending" || o.DeliveryStatus == "InTransit") &&
                        o.OrderStatus != "Completed"),
                    FailedOrders = allOrders.Count(o => o.DeliveryStatus == "Failed"),
                    Drivers = statistics
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = overallStats,
                    ErrorStatus = false,
                    Message = "تم جلب إحصائيات جميع السائقين بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all drivers statistics");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب إحصائيات السائقين: {ex.Message}"
                });
            }
        }
    }
}

using AutoMapper;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Authorization;
using POS.Db;
using POS.Models;
using POS.Models.Dtos;
using POS.Models.Requests;
using POS.Models.Response;
using POS.Services;
using System.Security.Claims;
using System.Text;

namespace POS.Controllers
{
    [ApiController]
    [Route("[controller]")]
   // [Authorize(Roles = "Admin")]
    [EnableCors("CorsPolicy")]
    public class AdminController : ControllerBase
    {

        private readonly DbConfig _dbConfig;
        private readonly ILogger<AdminController> _logger;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IOrderCheckoutService _orderCheckoutService;
        private readonly IItemImportService _itemImportService;
        private readonly ICommercialCatalogClearService _catalogClearService;

        public AdminController(
            ILogger<AdminController> logger,
            DbConfig dbConfig,
            IMapper mapper,
            IConfiguration configuration,
            IOrderCheckoutService orderCheckoutService,
            IItemImportService itemImportService,
            ICommercialCatalogClearService catalogClearService)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
            _configuration = configuration;
            _orderCheckoutService = orderCheckoutService;
            _itemImportService = itemImportService;
            _catalogClearService = catalogClearService;
        }

        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId && !x.IsDeleted);

            if (user != null && user.Role == "Commercial")
                return userId;

            return user?.InsertByUserId ?? userId;
        }

        private async Task<(bool Ok, string? ErrorKey)> TryVerifySensitiveCredentialAsync(int commercialUserId, string password)
        {
            var commercial = await _dbConfig.Users
                .FirstOrDefaultAsync(u => u.Id == commercialUserId && !u.IsDeleted);

            if (commercial != null
                && !string.IsNullOrWhiteSpace(commercial.Password)
                && BCrypt.Net.BCrypt.Verify(password, commercial.Password))
            {
                return (true, null);
            }

            var managers = await _dbConfig.Users
                .Where(u => !u.IsDeleted
                    && u.InsertByUserId == commercialUserId
                    && u.Role == SectionDefinitions.ManagerRole)
                .ToListAsync();

            var submittedCode = NormalizeLoginCode(password);

            foreach (var manager in managers)
            {
                if (manager.CanUseOwnLoginCodeForSensitiveActions
                    && !string.IsNullOrWhiteSpace(manager.LoginCode)
                    && submittedCode != null
                    && submittedCode == manager.LoginCode)
                {
                    return (true, null);
                }
            }

            foreach (var manager in managers)
            {
                if (!string.IsNullOrWhiteSpace(manager.Password)
                    && BCrypt.Net.BCrypt.Verify(password, manager.Password))
                {
                    return (true, null);
                }
            }

            return (false, "invalidSensitiveAuth");
        }

        private static string? NormalizeLoginCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            if (s.Length < 4 || s.Length > 12 || !s.All(char.IsDigit)) return null;
            return s;
        }

        private static bool IsManagerRole(string? role) =>
            string.Equals(role, SectionDefinitions.ManagerRole, StringComparison.OrdinalIgnoreCase);

        private async Task<(bool Ok, string? ErrorMessage)> ApplyManagerSensitiveLoginCodeSettingsAsync(
            User user,
            string role,
            string? loginCodeRaw,
            bool? canUseOwnLoginCode,
            int? excludeUserId = null)
        {
            if (!IsManagerRole(role))
            {
                user.CanUseOwnLoginCodeForSensitiveActions = false;
                if (!string.Equals(role, "Commercial", StringComparison.OrdinalIgnoreCase))
                    user.LoginCode = null;
                return (true, null);
            }

            user.CanUseOwnLoginCodeForSensitiveActions = canUseOwnLoginCode == true;

            if (!user.CanUseOwnLoginCodeForSensitiveActions)
                return (true, null);

            string? lc = null;
            if (!string.IsNullOrWhiteSpace(loginCodeRaw))
            {
                lc = NormalizeLoginCode(loginCodeRaw);
                if (lc == null)
                    return (false, "رمز الدخول يجب أن يكون من 4 إلى 12 رقماً");
            }
            else if (!string.IsNullOrWhiteSpace(user.LoginCode))
            {
                lc = user.LoginCode;
            }

            if (string.IsNullOrWhiteSpace(lc))
                return (false, "managerLoginCodeRequiredForSensitiveActions");

            var duplicateQuery = _dbConfig.Users.Where(u => u.LoginCode == lc && !u.IsDeleted);
            if (excludeUserId.HasValue)
                duplicateQuery = duplicateQuery.Where(u => u.Id != excludeUserId.Value);

            if (await duplicateQuery.AnyAsync())
                return (false, "رمز الدخول مستخدم من حساب آخر");

            user.LoginCode = lc;
            return (true, null);
        }

        private async Task<EndOfDayReportDto> BuildEndOfDayReportAsync(int commercialUserId)
        {
            var dayStart = DateTime.UtcNow.Date;
            var dayEnd = dayStart.AddDays(1);

            var orders = await _dbConfig.CustomerOrders
                .Where(o =>
                    !o.IsDeleted &&
                    (o.InsertByUserId == commercialUserId || o.User!.InsertByUserId == commercialUserId) &&
                    o.InsertDate >= dayStart &&
                    o.InsertDate < dayEnd)
                .ToListAsync();

            var orderIds = orders.Select(o => o.Id).ToList();
            var orderItems = orderIds.Any()
                ? await _dbConfig.CustomerOrderItems
                    .Include(oi => oi.Item)
                    .Where(oi => !oi.IsDeleted && orderIds.Contains(oi.CustomerOrderId))
                    .ToListAsync()
                : new List<CustomerOrderItem>();

            var itemsCount = orderItems.Count;
            var itemsQuantity = orderItems.Sum(x => x.Quantity);
            var grossSales = orderItems.Sum(x => x.SellingPrice * x.Quantity);
            var discountAmount = orders.Sum(x => x.DiscountAmount ?? 0m);
            var netSales = Math.Max(0m, grossSales - discountAmount);
            var totalCost = orderItems.Sum(x => x.PurchasingPrice * x.Quantity);
            var profit = netSales - totalCost;

            var paymentBreakdown = orders
                .GroupBy(x => string.IsNullOrWhiteSpace(x.PaymentMethod) ? "Cash" : x.PaymentMethod)
                .Select(g =>
                {
                    var groupOrderIds = g.Select(x => x.Id).ToHashSet();
                    var amount = orderItems
                        .Where(oi => groupOrderIds.Contains(oi.CustomerOrderId))
                        .Sum(oi => oi.SellingPrice * oi.Quantity);
                    return new EndOfDayPaymentDto
                    {
                        Method = g.Key,
                        OrdersCount = g.Count(),
                        Amount = amount
                    };
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            var topItems = orderItems
                .GroupBy(x => new { x.ItemId, ItemName = x.Item != null ? x.Item.Name : $"#{x.ItemId}" })
                .Select(g => new EndOfDayTopItemDto
                {
                    ItemId = g.Key.ItemId,
                    ItemName = g.Key.ItemName ?? string.Empty,
                    Quantity = g.Sum(x => x.Quantity),
                    SalesAmount = g.Sum(x => x.SellingPrice * x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ThenByDescending(x => x.SalesAmount)
                .Take(10)
                .ToList();

            return new EndOfDayReportDto
            {
                DayStart = dayStart,
                DayEnd = dayEnd.AddSeconds(-1),
                Totals = new EndOfDayTotalsDto
                {
                    OrdersCount = orders.Count,
                    ItemsCount = itemsCount,
                    ItemsQuantity = itemsQuantity,
                    GrossSales = grossSales,
                    DiscountAmount = discountAmount,
                    NetSales = netSales,
                    TotalCost = totalCost,
                    Profit = profit,
                    ReturnedAmount = 0,
                    ReturnedCount = 0
                },
                PaymentBreakdown = paymentBreakdown,
                TopItems = topItems
            };
        }

        private bool TryGetOrderInsertUtcRange(DateTime? startDate, DateTime? endDate, out DateTime fromUtc, out DateTime toUtcExclusive)
        {
            fromUtc = default;
            toUtcExclusive = default;

            if (!startDate.HasValue && !endDate.HasValue)
                return false;

            DateTime startDay;
            DateTime endDay;

            if (startDate.HasValue && endDate.HasValue)
            {
                startDay = startDate.Value.Date;
                endDay = endDate.Value.Date;
                if (endDay < startDay)
                    (startDay, endDay) = (endDay, startDay);
            }
            else if (startDate.HasValue)
            {
                startDay = endDay = startDate.Value.Date;
            }
            else
                return false;

            var tzId = (_configuration["BusinessSettings:TimeZoneId"] ?? "").Trim();
            TimeZoneInfo tz;
            try
            {
                tz = !string.IsNullOrEmpty(tzId)
                    ? TimeZoneInfo.FindSystemTimeZoneById(tzId)
                    : TimeZoneInfo.Local;
            }
            catch
            {
                tz = TimeZoneInfo.Local;
            }

            var localStart = DateTime.SpecifyKind(startDay, DateTimeKind.Unspecified);
            var localEndExclusive = DateTime.SpecifyKind(endDay.AddDays(1), DateTimeKind.Unspecified);
            fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
            toUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(localEndExclusive, tz);
            return true;
        }

        private static List<CustomerOrderItem> GetActiveOrderItems(IEnumerable<CustomerOrderItem>? items)
        {
            return items?
                .Where(item => item != null && !item.IsDeleted)
                .ToList() ?? new List<CustomerOrderItem>();
        }

        private IQueryable<CustomerOrderItem> QueryActiveOrderItemsForCommercial(int userId, int userInsertByUserId)
        {
            return _dbConfig.CustomerOrderItems
                .Where(x => !x.IsDeleted &&
                            x.CustomerOrder != null &&
                            !x.CustomerOrder.IsDeleted &&
                            (x.InsertByUserId == userId ||
                             x.User!.Id == userInsertByUserId ||
                             x.User!.InsertByUserId == userId));
        }

        private IQueryable<CustomerOrder> QueryActiveOrdersForCommercial(int userId)
        {
            return _dbConfig.CustomerOrders
                .Where(x => !x.IsDeleted &&
                            (x.InsertByUserId == userId || x.User!.InsertByUserId == userId));
        }

        private decimal SumOrdersSalesAmount(IQueryable<CustomerOrder> ordersQuery)
        {
            return ordersQuery
                .Select(o => o.OrderTotalAfterDiscount
                    ?? o.OrderSubTotal
                    ?? _dbConfig.CustomerOrderItems
                        .Where(i => i.CustomerOrderId == o.Id && !i.IsDeleted)
                        .Sum(i => (decimal?)(i.Quantity * i.SellingPrice))
                    ?? 0m)
                .Sum();
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("assignable-sections")]
        public ActionResult<GlobalResponse<object>> GetAssignableSections()
        {
            return Ok(new GlobalResponse<object>
            {
                Data = new { keys = SectionDefinitions.AssignableSectionKeys },
                ErrorStatus = false,
                Message = "done"
            });
        }

        // Add User
        [Authorize(Roles = "Commercial,Admin")]
        [HttpPost("AddUser")]
        public async Task<ActionResult<GlobalResponse<User>>> AddUser([FromForm] UserRequest request)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var currentUser = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);

                if (request.Role == "Commercial" && currentUser?.Role != "Admin")
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لإضافة مستخدمين تجاريين. فقط المدير الرئيسي يمكنه ذلك"
                    });
                }

                var commercialUserId = GetCommercialUserId();
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber && x.IsDeleted == false);

                if (currentUser?.Role == "Admin")
                {
                    if (user != null)
                    {
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = user,
                            ErrorStatus = true,
                            Message = "رقم الهاتف موجود بالفعل"
                        });
                    }
                }
                else if (user != null && user.InsertByUserId == commercialUserId)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = user,
                        ErrorStatus = true,
                        Message = "رقم الهاتف موجود بالفعل"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "كلمة المرور مطلوبة لإضافة مستخدم جديد"
                    });
                }

                var newUse = _mapper.Map<User>(request);
                newUse.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var (sectionsOk, sectionsError, sectionsJson) =
                    SectionPermissionService.ResolveManagerSectionsForSave(request.Role, request.AllowedSectionsJson);
                if (!sectionsOk)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = sectionsError ?? "selectAtLeastOneSection"
                    });
                }
                newUse.AllowedSectionsJson = sectionsJson;

                if (request.Role == "Commercial" && currentUser?.Role == "Admin")
                    newUse.InsertByUserId = currentUserId;
                else
                    newUse.InsertByUserId = commercialUserId;

                if (request.Role == "Commercial" && currentUser?.Role == "Admin" && request.Logo != null && request.Logo.Length > 0)
                {
                    try
                    {
                        newUse.Logo = await UploadIamgesAsync(request.Logo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading logo for new Commercial user");
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = $"خطأ في رفع الشعار: {ex.Message}"
                        });
                    }
                }

                if (request.Role == "Commercial" && currentUser?.Role == "Admin" && !string.IsNullOrEmpty(request.StoreName))
                    newUse.StoreName = request.StoreName;

                if (request.Role == "Commercial" && currentUser?.Role == "Admin" && !string.IsNullOrWhiteSpace(request.LoginCode))
                {
                    var lc = NormalizeLoginCode(request.LoginCode);
                    if (lc == null)
                    {
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "رمز الدخول يجب أن يكون من 4 إلى 12 رقماً"
                        });
                    }
                    if (await _dbConfig.Users.AnyAsync(u => u.LoginCode == lc && !u.IsDeleted))
                    {
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "رمز الدخول مستخدم من حساب آخر"
                        });
                    }
                    newUse.LoginCode = lc;
                }

                var (managerLoginOk, managerLoginError) = await ApplyManagerSensitiveLoginCodeSettingsAsync(
                    newUse,
                    request.Role,
                    request.LoginCode,
                    request.CanUseOwnLoginCodeForSensitiveActions);
                if (!managerLoginOk)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = managerLoginError ?? "managerLoginCodeRequiredForSensitiveActions"
                    });
                }

                _dbConfig.Users.Add(newUse);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<User>
                {
                    Data = newUse,
                    ErrorStatus = false,
                    Message = "تم إضافة المستخدم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user");
                return StatusCode(500, new GlobalResponse<User>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة المستخدم: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpPut("UpdateUser")]
        public async Task<ActionResult<GlobalResponse<User>>> UpdateUser([FromForm] UserRequest request, int id)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var currentUser = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);

                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
                if (user == null)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المستخدم غير موجود"
                    });
                }

                if (user.Role == "Commercial" && currentUser?.Role != "Admin")
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لتعديل المستخدمين التجاريين. فقط المدير الرئيسي يمكنه ذلك"
                    });
                }

                var commercialUserId = GetCommercialUserId();
                if (currentUser?.Role != "Admin" && user.Role != "Commercial" && user.InsertByUserId != commercialUserId)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لتعديل هذا المستخدم"
                    });
                }

                if (currentUser?.Role != "Admin" && request.Role == "Commercial")
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لتغيير الدور إلى تجاري. فقط المدير الرئيسي يمكنه ذلك"
                    });
                }

                var oldValues = new
                {
                    user.Name,
                    user.PhoneNumber,
                    user.Username,
                    user.Role,
                    user.StoreName,
                    user.Logo,
                    user.LoginCode
                };

                user.Name = request.Name;
                user.PhoneNumber = request.PhoneNumber;
                user.Username = request.Username;
                user.Role = request.Role;

                var (sectionsOk, sectionsError, sectionsJson) =
                    SectionPermissionService.ResolveManagerSectionsForSave(request.Role, request.AllowedSectionsJson);
                if (!sectionsOk)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = sectionsError ?? "selectAtLeastOneSection"
                    });
                }
                user.AllowedSectionsJson = sectionsJson;

                var (managerLoginOk, managerLoginError) = await ApplyManagerSensitiveLoginCodeSettingsAsync(
                    user,
                    request.Role,
                    request.LoginCode,
                    request.CanUseOwnLoginCodeForSensitiveActions,
                    id);
                if (!managerLoginOk)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = managerLoginError ?? "managerLoginCodeRequiredForSensitiveActions"
                    });
                }

                if (!string.IsNullOrWhiteSpace(request.Password))
                    user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

                if (currentUser?.Role == "Admin" && user.Role == "Commercial")
                {
                    if (request.Logo != null && request.Logo.Length > 0)
                    {
                        try
                        {
                            user.Logo = await UploadIamgesAsync(request.Logo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading logo for user {UserId}", id);
                            return BadRequest(new GlobalResponse<User>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"خطأ في رفع الشعار: {ex.Message}"
                            });
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(request.StoreName))
                        user.StoreName = request.StoreName;

                    if (string.IsNullOrWhiteSpace(request.LoginCode))
                        user.LoginCode = null;
                    else
                    {
                        var lc = NormalizeLoginCode(request.LoginCode);
                        if (lc == null)
                        {
                            return BadRequest(new GlobalResponse<User>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = "رمز الدخول يجب أن يكون من 4 إلى 12 رقماً"
                            });
                        }
                        if (await _dbConfig.Users.AnyAsync(u => u.LoginCode == lc && u.Id != id && !u.IsDeleted))
                        {
                            return BadRequest(new GlobalResponse<User>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = "رمز الدخول مستخدم من حساب آخر"
                            });
                        }
                        user.LoginCode = lc;
                    }
                }

                var newValues = new
                {
                    user.Name,
                    user.PhoneNumber,
                    user.Username,
                    user.Role,
                    user.StoreName,
                    user.Logo,
                    user.LoginCode
                };

                _dbConfig.Users.Update(user);
                await _dbConfig.SaveChangesAsync();

                await _dbConfig.LogAuditAsync(
                    "Update",
                    "User",
                    user.Id,
                    user.Name,
                    currentUserId,
                    commercialUserId,
                    oldValues,
                    newValues,
                    $"تم تعديل المستخدم: {user.Name}"
                );

                return Ok(new GlobalResponse<User>
                {
                    Data = user,
                    ErrorStatus = false,
                    Message = "تم تحديث المستخدم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new GlobalResponse<User>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث المستخدم: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpDelete("DeleteUser")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteUser(int id)
        {
            var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
            if (user == null)
            {
                return BadRequest(new GlobalResponse<User>
                {
                    Data = user,
                    ErrorStatus = true,
                    Message = "user not exsit"
                });
            }

            user!.IsDeleted = true;
            _dbConfig.Users.Update(user);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<User>
            {
                Data = user,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetUsers")]
        public ActionResult<GlobalResponse<PagedList<User>>> GetUsers(int pageNumber, int pageSize, string? info)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userInfo = _dbConfig.Users.FirstOrDefault(x => x.Id == userId && x.IsDeleted == false);

            if (userInfo.Role == "Admin")
            {
                var user = _dbConfig.Users.Where(x => x.IsDeleted == false ).AsQueryable();

                if (info != null)
                {
                    user = user.Where(x => x.PhoneNumber == info || x.Name.Contains(info) || x.Username.Contains(info));
                }
                var totalItems = user.Count();

                var pagedItems = user
                    .OrderByDescending(x => x.Id)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedList<User>(pagedItems, totalItems, pageNumber, pageSize);

                var response = new GlobalResponse<PagedList<User>>
                {
                    Data = pagedResult,
                    ErrorStatus = false,
                    Message = "Success"
                };

                return response;
            }
            else
            {
                var user = _dbConfig.Users.Where(x => x.IsDeleted == false && x.InsertByUserId == userId).AsQueryable();

                if (info != null)
                {
                    user = user.Where(x => x.PhoneNumber == info || x.Name.Contains(info) || x.Username.Contains(info));
                }
                var totalItems = user.Count();

                var pagedItems = user
                    .OrderByDescending(x => x.Id)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedList<User>(pagedItems, totalItems, pageNumber, pageSize);

                var response = new GlobalResponse<PagedList<User>>
                {
                    Data = pagedResult,
                    ErrorStatus = false,
                    Message = "Success"
                };

                return response;
            }   


        
        }


        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("AddTag")]
        public async Task<ActionResult<GlobalResponse<Tag>>> AddTag(TagRequset request)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var tag = await _dbConfig.Tags.FirstOrDefaultAsync(x => x.Name == request.Name && x.IsDeleted == false && x.InsertByUserId == userId);
            if (tag != null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = tag,
                    ErrorStatus = true,
                    Message = "Tag is already exsit"
                });
            }
            var newTag = _mapper.Map<Tag>(request);
            newTag.InsertByUserId = userId;
            _dbConfig.Tags.Add(newTag);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Tag>
            {
                Data = newTag,
                ErrorStatus = false,
                Message = "done"
            });
        }


        // updata tag
        // Update User 
        [Authorize(Roles = "Commercial")]

        [HttpPut("UpdateTag")]
        public async Task<ActionResult<GlobalResponse<Tag>>> UpdateTag(TagRequset request, int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var tag = await _dbConfig.Tags.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));
            if (tag == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = tag,
                    ErrorStatus = true,
                    Message = "tag not exsit"
                });
            }
            var uTag = _mapper.Map(request, tag);

            _dbConfig.Tags.Update(uTag);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Tag>
            {
                Data = uTag,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial")]
        [HttpDelete("DeleteTag")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteTag(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var tag = await _dbConfig.Tags.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));
            if (tag == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "tag not exsit"
                });
            }

            tag!.IsDeleted = true;
            _dbConfig.Tags.Update(tag);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Tag>
            {
                Data = tag,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpGet("GetTags")]
        public ActionResult<GlobalResponse<PagedList<Tag>>> GetTags(int pageNumber, int pageSize, string? info)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<PagedList<Tag>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var tag = _dbConfig.Tags.Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId)).AsQueryable();

            if (info != null)
            {
                tag = tag.Where(x => x.Name.Contains(info));
            }

            var totalItems = tag.Count();

            var pagedItems = tag
                .OrderByDescending(x => x.Id)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList();

            var pagedResult = new PagedList<Tag>(pagedItems, totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<Tag>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }


        // add item 
        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("AddItem")]
        public async Task<ActionResult<GlobalResponse<Item>>> AddItem([FromForm] ItemRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var itemCode = request.Code ?? RandomCode();
            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Name == request.Name && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));
            if (item != null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = item,
                    ErrorStatus = true,
                    Message = "Item is already exsit"
                });
            }
            var newItem = _mapper.Map<Item>(request);
            if(request.Image != null)
            {
                newItem.Image = await UploadIamgesAsync(request.Image);
            }
            newItem.Code = itemCode;
            newItem.InsertByUserId = userId;
            _dbConfig.Items.Add(newItem);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Item>
            {
                Data = newItem,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("ImportItems")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<GlobalResponse<ItemImportResultDto>>> ImportItems(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new GlobalResponse<ItemImportResultDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "invalidImportFile"
                });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not ".xlsx" and not ".xls")
            {
                return BadRequest(new GlobalResponse<ItemImportResultDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "invalidImportFile"
                });
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var commercialUserId = GetCommercialUserId();

                await using var stream = file.OpenReadStream();
                var importResult = await _itemImportService.ImportFromExcelAsync(stream, userId, commercialUserId);

                try
                {
                    await _dbConfig.LogAuditAsync(
                        "Import",
                        "Item",
                        0,
                        file.FileName,
                        userId,
                        commercialUserId,
                        description: $"Created={importResult.ItemsCreated}, Skipped={importResult.ItemsSkipped}, Tags={importResult.TagsCreated}, Errors={importResult.RowsWithErrors}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Audit log failed after ImportItems");
                }

                return Ok(new GlobalResponse<ItemImportResultDto>
                {
                    Data = importResult,
                    ErrorStatus = false,
                    Message = "importItemsSuccess"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ImportItems failed");
                return StatusCode(500, new GlobalResponse<ItemImportResultDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "importItemsFailed"
                });
            }
        }

        [Authorize(Roles = "Commercial")]
        [HttpPost("ClearCatalog")]
        public async Task<ActionResult<GlobalResponse<CatalogClearResultDto>>> ClearCatalog(
            [FromBody] CatalogClearRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new GlobalResponse<CatalogClearResultDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "passwordRequired"
                });
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var commercialUserId = GetCommercialUserId();

                var auth = await TryVerifySensitiveCredentialAsync(commercialUserId, request.Password);
                if (!auth.Ok)
                {
                    return Unauthorized(new GlobalResponse<CatalogClearResultDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = auth.ErrorKey ?? "invalidSensitiveAuth"
                    });
                }

                var result = await _catalogClearService.ClearCatalogAsync(commercialUserId);

                try
                {
                    await _dbConfig.LogAuditAsync(
                        "ClearCatalog",
                        "Catalog",
                        0,
                        "Tags,Items,Orders",
                        userId,
                        commercialUserId,
                        description: $"Tags={result.TagsCleared}, Items={result.ItemsCleared}, Orders={result.OrdersCleared}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Audit log failed after ClearCatalog");
                }

                return Ok(new GlobalResponse<CatalogClearResultDto>
                {
                    Data = result,
                    ErrorStatus = false,
                    Message = "catalogClearSuccess"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCatalog failed");
                return StatusCode(500, new GlobalResponse<CatalogClearResultDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "catalogClearFailed"
                });
            }
        }

        // update item 
        [Authorize(Roles = "Commercial")]
        [HttpPut("UpdateItem")]
        public async Task<ActionResult<GlobalResponse<Item>>> UpdateItem([FromForm]  ItemRequest request, int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
            if (item == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = item,
                    ErrorStatus = true,
                    Message = "user not exsit"
                });
            }
           

            item.Tags = request.Tags;
            item.PurchasingPrice = request.PurchasingPrice;
            item.DisCountPrice = request.DisCountPrice;
            item.Description = request.Description;
            item.SellingPrice = request.SellingPrice;
            item.Quantity = request.Quantity;
            item.Code = request.Code;
            item.Name = request.Name;
            item.Image = request.Image != null ? await UploadIamgesAsync(request.Image): item.Image;


            _dbConfig.Items.Update(item);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial")]
        [HttpDelete("DeleteItem")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteItem(int id)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);


            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
            if (item == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "item not exsit"
                });
            }

            item!.IsDeleted = true;
            _dbConfig.Items.Update(item);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }


        [Authorize(Roles = "Commercial,POS")]
        [HttpGet("GetItems")]
        public ActionResult<GlobalResponse<PagedList<Item>>> GetItems(int pageNumber, int pageSize, string? info)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<PagedList<Item>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var item = _dbConfig.Items.Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId)).AsQueryable();

            if (info != null)
            {
                item = item.Where(x => x.Code == info || x.Name.Contains(info) || x.Description!.Contains(info) || x.Tags!.Contains(info));
            }

            var totalItems = item.Count();

            var pagedItems = item
                .OrderByDescending(x => x.Id)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList();

            var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";
            foreach (var n in pagedItems)
            {
                if (!string.IsNullOrEmpty(n.Image))
                {
                    n.Image = imageBaseUrl + n.Image;
                }
            }

            var pagedResult = new PagedList<Item>(pagedItems, totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<Item>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial,POS,Reader")]
        [HttpGet("GetItemsByCode")]
        public async Task<ActionResult<GlobalResponse<Object>>> GetItemsByCode(string code)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<Object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var item =await _dbConfig.Items.Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId) && x.Code == code).FirstOrDefaultAsync();



            if (item == null)
            {
                return NotFound(new GlobalResponse<Object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "Item not found"
                });
            }

            var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

            if (!string.IsNullOrEmpty(item.Image))
            {
                item.Image = imageBaseUrl + item.Image;
            }
            
            var response = new GlobalResponse<Object>
            {
                Data = item,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("AddOrder")]
        public async Task<ActionResult<GlobalResponse<CustomerOrder>>> AddOrder(CustomerOrderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return Unauthorized(new GlobalResponse<CustomerOrder>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                if (request.CustomerOrderItem == null || !request.CustomerOrderItem.Any())
                {
                    return BadRequest(new GlobalResponse<CustomerOrder>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Order must contain at least one item"
                    }); 

                }
                
                var items = _dbConfig.Items
                    .Where(x => !x.IsDeleted && (x.InsertByUserId == userId ||  x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId))
                    .ToList();

                var orderCode = request.OrderCode ?? RandomCode();
                var paymentMethod = request.PaymentMethod ?? "Cash";
                int? creditCustomerId = null;

                if (string.Equals(paymentMethod, "Credit", StringComparison.OrdinalIgnoreCase))
                {
                    var commercialUserIdForCredit = GetCommercialUserId();

                    if (!request.CreditCustomerId.HasValue)
                    {
                        return BadRequest(new GlobalResponse<CustomerOrder>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "pleaseSelectCreditAccount"
                        });
                    }

                    var cust = await _dbConfig.Customers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c =>
                            c.Id == request.CreditCustomerId.Value
                            && !c.IsDeleted
                            && c.InsertByUserId == commercialUserIdForCredit);

                    if (cust == null)
                    {
                        return BadRequest(new GlobalResponse<CustomerOrder>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "invalidCreditCustomer"
                        });
                    }

                    creditCustomerId = cust.Id;
                }

                var paymentStatus = string.Equals(paymentMethod, "Credit", StringComparison.OrdinalIgnoreCase)
                    ? "Pending"
                    : (request.IsCheckout ? "Paid" : "Pending");

                var newOrder = new CustomerOrder
                {
                    OrderCode = orderCode,
                    PaymentMethod = paymentMethod,
                    InsertByUserId = userId,
                    DiscountType = request.DiscountType,
                    DiscountValue = request.DiscountValue,
                    DiscountAmount = request.DiscountAmount,
                    DiscountPercent = request.DiscountPercent,
                    OrderSubTotal = request.OrderSubTotal,
                    OrderTotalAfterDiscount = request.OrderTotalAfterDiscount,
                    CreditCustomerId = creditCustomerId,
                    PaymentStatus = paymentStatus,
                };
                _dbConfig.CustomerOrders.Add(newOrder);
                await _dbConfig.SaveChangesAsync();

                if (request.CustomerOrderItem != null && request.CustomerOrderItem.Any())
                {
                    var insertItems = new List<CustomerOrderItem>();
                    var itemIds = request.CustomerOrderItem.Select(x => x.ItemId).Distinct().ToList();
                    
                    // Validate all items exist before processing
                    var invalidItemIds = itemIds.Where(id => !items.Any(x => x.Id == id)).ToList();
                    if (invalidItemIds.Any())
                    {
                        _logger.LogWarning("Invalid item IDs in order: {ItemIds}", string.Join(", ", invalidItemIds));
                        return BadRequest(new GlobalResponse<CustomerOrder>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = $"Invalid item IDs: {string.Join(", ", invalidItemIds)}"
                        });
                    }

                    // Check inventory availability before processing
                    var itemsToUpdate = new List<Item>();
                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                        if (currentItem == null) continue;

                        // Calculate total quantity needed for this item (including duplicates in order)
                        var totalQuantityNeeded = request.CustomerOrderItem
                            .Where(x => x.ItemId == itemRequest.ItemId)
                            .Sum(x => x.Quantity);

                        // Check if enough quantity is available
                        if (currentItem.Quantity < totalQuantityNeeded)
                        {
                            _logger.LogWarning("Insufficient inventory for item {ItemId}: Available {Available}, Required {Required}", 
                                itemRequest.ItemId, currentItem.Quantity, totalQuantityNeeded);
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"Insufficient inventory for item '{currentItem.Name}'. Available: {currentItem.Quantity}, Required: {totalQuantityNeeded}"
                            });
                        }
                    }

                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var normalizedNotes = string.IsNullOrWhiteSpace(itemRequest.Notes)
                            ? null
                            : itemRequest.Notes.Trim();

                        var existingItem = insertItems.FirstOrDefault(x =>
                            x.ItemId == itemRequest.ItemId &&
                            string.Equals(x.Notes ?? string.Empty, normalizedNotes ?? string.Empty, StringComparison.Ordinal));

                        if (existingItem != null)
                        {
                            existingItem.Quantity += itemRequest.Quantity;
                        }
                        else
                        {
                            var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                            if (currentItem == null)
                            {
                                _logger.LogWarning("Item not found: {ItemId}", itemRequest.ItemId);
                                return BadRequest(new GlobalResponse<CustomerOrder>
                                {
                                    Data = null,
                                    ErrorStatus = true,
                                    Message = $"Item with ID {itemRequest.ItemId} not found"
                                });
                            }

                            // Use discount price if available, otherwise use selling price
                            var finalPrice = currentItem.DisCountPrice > 0 && currentItem.DisCountPrice != currentItem.SellingPrice
                                ? currentItem.DisCountPrice
                                : currentItem.SellingPrice;

                            var newOrderItem = new CustomerOrderItem
                            {
                                CustomerOrderId = newOrder.Id,
                                SellingPrice = finalPrice,
                                PurchasingPrice = currentItem.PurchasingPrice,
                                Quantity = itemRequest.Quantity,
                                ItemId = itemRequest.ItemId,
                                Notes = normalizedNotes,
                                InsertByUserId = userId,
                            };

                            insertItems.Add(newOrderItem);
                            
                            // Track items to update inventory
                            if (!itemsToUpdate.Any(x => x.Id == currentItem.Id))
                            {
                                itemsToUpdate.Add(currentItem);
                            }
                        }
                    }

                    // Update inventory quantities
                    foreach (var itemToUpdate in itemsToUpdate)
                    {
                        var totalQuantitySold = insertItems
                            .Where(x => x.ItemId == itemToUpdate.Id)
                            .Sum(x => x.Quantity);
                        
                        itemToUpdate.Quantity -= totalQuantitySold;
                        _dbConfig.Items.Update(itemToUpdate);
                    }

                    _dbConfig.CustomerOrderItems.AddRange(insertItems);
                    await _dbConfig.SaveChangesAsync();
                }

                if (request.IsCheckout)
                {
                    var checkoutError = await _orderCheckoutService.ApplyCheckoutAsync(
                        newOrder,
                        request,
                        userId,
                        GetCommercialUserId());
                    if (checkoutError != null)
                    {
                        return BadRequest(checkoutError);
                    }
                }

                _logger.LogInformation("Order created successfully: {OrderCode} by user {UserId}", orderCode, userId);

                return Ok(new GlobalResponse<CustomerOrder>
                {
                    Data = newOrder,
                    ErrorStatus = false,
                    Message = "Order added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new GlobalResponse<CustomerOrder>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "An error occurred while creating the order"
                });
            }
        }

        [Authorize(Roles = "Commercial")]
        [HttpDelete("DeleteOrder")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteOrder(int id)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var item = await _dbConfig.CustomerOrders.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && x.InsertByUserId == userId);
            if (item == null)
            {
                return BadRequest(new GlobalResponse<CustomerOrder>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "Order not found"
                });
            }

            item!.IsDeleted = true;
            _dbConfig.CustomerOrders.Update(item);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<CustomerOrder>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial")]
        [HttpGet("GetOrders")]
        public ActionResult<GlobalResponse<OrdersPagedResult>> GetOrders(int pageNumber, int pageSize, string? info, DateTime? startDate, DateTime? endDate, string? paymentMethod)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<OrdersPagedResult>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var items = _dbConfig.CustomerOrders
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId))
                    .Include(x => x.CustomerOrderItem)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.User)
                    .AsQueryable();

            if (!string.IsNullOrEmpty(info))
            {
                items = items.Where(x => x.OrderCode == info);
            }

            if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
            {
                items = items.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcEx);
            }

            if (!string.IsNullOrEmpty(paymentMethod))
            {
                items = items.Where(x => x.PaymentMethod == paymentMethod);
            }

            var totalItems = items.Count();
            var totalSales = SumOrdersSalesAmount(items);
            var totalSubTotal = items.Sum(o => o.OrderSubTotal ?? 0m);
            var totalDiscount = items.Sum(o => o.DiscountAmount ?? 0m);
            var orderIds = items.Select(o => o.Id);
            var totalItemsSold = _dbConfig.CustomerOrderItems
                .Where(i => !i.IsDeleted && orderIds.Contains(i.CustomerOrderId))
                .Sum(i => (int?)i.Quantity) ?? 0;

            var summary = new OrdersSummaryDto
            {
                TotalOrders = totalItems,
                TotalSubTotal = totalSubTotal,
                TotalDiscount = totalDiscount,
                TotalSales = totalSales,
                TotalItemsSold = totalItemsSold,
                AverageOrderValue = totalItems > 0 ? Math.Round(totalSales / totalItems, 2) : 0m
            };

            var ordersList = items
                .OrderByDescending(x => x.InsertDate)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(x =>
                {
                    var activeOrderItems = GetActiveOrderItems(x.CustomerOrderItem);
                    var lineTotal = activeOrderItems.Sum(item => item.SellingPrice * item.Quantity);
                    return new OrderDto
                    {
                        CustomerOrderItem = activeOrderItems,
                        OrderPrice = lineTotal,
                        OrderCode = x.OrderCode,
                        Id = x.Id,
                        ItemsCount = activeOrderItems.Count,
                        InsertDate = x.InsertDate,
                        PaymentMethod = x.PaymentMethod,
                        CreatedByUserId = x.User != null ? x.User.Id : null,
                        CreatedByUsername = x.User != null ? x.User.Username : null,
                        DiscountType = x.DiscountType,
                        DiscountValue = x.DiscountValue,
                        DiscountAmount = x.DiscountAmount,
                        DiscountPercent = x.DiscountPercent,
                        OrderSubTotal = x.OrderSubTotal,
                        OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                    };
                })
                .ToList();

            var pagedResult = new OrdersPagedResult(ordersList, totalItems, pageNumber, pageSize, summary);

            return Ok(new GlobalResponse<OrdersPagedResult>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            });
        }

        [Authorize(Roles = "Commercial")]
        [HttpGet("ExportOrders")]
        public ActionResult ExportOrders(string? info, DateTime? startDate, DateTime? endDate, string? paymentMethod)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
                return BadRequest();

            var userInsertByUserId = user.InsertByUserId;
            var items = _dbConfig.CustomerOrders
                .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId))
                .Include(x => x.CustomerOrderItem)
                .AsQueryable();

            if (!string.IsNullOrEmpty(info))
                items = items.Where(x => x.OrderCode == info);
            if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
                items = items.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcEx);
            if (!string.IsNullOrEmpty(paymentMethod))
                items = items.Where(x => x.PaymentMethod == paymentMethod);

            var ordersList = items
                .OrderByDescending(x => x.InsertDate)
                .ToList()
                .Select(x =>
                {
                    var activeOrderItems = GetActiveOrderItems(x.CustomerOrderItem);
                    return new
                    {
                        OrderCode = x.OrderCode ?? "",
                        InsertDate = x.InsertDate,
                        PaymentMethod = x.PaymentMethod ?? "",
                        OrderPrice = activeOrderItems.Sum(item => item.SellingPrice * item.Quantity),
                        DiscountAmount = x.DiscountAmount ?? 0,
                        OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                        ItemsCount = activeOrderItems.Count
                    };
                })
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("OrderCode,InsertDate,PaymentMethod,OrderPrice,DiscountAmount,FinalTotal,ItemsCount");
            foreach (var o in ordersList)
            {
                var dateStr = o.InsertDate.ToString("yyyy-MM-dd HH:mm");
                var finalTotal = o.OrderTotalAfterDiscount ?? o.OrderPrice;
                csv.AppendLine($"\"{EscapeCsv(o.OrderCode)}\",\"{dateStr}\",\"{EscapeCsv(o.PaymentMethod)}\",{o.OrderPrice},{o.DiscountAmount},{finalTotal},{o.ItemsCount}");
            }

            var csvContent = csv.ToString();
            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(csvContent);
            var bytes = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
            var fileName = $"orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [Authorize(Roles = "Commercial,Admin,POS")]
        [HttpPut("UpdateOrder/{id}")]
        public async Task<ActionResult<GlobalResponse<CustomerOrder>>> UpdateOrder(int id, CustomerOrderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                if (user == null)
                {
                    return BadRequest(new GlobalResponse<CustomerOrder>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                var userInsertByUserId = user.InsertByUserId;
                var existingOrder = await _dbConfig.CustomerOrders
                    .Include(x => x.CustomerOrderItem)
                    .ThenInclude(x => x.Item)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false &&
                        (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));

                if (existingOrder == null)
                {
                    return NotFound(new GlobalResponse<CustomerOrder>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفاتورة غير موجودة"
                    });
                }

                var activeOrderItems = existingOrder.CustomerOrderItem?
                    .Where(i => i != null && !i.IsDeleted)
                    .ToList() ?? new List<CustomerOrderItem>();

                var oldQtyByItem = activeOrderItems
                    .GroupBy(i => i.ItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                existingOrder.PaymentMethod = request.PaymentMethod ?? existingOrder.PaymentMethod;
                existingOrder.DiscountType = request.DiscountType;
                existingOrder.DiscountValue = request.DiscountValue;
                existingOrder.DiscountAmount = request.DiscountAmount;
                existingOrder.DiscountPercent = request.DiscountPercent;
                existingOrder.OrderSubTotal = request.OrderSubTotal;
                existingOrder.OrderTotalAfterDiscount = request.OrderTotalAfterDiscount;

                var now = DateTime.UtcNow;
                foreach (var item in activeOrderItems)
                {
                    item.IsDeleted = true;
                    item.UpdateDate = now;
                }

                var newOrderItems = new List<CustomerOrderItem>();
                var newQtyByItem = new Dictionary<int, int>();

                if (request.CustomerOrderItem != null && request.CustomerOrderItem.Count > 0)
                {
                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var currentItem = await _dbConfig.Items
                            .FirstOrDefaultAsync(x => x.Id == itemRequest.ItemId && x.IsDeleted == false &&
                                (x.InsertByUserId == userId || x.User!.Id == userInsertByUserId || x.User!.InsertByUserId == userId));

                        if (currentItem == null)
                        {
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"المنتج برقم {itemRequest.ItemId} غير موجود"
                            });
                        }

                        var sellingPrice = currentItem.DisCountPrice > 0 && currentItem.DisCountPrice < currentItem.SellingPrice
                            ? currentItem.DisCountPrice
                            : currentItem.SellingPrice;

                        var normalizedNotes = string.IsNullOrWhiteSpace(itemRequest.Notes)
                            ? null
                            : itemRequest.Notes.Trim();

                        var existingMerged = newOrderItems.FirstOrDefault(x =>
                            x.ItemId == itemRequest.ItemId &&
                            string.Equals(x.Notes ?? string.Empty, normalizedNotes ?? string.Empty, StringComparison.Ordinal));

                        if (existingMerged != null)
                        {
                            existingMerged.Quantity += itemRequest.Quantity;
                        }
                        else
                        {
                            newOrderItems.Add(new CustomerOrderItem
                            {
                                ItemId = itemRequest.ItemId,
                                Quantity = itemRequest.Quantity,
                                SellingPrice = sellingPrice,
                                PurchasingPrice = currentItem.PurchasingPrice,
                                Notes = normalizedNotes,
                                CustomerOrderId = existingOrder.Id,
                                InsertByUserId = userId,
                            });
                        }

                        if (!newQtyByItem.ContainsKey(itemRequest.ItemId))
                            newQtyByItem[itemRequest.ItemId] = 0;
                        newQtyByItem[itemRequest.ItemId] += itemRequest.Quantity;
                    }

                    var allItemIds = oldQtyByItem.Keys.Union(newQtyByItem.Keys).Distinct();
                    foreach (var itemId in allItemIds)
                    {
                        oldQtyByItem.TryGetValue(itemId, out var oldQty);
                        newQtyByItem.TryGetValue(itemId, out var newQty);
                        var delta = newQty - oldQty;
                        if (delta == 0) continue;

                        var stockItem = await _dbConfig.Items.FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted);
                        if (stockItem == null) continue;

                        if (delta > 0 && stockItem.Quantity < delta)
                        {
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"Insufficient inventory for item '{stockItem.Name}'. Available: {stockItem.Quantity}, Required: {delta}"
                            });
                        }

                        stockItem.Quantity -= delta;
                        _dbConfig.Items.Update(stockItem);
                    }

                    _dbConfig.CustomerOrderItems.AddRange(newOrderItems);
                }
                else
                {
                    foreach (var kvp in oldQtyByItem)
                    {
                        var stockItem = await _dbConfig.Items.FirstOrDefaultAsync(i => i.Id == kvp.Key && !i.IsDeleted);
                        if (stockItem != null)
                        {
                            stockItem.Quantity += kvp.Value;
                            _dbConfig.Items.Update(stockItem);
                        }
                    }
                }

                _dbConfig.CustomerOrders.Update(existingOrder);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<CustomerOrder>
                {
                    Data = existingOrder,
                    ErrorStatus = false,
                    Message = "Order updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
                return StatusCode(500, new GlobalResponse<CustomerOrder>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "An error occurred while updating the order"
                });
            }
        }


        private string RandomCode()
        {
            // Generate 9-digit order code: timestamp-based to ensure uniqueness
            var random = new Random();
            var timestamp = DateTime.UtcNow.Ticks % 1000000000; // Last 9 digits of ticks
            var randomPart = random.Next(100000, 999999); // 6 digits
            var code = (timestamp + randomPart) % 1000000000; // Ensure 9 digits
            return code.ToString().PadLeft(9, '0'); // Ensure exactly 9 digits
        }

        // get selse count

        [Authorize(Roles = "Commercial")]
        [HttpGet("GetSellsCount")]
        public ActionResult<GlobalResponse<object>> GetSellsCount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var today = DateTime.Today;

            var customerOrdersQuery = _dbConfig.CustomerOrders
                .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId ||  x.User.InsertByUserId == userId));

            var orderItemsQuery = _dbConfig.CustomerOrderItems
                .Where(x => x.CustomerOrder!.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));


          

            var totalItems = customerOrdersQuery.Count();

            var newOrderCount = new
            {
                total = totalItems,
                thisDay = customerOrdersQuery.Count(x => x.InsertDate.Date == today),
                thisWeek = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-7)),
                thisMonth = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-30))
            };

            var newItemsOrderCount = new
            {
                total = orderItemsQuery.Count(),
                thisDay = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date == today ? x.Quantity : 0),
                thisWeek = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-7) ? x.Quantity : 0),
                thisMonth = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-30) ? x.Quantity : 0)
            };

            var newCount = new
            {
                newOrderCount,
                newItemsOrderCount
            };

            var response = new GlobalResponse<object>
            {
                Data = newCount,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial")]
        [HttpGet("GetSellsCountByUser")]
        public ActionResult<GlobalResponse<object>> GetSellsCountByUser()
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var users = _dbConfig.Users.Where(x => x.Id == userId).ToList();

            var finelList = new List<object>();
            foreach( var user in users)
            {
                var today = DateTime.Today;

                var customerOrdersQuery = _dbConfig.CustomerOrders
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                var orderItemsQuery = _dbConfig.CustomerOrderItems
                    .Where(x => x.CustomerOrder!.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                var totalItems = customerOrdersQuery.Count();

                var newOrderCount = new
                {
                    total = totalItems,
                    thisDay = customerOrdersQuery.Count(x => x.InsertDate.Date == today),
                    thisWeek = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-7)),
                    thisMonth = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-30))
                };

                var newItemsOrderCount = new
                {
                    total = orderItemsQuery.Count(),
                    thisDay = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date == today ? x.Quantity : 0),
                    thisWeek = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-7) ? x.Quantity : 0),
                    thisMonth = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-30) ? x.Quantity : 0)
                };

                var newCount = new
                {
                    name = user.Name,
                    newOrderCount,
                    newItemsOrderCount
                };

                finelList.Add(newOrderCount);
              
            }   
            
            var response = new GlobalResponse<object>
            {
                Data = finelList,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }


        [Authorize(Roles = "Commercial,Admin,POS")]
        [HttpGet("GetDashboardStats")]
        public ActionResult<GlobalResponse<object>> GetDashboardStats()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                var today = DateTime.Today;

                var customerOrdersQuery = QueryActiveOrdersForCommercial(userId);
                var orderItemsQuery = QueryActiveOrderItemsForCommercial(userId, user!.InsertByUserId);

                // Items Statistics
                var itemsQuery = _dbConfig.Items
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                // Users Statistics
                var usersQuery = _dbConfig.Users
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.Id == user.InsertByUserId || x.InsertByUserId == userId));

                // Categories Statistics
                var tagsQuery = _dbConfig.Tags
                    .Where(x => x.IsDeleted == false);

                // Sales Amount — one total per order
                decimal CalculateSalesAmount(DateTime startDate, DateTime endDate)
                {
                    return SumOrdersSalesAmount(
                        customerOrdersQuery.Where(x =>
                            x.InsertDate.Date >= startDate &&
                            x.InsertDate.Date <= endDate));
                }

                decimal TotalAmount()
                {
                    return SumOrdersSalesAmount(customerOrdersQuery);
                }

                var stats = new
                {
                    orders = new
                    {
                        total = customerOrdersQuery.Count(),
                        today = customerOrdersQuery.Count(x => x.InsertDate.Date == today),
                        thisWeek = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-7)),
                        thisMonth = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-30))
                    },
                    items = new
                    {
                        total = orderItemsQuery.Count(),
                        today = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date == today ? x.Quantity : 0),
                        thisWeek = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-7) ? x.Quantity : 0),
                        thisMonth = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-30) ? x.Quantity : 0)
                    },
                    salesAmount = new
                    {
                        total = TotalAmount(),
                        today = CalculateSalesAmount(today, today),
                        thisWeek = CalculateSalesAmount(today.AddDays(-7), today),
                        thisMonth = CalculateSalesAmount(today.AddDays(-30), today)
                    },
                    products = new
                    {
                        total = itemsQuery.Count(),
                        active = itemsQuery.Count(x => x.IsDeleted == false)
                    },
                    users = new
                    {
                        total = usersQuery.Count(),
                        active = usersQuery.Count(x => x.IsDeleted == false)
                    },
                    categories = new
                    {
                        total = tagsQuery.Count(),
                        active = tagsQuery.Count(x => x.IsDeleted == false)
                    }
                };

                var response = new GlobalResponse<object>
                {
                    Data = stats,
                    ErrorStatus = false,
                    Message = "Success"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        // Advanced Reports Endpoints

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetProfitReport")]
        public ActionResult<GlobalResponse<object>> GetProfitReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                IQueryable<CustomerOrderItem> orderItemsQuery = QueryActiveOrderItemsForCommercial(userId, user!.InsertByUserId)
                    .Include(x => x.Item)
                    .Include(x => x.CustomerOrder);

                if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
                {
                    orderItemsQuery = orderItemsQuery.Where(x =>
                        x.CustomerOrder != null &&
                        x.CustomerOrder.InsertDate >= fromUtc &&
                        x.CustomerOrder.InsertDate < toUtcEx);
                }

                var profitData = orderItemsQuery
                    .Select(x => new
                    {
                        SellingPrice = x.SellingPrice,
                        PurchasingPrice = x.Item.PurchasingPrice,
                        Quantity = x.Quantity
                    })
                    .ToList();

                var totalSales = profitData.Sum(x => x.SellingPrice * x.Quantity);
                var totalCost = profitData.Sum(x => x.PurchasingPrice * x.Quantity);
                var totalProfit = totalSales - totalCost;
                var profitMargin = totalSales > 0 ? (totalProfit / totalSales) * 100 : 0;

                var report = new
                {
                    totalSales = totalSales,
                    totalCost = totalCost,
                    totalProfit = totalProfit,
                    profitMargin = Math.Round(profitMargin, 2),
                    totalItemsSold = profitData.Sum(x => x.Quantity),
                    period = new
                    {
                        startDate = startDate?.ToString("yyyy-MM-dd"),
                        endDate = endDate?.AddDays(-1).ToString("yyyy-MM-dd")
                    }
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = report,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profit report");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetTopSellingItems")]
        public ActionResult<GlobalResponse<object>> GetTopSellingItems(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                IQueryable<CustomerOrderItem> orderItemsQuery = QueryActiveOrderItemsForCommercial(userId, user!.InsertByUserId)
                    .Include(x => x.Item)
                    .Include(x => x.CustomerOrder);

                if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
                {
                    orderItemsQuery = orderItemsQuery.Where(x =>
                        x.CustomerOrder != null &&
                        x.CustomerOrder.InsertDate >= fromUtc &&
                        x.CustomerOrder.InsertDate < toUtcEx);
                }

                if (topCount < 1) topCount = 10;
                if (topCount > 500) topCount = 500;

                var summary = new TopSellingItemsSummaryDto
                {
                    TotalQuantitySold = orderItemsQuery.Sum(x => (int?)x.Quantity) ?? 0,
                    TotalSales = orderItemsQuery.Sum(x => (decimal?)(x.SellingPrice * x.Quantity)) ?? 0m,
                    TotalDistinctItems = orderItemsQuery.Select(x => x.ItemId).Distinct().Count(),
                    TotalOrders = orderItemsQuery.Select(x => x.CustomerOrderId).Distinct().Count()
                };

                var topItems = orderItemsQuery
                    .GroupBy(x => new { x.ItemId, x.Item.Name, x.Item.Code })
                    .Select(g => new
                    {
                        itemId = g.Key.ItemId,
                        itemName = g.Key.Name,
                        itemCode = g.Key.Code,
                        totalQuantitySold = g.Sum(x => x.Quantity),
                        totalSales = g.Sum(x => x.SellingPrice * x.Quantity),
                        orderCount = g.Select(x => x.CustomerOrderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.totalQuantitySold)
                    .Take(topCount)
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = new { items = topItems, summary },
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling items");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetSalesByCategory")]
        public ActionResult<GlobalResponse<object>> GetSalesByCategory(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                IQueryable<CustomerOrderItem> orderItemsQuery = QueryActiveOrderItemsForCommercial(userId, user!.InsertByUserId)
                    .Include(x => x.Item)
                    .Include(x => x.CustomerOrder);

                if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
                {
                    orderItemsQuery = orderItemsQuery.Where(x =>
                        x.CustomerOrder != null &&
                        x.CustomerOrder.InsertDate >= fromUtc &&
                        x.CustomerOrder.InsertDate < toUtcEx);
                }

                var salesByCategory = orderItemsQuery
                    .Where(x => !string.IsNullOrEmpty(x.Item.Tags))
                    .GroupBy(x => x.Item.Tags)
                    .Select(g => new
                    {
                        category = g.Key,
                        totalSales = g.Sum(x => x.SellingPrice * x.Quantity),
                        totalQuantity = g.Sum(x => x.Quantity),
                        itemCount = g.Select(x => x.ItemId).Distinct().Count(),
                        orderCount = g.Select(x => x.CustomerOrderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.totalSales)
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = salesByCategory,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales by category");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetSalesByEmployee")]
        public ActionResult<GlobalResponse<object>> GetSalesByEmployee(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                IQueryable<CustomerOrder> ordersQuery = QueryActiveOrdersForCommercial(userId)
                    .Include(x => x.User)
                    .Include(x => x.CustomerOrderItem);

                if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
                {
                    ordersQuery = ordersQuery.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcEx);
                }

                var salesByEmployee = ordersQuery
                    .ToList()
                    .GroupBy(x => new { x.InsertByUserId, Username = x.User?.Username ?? "" })
                    .Select(g => new
                    {
                        employeeId = g.Key.InsertByUserId,
                        employeeName = g.Key.Username,
                        totalOrders = g.Count(),
                        totalSales = g.Sum(o =>
                            GetActiveOrderItems(o.CustomerOrderItem).Sum(x => x.SellingPrice * x.Quantity)),
                        totalItemsSold = g.Sum(o =>
                            GetActiveOrderItems(o.CustomerOrderItem).Sum(x => x.Quantity))
                    })
                    .OrderByDescending(x => x.totalSales)
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = salesByEmployee,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales by employee");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetLowStockItems")]
        public ActionResult<GlobalResponse<object>> GetLowStockItems(int threshold = 10)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                var itemsQuery = _dbConfig.Items
                    .Where(x => x.IsDeleted == false && 
                                (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                var lowStockItems = itemsQuery
                    .Where(x => x.Quantity <= threshold)
                    .Select(x => new
                    {
                        itemId = x.Id,
                        itemName = x.Name,
                        itemCode = x.Code,
                        currentQuantity = x.Quantity,
                        threshold = threshold,
                        category = x.Tags
                    })
                    .OrderBy(x => x.currentQuantity)
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = lowStockItems,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock items");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Reader")]
        [HttpGet("ItemPrice")]
        public async Task<ActionResult<GlobalResponse<Item>>> ItemPrice(string code)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.Where(x => x.InsertByUserId == userId).FirstOrDefault();

            if (user == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Code == code && (x.InsertByUserId == userId || x.User!.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
            if (item == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "item not exsit"
                });
            }
            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,POS,Admin")]
        [HttpGet("CommercialUserInfo")]
        public async Task<ActionResult<GlobalResponse<CommercialUserInfoDto>>> GetCommercialUserInfo()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<CommercialUserInfoDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المستخدم غير موجود"
                    });
                }

                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

                var userInfo = new CommercialUserInfoDto
                {
                    StoreName = commercialUser.StoreName ?? commercialUser.Name,
                    Logo = string.IsNullOrEmpty(commercialUser.Logo) ? null : imageBaseUrl + commercialUser.Logo
                };

                return Ok(new GlobalResponse<CommercialUserInfoDto>
                {
                    Data = userInfo,
                    ErrorStatus = false,
                    Message = "تم جلب المعلومات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commercial user info");
                return StatusCode(500, new GlobalResponse<CommercialUserInfoDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب المعلومات: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Manager")]
        [HttpPost("VerifySensitiveActionPassword")]
        public async Task<ActionResult<GlobalResponse<object>>> VerifySensitiveActionPassword([FromBody] SensitiveActionPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "كلمة المرور مطلوبة"
                });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var currentUser = await _dbConfig.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (currentUser == null)
            {
                return Unauthorized(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "المستخدم غير موجود"
                });
            }

            if (IsManagerRole(currentUser.Role) && currentUser.CanUseOwnLoginCodeForSensitiveActions)
            {
                if (string.IsNullOrWhiteSpace(currentUser.LoginCode))
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "managerLoginCodeNotConfigured"
                    });
                }

                var submittedCode = NormalizeLoginCode(request.Password);
                if (submittedCode == null || submittedCode != currentUser.LoginCode)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "invalidManagerLoginCode"
                    });
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = new { action = request.ActionKey ?? "general", verified = true },
                    ErrorStatus = false,
                    Message = "تم التحقق بنجاح"
                });
            }

            var commercialUserId = GetCommercialUserId();
            var (verified, errorKey) = await TryVerifySensitiveCredentialAsync(commercialUserId, request.Password);
            if (!verified)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = errorKey ?? "invalidSensitiveAuth"
                });
            }

            return Ok(new GlobalResponse<object>
            {
                Data = new { action = request.ActionKey ?? "general", verified = true },
                ErrorStatus = false,
                Message = "تم التحقق بنجاح"
            });
        }

        [AuthorizeSection("endOfDayReport", Roles = "Commercial")]
        [HttpGet("GetEndOfDaySummary")]
        public async Task<ActionResult<GlobalResponse<EndOfDayReportDto>>> GetEndOfDaySummary()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var report = await BuildEndOfDayReportAsync(commercialUserId);

                return Ok(new GlobalResponse<EndOfDayReportDto>
                {
                    Data = report,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting end of day summary");
                return StatusCode(500, new GlobalResponse<EndOfDayReportDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء استخراج تقرير نهاية اليوم"
                });
            }
        }

        [AuthorizeSection("endOfDayReport", Roles = "Commercial")]
        [HttpGet("ExportEndOfDaySummary")]
        public async Task<IActionResult> ExportEndOfDaySummary()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var r = await BuildEndOfDayReportAsync(commercialUserId);

                using var workbook = new XLWorkbook();

                var summarySheet = workbook.Worksheets.Add("Summary");
                summarySheet.Cell(1, 1).Value = "Key";
                summarySheet.Cell(1, 2).Value = "Value";
                summarySheet.Cell(2, 1).Value = "DayStart";
                summarySheet.Cell(2, 2).Value = r.DayStart;
                summarySheet.Cell(3, 1).Value = "DayEnd";
                summarySheet.Cell(3, 2).Value = r.DayEnd;
                summarySheet.Cell(4, 1).Value = "OrdersCount";
                summarySheet.Cell(4, 2).Value = r.Totals.OrdersCount;
                summarySheet.Cell(5, 1).Value = "ItemsCount";
                summarySheet.Cell(5, 2).Value = r.Totals.ItemsCount;
                summarySheet.Cell(6, 1).Value = "ItemsQuantity";
                summarySheet.Cell(6, 2).Value = r.Totals.ItemsQuantity;
                summarySheet.Cell(7, 1).Value = "GrossSales";
                summarySheet.Cell(7, 2).Value = r.Totals.GrossSales;
                summarySheet.Cell(8, 1).Value = "DiscountAmount";
                summarySheet.Cell(8, 2).Value = r.Totals.DiscountAmount;
                summarySheet.Cell(9, 1).Value = "NetSales";
                summarySheet.Cell(9, 2).Value = r.Totals.NetSales;
                summarySheet.Cell(10, 1).Value = "TotalCost";
                summarySheet.Cell(10, 2).Value = r.Totals.TotalCost;
                summarySheet.Cell(11, 1).Value = "Profit";
                summarySheet.Cell(11, 2).Value = r.Totals.Profit;

                var paymentsSheet = workbook.Worksheets.Add("PaymentBreakdown");
                paymentsSheet.Cell(1, 1).Value = "Method";
                paymentsSheet.Cell(1, 2).Value = "OrdersCount";
                paymentsSheet.Cell(1, 3).Value = "Amount";
                var paymentRow = 2;
                foreach (var p in r.PaymentBreakdown)
                {
                    paymentsSheet.Cell(paymentRow, 1).Value = p.Method ?? string.Empty;
                    paymentsSheet.Cell(paymentRow, 2).Value = p.OrdersCount;
                    paymentsSheet.Cell(paymentRow, 3).Value = p.Amount;
                    paymentRow++;
                }

                var topItemsSheet = workbook.Worksheets.Add("TopItems");
                topItemsSheet.Cell(1, 1).Value = "ItemName";
                topItemsSheet.Cell(1, 2).Value = "Quantity";
                topItemsSheet.Cell(1, 3).Value = "SalesAmount";
                var topItemsRow = 2;
                foreach (var i in r.TopItems)
                {
                    topItemsSheet.Cell(topItemsRow, 1).Value = i.ItemName ?? string.Empty;
                    topItemsSheet.Cell(topItemsRow, 2).Value = i.Quantity;
                    topItemsSheet.Cell(topItemsRow, 3).Value = i.SalesAmount;
                    topItemsRow++;
                }

                foreach (var sheet in workbook.Worksheets)
                {
                    var headerRange = sheet.Range(1, 1, 1, sheet.LastColumnUsed()?.ColumnNumber() ?? 1);
                    headerRange.Style.Font.Bold = true;
                    sheet.Columns().AdjustToContents();
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var fileName = $"end_of_day_{DateTime.UtcNow:yyyyMMdd}.xlsx";
                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting end of day summary");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء تنزيل تقرير نهاية اليوم"
                });
            }
        }

        private async Task<string> UploadIamgesAsync(IFormFile imageFile)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var validImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

            var fileExtension = Path.GetExtension(imageFile.FileName);
            if (!validImageExtensions.Contains(fileExtension.ToLower()))
            {
                return "not a valid image extension";
            }

            var fileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(path, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }

        // Seed Database
   //     [Authorize(Roles = "Admin")]
        [HttpPost("SeedData")]
        public ActionResult<GlobalResponse<string>> ExecuteSeedData([FromBody] SeedDataRequest request)
        {
            try
            {
                POS.Db.SeedData.SeedSummary summary;
                if (request.SeedDemoAccounts)
                {
                    summary = POS.Db.SeedData.SeedDemoEnvironment(_dbConfig);
                }
                else if (request.CatalogOnly)
                {
                    var catalog = POS.Db.SeedData.SeedCatalogOnly(_dbConfig, request.CommercialUserId);
                    summary = new POS.Db.SeedData.SeedSummary
                    {
                        CommercialUserId = request.CommercialUserId,
                        Tags = catalog.Tags,
                        Items = catalog.Items
                    };
                }
                else
                {
                    summary = POS.Db.SeedData.SeedDatabase(_dbConfig, request.CommercialUserId);
                }

                return Ok(new GlobalResponse<string>
                {
                    Data = summary.ToMessage(),
                    ErrorStatus = false,
                    Message = summary.ToMessage()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                return BadRequest(new GlobalResponse<string>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة البيانات: {ex.Message}"
                });
            }
        }

    }

}
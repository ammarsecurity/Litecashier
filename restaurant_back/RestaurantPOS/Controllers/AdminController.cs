using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Hubs;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Dtos;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Restaurant;
using RestaurantPOS.Models.Response;
using ClosedXML.Excel;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace RestaurantPOS.Controllers
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
        private readonly IHubContext<OrderHub> _hubContext;

        public AdminController(ILogger<AdminController> logger, DbConfig dbConfig, IMapper mapper, IConfiguration configuration, IHubContext<OrderHub> hubContext)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        // Helper method to get Commercial User ID
        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId && !x.IsDeleted);
            
            if (user != null && user.Role == "Commercial")
            {
                return userId;
            }
            
            return user?.InsertByUserId ?? userId;
        }

        /// <summary>
        /// Validates sensitive-action credential for a tenant: commercial password,
        /// then any manager's confirmation code (if enabled), then any manager's login password.
        /// </summary>
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

        /// <summary>
        /// تحويل تاريخ البداية/النهاية من الواجهة (يوم تقويمي) إلى نطاق UTC [from, to) لمقارنة <see cref="CustomerOrder.InsertDate"/> المخزَّنة بـ UtcNow.
        /// يدعم start فقط (يوم واحد) أو start+end.
        /// </summary>
        private DateTime GetBusinessLocalToday()
        {
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

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
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

        /// <summary>رمز دخول رقمي 4–12 خانة؛ فارغ = لا يُستخدم</summary>
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
                {
                    user.LoginCode = null;
                }
                return (true, null);
            }

            user.CanUseOwnLoginCodeForSensitiveActions = canUseOwnLoginCode == true;

            if (!user.CanUseOwnLoginCodeForSensitiveActions)
            {
                return (true, null);
            }

            string? lc = null;
            if (!string.IsNullOrWhiteSpace(loginCodeRaw))
            {
                lc = NormalizeLoginCode(loginCodeRaw);
                if (lc == null)
                {
                    return (false, "رمز الدخول يجب أن يكون من 4 إلى 12 رقماً");
                }
            }
            else if (!string.IsNullOrWhiteSpace(user.LoginCode))
            {
                lc = user.LoginCode;
            }

            if (string.IsNullOrWhiteSpace(lc))
            {
                return (false, "managerLoginCodeRequiredForSensitiveActions");
            }

            var duplicateQuery = _dbConfig.Users.Where(u => u.LoginCode == lc && !u.IsDeleted);
            if (excludeUserId.HasValue)
            {
                duplicateQuery = duplicateQuery.Where(u => u.Id != excludeUserId.Value);
            }

            if (await duplicateQuery.AnyAsync())
            {
                return (false, "رمز الدخول مستخدم من حساب آخر");
            }

            user.LoginCode = lc;
            return (true, null);
        }

        private static decimal ResolveSellingPrice(Item item)
        {
            if (item.DisCountPrice > 0 && item.DisCountPrice != item.SellingPrice)
            {
                return item.DisCountPrice;
            }
            return item.SellingPrice;
        }

        private static List<CustomerOrderItem> GetActiveOrderItems(IEnumerable<CustomerOrderItem>? items)
        {
            return items?
                .Where(item => item != null && !item.IsDeleted)
                .ToList() ?? new List<CustomerOrderItem>();
        }

        private static bool IsUnpaidOrder(CustomerOrder order)
        {
            return !string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Finds the latest unpaid DineIn order linked to a table (OrderTables or TableId).
        /// </summary>
        private async Task<CustomerOrder?> FindUnpaidDineInOrderForTableAsync(int tableId, int commercialUserId)
        {
            var orderIdsFromLinks = await _dbConfig.OrderTables
                .Where(ot => ot.TableId == tableId && !ot.IsDeleted)
                .Select(ot => ot.OrderId)
                .Distinct()
                .ToListAsync();

            if (orderIdsFromLinks.Count > 0)
            {
                var fromLinks = await _dbConfig.CustomerOrders
                    .Where(o => orderIdsFromLinks.Contains(o.Id)
                        && !o.IsDeleted
                        && o.OrderType == "DineIn"
                        && o.PaymentStatus != "Paid")
                    .OrderByDescending(o => o.InsertDate)
                    .FirstOrDefaultAsync();

                if (fromLinks != null)
                {
                    return fromLinks;
                }
            }

            return await _dbConfig.CustomerOrders
                .Where(o => !o.IsDeleted
                    && o.OrderType == "DineIn"
                    && o.PaymentStatus != "Paid"
                    && o.TableId == tableId
                    && (o.InsertByUserId == commercialUserId || o.User!.InsertByUserId == commercialUserId))
                .OrderByDescending(o => o.InsertDate)
                .FirstOrDefaultAsync();
        }

        private async Task<CustomerOrder?> LoadOrderWithItemsAsync(int orderId)
        {
            return await _dbConfig.CustomerOrders
                .Include(o => o.CustomerOrderItem!)
                .ThenInclude(oi => oi.Item)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
        }

        private async Task RepairTableActiveOrderLinkAsync(Table table, CustomerOrder order)
        {
            if (table.CurrentOrderId == order.Id
                && string.Equals(table.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            table.CurrentOrderId = order.Id;
            table.Status = "Occupied";
            table.UpdateDate = DateTime.Now;
            _dbConfig.Tables.Update(table);
            await _dbConfig.SaveChangesAsync();
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

        private static ReturnedOrderItemDto MapReturnedOrderItemDto(ReturnedOrderItem entity)
        {
            return new ReturnedOrderItemDto
            {
                Id = entity.Id,
                CustomerOrderId = entity.CustomerOrderId,
                CustomerOrderItemId = entity.CustomerOrderItemId,
                TableId = entity.TableId,
                TableNumber = entity.TableNumber,
                MergedTableNumbers = entity.MergedTableNumbers,
                OrderCode = entity.OrderCode,
                OrderType = entity.OrderType,
                PaymentMethod = entity.PaymentMethod,
                ItemId = entity.ItemId,
                ItemName = entity.ItemName,
                Quantity = entity.Quantity,
                UnitPrice = entity.UnitPrice,
                LineTotal = entity.LineTotal,
                Reason = entity.Reason,
                DeletedByUserId = entity.DeletedByUserId,
                DeletedByUsername = entity.DeletedByUsername,
                InsertDate = entity.InsertDate
            };
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var needsQuotes = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            var escaped = value.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{escaped}\"" : escaped;
        }

        private async Task<(bool IsBlocked, string? BlockMessage, EndOfDayReportDto? Data)> BuildEndOfDayReportAsync(int commercialUserId)
        {
            var businessToday = GetBusinessLocalToday();
            if (!TryGetOrderInsertUtcRange(businessToday, businessToday, out var fromUtc, out var toUtcExclusive))
            {
                fromUtc = DateTime.UtcNow.Date;
                toUtcExclusive = fromUtc.AddDays(1);
            }

            var allTables = await _dbConfig.Tables
                .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId)
                .ToListAsync();

            var occupiedTables = allTables
                .Where(t => string.Equals((t.Status ?? "").Trim(), "Occupied", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (occupiedTables.Any())
            {
                return (true, "لا يمكن استخراج تقرير نهاية اليوم قبل إغلاق كل الطاولات المشغولة", null);
            }

            var orders = await _dbConfig.CustomerOrders
                .Include(o => o.User)
                .Where(o =>
                    !o.IsDeleted &&
                    o.InsertDate >= fromUtc &&
                    o.InsertDate < toUtcExclusive &&
                    (o.InsertByUserId == commercialUserId ||
                     (o.User != null && o.User.InsertByUserId == commercialUserId)))
                .ToListAsync();

            var orderIds = orders.Select(o => o.Id).ToList();
            var orderItems = orderIds.Any()
                ? await _dbConfig.CustomerOrderItems
                    .Include(oi => oi.Item)
                    .Where(oi => !oi.IsDeleted && orderIds.Contains(oi.CustomerOrderId))
                    .ToListAsync()
                : new List<CustomerOrderItem>();

            var orderTables = orderIds.Any()
                ? await _dbConfig.OrderTables
                    .Include(ot => ot.Table)
                    .Where(ot => !ot.IsDeleted && orderIds.Contains(ot.OrderId))
                    .ToListAsync()
                : new List<OrderTable>();

            var tableDict = allTables.ToDictionary(t => t.Id, t => t);
            var orderTablesMap = orderTables
                .GroupBy(x => x.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Where(x => x.Table != null && !x.Table.IsDeleted)
                        .Select(x => x.Table!)
                        .Distinct()
                        .ToList());

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

            var invoiceTableRows = new List<(int? TableId, string TableNumber, decimal Amount)>();
            foreach (var order in orders)
            {
                var amount = orderItems
                    .Where(oi => oi.CustomerOrderId == order.Id)
                    .Sum(oi => oi.SellingPrice * oi.Quantity);

                if (orderTablesMap.TryGetValue(order.Id, out var linkedTables) && linkedTables.Any())
                {
                    foreach (var linkedTable in linkedTables)
                    {
                        invoiceTableRows.Add((linkedTable.Id, linkedTable.TableNumber, amount));
                    }
                }
                else if (order.TableId.HasValue && tableDict.TryGetValue(order.TableId.Value, out var fallbackTable))
                {
                    invoiceTableRows.Add((fallbackTable.Id, fallbackTable.TableNumber, amount));
                }
                else
                {
                    var typeLabel = string.IsNullOrWhiteSpace(order.OrderType) || order.OrderType == "DineIn"
                        ? "-"
                        : order.OrderType;
                    invoiceTableRows.Add((null, typeLabel, amount));
                }
            }

            var invoicesByTable = invoiceTableRows
                .GroupBy(x => new { x.TableId, x.TableNumber })
                .Select(g => new EndOfDayTableInvoicesDto
                {
                    TableId = g.Key.TableId,
                    TableNumber = g.Key.TableNumber,
                    InvoicesCount = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.TableId ?? int.MaxValue)
                .ThenBy(x => x.TableNumber)
                .ToList();

            var topItems = orderItems
                .GroupBy(x => new { x.ItemId, ItemName = x.Item != null ? x.Item.Name : $"#{x.ItemId}" })
                .Select(g => new EndOfDayTopItemDto
                {
                    ItemId = g.Key.ItemId,
                    ItemName = g.Key.ItemName,
                    Quantity = g.Sum(x => x.Quantity),
                    SalesAmount = g.Sum(x => x.SellingPrice * x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ThenByDescending(x => x.SalesAmount)
                .Take(10)
                .ToList();

            var ordersByType = orders
                .GroupBy(o => string.IsNullOrWhiteSpace(o.OrderType) ? "DineIn" : o.OrderType)
                .Select(g =>
                {
                    var groupOrderIds = g.Select(x => x.Id).ToHashSet();
                    var amount = orderItems
                        .Where(oi => groupOrderIds.Contains(oi.CustomerOrderId))
                        .Sum(oi => oi.SellingPrice * oi.Quantity);
                    return new EndOfDayOrderTypeDto
                    {
                        OrderType = g.Key,
                        OrdersCount = g.Count(),
                        TotalAmount = amount
                    };
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            var returnedItems = await _dbConfig.ReturnedOrderItems
                .Where(x =>
                    !x.IsDeleted &&
                    x.InsertByUserId == commercialUserId &&
                    x.InsertDate >= fromUtc &&
                    x.InsertDate < toUtcExclusive)
                .OrderByDescending(x => x.InsertDate)
                .Select(x => new EndOfDayReturnedItemDto
                {
                    Id = x.Id,
                    OrderCode = x.OrderCode,
                    ItemName = x.ItemName,
                    TableNumber = x.TableNumber,
                    MergedTableNumbers = x.MergedTableNumbers,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    LineTotal = x.LineTotal,
                    DeletedByUsername = x.DeletedByUsername,
                    InsertDate = x.InsertDate
                })
                .ToListAsync();

            var tableStatus = new EndOfDayTableStatusDto
            {
                TotalTables = allTables.Count,
                AvailableTables = allTables.Count(t => string.Equals((t.Status ?? "").Trim(), "Available", StringComparison.OrdinalIgnoreCase)),
                OccupiedTables = occupiedTables.Count,
                ReservedTables = allTables.Count(t => string.Equals((t.Status ?? "").Trim(), "Reserved", StringComparison.OrdinalIgnoreCase)),
                OutOfServiceTables = allTables.Count(t =>
                    string.Equals((t.Status ?? "").Trim(), "OutOfService", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((t.Status ?? "").Trim(), "Out_Of_Service", StringComparison.OrdinalIgnoreCase))
            };

            var report = new EndOfDayReportDto
            {
                DayStart = businessToday,
                DayEnd = businessToday.AddDays(1).AddSeconds(-1),
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
                    ReturnedAmount = returnedItems.Sum(x => x.LineTotal),
                    ReturnedCount = returnedItems.Count
                },
                TableStatus = tableStatus,
                PaymentBreakdown = paymentBreakdown,
                OrdersByType = ordersByType,
                InvoicesByTable = invoicesByTable,
                TopItems = topItems,
                ReturnedItems = returnedItems
            };

            return (false, null, report);
        }

        private async Task EmitTableUpdatedAsync(Table table)
        {
            await _hubContext.Clients.All.SendAsync("TableUpdated", new
            {
                TableId = table.Id,
                Status = table.Status,
                TableNumber = table.TableNumber,
                Zone = table.Zone,
                CurrentOrderId = table.CurrentOrderId
            });
        }

        private async Task<CustomerOrder> ResolveOrCreateDestinationOrderAsync(
            Table destinationTable,
            CustomerOrder sourceOrder,
            int userId)
        {
            CustomerOrder? destinationOrder = null;
            if (destinationTable.CurrentOrderId.HasValue)
            {
                destinationOrder = await _dbConfig.CustomerOrders
                    .Include(o => o.CustomerOrderItem)
                    .FirstOrDefaultAsync(o => o.Id == destinationTable.CurrentOrderId.Value && !o.IsDeleted);
            }

            if (destinationOrder != null)
            {
                return destinationOrder;
            }

            destinationOrder = new CustomerOrder
            {
                OrderCode = RandomCode(),
                PaymentMethod = sourceOrder.PaymentMethod,
                InsertByUserId = userId,
                TableId = destinationTable.Id,
                OrderType = "DineIn",
                Notes = sourceOrder.Notes,
                OrderStatus = "Pending",
                PaymentStatus = "Pending"
            };
            _dbConfig.CustomerOrders.Add(destinationOrder);
            await _dbConfig.SaveChangesAsync();

            destinationTable.CurrentOrderId = destinationOrder.Id;
            destinationTable.Status = "Occupied";
            _dbConfig.Tables.Update(destinationTable);

            var hasDestinationOrderTable = await _dbConfig.OrderTables
                .AnyAsync(ot => ot.OrderId == destinationOrder.Id && ot.TableId == destinationTable.Id && !ot.IsDeleted);
            if (!hasDestinationOrderTable)
            {
                _dbConfig.OrderTables.Add(new OrderTable
                {
                    OrderId = destinationOrder.Id,
                    TableId = destinationTable.Id,
                    IsPrimary = true,
                    InsertByUserId = userId
                });
            }

            await _dbConfig.SaveChangesAsync();
            return destinationOrder;
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
                
                // Only Admin can add Commercial users
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
                
                // Check if phone number exists for the same commercial user or globally if Admin
                if (currentUser?.Role == "Admin")
                {
                    // Admin can check globally
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
                else
                {
                    // Commercial users can only check within their own users
                    if (user != null && user.InsertByUserId == commercialUserId)
                    {
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = user,
                            ErrorStatus = true,
                            Message = "رقم الهاتف موجود بالفعل"
                        });
                    }
                }

                // Validate password is provided for new users
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
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                newUse.Password = passwordHash;

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
                
                // Set InsertByUserId based on role
                if (request.Role == "Commercial" && currentUser?.Role == "Admin")
                {
                    // Admin creating Commercial user - set InsertByUserId to Admin's ID or 0
                    newUse.InsertByUserId = currentUserId;
                }
                else
                {
                    // Commercial user creating sub-user
                    newUse.InsertByUserId = commercialUserId;
                }
                
                // Handle logo upload for Commercial users created by Admin
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
                
                // Set restaurant name for Commercial users created by Admin
                if (request.Role == "Commercial" && currentUser?.Role == "Admin" && !string.IsNullOrEmpty(request.RestaurantName))
                {
                    newUse.RestaurantName = request.RestaurantName;
                }

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
                
                // Check if user exists
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

                // Only Admin can update Commercial users
                if (user.Role == "Commercial" && currentUser?.Role != "Admin")
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لتعديل المستخدمين التجاريين. فقط المدير الرئيسي يمكنه ذلك"
                    });
                }

                // Commercial users can only update their own sub-users (not Commercial)
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

                // Prevent Commercial users from changing role to Commercial
                if (currentUser?.Role != "Admin" && request.Role == "Commercial")
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لتغيير الدور إلى تجاري. فقط المدير الرئيسي يمكنه ذلك"
                    });
                }

                // Store old values for audit log
                var oldValues = new
                {
                    Name = user.Name,
                    PhoneNumber = user.PhoneNumber,
                    Username = user.Username,
                    Role = user.Role,
                    RestaurantName = user.RestaurantName,
                    Logo = user.Logo,
                    LoginCode = user.LoginCode
                };

                // Update basic fields
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
                
                // Update password only if provided and not empty
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                // Admin can update Logo and RestaurantName for Commercial users
                if (currentUser?.Role == "Admin" && user.Role == "Commercial")
                {
                    // Upload logo if provided and has content
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
                    
                    // Update restaurant name if provided
                    if (!string.IsNullOrWhiteSpace(request.RestaurantName))
                    {
                        user.RestaurantName = request.RestaurantName;
                    }

                    if (string.IsNullOrWhiteSpace(request.LoginCode))
                    {
                        user.LoginCode = null;
                    }
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

                // Store new values for audit log
                var newValues = new
                {
                    Name = user.Name,
                    PhoneNumber = user.PhoneNumber,
                    Username = user.Username,
                    Role = user.Role,
                    RestaurantName = user.RestaurantName,
                    Logo = user.Logo,
                    LoginCode = user.LoginCode
                };

                _dbConfig.Users.Update(user);
                await _dbConfig.SaveChangesAsync();

                // Log audit
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var currentUser = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);
            var commercialUserId = GetCommercialUserId();
            var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && x.InsertByUserId == commercialUserId);
            if (user == null)
            {
                return BadRequest(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "user not exsit"
                });
            }

            var userName = user.Name;
            user!.IsDeleted = true;
            _dbConfig.Users.Update(user);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            await _dbConfig.LogAuditAsync(
                "Delete",
                "User",
                user.Id,
                userName,
                userId,
                commercialUserId,
                null,
                null,
                $"تم حذف المستخدم: {userName}"
            );

            return Ok(new GlobalResponse<int>
            {
                Data = id,
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

            if (userInfo != null && userInfo.Role == "Admin")
            {
                var user = _dbConfig.Users.Where(x => x.IsDeleted == false).AsQueryable();

                if (info != null)
                {
                    user = user.Where(x => x.PhoneNumber == info || x.Name.Contains(info) || x.Username.Contains(info));
                }
                var totalItems = user.Count();
                var pagedUsers = user
                    .OrderByDescending(x => x.Id)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedList<User>(pagedUsers, totalItems, pageNumber, pageSize);

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
                var commercialUserId = GetCommercialUserId();
                var user = _dbConfig.Users.Where(x => x.IsDeleted == false && x.InsertByUserId == commercialUserId).AsQueryable();

                if (info != null)
                {
                    user = user.Where(x => x.PhoneNumber == info || x.Name.Contains(info) || x.Username.Contains(info));
                }
                var totalItems = user.Count();
                var pagedUsers = user
                    .OrderByDescending(x => x.Id)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedList<User>(pagedUsers, totalItems, pageNumber, pageSize);

                var response = new GlobalResponse<PagedList<User>>
                {
                    Data = pagedResult,
                    ErrorStatus = false,
                    Message = "Success"
                };

                return response;
            }   


        
        }


        [AuthorizeSection("category", Roles = "Commercial,POS,Admin")]
        [HttpPost("AddTag")]
        public async Task<ActionResult<GlobalResponse<Tag>>> AddTag(TagRequset request)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _dbConfig.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            var userInsertByUserId = user?.InsertByUserId ?? userId;

            if (request.ParentTagId.HasValue)
            {
                var parent = await _dbConfig.Tags
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(x => x.Id == request.ParentTagId.Value && x.IsDeleted == false);
                if (parent == null)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم الأب غير موجود"
                    });
                }

                var parentScoped = parent.InsertByUserId == userId ||
                    (parent.User != null && (parent.User.Id == userInsertByUserId || parent.User.InsertByUserId == userId));
                if (!parentScoped)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم الأب غير موجود"
                    });
                }

                if (parent.ParentTagId != null)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يُسمح بمستوين فقط: قسم رئيسي ثم قسم فرعي"
                    });
                }
            }

            var tag = await _dbConfig.Tags.FirstOrDefaultAsync(x =>
                x.Name == request.Name && x.IsDeleted == false && x.InsertByUserId == userId &&
                x.ParentTagId == request.ParentTagId);
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


        // GET: api/Admin/CommercialUserInfo
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
                    RestaurantName = commercialUser.RestaurantName ?? commercialUser.Name,
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

        [Authorize(Roles = "Commercial,POS,Waiter,Manager")]
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

        // updata tag
        // Update User 
        [AuthorizeSection("category", Roles = "Commercial,Admin")]
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

            var tag = await _dbConfig.Tags
                .Include(t => t.User)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User!.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
            if (tag == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = tag,
                    ErrorStatus = true,
                    Message = "tag not exsit"
                });
            }

            if (request.ParentTagId == id)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "لا يمكن أن يكون القسم أبًا لنفسه"
                });
            }

            var hasChildren = await _dbConfig.Tags.AnyAsync(x => x.ParentTagId == id && x.IsDeleted == false);
            if (hasChildren && request.ParentTagId != null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "لا يمكن ربط قسم له أقسام فرعية كقسم فرعي تحت قسم آخر"
                });
            }

            if (request.ParentTagId.HasValue)
            {
                var parent = await _dbConfig.Tags
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(x => x.Id == request.ParentTagId.Value && x.IsDeleted == false);
                if (parent == null)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم الأب غير موجود"
                    });
                }

                var parentScoped = parent.InsertByUserId == userId ||
                    (parent.User != null && (parent.User.Id == user.InsertByUserId || parent.User.InsertByUserId == userId));
                if (!parentScoped)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم الأب غير موجود"
                    });
                }

                if (parent.ParentTagId != null)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يُسمح بمستوين فقط: قسم رئيسي ثم قسم فرعي"
                    });
                }
            }

            var duplicate = await _dbConfig.Tags.FirstOrDefaultAsync(x =>
                x.Id != id &&
                x.Name == request.Name && x.IsDeleted == false && x.InsertByUserId == userId &&
                x.ParentTagId == request.ParentTagId);
            if (duplicate != null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = duplicate,
                    ErrorStatus = true,
                    Message = "Tag is already exsit"
                });
            }

            // Store old values for audit log
            var oldValues = new
            {
                Name = tag.Name,
                ParentTagId = tag.ParentTagId
            };

            var uTag = _mapper.Map(request, tag);

            // Store new values for audit log
            var newValues = new
            {
                Name = uTag.Name,
                ParentTagId = uTag.ParentTagId
            };

            _dbConfig.Tags.Update(uTag);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;
            await _dbConfig.LogAuditAsync(
                "Update",
                "Tag",
                uTag.Id,
                uTag.Name,
                userId,
                commercialUserId,
                oldValues,
                newValues,
                $"تم تعديل القسم: {uTag.Name}"
            );

            return Ok(new GlobalResponse<Tag>
            {
                Data = uTag,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [AuthorizeSection("category", Roles = "Commercial")]
        [HttpDelete("DeleteTag")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteTag(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var tag = await _dbConfig.Tags
                .Include(t => t.User)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User!.Id == userInsertByUserId || x.User.InsertByUserId == userId));
            if (tag == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "tag not exsit"
                });
            }

            var children = await _dbConfig.Tags.Where(x => x.ParentTagId == id && x.IsDeleted == false).ToListAsync();
            foreach (var child in children)
            {
                child.IsDeleted = true;
                child.UpdateDate = DateTime.UtcNow;
                _dbConfig.Tags.Update(child);
            }

            tag!.IsDeleted = true;
            tag.UpdateDate = DateTime.UtcNow;
            _dbConfig.Tags.Update(tag);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Tag>
            {
                Data = tag,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [AuthorizeSection("category", Roles = "Commercial,POS,Waiter")]
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
            var pagedTags = tag
                .OrderBy(x => x.Name)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList();

            var pagedResult = new PagedList<Tag>(pagedTags, totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<Tag>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [AuthorizeSection("category", Roles = "Admin,Commercial")]
        [HttpPost("GenerateCategoriesWithAI")]
        public async Task<ActionResult<GlobalResponse<List<string>>>> GenerateCategoriesWithAI(GenerateCategoriesRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return BadRequest(new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الوصف مطلوب"
                    });
                }

                var apiKey = _configuration["OpenAISettings:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "OpenAI API Key غير موجود في الإعدادات"
                    });
                }

                var maxCategories = Math.Min(Math.Max(request.MaxCategories, 1), 20); // بين 1 و 20
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var currentUser = await _dbConfig.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
                var userInsertByUserId = currentUser?.InsertByUserId ?? userId;

                string? parentCategoryName = null;
                var avoidNames = new List<string>();
                if (request.ExistingCategories != null && request.ExistingCategories.Count > 0)
                    avoidNames.AddRange(request.ExistingCategories.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()));

                if (request.ParentTagId.HasValue)
                {
                    var parent = await _dbConfig.Tags
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(x => x.Id == request.ParentTagId.Value && !x.IsDeleted);
                    if (parent == null)
                    {
                        return BadRequest(new GlobalResponse<List<string>>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "التصنيف الرئيسي غير موجود"
                        });
                    }
                    var parentScoped = parent.InsertByUserId == userId ||
                        (parent.User != null && (parent.User.Id == userInsertByUserId || parent.User.InsertByUserId == userId));
                    if (!parentScoped)
                    {
                        return BadRequest(new GlobalResponse<List<string>>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "لا يمكن استخدام هذا التصنيف كأب"
                        });
                    }
                    if (parent.ParentTagId != null)
                    {
                        return BadRequest(new GlobalResponse<List<string>>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "يُسمح بمستوين فقط: اختر تصنيفاً رئيسياً (ليس فرعياً)"
                        });
                    }
                    parentCategoryName = parent.Name;
                    var existingSubs = await _dbConfig.Tags.AsNoTracking()
                        .Where(t => t.ParentTagId == parent.Id && !t.IsDeleted && t.InsertByUserId == userId)
                        .Select(t => t.Name)
                        .ToListAsync();
                    foreach (var n in existingSubs)
                    {
                        if (string.IsNullOrEmpty(n)) continue;
                        if (!avoidNames.Contains(n, StringComparer.OrdinalIgnoreCase))
                            avoidNames.Add(n);
                    }
                }

                string prompt;
                if (!string.IsNullOrEmpty(parentCategoryName))
                {
                    prompt = $"أنشئ قائمة بتصنيفات فرعية مناسبة لمطعم، تندرج جميعها تحت التصنيف الرئيسي «{parentCategoryName}».\n\n";
                    prompt += $"استخدم أيضاً السياق التالي من صاحب المطعم:\n{request.Description}\n\n";
                    prompt += "التصنيفات الفرعية يجب أن تكون أسماء أقسام داخلية لهذا القسم الرئيسي فقط (مثل أنواع ضمن «المشروبات» أو «المقبلات»)، وليست أقساماً رئيسية أخرى.\n\n";
                }
                else
                {
                    prompt = $"أنشئ قائمة بأقسام رئيسية مناسبة لمطعم بناءً على الوصف التالي:\n{request.Description}\n\n";
                }

                if (avoidNames.Count > 0)
                {
                    var existingCategoriesList = string.Join("، ", avoidNames.Distinct());
                    prompt += $"الأسماء التالية موجودة بالفعل ولا يجب تكرارها:\n{existingCategoriesList}\n\n";
                    prompt += "أنشئ أسماء جديدة مختلفة عن القائمة أعلاه.\n\n";
                }

                prompt += $"يجب أن تكون الأسماء باللغة العربية ومناسبة لنوع المطعم. أعد قائمة بأسماء التصنيفات فقط بدون شرح أو ترقيم، كل اسم في سطر منفصل. الحد الأقصى: {maxCategories} اسم.";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500,
                    temperature = 0.7
                };

                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API Error: {Error}", errorContent);
                    return StatusCode(500, new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "حدث خطأ أثناء الاتصال بـ OpenAI API"
                    });
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest(new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم الحصول على استجابة من OpenAI"
                    });
                }

                // Parse the response - split by newlines and clean up
                var categories = content
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().TrimStart('-', '*', '•', ' '))
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Take(maxCategories)
                    .ToList();

                if (categories.Count == 0)
                {
                    return BadRequest(new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم العثور على أقسام في الاستجابة"
                    });
                }

                return Ok(new GlobalResponse<List<string>>
                {
                    Data = categories,
                    ErrorStatus = false,
                    Message = "تم إنشاء الأقسام بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating categories with AI");
                return StatusCode(500, new GlobalResponse<List<string>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        private const string TagCategorySeparator = " › ";

        private static bool TagIsInUserScope(Tag tag, int userId, int userInsertByUserId)
        {
            return tag.InsertByUserId == userId ||
                   (tag.User != null && (tag.User.Id == userInsertByUserId || tag.User.InsertByUserId == userId));
        }

        /// <summary>يحدد نص حقل Tags للأطباق عند توليدها ضمن تصنيف محدد.</summary>
        private async Task<(bool Ok, string? ErrorMessage, string? FixedCategoryPath)> ResolveAiItemsFixedCategoryAsync(
            int userId, int? rootTagId, int? subTagId)
        {
            if (!rootTagId.HasValue)
                return (true, null, null);

            var currentUser = await _dbConfig.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            var userInsertByUserId = currentUser?.InsertByUserId ?? userId;

            var root = await _dbConfig.Tags.Include(t => t.User).FirstOrDefaultAsync(x => x.Id == rootTagId.Value && !x.IsDeleted);
            if (root == null)
                return (false, "التصنيف الرئيسي غير موجود", null);
            if (!TagIsInUserScope(root, userId, userInsertByUserId))
                return (false, "لا يمكن استخدام هذا القسم", null);
            if (root.ParentTagId != null)
                return (false, "اختر تصنيفاً رئيسياً فقط (ليس فرعياً)", null);

            var hasChildren = await _dbConfig.Tags.AnyAsync(t => t.ParentTagId == root.Id && !t.IsDeleted);

            if (hasChildren)
            {
                if (!subTagId.HasValue)
                    return (false, "هذا القسم يحتوي تصنيفات فرعية — اختر قسماً فرعياً", null);

                var sub = await _dbConfig.Tags.Include(t => t.User).FirstOrDefaultAsync(x => x.Id == subTagId.Value && !x.IsDeleted);
                if (sub == null)
                    return (false, "التصنيف الفرعي غير موجود", null);
                if (sub.ParentTagId != root.Id)
                    return (false, "التصنيف الفرعي لا يتبع القسم الرئيسي المختار", null);
                if (!TagIsInUserScope(sub, userId, userInsertByUserId))
                    return (false, "لا يمكن استخدام هذا القسم الفرعي", null);

                var rootName = root.Name ?? "";
                var subName = sub.Name ?? "";
                return (true, null, $"{rootName}{TagCategorySeparator}{subName}");
            }

            if (subTagId.HasValue)
                return (false, "هذا القسم الرئيسي بلا أقسام فرعية — أزل اختيار التصنيف الفرعي", null);

            return (true, null, root.Name ?? "");
        }

        [AuthorizeSection("items", Roles = "Admin,Commercial")]
        [HttpPost("GenerateItemsWithAI")]
        public async Task<ActionResult<GlobalResponse<List<GeneratedItemDto>>>> GenerateItemsWithAI(GenerateItemsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return BadRequest(new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الوصف مطلوب"
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var scopeResult = await ResolveAiItemsFixedCategoryAsync(userId, request.RootTagId, request.SubTagId);
                if (!scopeResult.Ok)
                {
                    return BadRequest(new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = scopeResult.ErrorMessage ?? "تعذر تحديد القسم"
                    });
                }

                var fixedCategoryPath = scopeResult.FixedCategoryPath;

                var apiKey = _configuration["OpenAISettings:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "OpenAI API Key غير موجود في الإعدادات"
                    });
                }

                var maxItems = Math.Min(Math.Max(request.MaxItems, 1), 20);

                string prompt;
                if (!string.IsNullOrEmpty(fixedCategoryPath))
                {
                    prompt = $"أنشئ قائمة بأطباق ومشروبات مناسبة لمطعم، تندرج جميعها تحت القسم «{fixedCategoryPath}».\n\n";
                    prompt += $"سياق إضافي من صاحب المطعم:\n{request.Description}\n\n";
                    prompt += "ركز على أصناف منطقية لهذا القسم فقط.\n\n";
                }
                else
                {
                    prompt = $"أنشئ قائمة بأطباق ومشروبات مناسبة لمطعم بناءً على الوصف التالي:\n{request.Description}\n\n";
                }

                if (request.ExistingItems != null && request.ExistingItems.Count > 0)
                {
                    var existingItemsList = string.Join(", ", request.ExistingItems.Select(i => i.Name));
                    prompt += $"الأطباق التالية موجودة بالفعل ولا يجب تكرارها:\n{existingItemsList}\n\n";
                    prompt += "أنشئ أطباقاً جديدة مختلفة عن الأسماء أعلاه.\n\n";
                }

                if (!string.IsNullOrEmpty(fixedCategoryPath))
                {
                    prompt += $"يجب أن تكون الأسماء بالعربية. أعد كل طبق في سطر بالشكل التالي (بدون عمود قسم):\nاسم الطبق | السعر (رقم فقط بدون فواصل) | وصف قصير اختياري\nمثال: عصير برتقال طازج | 2500 | عصير طبيعي\nالحد الأقصى: {maxItems} طبق.";
                }
                else
                {
                    prompt += $"يجب أن تكون الأطباق باللغة العربية ومناسبة لنوع المطعم. أعد قائمة بكل طبق في سطر منفصل بالشكل التالي:\nاسم الطبق | القسم | السعر (بالأرقام فقط بدون عملة) | الوصف (اختياري)\nمثال: حمص | مقبلات | 3000 | طبق حمص تقليدي من المطبخ العراقي\nالحد الأقصى للأطباق: {maxItems} طبق.";
                }

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API Error: {Error}", errorContent);
                    return StatusCode(500, new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "حدث خطأ أثناء الاتصال بـ OpenAI API"
                    });
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest(new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم الحصول على استجابة من OpenAI"
                    });
                }

                // Parse the response
                var items = new List<GeneratedItemDto>();
                var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines.Take(maxItems))
                {
                    var trimmedLine = line.Trim().TrimStart('-', '*', '•', ' ');
                    if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                    var parts = trimmedLine.Split('|');
                    if (parts.Length < 2) continue;

                    if (!string.IsNullOrEmpty(fixedCategoryPath))
                    {
                        var item = new GeneratedItemDto
                        {
                            Category = fixedCategoryPath,
                            Name = parts[0].Trim()
                        };
                        if (string.IsNullOrWhiteSpace(item.Name)) continue;

                        if (decimal.TryParse(parts[1].Trim().Replace(",", ""), System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var priceFixed))
                        {
                            item.SellingPrice = priceFixed;
                            item.DisCountPrice = priceFixed;
                            item.PurchasingPrice = priceFixed * 0.6m;
                            item.Description = parts.Length >= 3
                                ? string.Join("|", parts.Skip(2)).Trim()
                                : null;
                        }
                        else if (parts.Length >= 4 &&
                                 decimal.TryParse(parts[2].Trim().Replace(",", ""), System.Globalization.NumberStyles.Any,
                                     System.Globalization.CultureInfo.InvariantCulture, out var priceAlt))
                        {
                            item.SellingPrice = priceAlt;
                            item.DisCountPrice = priceAlt;
                            item.PurchasingPrice = priceAlt * 0.6m;
                            item.Description = parts.Length > 3 ? parts[3].Trim() : null;
                        }
                        else
                        {
                            item.SellingPrice = 0;
                            item.DisCountPrice = 0;
                            item.PurchasingPrice = 0;
                            item.Description = parts.Length >= 2 ? string.Join("|", parts.Skip(1)).Trim() : null;
                        }

                        items.Add(item);
                        continue;
                    }

                    var itemFree = new GeneratedItemDto
                    {
                        Name = parts[0].Trim(),
                        Category = parts.Length > 1 ? parts[1].Trim() : "مواد اخرى",
                        Description = parts.Length > 3 ? parts[3].Trim() : null
                    };

                    if (parts.Length > 2 && decimal.TryParse(parts[2].Trim().Replace(",", ""), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price))
                    {
                        itemFree.SellingPrice = price;
                        itemFree.DisCountPrice = price;
                        itemFree.PurchasingPrice = price * 0.6m;
                    }
                    else
                    {
                        itemFree.SellingPrice = 0;
                        itemFree.DisCountPrice = 0;
                        itemFree.PurchasingPrice = 0;
                    }

                    items.Add(itemFree);
                }

                if (items.Count == 0)
                {
                    return BadRequest(new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم العثور على أطباق في الاستجابة"
                    });
                }

                return Ok(new GlobalResponse<List<GeneratedItemDto>>
                {
                    Data = items,
                    ErrorStatus = false,
                    Message = "تم إنشاء الأطباق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating items with AI");
                return StatusCode(500, new GlobalResponse<List<GeneratedItemDto>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("AddMultipleItems")]
        public async Task<ActionResult<GlobalResponse<List<Item>>>> AddMultipleItems(List<GeneratedItemDto> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                {
                    return BadRequest(new GlobalResponse<List<Item>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لا توجد أطباق للحفظ"
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var savedItems = new List<Item>();
                var errors = new List<string>();

                foreach (var itemDto in items)
                {
                    if (string.IsNullOrWhiteSpace(itemDto.Name))
                    {
                        errors.Add("اسم الطبق مطلوب");
                        continue;
                    }

                    // Check if item already exists
                    var existingItem = await _dbConfig.Items.FirstOrDefaultAsync(
                        x => x.Name == itemDto.Name && 
                        x.IsDeleted == false && 
                        x.InsertByUserId == userId);

                    if (existingItem != null)
                    {
                        errors.Add($"الطبق '{itemDto.Name}' موجود بالفعل");
                        continue;
                    }

                    var newItem = new Item
                    {
                        Name = itemDto.Name,
                        Description = itemDto.Description,
                        SellingPrice = itemDto.SellingPrice,
                        PurchasingPrice = itemDto.PurchasingPrice,
                        DisCountPrice = itemDto.DisCountPrice,
                        Tags = itemDto.Category,
                        IsAvailable = true,
                        Code = $"ITEM{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}",
                        InsertByUserId = userId,
                        InsertDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        IsDeleted = false
                    };

                    _dbConfig.Items.Add(newItem);
                    savedItems.Add(newItem);
                }

                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<List<Item>>
                {
                    Data = savedItems,
                    ErrorStatus = false,
                    Message = errors.Count > 0 
                        ? $"تم حفظ {savedItems.Count} طبق بنجاح. {string.Join(", ", errors)}"
                        : $"تم حفظ {savedItems.Count} طبق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple items");
                return StatusCode(500, new GlobalResponse<List<Item>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("AddMultipleTags")]
        public async Task<ActionResult<GlobalResponse<List<Tag>>>> AddMultipleTags(List<TagRequset> tags)
        {
            try
            {
                if (tags == null || tags.Count == 0)
                {
                    return BadRequest(new GlobalResponse<List<Tag>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لا توجد أقسام للحفظ"
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var savedTags = new List<Tag>();
                var errors = new List<string>();

                foreach (var tagRequest in tags)
                {
                    if (string.IsNullOrWhiteSpace(tagRequest.Name))
                    {
                        errors.Add("اسم القسم مطلوب");
                        continue;
                    }

                    // Check if tag already exists (same name under same parent)
                    var existingTag = await _dbConfig.Tags.FirstOrDefaultAsync(
                        x => x.Name == tagRequest.Name && 
                        x.IsDeleted == false && 
                        x.InsertByUserId == userId &&
                        x.ParentTagId == tagRequest.ParentTagId);

                    if (existingTag != null)
                    {
                        errors.Add($"القسم '{tagRequest.Name}' موجود بالفعل");
                        continue;
                    }

                    var newTag = _mapper.Map<Tag>(tagRequest);
                    newTag.InsertByUserId = userId;
                    _dbConfig.Tags.Add(newTag);
                    savedTags.Add(newTag);
                }

                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<List<Tag>>
                {
                    Data = savedTags,
                    ErrorStatus = false,
                    Message = errors.Count > 0 
                        ? $"تم حفظ {savedTags.Count} قسم بنجاح. {string.Join(", ", errors)}"
                        : $"تم حفظ {savedTags.Count} قسم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple tags");
                return StatusCode(500, new GlobalResponse<List<Tag>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        // add item 
        [AuthorizeSection("items", Roles = "Commercial,POS")]
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

        // update item 
        [AuthorizeSection("items", Roles = "Commercial")]
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
           

            // Store old values for audit log
            var oldValues = new
            {
                Name = item.Name,
                Code = item.Code,
                SellingPrice = item.SellingPrice,
                PurchasingPrice = item.PurchasingPrice,
                DisCountPrice = item.DisCountPrice,
                Description = item.Description,
                Tags = item.Tags,
                IsAvailable = item.IsAvailable,
                Image = item.Image
            };

            item.Tags = request.Tags;
            item.PurchasingPrice = request.PurchasingPrice;
            item.DisCountPrice = request.DisCountPrice;
            item.Description = request.Description;
            item.SellingPrice = request.SellingPrice;
            item.IsAvailable = request.IsAvailable;
            item.Code = request.Code;
            item.Name = request.Name;
            item.Image = request.Image != null ? await UploadIamgesAsync(request.Image): item.Image;

            // Store new values for audit log
            var newValues = new
            {
                Name = item.Name,
                Code = item.Code,
                SellingPrice = item.SellingPrice,
                PurchasingPrice = item.PurchasingPrice,
                DisCountPrice = item.DisCountPrice,
                Description = item.Description,
                Tags = item.Tags,
                IsAvailable = item.IsAvailable,
                Image = item.Image
            };

            _dbConfig.Items.Update(item);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;
            await _dbConfig.LogAuditAsync(
                "Update",
                "Item",
                item.Id,
                item.Name,
                userId,
                commercialUserId,
                oldValues,
                newValues,
                $"تم تعديل الصنف: {item.Name}"
            );

            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [AuthorizeSection("items", Roles = "Commercial")]
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

            // Log audit
            var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;
            await _dbConfig.LogAuditAsync(
                "Delete",
                "Item",
                item.Id,
                item.Name,
                userId,
                commercialUserId,
                null,
                null,
                $"تم حذف الصنف: {item.Name}"
            );

            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }


        [AuthorizeSection("items", "reports", Roles = "Commercial,POS,Waiter")]
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
            var item = _dbConfig.Items
                .Include(x => x.User)
                .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || (x.User != null && x.User.Id == userInsertByUserId) || (x.User != null && x.User.InsertByUserId == userId)))
                .AsQueryable();

            if (info != null)
            {
                item = item.Where(x => x.Code == info || x.Name.Contains(info) || x.Description!.Contains(info) || x.Tags!.Contains(info));
            }

            var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

            var totalItems = item.Count();
            var itemList = item
                .OrderByDescending(x => x.Id)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList();
            
            foreach(var n in itemList)
            {
                if (!string.IsNullOrEmpty(n.Image))
            {
                n.Image = imageBaseUrl + n.Image;
            }
            }

            var pagedResult = new PagedList<Item>(itemList, totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<Item>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [AuthorizeSection("items", Roles = "Commercial,POS,Reader,Waiter")]
        [HttpGet("GetItemsByCode")]
        public async Task<ActionResult<GlobalResponse<Object>>> GetItemsByCode(string code)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);


            var item =await _dbConfig.Items.Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId) && x.Code == code).FirstOrDefaultAsync();



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

            item.Image = imageBaseUrl + item.Image;
            
            var response = new GlobalResponse<Object>
            {
                Data = item,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
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

                var commercialUserIdForTableGuard = GetCommercialUserId();
                var tableIdsToGuard = new List<int>();
                if (request.TableIds != null && request.TableIds.Any())
                {
                    tableIdsToGuard.AddRange(request.TableIds.Distinct());
                }
                else if (request.TableId.HasValue)
                {
                    tableIdsToGuard.Add(request.TableId.Value);
                }

                foreach (var tableIdToGuard in tableIdsToGuard.Distinct())
                {
                    var unpaidOnTable = await FindUnpaidDineInOrderForTableAsync(
                        tableIdToGuard,
                        commercialUserIdForTableGuard);

                    if (unpaidOnTable != null && IsUnpaidOrder(unpaidOnTable))
                    {
                        return BadRequest(new GlobalResponse<CustomerOrder>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "activeTableOrderExists"
                        });
                    }
                }
                
                // Load items with user information to avoid lazy loading issues
                var items = await _dbConfig.Items
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId))
                    .ToListAsync();

                var orderCode = request.OrderCode ?? RandomCode();
                var orderType = request.OrderType ?? "DineIn";
                var numberOfGuests = (orderType == "DineIn" && request.NumberOfGuests.HasValue && request.NumberOfGuests.Value > 0)
                    ? request.NumberOfGuests.Value
                    : 0;
                
                // Calculate DailySequenceNumber for all orders (resets daily)
                int? dailySequenceNumber = null;
                try
                {
                    var commercialUserId = GetCommercialUserId();
                    var today = DateTime.UtcNow.Date;
                    var tomorrow = today.AddDays(1);

                    // Get max daily sequence for current business day
                    var ordersToday = await _dbConfig.CustomerOrders
                        .Where(o => o.InsertByUserId == commercialUserId
                            && o.InsertDate >= today
                            && o.InsertDate < tomorrow
                            && o.DailySequenceNumber.HasValue
                            && !o.IsDeleted)
                        .Select(o => o.DailySequenceNumber.Value)
                        .ToListAsync();

                    var maxSequenceToday = ordersToday.Any() ? ordersToday.Max() : 0;
                    dailySequenceNumber = maxSequenceToday + 1;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating DailySequenceNumber");
                    dailySequenceNumber = null;
                }

                // Handle Delivery Driver
                int? deliveryDriverId = null;
                if (orderType == "Delivery")
                {
                    if (request.DeliveryDriverId.HasValue)
                    {
                        // Use existing driver
                        var existingDriver = await _dbConfig.DeliveryDrivers
                            .FirstOrDefaultAsync(d => d.Id == request.DeliveryDriverId.Value 
                                && !d.IsDeleted 
                                && d.IsActive);
                        
                        if (existingDriver != null)
                        {
                            deliveryDriverId = existingDriver.Id;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(request.NewDriverName) 
                        && !string.IsNullOrWhiteSpace(request.NewDriverPhone))
                    {
                        // Create new driver
                        var commercialUserId = GetCommercialUserId();
                        var newDriver = new DeliveryDriver
                        {
                            Name = request.NewDriverName.Trim(),
                            PhoneNumber = request.NewDriverPhone.Trim(),
                            Address = request.NewDriverAddress?.Trim(),
                            VehicleType = request.NewDriverVehicleType?.Trim(),
                            VehicleNumber = request.NewDriverVehicleNumber?.Trim(),
                            IsActive = true,
                            InsertByUserId = commercialUserId,
                            InsertDate = DateTime.UtcNow,
                            UpdateDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        
                        _dbConfig.DeliveryDrivers.Add(newDriver);
                        await _dbConfig.SaveChangesAsync();
                        deliveryDriverId = newDriver.Id;
                    }
                }

                int? creditEmployeeId = null;
                int? creditCustomerId = null;
                var paymentMethod = request.PaymentMethod ?? "Cash";
                if (string.Equals(paymentMethod, "Credit", StringComparison.OrdinalIgnoreCase))
                {
                    var commercialUserIdForCredit = GetCommercialUserId();
                    var hasEmp = request.CreditEmployeeId.HasValue;
                    var hasCust = request.CreditCustomerId.HasValue;
                    if (hasEmp == hasCust)
                    {
                        return BadRequest(new GlobalResponse<CustomerOrder>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "Credit orders require exactly one of employee or customer account."
                        });
                    }

                    if (hasEmp)
                    {
                        var emp = await _dbConfig.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e => e.Id == request.CreditEmployeeId!.Value
                                && !e.IsDeleted
                                && e.InsertByUserId == commercialUserIdForCredit);
                        if (emp == null)
                        {
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = "Invalid credit employee."
                            });
                        }

                        creditEmployeeId = emp.Id;
                    }

                    if (hasCust)
                    {
                        var cust = await _dbConfig.Customers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == request.CreditCustomerId!.Value
                                && !c.IsDeleted
                                && c.InsertByUserId == commercialUserIdForCredit);
                        if (cust == null)
                        {
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = "Invalid credit customer."
                            });
                        }

                        creditCustomerId = cust.Id;
                    }
                }

                var newOrder = new CustomerOrder
                {
                    OrderCode = orderCode,
                    PaymentMethod = paymentMethod,
                    InsertByUserId = userId,
                    TableId = request.TableId,
                    ReservationId = request.ReservationId,
                    OrderType = orderType,
                    NumberOfGuests = numberOfGuests,
                    Notes = request.Notes,
                    PagerNumber = request.PagerNumber,
                    OrderStatus = "Pending",
                    PaymentStatus = "Pending",
                    DailySequenceNumber = dailySequenceNumber,
                    DeliveryDriverId = deliveryDriverId,
                    DeliveryStatus = orderType == "Delivery" ? (request.DeliveryStatus ?? "Pending") : null,
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryPhoneNumber = request.DeliveryPhoneNumber,
                    DeliveryCustomerName = request.DeliveryCustomerName,
                    DeliveryFee = request.DeliveryFee,
                    CreditEmployeeId = creditEmployeeId,
                    CreditCustomerId = creditCustomerId,
                    DiscountType = request.DiscountType,
                    DiscountValue = request.DiscountValue,
                    DiscountAmount = request.DiscountAmount,
                    DiscountPercent = request.DiscountPercent,
                    OrderSubTotal = request.OrderSubTotal,
                    OrderTotalAfterDiscount = request.OrderTotalAfterDiscount,
                    DeliveryAssignedAt = deliveryDriverId.HasValue ? DateTime.UtcNow : null
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

                    // Check item availability before processing
                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                        if (currentItem == null) continue;

                        // Check if item is available
                        if (!currentItem.IsAvailable)
                        {
                            _logger.LogWarning("Item {ItemId} is not available", itemRequest.ItemId);
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"Item '{currentItem.Name}' is not available"
                            });
                        }
                    }

                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var existingItem = insertItems.FirstOrDefault(x => x.ItemId == itemRequest.ItemId);
                        if (existingItem != null)
                        {
                            // Increment the quantity of an existing item
                            existingItem.Quantity += itemRequest.Quantity;
                            if (!string.IsNullOrWhiteSpace(itemRequest.Notes))
                            {
                                existingItem.Notes = itemRequest.Notes.Trim();
                            }
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
                                Notes = string.IsNullOrWhiteSpace(itemRequest.Notes) ? null : itemRequest.Notes.Trim(),
                                InsertByUserId = userId,
                            };

                            insertItems.Add(newOrderItem);
                        }
                    }

                    _dbConfig.CustomerOrderItems.AddRange(insertItems);
                    await _dbConfig.SaveChangesAsync();


                    // Handle multiple tables (TableIds) or single table (TableId)
                    // Only process tables for DineIn orders
                    if (newOrder.OrderType == "DineIn" || string.IsNullOrEmpty(newOrder.OrderType))
                    {
                        var commercialUserId = GetCommercialUserId();
                        var tablesToUpdate = new List<Table>();
                        
                        if (request.TableIds != null && request.TableIds.Any())
                        {
                            // Multiple tables - create OrderTable entries
                            var orderTables = new List<OrderTable>();
                            var isFirst = true;
                            
                            foreach (var tableId in request.TableIds.Distinct())
                            {
                                var table = await _dbConfig.Tables
                                    .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                                
                                if (table != null)
                                {
                                    // Create OrderTable entry
                                    var orderTable = new OrderTable
                                    {
                                        OrderId = newOrder.Id,
                                        TableId = tableId,
                                        IsPrimary = isFirst,
                                        InsertByUserId = userId
                                    };
                                    orderTables.Add(orderTable);
                                    
                                    // Update table status
                                    table.Status = "Occupied";
                                    table.CurrentOrderId = newOrder.Id;
                                    tablesToUpdate.Add(table);
                                    
                                    isFirst = false;
                                }
                            }
                            
                            if (orderTables.Any())
                            {
                                _dbConfig.OrderTables.AddRange(orderTables);
                                await _dbConfig.SaveChangesAsync();
                                
                                // Set TableId to the first table for backward compatibility
                                newOrder.TableId = request.TableIds.First();
                                _dbConfig.CustomerOrders.Update(newOrder);
                                await _dbConfig.SaveChangesAsync();
                                
                                // Update all tables
                                foreach (var table in tablesToUpdate)
                                {
                                    _dbConfig.Tables.Update(table);
                                }
                                await _dbConfig.SaveChangesAsync();
                                
                            // SignalR side-effects should not fail successful order creation
                            try
                            {
                                foreach (var table in tablesToUpdate)
                                {
                                    await _hubContext.Clients.All.SendAsync("TableUpdated", new
                                    {
                                        TableId = table.Id,
                                        Status = table.Status,
                                        TableNumber = table.TableNumber,
                                        Zone = table.Zone,
                                        CurrentOrderId = newOrder.Id
                                    });
                                }
                            }
                            catch (Exception signalEx)
                            {
                                _logger.LogError(signalEx, "AddOrder table-merge TableUpdated signal failed after save");
                            }
                            }
                        }
                        else if (newOrder.TableId.HasValue)
                    {
                            // Single table - backward compatibility
                        var table = await _dbConfig.Tables.FirstOrDefaultAsync(t => t.Id == newOrder.TableId.Value);
                        if (table != null)
                        {
                            table.Status = "Occupied";
                            table.CurrentOrderId = newOrder.Id;
                            _dbConfig.Tables.Update(table);
                                await _dbConfig.SaveChangesAsync();
                                
                                // Create OrderTable entry for consistency
                                var orderTable = new OrderTable
                                {
                                    OrderId = newOrder.Id,
                                    TableId = table.Id,
                                    IsPrimary = true,
                                    InsertByUserId = userId
                                };
                                _dbConfig.OrderTables.Add(orderTable);
                            await _dbConfig.SaveChangesAsync();
                            
                            // SignalR side-effects should not fail successful order creation
                            try
                            {
                                await _hubContext.Clients.All.SendAsync("TableUpdated", new
                                {
                                    TableId = table.Id,
                                    Status = table.Status,
                                        TableNumber = table.TableNumber,
                                        Zone = table.Zone,
                                        CurrentOrderId = newOrder.Id
                                });
                            }
                            catch (Exception signalEx)
                            {
                                _logger.LogError(signalEx, "AddOrder single-table TableUpdated signal failed after save");
                            }
                            }
                        }
                    }
                }

                _logger.LogInformation("Order created successfully: {OrderCode} by user {UserId}", orderCode, userId);
                
                // Send SignalR notification for new order
                try
                {
                    await _hubContext.Clients.All.SendAsync("OrderAdded", new
                    {
                        OrderId = newOrder.Id,
                        OrderCode = newOrder.OrderCode,
                        TableId = newOrder.TableId,
                        OrderType = newOrder.OrderType
                    });
                    _logger.LogInformation("SignalR notification sent for OrderAdded: OrderId={OrderId}, TableId={TableId}", newOrder.Id, newOrder.TableId);

                    // Also send PublicOrderAdded for Takeaway/Delivery orders
                    if (newOrder.OrderType == "Takeaway" || newOrder.OrderType == "Delivery")
                    {
                        var commercialUserId = GetCommercialUserId();
                        await _hubContext.Clients.All.SendAsync("PublicOrderAdded", new
                        {
                            CommercialUserId = commercialUserId,
                            OrderId = newOrder.Id,
                            OrderCode = newOrder.OrderCode,
                            OrderType = newOrder.OrderType,
                            DailySequenceNumber = newOrder.DailySequenceNumber,
                            InsertDate = newOrder.InsertDate
                        });
                        _logger.LogInformation("SignalR notification sent for PublicOrderAdded: OrderId={OrderId}, CommercialUserId={CommercialUserId}", newOrder.Id, commercialUserId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SignalR notification for OrderAdded");
                }

                // Return a simple response without navigation properties to avoid serialization issues
                var responseData = new
                {
                    Id = newOrder.Id,
                    OrderCode = newOrder.OrderCode,
                    PaymentMethod = newOrder.PaymentMethod,
                    OrderType = newOrder.OrderType,
                    TableId = newOrder.TableId,
                    ReservationId = newOrder.ReservationId,
                    InsertDate = newOrder.InsertDate
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = responseData,
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

        [Authorize(Roles = "Commercial,POS,Waiter")]
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

                // Get existing order
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

                // Store old values for audit log (before any changes)
                var activeOrderItems = existingOrder.CustomerOrderItem?
                    .Where(i => i != null && !i.IsDeleted)
                    .ToList() ?? new List<CustomerOrderItem>();
                var oldItemsCount = activeOrderItems.Count;
                var oldOrderValues = new
                {
                    PaymentMethod = existingOrder.PaymentMethod,
                    OrderType = existingOrder.OrderType,
                    NumberOfGuests = existingOrder.NumberOfGuests,
                    OrderStatus = existingOrder.OrderStatus,
                    PaymentStatus = existingOrder.PaymentStatus,
                    TableId = existingOrder.TableId,
                    ReservationId = existingOrder.ReservationId,
                    Notes = existingOrder.Notes,
                    PagerNumber = existingOrder.PagerNumber,
                    DiscountType = existingOrder.DiscountType,
                    DiscountValue = existingOrder.DiscountValue,
                    DiscountAmount = existingOrder.DiscountAmount,
                    DiscountPercent = existingOrder.DiscountPercent,
                    OrderSubTotal = existingOrder.OrderSubTotal,
                    OrderTotalAfterDiscount = existingOrder.OrderTotalAfterDiscount,
                    ItemsCount = oldItemsCount
                };

                // Update order basic info
                existingOrder.PaymentMethod = request.PaymentMethod;
                existingOrder.OrderType = request.OrderType;
                existingOrder.NumberOfGuests = (request.OrderType == "DineIn" && request.NumberOfGuests.HasValue && request.NumberOfGuests.Value > 0)
                    ? request.NumberOfGuests.Value
                    : 0;
                existingOrder.Notes = request.Notes;
                existingOrder.PagerNumber = request.PagerNumber;
                existingOrder.TableId = request.TableId;
                existingOrder.ReservationId = request.ReservationId;
                existingOrder.DiscountType = request.DiscountType;
                existingOrder.DiscountValue = request.DiscountValue;
                existingOrder.DiscountAmount = request.DiscountAmount;
                existingOrder.DiscountPercent = request.DiscountPercent;
                existingOrder.OrderSubTotal = request.OrderSubTotal;
                existingOrder.OrderTotalAfterDiscount = request.OrderTotalAfterDiscount;

                // Handle order items update — soft-delete active lines so ReturnedOrderItems FK stays valid
                var now = DateTime.Now;
                foreach (var item in activeOrderItems)
                {
                    item.IsDeleted = true;
                    item.UpdateDate = now;
                }

                // Add new order items
                var newOrderItems = new List<CustomerOrderItem>();

                if (request.CustomerOrderItem != null && request.CustomerOrderItem.Count > 0)
                {
                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var currentItem = await _dbConfig.Items
                            .FirstOrDefaultAsync(x => x.Id == itemRequest.ItemId && x.IsDeleted == false &&
                                (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));

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
                            continue;
                        }

                        newOrderItems.Add(new CustomerOrderItem
                        {
                            ItemId = itemRequest.ItemId,
                            Quantity = itemRequest.Quantity,
                            SellingPrice = sellingPrice,
                            Notes = normalizedNotes,
                            CustomerOrderId = existingOrder.Id,
                            InsertByUserId = userId,
                            InsertDate = DateTime.Now
                        });
                    }

                    _dbConfig.CustomerOrderItems.AddRange(newOrderItems);
                }

                _dbConfig.CustomerOrders.Update(existingOrder);
                await _dbConfig.SaveChangesAsync();

                // Reload order items to get accurate count after save
                await _dbConfig.Entry(existingOrder)
                    .Collection(x => x.CustomerOrderItem)
                    .LoadAsync();

                // Log audit for order update
                var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;
                var newItemsCount = existingOrder.CustomerOrderItem?
                    .Count(item => item != null && !item.IsDeleted) ?? 0;
                var newOrderValues = new
                {
                    PaymentMethod = existingOrder.PaymentMethod,
                    OrderType = existingOrder.OrderType,
                    OrderStatus = existingOrder.OrderStatus,
                    PaymentStatus = existingOrder.PaymentStatus,
                    TableId = existingOrder.TableId,
                    ReservationId = existingOrder.ReservationId,
                    Notes = existingOrder.Notes,
                    PagerNumber = existingOrder.PagerNumber,
                    DiscountType = existingOrder.DiscountType,
                    DiscountValue = existingOrder.DiscountValue,
                    DiscountAmount = existingOrder.DiscountAmount,
                    DiscountPercent = existingOrder.DiscountPercent,
                    OrderSubTotal = existingOrder.OrderSubTotal,
                    OrderTotalAfterDiscount = existingOrder.OrderTotalAfterDiscount,
                    ItemsCount = newItemsCount
                };

                // Build changes description
                var changesDescription = new List<string>();
                
                if (oldOrderValues.PaymentMethod != newOrderValues.PaymentMethod)
                {
                    changesDescription.Add($"طريقة الدفع: {oldOrderValues.PaymentMethod ?? "---"} → {newOrderValues.PaymentMethod ?? "---"}");
                }
                if (oldOrderValues.OrderType != newOrderValues.OrderType)
                {
                    changesDescription.Add($"نوع الطلب: {oldOrderValues.OrderType ?? "---"} → {newOrderValues.OrderType ?? "---"}");
                }
                if (oldOrderValues.TableId != newOrderValues.TableId)
                {
                    changesDescription.Add($"رقم الطاولة: {oldOrderValues.TableId?.ToString() ?? "---"} → {newOrderValues.TableId?.ToString() ?? "---"}");
                }
                if (oldOrderValues.ReservationId != newOrderValues.ReservationId)
                {
                    changesDescription.Add($"رقم الحجز: {oldOrderValues.ReservationId?.ToString() ?? "---"} → {newOrderValues.ReservationId?.ToString() ?? "---"}");
                }
                if (oldOrderValues.Notes != newOrderValues.Notes)
                {
                    changesDescription.Add("تم تعديل الملاحظات");
                }
                
                // Check if items changed
                if (oldOrderValues.ItemsCount != newOrderValues.ItemsCount)
                {
                    changesDescription.Add($"عدد العناصر: {oldOrderValues.ItemsCount} → {newOrderValues.ItemsCount}");
                }

                // Always log audit - even if no visible changes, we still modified the order
                var description = changesDescription.Count > 0 
                    ? $"تم تعديل الطلب {existingOrder.OrderCode}: {string.Join(", ", changesDescription)}"
                    : $"تم تعديل الطلب {existingOrder.OrderCode}";

                await _dbConfig.LogAuditAsync(
                    "Update",
                    "CustomerOrder",
                    existingOrder.Id,
                    existingOrder.OrderCode,
                    userId,
                    commercialUserId,
                    oldOrderValues,
                    newOrderValues,
                    description
                );

                // Send SignalR notification
                try
                {
                    await _hubContext.Clients.All.SendAsync("OrderUpdated", new
                    {
                        OrderId = existingOrder.Id,
                        OrderCode = existingOrder.OrderCode,
                        TableId = existingOrder.TableId
                    });
                    _logger.LogInformation("SignalR notification sent for OrderUpdated: OrderId={OrderId}", existingOrder.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SignalR notification for OrderUpdated");
                }

                return Ok(new GlobalResponse<CustomerOrder>
                {
                    Data = existingOrder,
                    ErrorStatus = false,
                    Message = "تم تحديث الفاتورة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                return StatusCode(500, new GlobalResponse<CustomerOrder>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء تحديث الفاتورة"
                });
            }
        }

        [Authorize(Roles = "Commercial")]
        [HttpDelete("DeleteOrder")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteOrder(int id)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

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

            var orderCode = item.OrderCode;
            item!.IsDeleted = true;
            _dbConfig.CustomerOrders.Update(item);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var commercialUserId = user?.Role == "Commercial" ? userId : (user?.InsertByUserId ?? userId);
            await _dbConfig.LogAuditAsync(
                "Delete",
                "Order",
                item.Id,
                orderCode,
                userId,
                commercialUserId,
                null,
                null,
                $"تم حذف الطلب: {orderCode}"
            );

            return Ok(new GlobalResponse<CustomerOrder>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpGet("GetTableOrders")]
        public async Task<ActionResult<GlobalResponse<List<OrderDto>>>> GetTableOrders(int tableId)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                // Verify table belongs to this Commercial and get current order
                var table = await _dbConfig.Tables
                    .Include(t => t.CurrentOrder)
                    .ThenInclude(o => o!.CustomerOrderItem)
                    .ThenInclude(oi => oi.Item)
                    .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                
                if (table == null)
                {
                    return NotFound(new GlobalResponse<List<OrderDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة غير موجودة"
                    });
                }

                // Get only the current order (if exists)
                var orders = new List<CustomerOrder>();
                if (table.CurrentOrderId.HasValue && table.CurrentOrder != null && !table.CurrentOrder.IsDeleted)
                {
                    orders.Add(table.CurrentOrder);
                }
                else
                {
                    var fallbackOrder = await FindUnpaidDineInOrderForTableAsync(tableId, commercialUserId);
                    if (fallbackOrder != null)
                    {
                        var loaded = await LoadOrderWithItemsAsync(fallbackOrder.Id);
                        if (loaded != null)
                        {
                            orders.Add(loaded);
                            await RepairTableActiveOrderLinkAsync(table, loaded);
                        }
                    }
                }

                if (!orders.Any())
                {
                    return Ok(new GlobalResponse<List<OrderDto>>
                    {
                        Data = new List<OrderDto>(),
                        ErrorStatus = false,
                        Message = "لا يوجد طلب نشط على هذه الطاولة"
                    });
                }

                // Load OrderTables for the current order
                var orderIdsList = orders.Select(o => o.Id).ToList();
                var orderTables = await _dbConfig.OrderTables
                    .Where(ot => orderIdsList.Contains(ot.OrderId) && !ot.IsDeleted)
                    .Include(ot => ot.Table)
                    .ToListAsync();

                // Load all tables that might be needed (for backward compatibility)
                var allTableIds = orders
                    .Where(o => o.TableId.HasValue)
                    .Select(o => o.TableId.Value)
                    .Distinct()
                    .ToList();
                
                var allTablesDict = await _dbConfig.Tables
                    .Where(t => allTableIds.Contains(t.Id) && !t.IsDeleted)
                    .ToDictionaryAsync(t => t.Id);

                var orderDtos = orders.Select(x => {
                    var activeOrderItems = x.CustomerOrderItem?
                        .Where(item => item != null && !item.IsDeleted)
                        .ToList() ?? new List<CustomerOrderItem>();

                    // Get tables for this order from OrderTables
                    var tablesForOrder = orderTables
                        .Where(ot => ot.OrderId == x.Id && ot.Table != null && !ot.Table.IsDeleted)
                        .Select(ot => ot.Table!)
                        .Distinct()
                        .ToList();
                    
                    // If no tables found in OrderTables, check if TableId is set (backward compatibility)
                    if (!tablesForOrder.Any() && x.TableId.HasValue)
                    {
                        if (allTablesDict.TryGetValue(x.TableId.Value, out var singleTable))
                        {
                            tablesForOrder.Add(singleTable);
                        }
                    }
                    
                    // Convert to TableDto to avoid circular reference
                    var tablesDto = tablesForOrder.Select(t => new TableDto
                    {
                        Id = t.Id,
                        TableNumber = t.TableNumber,
                        Capacity = t.Capacity,
                        Status = t.Status,
                        Zone = t.Zone,
                        Notes = t.Notes
                    }).ToList();
                    
                    // Generate merged table numbers string
                    var mergedTableNumbers = tablesDto.Any() 
                        ? string.Join("و", tablesDto.OrderBy(t => t.TableNumber).Select(t => t.TableNumber))
                        : null;

                    return new OrderDto
                    {
                        CustomerOrderItem = activeOrderItems,
                        OrderPrice = activeOrderItems.Sum(item => item.SellingPrice * item.Quantity),
                        OrderCode = x.OrderCode,
                        Id = x.Id,
                        ItemsCount = activeOrderItems.Count,
                        DailySequenceNumber = x.DailySequenceNumber,
                        InsertDate = x.InsertDate,
                        CreatedByUserId = x.User != null ? x.User.Id : null,
                        CreatedByUsername = x.User != null ? x.User.Username : null,
                        PaymentMethod = x.PaymentMethod,
                        OrderType = x.OrderType,
                        NumberOfGuests = x.NumberOfGuests ?? 0,
                        DiscountType = x.DiscountType,
                        DiscountValue = x.DiscountValue,
                        DiscountAmount = x.DiscountAmount,
                        DiscountPercent = x.DiscountPercent,
                        OrderSubTotal = x.OrderSubTotal,
                        OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                        Tables = tablesDto,
                        MergedTableNumbers = mergedTableNumbers
                    };
                }).ToList();

                return Ok(new GlobalResponse<List<OrderDto>>
                {
                    Data = orderDtos,
                    ErrorStatus = false,
                    Message = "تم جلب طلب الطاولة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table orders");
                return StatusCode(500, new GlobalResponse<List<OrderDto>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء جلب طلبات الطاولة"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPost("LogReturnedOrderItem")]
        public async Task<ActionResult<GlobalResponse<ReturnedOrderItemDto>>> LogReturnedOrderItem([FromBody] LogReturnedOrderItemRequest request)
        {
            try
            {
                if (request == null || request.SourceOrderItemId <= 0)
                {
                    return BadRequest(new GlobalResponse<ReturnedOrderItemDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "بيانات غير صحيحة"
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);
                if (user == null)
                {
                    return Unauthorized(new GlobalResponse<ReturnedOrderItemDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                var commercialUserId = GetCommercialUserId();
                var sourceOrderItem = await _dbConfig.CustomerOrderItems
                    .Include(x => x.Item)
                    .Include(x => x.CustomerOrder)
                    .ThenInclude(o => o!.OrderTables)
                    .ThenInclude(ot => ot.Table)
                    .FirstOrDefaultAsync(x => x.Id == request.SourceOrderItemId && !x.IsDeleted);

                if (sourceOrderItem == null || sourceOrderItem.CustomerOrder == null || sourceOrderItem.Item == null)
                {
                    return NotFound(new GlobalResponse<ReturnedOrderItemDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "عنصر الفاتورة غير موجود"
                    });
                }

                var sourceOrder = sourceOrderItem.CustomerOrder;
                var orderBelongsToCommercial =
                    sourceOrder.InsertByUserId == commercialUserId ||
                    _dbConfig.Users.Any(u => u.Id == sourceOrder.InsertByUserId && u.InsertByUserId == commercialUserId);
                if (!orderBelongsToCommercial)
                {
                    return Forbid();
                }

                var requestedQty = request.DeletedQuantity ?? sourceOrderItem.Quantity;
                var deletedQty = Math.Max(1, Math.Min(sourceOrderItem.Quantity, requestedQty));

                var linkedTables = sourceOrder.OrderTables?
                    .Where(ot => !ot.IsDeleted && ot.Table != null && !ot.Table.IsDeleted)
                    .Select(ot => ot.Table!)
                    .OrderBy(t => t.TableNumber)
                    .ToList() ?? new List<Table>();

                Table? primaryTable = linkedTables.FirstOrDefault();
                if (primaryTable == null && sourceOrder.TableId.HasValue)
                {
                    primaryTable = await _dbConfig.Tables
                        .FirstOrDefaultAsync(t => t.Id == sourceOrder.TableId.Value && !t.IsDeleted);
                }

                var mergedTableNumbers = linkedTables.Count > 1
                    ? string.Join("و", linkedTables.Select(t => t.TableNumber))
                    : (linkedTables.Count == 1 ? linkedTables[0].TableNumber : primaryTable?.TableNumber);
                var unitPrice = sourceOrderItem.SellingPrice;

                var returnedItem = new ReturnedOrderItem
                {
                    CustomerOrderId = sourceOrder.Id,
                    CustomerOrderItemId = sourceOrderItem.Id,
                    TableId = primaryTable?.Id ?? sourceOrder.TableId,
                    TableNumber = primaryTable?.TableNumber,
                    MergedTableNumbers = mergedTableNumbers,
                    OrderCode = sourceOrder.OrderCode,
                    OrderType = sourceOrder.OrderType,
                    PaymentMethod = sourceOrder.PaymentMethod,
                    ItemId = sourceOrderItem.ItemId,
                    ItemName = sourceOrderItem.Item.Name ?? string.Empty,
                    Quantity = deletedQty,
                    UnitPrice = unitPrice,
                    LineTotal = unitPrice * deletedQty,
                    Reason = "DeletedFromPOS",
                    DeletedByUserId = userId,
                    DeletedByUsername = user.Username,
                    InsertByUserId = commercialUserId
                };

                _dbConfig.ReturnedOrderItems.Add(returnedItem);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<ReturnedOrderItemDto>
                {
                    Data = MapReturnedOrderItemDto(returnedItem),
                    ErrorStatus = false,
                    Message = "تم تسجيل المادة المسترجعة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging returned order item");
                return StatusCode(500, new GlobalResponse<ReturnedOrderItemDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء تسجيل المادة المسترجعة"
                });
            }
        }

        [AuthorizeSection("reports", Roles = "Commercial")]
        [HttpGet("GetReturnedOrderItems")]
        public ActionResult<GlobalResponse<PagedList<ReturnedOrderItemDto>>> GetReturnedOrderItems(
            int pageNumber,
            int pageSize,
            DateTime? startDate,
            DateTime? endDate,
            int? tableId,
            int? itemId,
            string? info)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return BadRequest(new GlobalResponse<PagedList<ReturnedOrderItemDto>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var commercialUserId = GetCommercialUserId();
            var query = _dbConfig.ReturnedOrderItems
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId)
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
                    query = query.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcEx);
            }
            else if (startDate.HasValue)
            {
                if (TryGetOrderInsertUtcRange(startDate, startDate, out var fromUtc, out var toUtcEx))
                    query = query.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcEx);
            }

            if (tableId.HasValue)
            {
                query = query.Where(x => x.TableId == tableId.Value);
            }

            if (itemId.HasValue)
            {
                query = query.Where(x => x.ItemId == itemId.Value);
            }

            if (!string.IsNullOrWhiteSpace(info))
            {
                var term = info.Trim();
                query = query.Where(x =>
                    x.OrderCode.Contains(term) ||
                    x.ItemName.Contains(term) ||
                    (x.TableNumber != null && x.TableNumber.Contains(term)) ||
                    (x.MergedTableNumbers != null && x.MergedTableNumbers.Contains(term)));
            }

            var totalItems = query.Count();
            var list = query
                .OrderByDescending(x => x.InsertDate)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(MapReturnedOrderItemDto)
                .ToList();

            var paged = new PagedList<ReturnedOrderItemDto>(list, totalItems, pageNumber, pageSize);
            return Ok(new GlobalResponse<PagedList<ReturnedOrderItemDto>>
            {
                Data = paged,
                ErrorStatus = false,
                Message = "Success"
            });
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpGet("GetMergedTables")]
        public async Task<ActionResult<GlobalResponse<List<int>>>> GetMergedTables(int tableId)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                // Verify table belongs to this Commercial
                var table = await _dbConfig.Tables
                    .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                if (table == null)
                {
                    return NotFound(new GlobalResponse<List<int>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة غير موجودة"
                    });
                }

                // Get current order for this table
                if (!table.CurrentOrderId.HasValue)
                {
                    return Ok(new GlobalResponse<List<int>>
                    {
                        Data = new List<int> { tableId },
                        ErrorStatus = false,
                        Message = "لا يوجد طلب نشط على هذه الطاولة"
                    });
                }

                // Get all tables linked to this order via OrderTables
                var mergedTableIds = await _dbConfig.OrderTables
                    .Where(ot => ot.OrderId == table.CurrentOrderId.Value && !ot.IsDeleted)
                    .Select(ot => ot.TableId)
                    .Distinct()
                    .ToListAsync();

                // If no merged tables found, return only this table
                if (!mergedTableIds.Any())
                {
                    mergedTableIds = new List<int> { tableId };
                }

                return Ok(new GlobalResponse<List<int>>
                {
                    Data = mergedTableIds,
                    ErrorStatus = false,
                    Message = "تم جلب الطاولات المدمجة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting merged tables");
                return StatusCode(500, new GlobalResponse<List<int>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء جلب الطاولات المدمجة"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPut("CloseTableOrder")]
        public async Task<ActionResult<GlobalResponse<object>>> CloseTableOrder([FromQuery] int? tableId, [FromBody] List<int>? tableIds = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                // Determine which tables to close
                var tablesToClose = new List<int>();
                if (tableIds != null && tableIds.Any())
                {
                    // Multiple tables (merged tables) - sent in body
                    tablesToClose = tableIds.Distinct().ToList();
                }
                else if (tableId.HasValue)
                {
                    // Single table (backward compatibility) - sent as query parameter
                    tablesToClose = new List<int> { tableId.Value };
                }
                else
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يرجى تحديد طاولة واحدة على الأقل"
                    });
                }

                var closedTables = new List<object>();
                var orderId = (int?)null;

                // Process each table
                foreach (var tid in tablesToClose)
                {
                    var table = await _dbConfig.Tables
                        .FirstOrDefaultAsync(t => t.Id == tid && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                    if (table == null)
                    {
                        continue; // Skip invalid tables
                    }

                    // Get the order associated with this table
                    var orderTable = await _dbConfig.OrderTables
                        .Include(ot => ot.Order)
                        .FirstOrDefaultAsync(ot => ot.TableId == tid && !ot.IsDeleted);

                    if (orderTable != null && orderTable.Order != null)
                    {
                        orderId = orderTable.Order.Id;
                        
                        // Mark OrderTable as deleted
                        orderTable.IsDeleted = true;
                        _dbConfig.OrderTables.Update(orderTable);
                }

                // Update table status to Available
                table.Status = "Available";
                table.CurrentOrderId = null;
                _dbConfig.Tables.Update(table);

                    closedTables.Add(new { TableId = table.Id, TableNumber = table.TableNumber });
                }

                await _dbConfig.SaveChangesAsync();

                // If order exists and all tables are closed, mark order as completed
                if (orderId.HasValue)
                {
                    var remainingOrderTables = await _dbConfig.OrderTables
                        .CountAsync(ot => ot.OrderId == orderId.Value && !ot.IsDeleted);

                    if (remainingOrderTables == 0)
                    {
                        var order = await _dbConfig.CustomerOrders
                            .FirstOrDefaultAsync(o => o.Id == orderId.Value);
                        
                        if (order != null)
                        {
                            order.OrderStatus = "Completed";
                            order.PaymentStatus = "Paid";
                            _dbConfig.CustomerOrders.Update(order);
                            await _dbConfig.SaveChangesAsync();
                        }
                    }
                }

                // Send SignalR notifications for all closed tables
                foreach (var tid in tablesToClose)
                {
                    try
                    {
                        var table = await _dbConfig.Tables
                            .FirstOrDefaultAsync(t => t.Id == tid && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                        
                        if (table != null)
                {
                    await _hubContext.Clients.All.SendAsync("TableUpdated", new
                    {
                        TableId = table.Id,
                        Status = table.Status,
                        TableNumber = table.TableNumber,
                                Zone = table.Zone,
                                CurrentOrderId = (int?)null
                    });
                        }
                }
                catch (Exception ex)
                {
                        _logger.LogError(ex, "Error sending SignalR notification for TableUpdated: TableId={TableId}", tid);
                    }
                }

                var message = tablesToClose.Count > 1 
                    ? $"تم إغلاق حساب {tablesToClose.Count} طاولات بنجاح"
                    : "تم إغلاق حساب الطاولة بنجاح";

                return Ok(new GlobalResponse<object>
                {
                    Data = new { ClosedTables = closedTables, Count = closedTables.Count },
                    ErrorStatus = false,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing table order");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء إغلاق حساب الطاولة"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPut("CancelTableOrder")]
        public async Task<ActionResult<GlobalResponse<object>>> CancelTableOrder([FromQuery] int? tableId, [FromBody] List<int>? tableIds = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

                var tablesToCancel = new List<int>();
                if (tableIds != null && tableIds.Any())
                {
                    tablesToCancel = tableIds.Distinct().ToList();
                }
                else if (tableId.HasValue)
                {
                    tablesToCancel = new List<int> { tableId.Value };
                }
                else
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يرجى تحديد طاولة واحدة على الأقل"
                    });
                }

                int? orderId = null;
                CustomerOrder? orderToCancel = null;

                foreach (var tid in tablesToCancel)
                {
                    var table = await _dbConfig.Tables
                        .FirstOrDefaultAsync(t => t.Id == tid && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                    if (table == null)
                    {
                        continue;
                    }

                    if (table.CurrentOrderId.HasValue)
                    {
                        orderId = table.CurrentOrderId.Value;
                        break;
                    }

                    var orderTable = await _dbConfig.OrderTables
                        .Include(ot => ot.Order)
                        .FirstOrDefaultAsync(ot => ot.TableId == tid && !ot.IsDeleted);

                    if (orderTable?.Order != null && !orderTable.Order.IsDeleted)
                    {
                        orderId = orderTable.Order.Id;
                        break;
                    }
                }

                if (orderId.HasValue)
                {
                    orderToCancel = await _dbConfig.CustomerOrders
                        .FirstOrDefaultAsync(o => o.Id == orderId.Value && !o.IsDeleted);

                    if (orderToCancel == null)
                    {
                        return NotFound(new GlobalResponse<object>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "الفاتورة غير موجودة"
                        });
                    }

                    if (string.Equals(orderToCancel.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new GlobalResponse<object>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "cannotCancelPaidOrder"
                        });
                    }
                }

                var cancelledTables = new List<object>();
                var tableIdsToFree = new HashSet<int>(tablesToCancel);

                if (orderToCancel != null)
                {
                    var linkedOrderTables = await _dbConfig.OrderTables
                        .Where(ot => ot.OrderId == orderToCancel.Id && !ot.IsDeleted)
                        .ToListAsync();

                    foreach (var ot in linkedOrderTables)
                    {
                        ot.IsDeleted = true;
                        _dbConfig.OrderTables.Update(ot);
                        tableIdsToFree.Add(ot.TableId);
                    }

                    orderToCancel.OrderStatus = "Cancelled";
                    orderToCancel.IsDeleted = true;
                    _dbConfig.CustomerOrders.Update(orderToCancel);
                }

                foreach (var tid in tableIdsToFree)
                {
                    var table = await _dbConfig.Tables
                        .FirstOrDefaultAsync(t => t.Id == tid && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                    if (table == null)
                    {
                        continue;
                    }

                    table.Status = "Available";
                    table.CurrentOrderId = null;
                    _dbConfig.Tables.Update(table);
                    cancelledTables.Add(new { TableId = table.Id, TableNumber = table.TableNumber });
                }

                await _dbConfig.SaveChangesAsync();

                if (orderToCancel != null)
                {
                    await _dbConfig.LogAuditAsync(
                        "Cancel",
                        "Order",
                        orderToCancel.Id,
                        orderToCancel.OrderCode,
                        userId,
                        commercialUserId,
                        null,
                        null,
                        $"تم إلغاء طلب الصالة: {orderToCancel.OrderCode}"
                    );
                }

                foreach (var tid in tableIdsToFree)
                {
                    try
                    {
                        var table = await _dbConfig.Tables
                            .FirstOrDefaultAsync(t => t.Id == tid && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                        if (table != null)
                        {
                            await _hubContext.Clients.All.SendAsync("TableUpdated", new
                            {
                                TableId = table.Id,
                                Status = table.Status,
                                TableNumber = table.TableNumber,
                                Zone = table.Zone,
                                CurrentOrderId = (int?)null
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending SignalR notification for TableUpdated: TableId={TableId}", tid);
                    }
                }

                var message = tablesToCancel.Count > 1
                    ? $"تم إلغاء طلب {tablesToCancel.Count} طاولات بنجاح"
                    : "تم إلغاء الطلب بنجاح";

                return Ok(new GlobalResponse<object>
                {
                    Data = new { CancelledTables = cancelledTables, Count = cancelledTables.Count, OrderId = orderToCancel?.Id },
                    ErrorStatus = false,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling table order");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء إلغاء الطلب"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPost("TransferOrderItem")]
        public async Task<ActionResult<GlobalResponse<object>>> TransferOrderItem([FromBody] TransferOrderItemRequest request)
        {
            if (request == null || request.SourceTableId <= 0 || request.DestinationTableId <= 0 || request.SourceOrderItemId <= 0)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "بيانات النقل غير مكتملة" });
            }

            if (request.SourceTableId == request.DestinationTableId)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "يرجى اختيار طاولة مختلفة" });
            }

            var commercialUserId = GetCommercialUserId();
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var strategy = _dbConfig.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbConfig.Database.BeginTransactionAsync();
                try
                {
                    var sourceTable = await _dbConfig.Tables
                        .FirstOrDefaultAsync(t => t.Id == request.SourceTableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                    var destinationTable = await _dbConfig.Tables
                        .FirstOrDefaultAsync(t => t.Id == request.DestinationTableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                if (sourceTable == null || destinationTable == null)
                {
                    return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الطاولة المصدر أو الهدف غير موجودة" });
                }

                if (!sourceTable.CurrentOrderId.HasValue)
                {
                    return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "لا يوجد طلب نشط على الطاولة المصدر" });
                }

                var sourceOrder = await _dbConfig.CustomerOrders
                    .Include(o => o.CustomerOrderItem)
                    .FirstOrDefaultAsync(o => o.Id == sourceTable.CurrentOrderId.Value && !o.IsDeleted);
                if (sourceOrder == null)
                {
                    return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الفاتورة المصدر غير موجودة" });
                }

                var sourceItem = sourceOrder.CustomerOrderItem?
                    .FirstOrDefault(i => !i.IsDeleted && i.Id == request.SourceOrderItemId);
                if (sourceItem == null)
                {
                    return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "عنصر الفاتورة غير موجود" });
                }

                var transferQuantity = request.TransferQuantity ?? sourceItem.Quantity;
                if (transferQuantity <= 0)
                {
                    return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "كمية النقل غير صالحة" });
                }
                if (transferQuantity > sourceItem.Quantity)
                {
                    return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "كمية النقل أكبر من الكمية المتاحة" });
                }

                var destinationOrder = await ResolveOrCreateDestinationOrderAsync(destinationTable, sourceOrder, userId);
                var itemEntity = await _dbConfig.Items.FirstOrDefaultAsync(i => i.Id == sourceItem.ItemId && !i.IsDeleted);
                if (itemEntity == null)
                {
                    return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "المادة غير موجودة" });
                }

                var repricedUnit = ResolveSellingPrice(itemEntity);
                var destinationExisting = await _dbConfig.CustomerOrderItems
                    .FirstOrDefaultAsync(i => i.CustomerOrderId == destinationOrder.Id && i.ItemId == sourceItem.ItemId && i.SellingPrice == repricedUnit && !i.IsDeleted);

                if (destinationExisting != null)
                {
                    destinationExisting.Quantity += transferQuantity;
                    _dbConfig.CustomerOrderItems.Update(destinationExisting);
                }
                else
                {
                    _dbConfig.CustomerOrderItems.Add(new CustomerOrderItem
                    {
                        CustomerOrderId = destinationOrder.Id,
                        ItemId = sourceItem.ItemId,
                        Quantity = transferQuantity,
                        SellingPrice = repricedUnit,
                        PurchasingPrice = itemEntity.PurchasingPrice,
                        Notes = sourceItem.Notes,
                        InsertByUserId = userId
                    });
                }

                if (transferQuantity == sourceItem.Quantity)
                {
                    sourceItem.IsDeleted = true;
                }
                else
                {
                    sourceItem.Quantity -= transferQuantity;
                }
                _dbConfig.CustomerOrderItems.Update(sourceItem);
                await _dbConfig.SaveChangesAsync();

                var remainingItems = await _dbConfig.CustomerOrderItems
                    .CountAsync(i => i.CustomerOrderId == sourceOrder.Id && !i.IsDeleted);
                if (remainingItems == 0)
                {
                    sourceOrder.IsDeleted = true;
                    sourceTable.CurrentOrderId = null;
                    sourceTable.Status = "Available";
                    _dbConfig.CustomerOrders.Update(sourceOrder);
                    _dbConfig.Tables.Update(sourceTable);

                    var sourceOrderTables = await _dbConfig.OrderTables
                        .Where(ot => ot.OrderId == sourceOrder.Id && !ot.IsDeleted)
                        .ToListAsync();
                    foreach (var orderTable in sourceOrderTables)
                    {
                        orderTable.IsDeleted = true;
                        _dbConfig.OrderTables.Update(orderTable);
                    }
                }
                else
                {
                    sourceTable.Status = "Occupied";
                    _dbConfig.Tables.Update(sourceTable);
                }

                destinationTable.Status = "Occupied";
                destinationTable.CurrentOrderId = destinationOrder.Id;
                _dbConfig.Tables.Update(destinationTable);

                await _dbConfig.SaveChangesAsync();
                await transaction.CommitAsync();

                var responsePayload = new GlobalResponse<object>
                {
                    Data = new
                    {
                        sourceOrderId = sourceOrder.Id,
                        destinationOrderId = destinationOrder.Id,
                        movedItemIds = new[] { sourceItem.ItemId },
                        movedQuantity = transferQuantity,
                        repricedCount = 1,
                        mergedCount = destinationExisting != null ? 1 : 0
                    },
                    ErrorStatus = false,
                    Message = "تم نقل العنصر بنجاح"
                };

                try
                {
                    await EmitTableUpdatedAsync(sourceTable);
                    await EmitTableUpdatedAsync(destinationTable);

                    await _hubContext.Clients.All.SendAsync("OrderTransferred", new
                    {
                        Mode = "item",
                        SourceTableId = sourceTable.Id,
                        DestinationTableId = destinationTable.Id,
                        SourceOrderId = sourceOrder.Id,
                        DestinationOrderId = destinationOrder.Id
                    });

                    await _dbConfig.LogAuditAsync(
                        action: "TransferItem",
                        entityType: "CustomerOrder",
                        entityId: sourceOrder.Id,
                        entityName: sourceOrder.OrderCode,
                        userId: userId,
                        commercialUserId: commercialUserId,
                        description: $"Item transfer from table {sourceTable.TableNumber} to {destinationTable.TableNumber}",
                        newValues: new { request.SourceOrderItemId, transferQuantity, repricedUnit, sourceTableId = sourceTable.Id, destinationTableId = destinationTable.Id });
                }
                catch (Exception sideEffectEx)
                {
                    _logger.LogError(sideEffectEx, "TransferOrderItem side effects failed after commit");
                }

                    return Ok(responsePayload);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in TransferOrderItem");
                    return StatusCode(500, new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "حدث خطأ أثناء نقل العنصر" });
                }
            });
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPost("TransferFullOrder")]
        public async Task<ActionResult<GlobalResponse<object>>> TransferFullOrder([FromBody] TransferFullOrderRequest request)
        {
            if (request == null || request.SourceTableId <= 0 || request.DestinationTableId <= 0)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "بيانات النقل غير مكتملة" });
            }

            if (request.SourceTableId == request.DestinationTableId)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "يرجى اختيار طاولة مختلفة" });
            }

            var commercialUserId = GetCommercialUserId();
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var strategy = _dbConfig.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbConfig.Database.BeginTransactionAsync();
                try
                {
                    var sourceTable = await _dbConfig.Tables.FirstOrDefaultAsync(t => t.Id == request.SourceTableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                    var destinationTable = await _dbConfig.Tables.FirstOrDefaultAsync(t => t.Id == request.DestinationTableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                if (sourceTable == null || destinationTable == null)
                {
                    return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الطاولة المصدر أو الهدف غير موجودة" });
                }
                if (!sourceTable.CurrentOrderId.HasValue)
                {
                    return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "لا يوجد طلب نشط على الطاولة المصدر" });
                }

                var sourceOrder = await _dbConfig.CustomerOrders
                    .Include(o => o.CustomerOrderItem)
                    .FirstOrDefaultAsync(o => o.Id == sourceTable.CurrentOrderId.Value && !o.IsDeleted);
                if (sourceOrder == null)
                {
                    return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الفاتورة المصدر غير موجودة" });
                }

                var destinationOrder = await ResolveOrCreateDestinationOrderAsync(destinationTable, sourceOrder, userId);
                var sourceActiveItems = sourceOrder.CustomerOrderItem?.Where(i => !i.IsDeleted).ToList() ?? new List<CustomerOrderItem>();

                var repricedCount = 0;
                var mergedCount = 0;
                var movedItemIds = new List<int>();
                foreach (var sourceItem in sourceActiveItems)
                {
                    var itemEntity = await _dbConfig.Items.FirstOrDefaultAsync(i => i.Id == sourceItem.ItemId && !i.IsDeleted);
                    if (itemEntity == null) continue;

                    movedItemIds.Add(sourceItem.ItemId);
                    var repricedUnit = ResolveSellingPrice(itemEntity);
                    repricedCount++;

                    var destinationExisting = await _dbConfig.CustomerOrderItems
                        .FirstOrDefaultAsync(i => i.CustomerOrderId == destinationOrder.Id && i.ItemId == sourceItem.ItemId && i.SellingPrice == repricedUnit && !i.IsDeleted);

                    if (destinationExisting != null)
                    {
                        destinationExisting.Quantity += sourceItem.Quantity;
                        _dbConfig.CustomerOrderItems.Update(destinationExisting);
                        mergedCount++;
                    }
                    else
                    {
                        _dbConfig.CustomerOrderItems.Add(new CustomerOrderItem
                        {
                            CustomerOrderId = destinationOrder.Id,
                            ItemId = sourceItem.ItemId,
                            Quantity = sourceItem.Quantity,
                            SellingPrice = repricedUnit,
                            PurchasingPrice = itemEntity.PurchasingPrice,
                            InsertByUserId = userId
                        });
                    }
                    sourceItem.IsDeleted = true;
                    _dbConfig.CustomerOrderItems.Update(sourceItem);
                }

                sourceOrder.IsDeleted = true;
                _dbConfig.CustomerOrders.Update(sourceOrder);

                sourceTable.CurrentOrderId = null;
                sourceTable.Status = "Available";
                destinationTable.CurrentOrderId = destinationOrder.Id;
                destinationTable.Status = "Occupied";
                _dbConfig.Tables.Update(sourceTable);
                _dbConfig.Tables.Update(destinationTable);

                var sourceOrderTables = await _dbConfig.OrderTables
                    .Where(ot => ot.OrderId == sourceOrder.Id && !ot.IsDeleted)
                    .ToListAsync();
                foreach (var ot in sourceOrderTables)
                {
                    ot.IsDeleted = true;
                    _dbConfig.OrderTables.Update(ot);
                }

                var destinationOrderTable = await _dbConfig.OrderTables
                    .FirstOrDefaultAsync(ot => ot.OrderId == destinationOrder.Id && ot.TableId == destinationTable.Id && !ot.IsDeleted);
                if (destinationOrderTable == null)
                {
                    _dbConfig.OrderTables.Add(new OrderTable
                    {
                        OrderId = destinationOrder.Id,
                        TableId = destinationTable.Id,
                        IsPrimary = true,
                        InsertByUserId = userId
                    });
                }

                await _dbConfig.SaveChangesAsync();
                await transaction.CommitAsync();

                var responsePayload = new GlobalResponse<object>
                {
                    Data = new
                    {
                        sourceOrderId = sourceOrder.Id,
                        destinationOrderId = destinationOrder.Id,
                        movedItemIds,
                        repricedCount,
                        mergedCount
                    },
                    ErrorStatus = false,
                    Message = "تم نقل الطلب بالكامل بنجاح"
                };

                try
                {
                    await EmitTableUpdatedAsync(sourceTable);
                    await EmitTableUpdatedAsync(destinationTable);
                    await _hubContext.Clients.All.SendAsync("OrderTransferred", new
                    {
                        Mode = "full",
                        SourceTableId = sourceTable.Id,
                        DestinationTableId = destinationTable.Id,
                        SourceOrderId = sourceOrder.Id,
                        DestinationOrderId = destinationOrder.Id
                    });

                    await _dbConfig.LogAuditAsync(
                        action: "TransferFullOrder",
                        entityType: "CustomerOrder",
                        entityId: sourceOrder.Id,
                        entityName: sourceOrder.OrderCode,
                        userId: userId,
                        commercialUserId: commercialUserId,
                        description: $"Full order transfer from table {sourceTable.TableNumber} to {destinationTable.TableNumber}",
                        newValues: new { sourceTableId = sourceTable.Id, destinationTableId = destinationTable.Id, repricedCount, mergedCount });
                }
                catch (Exception sideEffectEx)
                {
                    _logger.LogError(sideEffectEx, "TransferFullOrder side effects failed after commit");
                }

                    return Ok(responsePayload);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in TransferFullOrder");
                    return StatusCode(500, new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "حدث خطأ أثناء نقل الطلب بالكامل" });
                }
            });
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPost("MergeTableOrders")]
        public async Task<ActionResult<GlobalResponse<object>>> MergeTableOrders([FromBody] MergeTableOrdersRequest request)
        {
            if (request == null || request.SourceTableId <= 0 || request.DestinationTableId <= 0)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "بيانات الدمج غير مكتملة" });
            }

            if (request.SourceTableId == request.DestinationTableId)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "يرجى اختيار طاولتين مختلفتين" });
            }

            var commercialUserId = GetCommercialUserId();
            var destinationTable = await _dbConfig.Tables
                .FirstOrDefaultAsync(t => t.Id == request.DestinationTableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);
            if (destinationTable == null || !destinationTable.CurrentOrderId.HasValue)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الدمج يتطلب وجود فاتورة نشطة على الطاولة الهدف" });
            }

            var transferRequest = new TransferFullOrderRequest
            {
                SourceTableId = request?.SourceTableId ?? 0,
                DestinationTableId = request?.DestinationTableId ?? 0
            };
            var result = await TransferFullOrder(transferRequest);
            if (result.Result is ObjectResult obj && obj.Value is GlobalResponse<object> payload)
            {
                if (payload.ErrorStatus != true)
                {
                    payload.Message = "تم دمج الفاتورتين بنجاح";
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                    await _dbConfig.LogAuditAsync(
                        action: "MergeTableOrders",
                        entityType: "CustomerOrder",
                        entityId: destinationTable.CurrentOrderId.Value,
                        entityName: destinationTable.TableNumber,
                        userId: userId,
                        commercialUserId: commercialUserId,
                        description: $"Merged source table {request.SourceTableId} into destination table {request.DestinationTableId}");
                }
            }
            return result;
        }

        [AuthorizeSection("reports", "orderQueue", Roles = "Commercial")]
        [HttpGet("GetOrders")]
        public ActionResult<GlobalResponse<OrdersPagedResult>> GetOrders(int pageNumber, int pageSize, string? info, DateTime? startDate, DateTime? endDate, string? orderType, string? paymentMethod, int? deliveryDriverId)
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
                    .Include(x => x.DeliveryDriver)
                    .Include(x => x.CreditEmployee)
                    .Include(x => x.CreditCustomer)
                    .Include(x => x.OrderTables)
                    .ThenInclude(ot => ot.Table)
                    .AsQueryable();

           
            

            // Filter by OrderCode
            if (!string.IsNullOrEmpty(info))
            {
                items = items.Where(x => x.OrderCode == info);
            }

            if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
            {
                items = items.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcEx);
            }

            // Filter by OrderType
            if (!string.IsNullOrEmpty(orderType))
            {
                items = items.Where(x => x.OrderType == orderType);
            }

            // Filter by PaymentMethod
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                items = items.Where(x => x.PaymentMethod == paymentMethod);
            }

            // Filter by DeliveryDriverId
            if (deliveryDriverId.HasValue)
            {
                items = items.Where(x => x.DeliveryDriverId == deliveryDriverId.Value);
            }

            var totalItems = items.Count();

            var totalSales = SumOrdersSalesAmount(items);
            var totalSubTotal = items.Sum(o => o.OrderSubTotal ?? 0m);
            var totalDiscount = items.Sum(o => o.DiscountAmount ?? 0m);
            var totalItemsSold = _dbConfig.CustomerOrderItems
                .Where(i => !i.IsDeleted && items.Select(o => o.Id).Contains(i.CustomerOrderId))
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

            // Map to OrderDto after filtering
            var ordersList = items
                .OrderByDescending(x => x.InsertDate)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(x => {
                    // Get tables for this order
                    var orderTables = x.OrderTables?
                        .Where(ot => !ot.IsDeleted && ot.Table != null)
                        .Select(ot => ot.Table!)
                        .ToList() ?? new List<Table>();
                    
                    // Convert to TableDto
                    var tableDtos = orderTables.Select(t => new TableDto
                    {
                        Id = t.Id,
                        TableNumber = t.TableNumber,
                        Capacity = t.Capacity,
                        Status = t.Status,
                        Zone = t.Zone,
                        Notes = t.Notes
                    }).ToList();
                    
                    // Build merged table numbers string (e.g., "1و3و5")
                    var mergedTableNumbers = orderTables.Count > 1
                        ? string.Join("و", orderTables.OrderBy(t => t.TableNumber).Select(t => t.TableNumber))
                        : (orderTables.Count == 1 ? orderTables[0].TableNumber : null);

                    var activeOrderItems = GetActiveOrderItems(x.CustomerOrderItem);
                    
                    return new OrderDto
                {
                    CustomerOrderItem = activeOrderItems,
                    OrderPrice = activeOrderItems.Sum(item => item.SellingPrice * item.Quantity),
                    OrderCode = x.OrderCode,
                    Id = x.Id,
                    ItemsCount = activeOrderItems.Count,
                    DailySequenceNumber = x.DailySequenceNumber,
                    InsertDate = x.InsertDate,
                    CreatedAt = x.InsertDate,
                    CreatedByUserId = x.User != null ? x.User.Id : null,
                    CreatedByUsername = x.User != null ? x.User.Username : null,
                    PaymentMethod = x.PaymentMethod,
                    CreditEmployeeName = x.CreditEmployee != null ? x.CreditEmployee.Name : null,
                    CreditCustomerName = x.CreditCustomer != null ? x.CreditCustomer.Name : null,
                    OrderType = x.OrderType,
                    OrderStatus = x.OrderStatus,
                    Notes = x.Notes,
                    Total = activeOrderItems.Sum(item => item.SellingPrice * item.Quantity),
                    DiscountType = x.DiscountType,
                    DiscountValue = x.DiscountValue,
                    DiscountAmount = x.DiscountAmount,
                    DiscountPercent = x.DiscountPercent,
                    OrderSubTotal = x.OrderSubTotal,
                    OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                        // Tables information
                        Tables = tableDtos.Any() ? tableDtos : null,
                        MergedTableNumbers = mergedTableNumbers,
                    // Delivery fields
                    DeliveryDriverId = x.DeliveryDriverId,
                    DeliveryDriver = x.DeliveryDriver,
                    DeliveryStatus = x.DeliveryStatus,
                    DeliveryAddress = x.DeliveryAddress,
                    DeliveryPhoneNumber = x.DeliveryPhoneNumber,
                    DeliveryCustomerName = x.DeliveryCustomerName,
                    DeliveryFee = x.DeliveryFee
                    };
                })
                .ToList();

            var pagedResult = new OrdersPagedResult(ordersList, totalItems, pageNumber, pageSize, summary);

            var response = new GlobalResponse<OrdersPagedResult>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;

     
        }

        [AuthorizeSection("reports", "orderQueue", Roles = "Commercial,Admin")]
        [HttpGet("ExportOrders")]
        public ActionResult ExportOrders(string? info, DateTime? startDate, DateTime? endDate, string? orderType, string? paymentMethod, int? deliveryDriverId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
                return BadRequest();

            var userInsertByUserId = user.InsertByUserId;
            var items = _dbConfig.CustomerOrders
                .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId))
                .Include(x => x.CustomerOrderItem)
                .Include(x => x.OrderTables)
                .ThenInclude(ot => ot.Table)
                .AsQueryable();

            if (!string.IsNullOrEmpty(info))
                items = items.Where(x => x.OrderCode == info);
            if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
            {
                items = items.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcEx);
            }
            if (!string.IsNullOrEmpty(orderType))
                items = items.Where(x => x.OrderType == orderType);
            if (!string.IsNullOrEmpty(paymentMethod))
                items = items.Where(x => x.PaymentMethod == paymentMethod);
            if (deliveryDriverId.HasValue)
                items = items.Where(x => x.DeliveryDriverId == deliveryDriverId.Value);

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
                        OrderType = x.OrderType ?? "",
                        PaymentMethod = x.PaymentMethod ?? "",
                        OrderPrice = activeOrderItems.Sum(item => item.SellingPrice * item.Quantity),
                        DiscountAmount = x.DiscountAmount ?? 0,
                        OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                        ItemsCount = activeOrderItems.Count
                    };
                })
                .ToList();

            var csv = new StringBuilder();
            var header = "OrderCode,InsertDate,OrderType,PaymentMethod,OrderPrice,DiscountAmount,FinalTotal,ItemsCount";
            csv.AppendLine(header);
            foreach (var o in ordersList)
            {
                var dateStr = o.InsertDate.ToString("yyyy-MM-dd HH:mm");
                var finalTotal = o.OrderTotalAfterDiscount ?? o.OrderPrice;
                var line = $"\"{EscapeCsv(o.OrderCode)}\",\"{dateStr}\",\"{EscapeCsv(o.OrderType)}\",\"{EscapeCsv(o.PaymentMethod)}\",{o.OrderPrice},{o.DiscountAmount},{finalTotal},{o.ItemsCount}";
                csv.AppendLine(line);
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

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
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

        [AuthorizeSection("reports", Roles = "Commercial")]
        [HttpGet("GetSellsCount")]
        public ActionResult<GlobalResponse<object>> GetSellsCount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            var today = DateTime.Today;

            var customerOrdersQuery = _dbConfig.CustomerOrders
                .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId ||  x.User.InsertByUserId == userId));

            var orderItemsQuery = QueryActiveOrderItemsForCommercial(userId, user!.InsertByUserId);


          

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
                ErrorStatus = true,
                Message = "Success"
            };

            return response;
        }

        [AuthorizeSection("reports", Roles = "Commercial")]
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

                var orderItemsQuery = QueryActiveOrderItemsForCommercial(userId, user.InsertByUserId);

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
                ErrorStatus = true,
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

                // Orders Statistics
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

                // Sales Amount — one total per order (avoids counting replaced line items)
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
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        // Advanced Reports Endpoints

        [AuthorizeSection("reports", Roles = "Commercial,Admin")]
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

                if (startDate.HasValue)
                {
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    endDate = endDate.Value.AddDays(1);
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date < endDate.Value.Date);
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
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("reports", Roles = "Commercial,Admin")]
        [HttpGet("GetTopSellingItems")]
        public ActionResult<GlobalResponse<object>> GetTopSellingItems(
            int topCount = 10,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? orderType = null,
            string? paymentMethod = null)
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

                if (!string.IsNullOrEmpty(orderType))
                {
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.OrderType == orderType);
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.PaymentMethod == paymentMethod);
                }

                var summary = new TopSellingItemsSummaryDto
                {
                    TotalQuantitySold = orderItemsQuery.Sum(x => (int?)x.Quantity) ?? 0,
                    TotalSales = orderItemsQuery.Sum(x => (decimal?)(x.SellingPrice * x.Quantity)) ?? 0m,
                    TotalDistinctItems = orderItemsQuery.Select(x => x.ItemId).Distinct().Count(),
                    TotalOrders = orderItemsQuery.Select(x => x.CustomerOrderId).Distinct().Count()
                };

                if (topCount < 1)
                    topCount = 10;
                if (topCount > 500)
                    topCount = 500;

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
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("reports", Roles = "Commercial,Admin")]
        [HttpGet("GetSalesByCategory")]
        public ActionResult<GlobalResponse<object>> GetSalesByCategory(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
                var commercialUserId = GetCommercialUserId();

                IQueryable<CustomerOrderItem> orderItemsQuery = QueryActiveOrderItemsForCommercial(userId, user!.InsertByUserId)
                    .Include(x => x.Item)
                    .Include(x => x.CustomerOrder);

                if (startDate.HasValue)
                {
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date >= startDate.Value.Date);
                }

                DateTime? endDateForExpenses = endDate;
                if (endDate.HasValue)
                {
                    endDateForExpenses = endDate.Value.AddDays(1);
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date < endDateForExpenses.Value.Date);
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
                    .ToList();

                // التاجات ضمن نطاق المستخدم (نفس منطق GetTags)
                var userInsertByUserId = user.InsertByUserId;
                var tagsInScope = _dbConfig.Tags
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId))
                    .ToList();
                var tagNameToId = tagsInScope
                    .Where(t => !string.IsNullOrEmpty(t.Name))
                    .GroupBy(t => t.Name!)
                    .ToDictionary(g => g.Key, g => g.First().Id);

                // صرفيات الفئة حسب TagId فقط (نفس الفترة)
                var expensesQuery = _dbConfig.Expenses
                    .Where(e => !e.IsDeleted && e.InsertByUserId == commercialUserId && e.TagId != null);

                if (startDate.HasValue)
                    expensesQuery = expensesQuery.Where(e => e.Date.Date >= startDate.Value.Date);
                if (endDateForExpenses.HasValue)
                    expensesQuery = expensesQuery.Where(e => e.Date.Date < endDateForExpenses.Value.Date);

                var expensesByTagIdList = expensesQuery
                    .GroupBy(e => e.TagId!.Value)
                    .Select(g => new { TagId = g.Key, TotalAmount = g.Sum(e => e.Amount) })
                    .ToList();
                var expensesByTagIdDict = expensesByTagIdList.ToDictionary(x => x.TagId, x => x.TotalAmount);

                // أسماء الفئات: مبيعات + تاجات ظهرت في الصرفيات عبر TagId
                var tagIdsInExpenses = expensesByTagIdList.Select(x => x.TagId).Distinct().ToList();
                var tagNamesFromExpenses = tagsInScope
                    .Where(t => tagIdsInExpenses.Contains(t.Id) && !string.IsNullOrEmpty(t.Name))
                    .Select(t => t.Name!);
                var salesCategoryNames = salesByCategory.Select(s => s.category).Where(c => !string.IsNullOrEmpty(c));
                var allCategoryNames = salesCategoryNames.Union(tagNamesFromExpenses).Distinct().ToList();

                var merged = allCategoryNames.Select(cat => new
                {
                    category = cat,
                    totalSales = salesByCategory.FirstOrDefault(s => s.category == cat)?.totalSales ?? 0,
                    totalQuantity = salesByCategory.FirstOrDefault(s => s.category == cat)?.totalQuantity ?? 0,
                    itemCount = salesByCategory.FirstOrDefault(s => s.category == cat)?.itemCount ?? 0,
                    orderCount = salesByCategory.FirstOrDefault(s => s.category == cat)?.orderCount ?? 0,
                    totalExpenses = tagNameToId.TryGetValue(cat, out var tagId) && expensesByTagIdDict.TryGetValue(tagId, out var amt) ? amt : 0
                })
                .OrderByDescending(x => x.totalSales)
                .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = merged,
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
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("reports", Roles = "Commercial,Admin")]
        [HttpGet("GetSalesReportStaff")]
        public ActionResult<GlobalResponse<object>> GetSalesReportStaff()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var staff = _dbConfig.Users
                    .AsNoTracking()
                    .Where(u => u.InsertByUserId == commercialUserId &&
                                (u.Role == "POS" || u.Role == "Waiter"))
                    .OrderBy(u => u.Name)
                    .ThenBy(u => u.Username)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        username = u.Username,
                        role = u.Role
                    })
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = staff,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales report staff list");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        [AuthorizeSection("reports", Roles = "Commercial,Admin")]
        [HttpGet("GetSalesByEmployee")]
        public ActionResult<GlobalResponse<object>> GetSalesByEmployee(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? roleFilter = null,
            int? createdByUserId = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                var ordersQuery = _dbConfig.CustomerOrders
                    .Include(x => x.User)
                    .Include(x => x.CustomerOrderItem)
                    .Where(x => x.IsDeleted == false &&
                                (x.InsertByUserId == userId || x.User.InsertByUserId == userId));

                if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcEx))
                {
                    ordersQuery = ordersQuery.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcEx);
                }

                if (createdByUserId is int staffId && staffId > 0)
                {
                    var commercialUserId = GetCommercialUserId();
                    var staffOk = _dbConfig.Users.AsNoTracking().Any(u =>
                        u.Id == staffId &&
                        u.InsertByUserId == commercialUserId &&
                        (u.Role == "POS" || u.Role == "Waiter"));
                    if (!staffOk)
                    {
                        return BadRequest(new GlobalResponse<object>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "Invalid or unauthorized employee."
                        });
                    }

                    ordersQuery = ordersQuery.Where(x => x.InsertByUserId == staffId);
                }

                var roles = ParseSalesStaffRoleFilter(roleFilter);
                if (roles != null && roles.Count > 0)
                {
                    ordersQuery = ordersQuery.Where(x => x.User != null && roles.Contains(x.User.Role));
                }

                var salesByEmployee = ordersQuery
                    .Where(x => x.User != null)
                    .GroupBy(x => new { x.InsertByUserId, x.User!.Username })
                    .Select(g => new
                    {
                        employeeId = g.Key.InsertByUserId,
                        employeeName = g.Key.Username,
                        totalOrders = g.Count(),
                        totalSales = g.SelectMany(o => o.CustomerOrderItem.Where(i => !i.IsDeleted)).Sum(x => x.SellingPrice * x.Quantity),
                        totalItemsSold = g.SelectMany(o => o.CustomerOrderItem.Where(i => !i.IsDeleted)).Sum(x => x.Quantity)
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
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        /// <summary>فلتر أدوار موظفي البيع: فراغ = الكل؛ POS؛ Waiter؛ SalesStaff = POS + Waiter.</summary>
        private static List<string>? ParseSalesStaffRoleFilter(string? roleFilter)
        {
            var raw = (roleFilter ?? "").Trim();
            if (string.IsNullOrEmpty(raw))
                return null;

            if (raw.Equals("SalesStaff", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("POS,Waiter", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("POS_WAITER", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { "POS", "Waiter" };
            }

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Equals("POS", StringComparison.OrdinalIgnoreCase))
                    set.Add("POS");
                else if (part.Equals("Waiter", StringComparison.OrdinalIgnoreCase))
                    set.Add("Waiter");
            }

            return set.Count > 0 ? set.ToList() : null;
        }

        [AuthorizeSection("endOfDayReport", Roles = "Commercial")]
        [HttpGet("GetEndOfDaySummary")]
        public async Task<ActionResult<GlobalResponse<EndOfDayReportDto>>> GetEndOfDaySummary()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var reportResult = await BuildEndOfDayReportAsync(commercialUserId);
                if (reportResult.IsBlocked)
                {
                    return BadRequest(new GlobalResponse<EndOfDayReportDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = reportResult.BlockMessage
                    });
                }

                return Ok(new GlobalResponse<EndOfDayReportDto>
                {
                    Data = reportResult.Data,
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
                var reportResult = await BuildEndOfDayReportAsync(commercialUserId);
                if (reportResult.IsBlocked || reportResult.Data == null)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = reportResult.BlockMessage ?? "لا يمكن استخراج التقرير"
                    });
                }

                var r = reportResult.Data;
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
                summarySheet.Cell(12, 1).Value = "ReturnedAmount";
                summarySheet.Cell(12, 2).Value = r.Totals.ReturnedAmount;
                summarySheet.Cell(13, 1).Value = "ReturnedCount";
                summarySheet.Cell(13, 2).Value = r.Totals.ReturnedCount;
                summarySheet.Cell(14, 1).Value = "TotalTables";
                summarySheet.Cell(14, 2).Value = r.TableStatus.TotalTables;
                summarySheet.Cell(15, 1).Value = "AvailableTables";
                summarySheet.Cell(15, 2).Value = r.TableStatus.AvailableTables;
                summarySheet.Cell(16, 1).Value = "OccupiedTables";
                summarySheet.Cell(16, 2).Value = r.TableStatus.OccupiedTables;
                summarySheet.Cell(17, 1).Value = "ReservedTables";
                summarySheet.Cell(17, 2).Value = r.TableStatus.ReservedTables;
                summarySheet.Cell(18, 1).Value = "OutOfServiceTables";
                summarySheet.Cell(18, 2).Value = r.TableStatus.OutOfServiceTables;

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

                var orderTypesSheet = workbook.Worksheets.Add("OrdersByType");
                orderTypesSheet.Cell(1, 1).Value = "OrderType";
                orderTypesSheet.Cell(1, 2).Value = "OrdersCount";
                orderTypesSheet.Cell(1, 3).Value = "TotalAmount";
                var orderTypeRow = 2;
                foreach (var ot in r.OrdersByType)
                {
                    orderTypesSheet.Cell(orderTypeRow, 1).Value = ot.OrderType ?? string.Empty;
                    orderTypesSheet.Cell(orderTypeRow, 2).Value = ot.OrdersCount;
                    orderTypesSheet.Cell(orderTypeRow, 3).Value = ot.TotalAmount;
                    orderTypeRow++;
                }

                var invoicesSheet = workbook.Worksheets.Add("InvoicesByTable");
                invoicesSheet.Cell(1, 1).Value = "TableNumber";
                invoicesSheet.Cell(1, 2).Value = "InvoicesCount";
                invoicesSheet.Cell(1, 3).Value = "TotalAmount";
                var invoiceRow = 2;
                foreach (var t in r.InvoicesByTable)
                {
                    invoicesSheet.Cell(invoiceRow, 1).Value = t.TableNumber ?? string.Empty;
                    invoicesSheet.Cell(invoiceRow, 2).Value = t.InvoicesCount;
                    invoicesSheet.Cell(invoiceRow, 3).Value = t.TotalAmount;
                    invoiceRow++;
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

                var returnedItemsSheet = workbook.Worksheets.Add("ReturnedItems");
                returnedItemsSheet.Cell(1, 1).Value = "OrderCode";
                returnedItemsSheet.Cell(1, 2).Value = "ItemName";
                returnedItemsSheet.Cell(1, 3).Value = "Table";
                returnedItemsSheet.Cell(1, 4).Value = "Quantity";
                returnedItemsSheet.Cell(1, 5).Value = "UnitPrice";
                returnedItemsSheet.Cell(1, 6).Value = "LineTotal";
                returnedItemsSheet.Cell(1, 7).Value = "DeletedBy";
                returnedItemsSheet.Cell(1, 8).Value = "DeletedAt";
                var returnedRow = 2;
                foreach (var item in r.ReturnedItems)
                {
                    returnedItemsSheet.Cell(returnedRow, 1).Value = item.OrderCode ?? string.Empty;
                    returnedItemsSheet.Cell(returnedRow, 2).Value = item.ItemName ?? string.Empty;
                    returnedItemsSheet.Cell(returnedRow, 3).Value = item.MergedTableNumbers ?? item.TableNumber ?? "-";
                    returnedItemsSheet.Cell(returnedRow, 4).Value = item.Quantity;
                    returnedItemsSheet.Cell(returnedRow, 5).Value = item.UnitPrice;
                    returnedItemsSheet.Cell(returnedRow, 6).Value = item.LineTotal;
                    returnedItemsSheet.Cell(returnedRow, 7).Value = item.DeletedByUsername ?? string.Empty;
                    returnedItemsSheet.Cell(returnedRow, 8).Value = item.InsertDate;
                    returnedRow++;
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

        // get Item Price 
        [Authorize(Roles = "Commercial,POS,Reader")]
        [HttpDelete("ItemPrice")]
        public async Task<ActionResult<GlobalResponse<int>>> ItemPrice(string code)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.Where(x => x.InsertByUserId == userId).FirstOrDefault();

            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Code == code && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
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


        // POST: api/Admin/UploadItemImage/{itemId}
        [AuthorizeSection("items", Roles = "Commercial,POS")]
        [HttpPost("UploadItemImage/{itemId}")]
        public async Task<ActionResult<GlobalResponse<object>>> UploadItemImage(int itemId, [FromForm] IFormFile image)
        {
            try
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

                var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == itemId && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                if (item == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Item not found"
                    });
                }

                if (image == null || image.Length == 0)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Image file is required"
                    });
                }

                var imageFileName = await UploadIamgesAsync(image);
                item.Image = imageFileName;
                _dbConfig.Items.Update(item);
                await _dbConfig.SaveChangesAsync();

                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";
                var fullImageUrl = imageBaseUrl + imageFileName;

                return Ok(new GlobalResponse<object>
                {
                    Data = new { Image = fullImageUrl, ImageFileName = imageFileName },
                    ErrorStatus = false,
                    Message = "تم رفع الصورة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading item image for item {ItemId}", itemId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء رفع الصورة: {ex.Message}"
                });
            }
        }

        // POST: api/Admin/UploadMultipleItemImages
        [AuthorizeSection("items", Roles = "Commercial,POS")]
        [HttpPost("UploadMultipleItemImages")]
        public async Task<ActionResult<GlobalResponse<object>>> UploadMultipleItemImages([FromForm] List<IFormFile> images, [FromForm] List<int> itemIds)
        {
            try
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

                if (images == null || !images.Any())
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Images are required"
                    });
                }

                if (itemIds == null || !itemIds.Any())
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Item IDs are required"
                    });
                }

                if (images.Count != itemIds.Count)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Number of images must match number of item IDs"
                    });
                }

                var results = new List<object>();
                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

                for (int i = 0; i < images.Count; i++)
                {
                    var image = images[i];
                    var itemId = itemIds[i];

                    try
                    {
                        var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == itemId && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
                        
                        if (item == null)
                        {
                            results.Add(new { ItemId = itemId, Success = false, Message = "Item not found" });
                            continue;
                        }

                        if (image == null || image.Length == 0)
                        {
                            results.Add(new { ItemId = itemId, Success = false, Message = "Image file is empty" });
                            continue;
                        }

                        var imageFileName = await UploadIamgesAsync(image);
                        item.Image = imageFileName;
                        _dbConfig.Items.Update(item);
                        
                        results.Add(new 
                        { 
                            ItemId = itemId, 
                            ItemName = item.Name,
                            Success = true, 
                            Image = imageBaseUrl + imageFileName,
                            ImageFileName = imageFileName,
                            Message = "تم رفع الصورة بنجاح" 
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading image for item {ItemId}", itemId);
                        results.Add(new { ItemId = itemId, Success = false, Message = $"حدث خطأ: {ex.Message}" });
                    }
                }

                await _dbConfig.SaveChangesAsync();

                var successCount = results.Count(r => ((dynamic)r).Success == true);
                var failCount = results.Count - successCount;

                return Ok(new GlobalResponse<object>
            {
                    Data = new 
                    { 
                        Results = results,
                        SuccessCount = successCount,
                        FailCount = failCount,
                        TotalCount = results.Count
                    },
                ErrorStatus = false,
                    Message = $"تم رفع {successCount} صورة بنجاح من أصل {results.Count}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple item images");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء رفع الصور: {ex.Message}"
            });
        }
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("GenerateItemImageWithAI")]
        public async Task<ActionResult<GlobalResponse<string>>> GenerateItemImageWithAI(GenerateItemImageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ItemName))
                {
                    return BadRequest(new GlobalResponse<string>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم الطبق مطلوب"
                    });
                }

                var apiKey = _configuration["OpenAISettings:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new GlobalResponse<string>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "OpenAI API Key غير موجود في الإعدادات"
                    });
                }

                // بناء prompt للصورة
                var prompt = $"A high-quality, appetizing food photography image of {request.ItemName}";
                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    prompt += $" from the {request.Category} category";
                }
                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    prompt += $". {request.Description}";
                }
                prompt += ". Professional food photography, well-lit, appetizing, restaurant quality, on a clean plate or dish, high resolution, realistic style.";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "dall-e-3",
                    prompt = prompt,
                    n = 1,
                    size = "1024x1024",
                    quality = "standard"
                };

                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/images/generations", requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI DALL-E API Error: {Error}", errorContent);
                    return StatusCode(500, new GlobalResponse<string>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "حدث خطأ أثناء الاتصال بـ OpenAI API"
                    });
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var imageUrl = jsonResponse.GetProperty("data")[0].GetProperty("url").GetString();

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return BadRequest(new GlobalResponse<string>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم الحصول على صورة من OpenAI"
                    });
                }

                return Ok(new GlobalResponse<string>
                {
                    Data = imageUrl,
                    ErrorStatus = false,
                    Message = "تم إنشاء الصورة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating item image with AI");
                return StatusCode(500, new GlobalResponse<string>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("SaveGeneratedItemImage/{itemId}")]
        public async Task<ActionResult<GlobalResponse<object>>> SaveGeneratedItemImage(int itemId, [FromBody] SaveGeneratedImageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رابط الصورة مطلوب"
                    });
                }

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

                var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == itemId && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                if (item == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Item not found"
                    });
                }

                // تحميل الصورة من URL وحفظها
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(request.ImageUrl);

                var imageExtension = ".png"; // DALL-E يعيد PNG
                var fileName = $"{Guid.NewGuid()}{imageExtension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var filePath = Path.Combine(path, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                item.Image = fileName;
                _dbConfig.Items.Update(item);
                await _dbConfig.SaveChangesAsync();

                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";
                var fullImageUrl = imageBaseUrl + fileName;

                return Ok(new GlobalResponse<object>
                {
                    Data = new { Image = fullImageUrl, ImageFileName = fileName },
                    ErrorStatus = false,
                    Message = "تم حفظ الصورة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving generated item image for item {ItemId}", itemId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حفظ الصورة: {ex.Message}"
                });
            }
        }
        

        // upload images 
        private async Task<string> UploadIamgesAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("Image file is null or empty");
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var validImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            
            var fileName = imageFile.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Image file name is null or empty");
            }

            var fileExtension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(fileExtension) || !validImageExtensions.Contains(fileExtension.ToLower()))
            {
                throw new ArgumentException("Invalid image extension. Allowed extensions: .jpg, .jpeg, .png, .gif");
            }

            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(path, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return uniqueFileName;
        }

        // Seed Database
   //     [Authorize(Roles = "Admin")]
        [HttpPost("SeedData")]
        public ActionResult<GlobalResponse<string>> ExecuteSeedData([FromBody] SeedDataRequest request)
        {
            try
            {
                int commercialUserId = request.CommercialUserId;
                RestaurantPOS.Db.SeedData.SeedDatabase(_dbConfig, commercialUserId);

                var message =
                    $"تم إضافة البيانات بنجاح للمستخدم التجاري رقم {commercialUserId}";
                
                return Ok(new GlobalResponse<string>
                {
                    Data = "Database seeded successfully",
                    ErrorStatus = false,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                return BadRequest(new GlobalResponse<string>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"خطأ في إضافة البيانات: {ex.Message}"
                });
            }
        }

    }

}

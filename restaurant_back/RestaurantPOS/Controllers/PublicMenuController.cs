using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Hubs;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Services;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class PublicMenuController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<PublicMenuController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly ICardPaymentProcessingService _cardPaymentProcessing;
        private readonly IOrderCheckoutService _orderCheckoutService;

        public PublicMenuController(
            ILogger<PublicMenuController> logger,
            DbConfig dbConfig,
            IConfiguration configuration,
            IHubContext<OrderHub> hubContext,
            ICardPaymentProcessingService cardPaymentProcessing,
            IOrderCheckoutService orderCheckoutService)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _configuration = configuration;
            _hubContext = hubContext;
            _cardPaymentProcessing = cardPaymentProcessing;
            _orderCheckoutService = orderCheckoutService;
        }

        /// <summary>
        /// Calendar day(s) from the client → UTC [from, to) for InsertDate comparison.
        /// </summary>
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

        // GET: api/PublicMenu/{commercialUserId}
        [AllowAnonymous]
        [HttpGet("{commercialUserId}")]
        public async Task<ActionResult<GlobalResponse<PublicMenuDto>>> GetPublicMenu(int commercialUserId)
        {
            try
            {
                // Verify that the user exists and is a Commercial user
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<PublicMenuDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المطعم غير موجود"
                    });
                }

                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

                // Get all available items for this Commercial user
                var items = await _dbConfig.Items
                    .Include(x => x.User)
                    .Where(x => x.IsDeleted == false 
                        && x.IsAvailable == true
                        && (x.InsertByUserId == commercialUserId || (x.User != null && x.User.InsertByUserId == commercialUserId)))
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                var menuItems = items.Select(item => new PublicMenuItemDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Image = string.IsNullOrEmpty(item.Image) ? null : imageBaseUrl + item.Image,
                    SellingPrice = item.SellingPrice,
                    DiscountPrice = item.DisCountPrice > 0 && item.DisCountPrice != item.SellingPrice ? item.DisCountPrice : null,
                    Tags = item.Tags,
                    Code = item.Code
                }).ToList();

                var publicMenu = new PublicMenuDto
                {
                    RestaurantName = commercialUser.RestaurantName ?? commercialUser.Name,
                    Logo = string.IsNullOrEmpty(commercialUser.Logo) ? null : imageBaseUrl + commercialUser.Logo,
                    Items = menuItems
                };

                return Ok(new GlobalResponse<PublicMenuDto>
                {
                    Data = publicMenu,
                    ErrorStatus = false,
                    Message = "تم جلب المنيو بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public menu for Commercial user {CommercialUserId}", commercialUserId);
                return StatusCode(500, new GlobalResponse<PublicMenuDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب المنيو: {ex.Message}"
                });
            }
        }

        // GET: api/PublicMenu/{commercialUserId}/items
        [AllowAnonymous]
        [HttpGet("{commercialUserId}/items")]
        public async Task<ActionResult<List<SimpleItemDto>>> GetItems(int commercialUserId)
        {
            try
            {
                // Verify that the user exists and is a Commercial user
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new List<SimpleItemDto>());
                }

                // Get all available items for this Commercial user
                var items = await _dbConfig.Items
                    .Include(x => x.User)
                    .Where(x => x.IsDeleted == false 
                        && x.IsAvailable == true
                        && (x.InsertByUserId == commercialUserId || (x.User != null && x.User.InsertByUserId == commercialUserId)))
                    .OrderBy(x => x.Name)
                    .Select(x => new SimpleItemDto
                    {
                        Name = x.Name,
                        Category = x.Tags ?? "Other"
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for Commercial user {CommercialUserId}", commercialUserId);
                return StatusCode(500, new List<SimpleItemDto>());
            }
        }

        // GET: api/PublicMenu/{commercialUserId}/categories
        [AllowAnonymous]
        [HttpGet("{commercialUserId}/categories")]
        public async Task<ActionResult<GlobalResponse<List<string>>>> GetCategories(int commercialUserId)
        {
            try
            {
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المطعم غير موجود"
                    });
                }

                var categories = await _dbConfig.Items
                    .Include(x => x.User)
                    .Where(x => x.IsDeleted == false 
                        && x.IsAvailable == true
                        && !string.IsNullOrEmpty(x.Tags)
                        && (x.InsertByUserId == commercialUserId || (x.User != null && x.User.InsertByUserId == commercialUserId)))
                    .Select(x => x.Tags!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<string>>
                {
                    Data = categories,
                    ErrorStatus = false,
                    Message = "تم جلب الأقسام بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories for Commercial user {CommercialUserId}", commercialUserId);
                return StatusCode(500, new GlobalResponse<List<string>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الأقسام: {ex.Message}"
                });
            }
        }

        // POST: api/PublicMenu/{commercialUserId}/order
        [AllowAnonymous]
        [HttpGet("{commercialUserId}/payment-capabilities")]
        public async Task<ActionResult<GlobalResponse<object>>> GetPaymentCapabilities(int commercialUserId)
        {
            var commercialUser = await _dbConfig.Users
                .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

            if (commercialUser == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "المطعم غير موجود"
                });
            }

            var cardPaymentEnabled = await _dbConfig.PaymentDevices
                .AnyAsync(d => !d.IsDeleted && d.IsActive && d.InsertByUserId == commercialUserId);

            return Ok(new GlobalResponse<object>
            {
                Data = new { cardPaymentEnabled },
                ErrorStatus = false,
                Message = "OK"
            });
        }

        [AllowAnonymous]
        [HttpPost("{commercialUserId}/card-payment/start")]
        public async Task<ActionResult<GlobalResponse<object>>> StartPublicCardPayment(
            int commercialUserId,
            [FromBody] CardPaymentSaleRequest request)
        {
            var commercialUser = await _dbConfig.Users
                .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

            if (commercialUser == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "المطعم غير موجود"
                });
            }

            try
            {
                var (tx, device, errorKey) = await _cardPaymentProcessing.PrepareTransactionAsync(
                    commercialUserId,
                    commercialUserId,
                    request.Amount,
                    request.TipAmount,
                    request.CurrencyCode ?? "IQD",
                    request.PaymentDeviceId);

                if (tx == null || errorKey != null)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = errorKey ?? "cardPaymentFailed"
                    });
                }

                _cardPaymentProcessing.EnqueueSaleProcessing(tx.Id, commercialUserId);
                var statusDto = _cardPaymentProcessing.ToStatusDto(tx, device);

                return Ok(new GlobalResponse<object>
                {
                    Data = new
                    {
                        transactionId = tx.Id,
                        status = statusDto.Status,
                        amount = statusDto.Amount,
                        currencyCode = statusDto.CurrencyCode,
                        deviceName = statusDto.DeviceName,
                        isTerminal = statusDto.IsTerminal
                    },
                    ErrorStatus = false,
                    Message = "cardPaymentProcessing"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Public card payment start failed for Commercial user {CommercialUserId}", commercialUserId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpGet("{commercialUserId}/card-payment/{transactionId}/status")]
        public async Task<ActionResult<GlobalResponse<CardPaymentStatusDto>>> GetPublicCardPaymentStatus(
            int commercialUserId,
            int transactionId)
        {
            var tx = await _dbConfig.CardPaymentTransactions
                .Include(t => t.PaymentDevice)
                .FirstOrDefaultAsync(t =>
                    t.Id == transactionId
                    && !t.IsDeleted
                    && t.InsertByUserId == commercialUserId);

            if (tx == null)
            {
                return NotFound(new GlobalResponse<CardPaymentStatusDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "cardPaymentTransactionNotFound"
                });
            }

            return Ok(new GlobalResponse<CardPaymentStatusDto>
            {
                Data = _cardPaymentProcessing.ToStatusDto(tx, tx.PaymentDevice),
                ErrorStatus = false
            });
        }

        [AllowAnonymous]
        [HttpPost("{commercialUserId}/card-payment/{transactionId}/cancel")]
        public async Task<ActionResult<GlobalResponse<CardPaymentStatusDto>>> CancelPublicCardPayment(
            int commercialUserId,
            int transactionId)
        {
            var (success, errorKey) = await _cardPaymentProcessing.CancelTransactionAsync(
                transactionId,
                commercialUserId,
                commercialUserId);

            if (!success)
            {
                return BadRequest(new GlobalResponse<CardPaymentStatusDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = errorKey ?? "cardPaymentFailed"
                });
            }

            var tx = await _dbConfig.CardPaymentTransactions
                .Include(t => t.PaymentDevice)
                .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted);

            return Ok(new GlobalResponse<CardPaymentStatusDto>
            {
                Data = tx != null ? _cardPaymentProcessing.ToStatusDto(tx, tx.PaymentDevice) : null,
                ErrorStatus = false,
                Message = "cardPaymentCancelled"
            });
        }

        [AllowAnonymous]
        [HttpPost("{commercialUserId}/order")]
        public async Task<ActionResult<GlobalResponse<object>>> CreatePublicOrder(int commercialUserId, PublicOrderRequest request)
        {
            try
            {
                // Verify that the user exists and is a Commercial user
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المطعم غير موجود"
                    });
                }

                if (request.CustomerOrderItem == null || !request.CustomerOrderItem.Any())
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يجب أن يحتوي الطلب على عنصر واحد على الأقل"
                    });
                }

                // Load items for this commercial user
                var items = await _dbConfig.Items
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted 
                        && (x.InsertByUserId == commercialUserId || (x.User != null && x.User.InsertByUserId == commercialUserId)))
                    .ToListAsync();

                // Validate items
                var itemIds = request.CustomerOrderItem.Select(x => x.ItemId).Distinct().ToList();
                var invalidItemIds = itemIds.Where(id => !items.Any(x => x.Id == id)).ToList();
                if (invalidItemIds.Any())
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = $"عناصر غير صحيحة: {string.Join(", ", invalidItemIds)}"
                    });
                }

                // Check item availability
                foreach (var itemRequest in request.CustomerOrderItem)
                {
                    var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                    if (currentItem == null) continue;

                    if (!currentItem.IsAvailable)
                    {
                        return BadRequest(new GlobalResponse<object>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = $"العنصر '{currentItem.Name}' غير متاح"
                        });
                    }
                }

                // Generate order code
                var orderCode = request.OrderCode ?? GenerateOrderCode();

                // Get daily sequence number
                var today = DateTime.UtcNow.Date;
                var lastOrderToday = await _dbConfig.CustomerOrders
                    .Where(o => o.InsertByUserId == commercialUserId 
                        && o.InsertDate.Date == today
                        && o.DailySequenceNumber.HasValue)
                    .OrderByDescending(o => o.DailySequenceNumber)
                    .FirstOrDefaultAsync();

                var dailySequenceNumber = (lastOrderToday?.DailySequenceNumber ?? 0) + 1;

                var orderSubTotal = CalculatePublicOrderSubTotal(request, items);
                var paymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod)
                    ? "Cash"
                    : request.PaymentMethod.Trim();
                var isCardPayment = string.Equals(paymentMethod, "Card", StringComparison.OrdinalIgnoreCase);

                if (isCardPayment)
                {
                    if (!request.CardPaymentTransactionId.HasValue)
                    {
                        return BadRequest(new GlobalResponse<object>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "cardPaymentTransactionRequired"
                        });
                    }

                    var validationError = await _orderCheckoutService.ValidateCardTransactionForCheckoutAsync(
                        request.CardPaymentTransactionId.Value,
                        orderSubTotal,
                        commercialUserId);

                    if (validationError != null)
                    {
                        return BadRequest(validationError);
                    }
                }

                // Handle Delivery Driver
                int? deliveryDriverId = null;
                if (request.OrderType == "Delivery")
                {
                    if (request.DeliveryDriverId.HasValue)
                    {
                        // Use existing driver
                        var existingDriver = await _dbConfig.DeliveryDrivers
                            .FirstOrDefaultAsync(d => d.Id == request.DeliveryDriverId.Value 
                                && !d.IsDeleted 
                                && d.IsActive
                                && d.InsertByUserId == commercialUserId);
                        
                        if (existingDriver != null)
                        {
                            deliveryDriverId = existingDriver.Id;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(request.NewDriverName) 
                        && !string.IsNullOrWhiteSpace(request.NewDriverPhone))
                    {
                        // Create new driver
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

                // Create order
                var newOrder = new CustomerOrder
                {
                    OrderCode = orderCode,
                    PaymentMethod = paymentMethod,
                    InsertByUserId = commercialUserId,
                    OrderType = request.OrderType ?? "Takeaway",
                    Notes = request.Notes,
                    PagerNumber = request.PagerNumber,
                    OrderStatus = isCardPayment ? "Processing" : "Pending",
                    PaymentStatus = isCardPayment ? "Paid" : "Pending",
                    DailySequenceNumber = dailySequenceNumber,
                    DeliveryDriverId = deliveryDriverId,
                    DeliveryStatus = request.OrderType == "Delivery" ? (request.DeliveryStatus ?? "Pending") : null,
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryPhoneNumber = request.DeliveryPhoneNumber,
                    DeliveryCustomerName = request.DeliveryCustomerName,
                    DeliveryFee = request.DeliveryFee,
                    DiscountType = request.DiscountType,
                    DiscountValue = request.DiscountValue,
                    DiscountAmount = request.DiscountAmount,
                    DiscountPercent = request.DiscountPercent,
                    OrderSubTotal = orderSubTotal,
                    OrderTotalAfterDiscount = request.OrderTotalAfterDiscount ?? orderSubTotal,
                    DeliveryAssignedAt = deliveryDriverId.HasValue ? DateTime.UtcNow : null
                };

                _dbConfig.CustomerOrders.Add(newOrder);
                await _dbConfig.SaveChangesAsync();

                // Create order items
                var insertItems = new List<CustomerOrderItem>();
                foreach (var itemRequest in request.CustomerOrderItem)
                {
                    var existingItem = insertItems.FirstOrDefault(x => x.ItemId == itemRequest.ItemId);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += itemRequest.Quantity;
                    }
                    else
                    {
                        var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                        if (currentItem == null) continue;

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
                            InsertByUserId = commercialUserId,
                        };

                        insertItems.Add(newOrderItem);
                    }
                }

                _dbConfig.CustomerOrderItems.AddRange(insertItems);
                await _dbConfig.SaveChangesAsync();

                if (isCardPayment && request.CardPaymentTransactionId.HasValue)
                {
                    await _orderCheckoutService.LinkCardTransactionToOrderAsync(
                        request.CardPaymentTransactionId.Value,
                        newOrder.Id);
                }

                var responseData = new
                {
                    Id = newOrder.Id,
                    OrderCode = newOrder.OrderCode,
                    PaymentMethod = newOrder.PaymentMethod,
                    OrderType = newOrder.OrderType,
                    OrderStatus = newOrder.OrderStatus,
                    PaymentStatus = newOrder.PaymentStatus,
                    InsertDate = newOrder.InsertDate,
                    Total = insertItems.Sum(x => x.SellingPrice * x.Quantity)
                };

                // Send SignalR notification for public order added
                try
                {
                    await _hubContext.Clients.All.SendAsync("PublicOrderAdded", new
                    {
                        CommercialUserId = commercialUserId,
                        OrderId = newOrder.Id,
                        OrderCode = newOrder.OrderCode,
                        OrderType = newOrder.OrderType,
                        OrderStatus = newOrder.OrderStatus,
                        PaymentStatus = newOrder.PaymentStatus,
                        PaymentMethod = newOrder.PaymentMethod,
                        ShouldAutoPrint = isCardPayment,
                        DailySequenceNumber = newOrder.DailySequenceNumber,
                        InsertDate = newOrder.InsertDate
                    });
                    _logger.LogInformation("SignalR notification sent for PublicOrderAdded: OrderId={OrderId}, CommercialUserId={CommercialUserId}", newOrder.Id, commercialUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SignalR notification for PublicOrderAdded");
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = responseData,
                    ErrorStatus = false,
                    Message = "تم إنشاء الطلب بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating public order for Commercial user {CommercialUserId}", commercialUserId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إنشاء الطلب: {ex.Message}"
                });
            }
        }

        private static decimal CalculatePublicOrderSubTotal(PublicOrderRequest request, List<Item> items)
        {
            if (request.CustomerOrderItem == null || request.CustomerOrderItem.Count == 0)
            {
                return 0;
            }

            decimal total = 0;
            foreach (var itemRequest in request.CustomerOrderItem)
            {
                var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                if (currentItem == null)
                {
                    continue;
                }

                var finalPrice = currentItem.DisCountPrice > 0
                    && currentItem.DisCountPrice != currentItem.SellingPrice
                    ? currentItem.DisCountPrice
                    : currentItem.SellingPrice;

                total += finalPrice * itemRequest.Quantity;
            }

            return total;
        }

        private string GenerateOrderCode()
        {
            // Generate 9-digit order code: timestamp-based to ensure uniqueness
            var random = new Random();
            var timestamp = DateTime.UtcNow.Ticks % 1000000000; // Last 9 digits of ticks
            var randomPart = random.Next(100000, 999999); // 6 digits
            var code = (timestamp + randomPart) % 1000000000; // Ensure 9 digits
            return code.ToString().PadLeft(9, '0'); // Ensure exactly 9 digits
        }

        // GET: api/PublicMenu/{commercialUserId}/orders
        [AuthorizeSection("publicOrders", "orderQueue", Roles = "Commercial,Admin,POS")]
        [HttpGet("{commercialUserId}/orders")]
        public async Task<ActionResult<GlobalResponse<object>>> GetPublicOrders(int commercialUserId, int pageNumber = 0, int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null, int? dailySequenceNumber = null, string? orderCode = null, string? orderType = null, int? deliveryDriverId = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    return Unauthorized(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المستخدم غير موجود"
                    });
                }

                // Verify commercial user
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المطعم غير موجود"
                    });
                }

                // Check if user has access to this commercial user's orders
                if (userId != commercialUserId && user.InsertByUserId != commercialUserId)
                {
                    return Forbid();
                }

                // Get public orders (orders where OrderType is Takeaway or Delivery)
                // Include orders created by commercialUserId or by users under the same commercial user
                var ordersQuery = _dbConfig.CustomerOrders
                    .Include(x => x.CustomerOrderItem)
                        .ThenInclude(x => x.Item)
                    .Include(x => x.User)
                    .Include(x => x.DeliveryDriver)
                    .Where(x => !x.IsDeleted 
                        && (x.OrderType == "Takeaway" || x.OrderType == "Delivery")
                        && (x.InsertByUserId == commercialUserId 
                            || (x.User != null && x.User.InsertByUserId == commercialUserId)))
                    .AsQueryable();

                if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcExclusive))
                {
                    ordersQuery = ordersQuery.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcExclusive);
                }

                // Filter by dailySequenceNumber
                if (dailySequenceNumber.HasValue)
                {
                    ordersQuery = ordersQuery.Where(x => x.DailySequenceNumber == dailySequenceNumber.Value);
                }

                // Filter by orderCode
                if (!string.IsNullOrEmpty(orderCode))
                {
                    ordersQuery = ordersQuery.Where(x => x.OrderCode.Contains(orderCode));
                }

                // Filter by orderType
                if (!string.IsNullOrEmpty(orderType))
                {
                    ordersQuery = ordersQuery.Where(x => x.OrderType == orderType);
                }

                // Filter by deliveryDriverId
                if (deliveryDriverId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(x => x.DeliveryDriverId == deliveryDriverId.Value);
                }

                ordersQuery = ordersQuery.OrderByDescending(x => x.InsertDate);

                var totalItems = await ordersQuery.CountAsync();

                var orders = await ordersQuery
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        Id = x.Id,
                        OrderCode = x.OrderCode,
                        PaymentMethod = x.PaymentMethod,
                        OrderType = x.OrderType,
                        OrderStatus = x.OrderStatus ?? "Pending",
                        PaymentStatus = x.PaymentStatus ?? "Pending",
                        HiddenFromQueueDisplay = x.HiddenFromQueueDisplay,
                        DailySequenceNumber = x.DailySequenceNumber ?? 0,
                        InsertDate = x.InsertDate,
                        Notes = x.Notes,
                        OrderPrice = x.CustomerOrderItem.Sum(item => item.SellingPrice * item.Quantity),
                        DiscountType = x.DiscountType,
                        DiscountValue = x.DiscountValue,
                        DiscountAmount = x.DiscountAmount,
                        DiscountPercent = x.DiscountPercent,
                        OrderSubTotal = x.OrderSubTotal,
                        OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                        ItemsCount = x.CustomerOrderItem.Count(),
                        // Delivery fields
                        DeliveryDriverId = x.DeliveryDriverId,
                        DeliveryDriver = x.DeliveryDriver != null ? new
                        {
                            Id = x.DeliveryDriver.Id,
                            Name = x.DeliveryDriver.Name,
                            PhoneNumber = x.DeliveryDriver.PhoneNumber,
                            Address = x.DeliveryDriver.Address,
                            VehicleType = x.DeliveryDriver.VehicleType,
                            VehicleNumber = x.DeliveryDriver.VehicleNumber
                        } : null,
                        DeliveryStatus = x.DeliveryStatus,
                        DeliveryAddress = x.DeliveryAddress,
                        DeliveryPhoneNumber = x.DeliveryPhoneNumber,
                        DeliveryCustomerName = x.DeliveryCustomerName,
                        DeliveryFee = x.DeliveryFee,
                        CustomerOrderItem = x.CustomerOrderItem.Select(item => new
                        {
                            Id = item.Id,
                            ItemId = item.ItemId,
                            ItemName = item.Item != null ? item.Item.Name : "",
                            Tags = item.Item != null ? item.Item.Tags : null,
                            Notes = item.Notes,
                            Quantity = item.Quantity,
                            SellingPrice = item.SellingPrice,
                            Total = item.SellingPrice * item.Quantity
                        }).ToList()
                    })
                    .ToListAsync();

                var result = new
                {
                    Items = orders,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = result,
                    ErrorStatus = false,
                    Message = "تم جلب الطلبات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public orders for Commercial user {CommercialUserId}", commercialUserId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الطلبات: {ex.Message}"
                });
            }
        }

        // GET: api/PublicMenu/{commercialUserId}/orders/{orderId}
        [AuthorizeSection("publicOrders", "orderQueue", Roles = "Commercial,Admin,POS")]
        [HttpGet("{commercialUserId}/orders/{orderId}")]
        public async Task<ActionResult<GlobalResponse<object>>> GetPublicOrderById(int commercialUserId, int orderId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    return Unauthorized(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المستخدم غير موجود"
                    });
                }

                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المطعم غير موجود"
                    });
                }

                if (userId != commercialUserId && user.InsertByUserId != commercialUserId)
                {
                    return Forbid();
                }

                var order = await _dbConfig.CustomerOrders
                    .Include(x => x.CustomerOrderItem)
                        .ThenInclude(x => x.Item)
                    .Include(x => x.DeliveryDriver)
                    .Where(x => !x.IsDeleted
                        && x.Id == orderId
                        && (x.OrderType == "Takeaway" || x.OrderType == "Delivery")
                        && (x.InsertByUserId == commercialUserId
                            || (x.User != null && x.User.InsertByUserId == commercialUserId)))
                    .Select(x => new
                    {
                        Id = x.Id,
                        OrderCode = x.OrderCode,
                        PaymentMethod = x.PaymentMethod,
                        OrderType = x.OrderType,
                        OrderStatus = x.OrderStatus ?? "Pending",
                        PaymentStatus = x.PaymentStatus ?? "Pending",
                        HiddenFromQueueDisplay = x.HiddenFromQueueDisplay,
                        DailySequenceNumber = x.DailySequenceNumber ?? 0,
                        InsertDate = x.InsertDate,
                        Notes = x.Notes,
                        OrderPrice = x.CustomerOrderItem.Sum(item => item.SellingPrice * item.Quantity),
                        DiscountType = x.DiscountType,
                        DiscountValue = x.DiscountValue,
                        DiscountAmount = x.DiscountAmount,
                        DiscountPercent = x.DiscountPercent,
                        OrderSubTotal = x.OrderSubTotal,
                        OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                        ItemsCount = x.CustomerOrderItem.Count(),
                        DeliveryDriverId = x.DeliveryDriverId,
                        DeliveryDriver = x.DeliveryDriver != null ? new
                        {
                            Id = x.DeliveryDriver.Id,
                            Name = x.DeliveryDriver.Name,
                            PhoneNumber = x.DeliveryDriver.PhoneNumber,
                            Address = x.DeliveryDriver.Address,
                            VehicleType = x.DeliveryDriver.VehicleType,
                            VehicleNumber = x.DeliveryDriver.VehicleNumber
                        } : null,
                        DeliveryStatus = x.DeliveryStatus,
                        DeliveryAddress = x.DeliveryAddress,
                        DeliveryPhoneNumber = x.DeliveryPhoneNumber,
                        DeliveryCustomerName = x.DeliveryCustomerName,
                        DeliveryFee = x.DeliveryFee,
                        CustomerOrderItem = x.CustomerOrderItem.Select(item => new
                        {
                            Id = item.Id,
                            ItemId = item.ItemId,
                            ItemName = item.Item != null ? item.Item.Name : "",
                            Tags = item.Item != null ? item.Item.Tags : null,
                            Notes = item.Notes,
                            Quantity = item.Quantity,
                            SellingPrice = item.SellingPrice,
                            Total = item.SellingPrice * item.Quantity
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطلب غير موجود"
                    });
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = order,
                    ErrorStatus = false,
                    Message = "تم جلب الطلب بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public order {OrderId} for Commercial user {CommercialUserId}", orderId, commercialUserId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الطلب: {ex.Message}"
                });
            }
        }

        // GET: api/PublicMenu/{commercialUserId}/queue-display
        [AllowAnonymous]
        [HttpGet("{commercialUserId}/queue-display")]
        public async Task<ActionResult<GlobalResponse<object>>> GetQueueDisplayOrders(
            int commercialUserId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المطعم غير موجود"
                    });
                }

                var ordersQuery = _dbConfig.CustomerOrders
                    .Include(x => x.CustomerOrderItem)
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted
                        && !x.HiddenFromQueueDisplay
                        && (x.OrderType == "Takeaway" || x.OrderType == "Delivery")
                        && (x.InsertByUserId == commercialUserId
                            || (x.User != null && x.User.InsertByUserId == commercialUserId)))
                    .AsQueryable();

                if (TryGetOrderInsertUtcRange(startDate, endDate, out var fromUtc, out var toUtcExclusive))
                {
                    ordersQuery = ordersQuery.Where(x => x.InsertDate >= fromUtc && x.InsertDate < toUtcExclusive);
                }

                var activeStatuses = new[] { "Pending", "Processing", "Ready", "Completed" };
                ordersQuery = ordersQuery.Where(x =>
                    activeStatuses.Contains(x.OrderStatus ?? "Pending"));

                var orders = await ordersQuery
                    .OrderByDescending(x => x.InsertDate)
                    .Select(x => new
                    {
                        Id = x.Id,
                        OrderCode = x.OrderCode,
                        OrderType = x.OrderType,
                        OrderStatus = x.OrderStatus ?? "Pending",
                        DailySequenceNumber = x.DailySequenceNumber ?? 0,
                        InsertDate = x.InsertDate,
                        ItemsCount = x.CustomerOrderItem.Count(i => !i.IsDeleted),
                    })
                    .ToListAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = new { Items = orders },
                    ErrorStatus = false,
                    Message = "تم جلب الطلبات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue display for Commercial user {CommercialUserId}", commercialUserId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب الطلبات: {ex.Message}"
                });
            }
        }

        // GET: api/PublicMenu/{commercialUserId}/order-status/{orderCode}
        [AllowAnonymous]
        [HttpGet("{commercialUserId}/order-status/{orderCode}")]
        public async Task<ActionResult<GlobalResponse<object>>> GetOrderStatusByCode(int commercialUserId, string orderCode)
        {
            try
            {
                // Verify that the commercial user exists
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المطعم غير موجود"
                    });
                }

                // Find order by code
                var order = await _dbConfig.CustomerOrders
                    .Include(x => x.CustomerOrderItem)
                        .ThenInclude(x => x.Item)
                    .Include(x => x.Table)
                    .Include(x => x.DeliveryDriver)
                    .Where(x => !x.IsDeleted 
                        && x.OrderCode == orderCode
                        && (x.InsertByUserId == commercialUserId 
                            || (x.User != null && x.User.InsertByUserId == commercialUserId)))
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطلب غير موجود"
                    });
                }

                var orderData = new
                {
                    Id = order.Id,
                    OrderCode = order.OrderCode,
                    OrderStatus = order.OrderStatus ?? "Pending",
                    PaymentStatus = order.PaymentStatus ?? "Pending",
                    InsertDate = order.InsertDate,
                    OrderType = order.OrderType,
                    PaymentMethod = order.PaymentMethod,
                    Notes = order.Notes,
                    TableId = order.TableId,
                    TableNumber = order.Table?.TableNumber,
                    DailySequenceNumber = order.DailySequenceNumber ?? 0,
                    Total = order.OrderTotalAfterDiscount
                        ?? (order.CustomerOrderItem != null
                            ? order.CustomerOrderItem.Sum(item => item.SellingPrice * item.Quantity)
                            : 0),
                    DiscountType = order.DiscountType,
                    DiscountValue = order.DiscountValue,
                    DiscountAmount = order.DiscountAmount,
                    DiscountPercent = order.DiscountPercent,
                    OrderSubTotal = order.OrderSubTotal,
                    OrderTotalAfterDiscount = order.OrderTotalAfterDiscount,
                    ItemsCount = order.CustomerOrderItem != null ? order.CustomerOrderItem.Count() : 0,
                    Items = order.CustomerOrderItem != null ? order.CustomerOrderItem.Select(item => new
                    {
                        Id = item.Id,
                        ItemId = item.ItemId,
                        ItemName = item.Item != null ? item.Item.Name : "",
                        Quantity = item.Quantity,
                        SellingPrice = item.SellingPrice,
                        Total = item.SellingPrice * item.Quantity
                    }).Cast<object>().ToList() : new List<object>(),
                    DeliveryDriver = order.DeliveryDriver != null ? new
                    {
                        Id = order.DeliveryDriver.Id,
                        Name = order.DeliveryDriver.Name,
                        PhoneNumber = order.DeliveryDriver.PhoneNumber
                    } : null,
                    DeliveryStatus = order.DeliveryStatus,
                    DeliveryAddress = order.DeliveryAddress,
                    DeliveryPhoneNumber = order.DeliveryPhoneNumber,
                    DeliveryCustomerName = order.DeliveryCustomerName,
                    DeliveryFee = order.DeliveryFee
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = orderData,
                    ErrorStatus = false,
                    Message = "تم جلب حالة الطلب بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status by code for Commercial user {CommercialUserId}, OrderCode: {OrderCode}", commercialUserId, orderCode);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب حالة الطلب: {ex.Message}"
                });
            }
        }

        // PUT: api/PublicMenu/{commercialUserId}/orders/{orderId}/status
        [AuthorizeSection("publicOrders", "orderQueue", Roles = "Commercial,Admin,POS")]
        [HttpPut("{commercialUserId}/orders/{orderId}/status")]
        public async Task<ActionResult<GlobalResponse<object>>> UpdateOrderStatus(int commercialUserId, int orderId, UpdateOrderStatusRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    return Unauthorized(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المستخدم غير موجود"
                    });
                }

                // Verify commercial user
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المطعم غير موجود"
                    });
                }

                // Check if user has access
                if (userId != commercialUserId && user.InsertByUserId != commercialUserId)
                {
                    return Forbid();
                }

                var order = await _dbConfig.CustomerOrders
                    .FirstOrDefaultAsync(x => x.Id == orderId 
                        && x.InsertByUserId == commercialUserId 
                        && !x.IsDeleted);

                if (order == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطلب غير موجود"
                    });
                }

                // Store old values for audit log
                var oldOrderStatus = order.OrderStatus;
                var oldPaymentStatus = order.PaymentStatus;
                var oldDeliveryStatus = order.DeliveryStatus;
                var oldHiddenFromQueueDisplay = order.HiddenFromQueueDisplay;

                if (!string.IsNullOrEmpty(request.OrderStatus))
                {
                    order.OrderStatus = request.OrderStatus;
                    
                    // Auto-update DeliveryStatus when OrderStatus is Completed for Delivery orders
                    if (request.OrderStatus == "Completed" && order.OrderType == "Delivery")
                    {
                        // If DeliveryStatus is not already Completed or Delivered, update it
                        if (order.DeliveryStatus != "Completed" && order.DeliveryStatus != "Delivered")
                        {
                            order.DeliveryStatus = "Completed";
                        }
                    }
                }

                if (!string.IsNullOrEmpty(request.PaymentStatus))
                {
                    order.PaymentStatus = request.PaymentStatus;
                }

                if (request.HiddenFromQueueDisplay.HasValue)
                {
                    order.HiddenFromQueueDisplay = request.HiddenFromQueueDisplay.Value;
                }

                _dbConfig.CustomerOrders.Update(order);
                await _dbConfig.SaveChangesAsync();

                // Log audit for order status/payment status/delivery status update
                var changesDescription = new List<string>();
                var oldValues = new { OrderStatus = oldOrderStatus, PaymentStatus = oldPaymentStatus, DeliveryStatus = oldDeliveryStatus, HiddenFromQueueDisplay = oldHiddenFromQueueDisplay };
                var newValues = new { OrderStatus = order.OrderStatus, PaymentStatus = order.PaymentStatus, DeliveryStatus = order.DeliveryStatus, HiddenFromQueueDisplay = order.HiddenFromQueueDisplay };

                if (oldOrderStatus != order.OrderStatus)
                {
                    changesDescription.Add($"حالة الطلب: {oldOrderStatus} → {order.OrderStatus}");
                }
                if (oldPaymentStatus != order.PaymentStatus)
                {
                    changesDescription.Add($"حالة الدفع: {oldPaymentStatus} → {order.PaymentStatus}");
                }
                if (oldDeliveryStatus != order.DeliveryStatus)
                {
                    changesDescription.Add($"حالة التوصيل: {oldDeliveryStatus} → {order.DeliveryStatus}");
                }
                if (oldHiddenFromQueueDisplay != order.HiddenFromQueueDisplay)
                {
                    changesDescription.Add(order.HiddenFromQueueDisplay
                        ? "تم إخفاء الطلب من شاشة الانتظار"
                        : "تم إظهار الطلب على شاشة الانتظار");
                }

                if (changesDescription.Count > 0)
                {
                    await _dbConfig.LogAuditAsync(
                        "Update",
                        "CustomerOrder",
                        order.Id,
                        order.OrderCode,
                        userId,
                        commercialUserId,
                        oldValues,
                        newValues,
                        $"تم تعديل حالة الطلب {order.OrderCode}: {string.Join(", ", changesDescription)}"
                    );
                }

                // Send SignalR notification for public order updated
                try
                {
                    await _hubContext.Clients.All.SendAsync("PublicOrderUpdated", new
                    {
                        CommercialUserId = commercialUserId,
                        OrderId = order.Id,
                        OrderCode = order.OrderCode,
                        OrderStatus = order.OrderStatus,
                        PaymentStatus = order.PaymentStatus,
                        HiddenFromQueueDisplay = order.HiddenFromQueueDisplay
                    });
                    _logger.LogInformation("SignalR notification sent for PublicOrderUpdated: OrderId={OrderId}, CommercialUserId={CommercialUserId}", order.Id, commercialUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SignalR notification for PublicOrderUpdated");
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = new { Id = order.Id, OrderStatus = order.OrderStatus, PaymentStatus = order.PaymentStatus, HiddenFromQueueDisplay = order.HiddenFromQueueDisplay },
                    ErrorStatus = false,
                    Message = "تم تحديث حالة الطلب بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", orderId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث حالة الطلب: {ex.Message}"
                });
            }
        }
    }

    // Request DTO for updating order status
    public class UpdateOrderStatusRequest
    {
        public string? OrderStatus { get; set; } // Pending, Processing, Ready, Completed, Cancelled
        public string? PaymentStatus { get; set; } // Pending, Paid, Refunded
        public bool? HiddenFromQueueDisplay { get; set; }
        public int? DeliveryPointsPerOrder { get; set; } // عدد النقاط لكل عملية توصيل مكتملة
    }

    // DTO for public menu
    public class PublicMenuDto
    {
        public string RestaurantName { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public List<PublicMenuItemDto> Items { get; set; } = new List<PublicMenuItemDto>();
    }

    // DTO for public menu items
    public class PublicMenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? Tags { get; set; }
        public string? Code { get; set; }
    }

    // DTO for simple item (name and category only)
    public class SimpleItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    // Request DTO for public order
    public class PublicOrderRequest
    {
        public string? OrderCode { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card
        public required List<CustomerOrderItemRequest>? CustomerOrderItem { get; set; }
        public string OrderType { get; set; } = "Takeaway"; // Takeaway, DineIn, Delivery
        public string? Notes { get; set; } // ملاحظات الطلب
        public string? PagerNumber { get; set; } // رقم جهاز النداء
        
        // Delivery fields
        public int? DeliveryDriverId { get; set; } // سائق التوصيل (اختياري - يمكن استخدام سائق موجود)
        public string? DeliveryStatus { get; set; } // Pending, InTransit, Delivered, Failed, Completed
        public string? DeliveryAddress { get; set; } // عنوان التوصيل
        public string? DeliveryPhoneNumber { get; set; } // رقم هاتف المستلم
        public string? DeliveryCustomerName { get; set; } // اسم المستلم
        public decimal? DeliveryFee { get; set; } // رسوم التوصيل

        // Order discount fields
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? OrderSubTotal { get; set; }
        public decimal? OrderTotalAfterDiscount { get; set; }
        
        // معلومات سائق جديد (إذا لم يتم اختيار سائق موجود)
        public string? NewDriverName { get; set; }
        public string? NewDriverPhone { get; set; }
        public string? NewDriverAddress { get; set; }
        public string? NewDriverVehicleType { get; set; }
        public string? NewDriverVehicleNumber { get; set; }

        public int? CardPaymentTransactionId { get; set; }
    }
}


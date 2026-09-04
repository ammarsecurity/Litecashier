using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using POS.Authorization;
using POS.Db;
using POS.Hubs;
using POS.Models;
using POS.Models.Requests;
using POS.Models.Response;
using POS.Services;
using System.Security.Claims;

namespace POS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class PublicMenuController : ControllerBase
    {
        private readonly DbConfig _db;
        private readonly ILogger<PublicMenuController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IWarehouseStockService _warehouseStock;

        public PublicMenuController(
            DbConfig db,
            ILogger<PublicMenuController> logger,
            IConfiguration configuration,
            IHubContext<OrderHub> hubContext,
            IWarehouseStockService warehouseStock)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
            _hubContext = hubContext;
            _warehouseStock = warehouseStock;
        }

        [AllowAnonymous]
        [HttpGet("{commercialUserId:int}")]
        public async Task<ActionResult<GlobalResponse<PublicMenuDto>>> GetPublicMenu(int commercialUserId)
        {
            var store = await FindCommercialAsync(commercialUserId);
            if (store == null)
            {
                return NotFound(new GlobalResponse<PublicMenuDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "storeNotFound"
                });
            }

            var items = await AccessibleItemsQuery(commercialUserId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var defaultWh = await _warehouseStock.EnsureDefaultWarehouseAsync(commercialUserId);
            var stockedIds = items.Where(i => !i.IsNonInventory).Select(i => i.Id).ToList();
            var stockMap = stockedIds.Count == 0
                ? new Dictionary<int, int>()
                : await _warehouseStock.GetStocksForItemsAsync(stockedIds, defaultWh.Id);

            var menuItems = items.Select(item =>
            {
                stockMap.TryGetValue(item.Id, out var availableQty);
                var available = item.IsNonInventory || availableQty > 0;
                var unit = ResolvePublicPrice(item);
                return new PublicMenuItemDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Image = BuildAssetUrl(item.Image),
                    SellingPrice = item.SellingPrice,
                    DiscountPrice = unit != item.SellingPrice ? unit : null,
                    Tags = item.Tags,
                    Code = item.Code,
                    IsAvailable = available,
                    AvailableQuantity = item.IsNonInventory ? null : availableQty
                };
            }).ToList();

            var ads = await _db.PublicMenuAds
                .AsNoTracking()
                .Where(a => a.CommercialUserId == commercialUserId && a.IsActive && !a.IsDeleted)
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Id)
                .ToListAsync();

            return Ok(new GlobalResponse<PublicMenuDto>
            {
                Data = new PublicMenuDto
                {
                    StoreName = string.IsNullOrWhiteSpace(store.StoreName) ? store.Name : store.StoreName,
                    Logo = BuildAssetUrl(store.Logo),
                    DefaultProductImage = BuildAssetUrl(store.DefaultProductImage),
                    Items = menuItems,
                    Ads = ads.Select(MapAdDto).ToList()
                },
                ErrorStatus = false,
                Message = "ok"
            });
        }

        [AllowAnonymous]
        [HttpGet("{commercialUserId:int}/categories")]
        public async Task<ActionResult<GlobalResponse<List<string>>>> GetCategories(int commercialUserId)
        {
            var store = await FindCommercialAsync(commercialUserId);
            if (store == null)
            {
                return NotFound(new GlobalResponse<List<string>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "storeNotFound"
                });
            }

            var tags = await AccessibleItemsQuery(commercialUserId)
                .Where(x => x.Tags != null && x.Tags != "")
                .Select(x => x.Tags!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Ok(new GlobalResponse<List<string>>
            {
                Data = tags,
                ErrorStatus = false,
                Message = "ok"
            });
        }

        private const int MaxPublicMenuAds = 8;

        [AuthorizeSection("publicOrders", Roles = "Commercial,Admin")]
        [HttpGet("{commercialUserId:int}/ads")]
        public async Task<ActionResult<GlobalResponse<List<PublicMenuAdDto>>>> GetMenuAds(int commercialUserId)
        {
            if (!await StaffCanAccessAsync(commercialUserId))
                return Forbid();

            var ads = await _db.PublicMenuAds
                .AsNoTracking()
                .Where(a => a.CommercialUserId == commercialUserId && !a.IsDeleted)
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Id)
                .ToListAsync();

            return Ok(new GlobalResponse<List<PublicMenuAdDto>>
            {
                Data = ads.Select(MapAdDto).ToList(),
                ErrorStatus = false,
                Message = "ok"
            });
        }

        [AuthorizeSection("publicOrders", Roles = "Commercial,Admin")]
        [HttpPost("{commercialUserId:int}/ads")]
        [RequestSizeLimit(8 * 1024 * 1024)]
        public async Task<ActionResult<GlobalResponse<PublicMenuAdDto>>> UploadMenuAd(
            int commercialUserId,
            [FromForm] IFormFile? image,
            [FromForm] string? title)
        {
            if (!await StaffCanAccessAsync(commercialUserId))
                return Forbid();

            var store = await FindCommercialAsync(commercialUserId);
            if (store == null)
            {
                return NotFound(new GlobalResponse<PublicMenuAdDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "storeNotFound"
                });
            }

            if (image == null || image.Length == 0)
            {
                return BadRequest(new GlobalResponse<PublicMenuAdDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "imageRequired"
                });
            }

            var count = await _db.PublicMenuAds.CountAsync(a =>
                a.CommercialUserId == commercialUserId && !a.IsDeleted);
            if (count >= MaxPublicMenuAds)
            {
                return BadRequest(new GlobalResponse<PublicMenuAdDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "menuAdsLimitReached"
                });
            }

            var fileName = await UploadImageAsync(image);
            if (fileName == "not a valid image extension")
            {
                return BadRequest(new GlobalResponse<PublicMenuAdDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "invalidImage"
                });
            }

            var maxSort = await _db.PublicMenuAds
                .Where(a => a.CommercialUserId == commercialUserId && !a.IsDeleted)
                .Select(a => (int?)a.SortOrder)
                .MaxAsync() ?? -1;

            var now = DateTime.UtcNow;
            var ad = new PublicMenuAd
            {
                CommercialUserId = commercialUserId,
                Image = fileName,
                Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                SortOrder = maxSort + 1,
                IsActive = true,
                InsertDate = now,
                UpdateDate = now,
                IsDeleted = false
            };
            _db.PublicMenuAds.Add(ad);
            await _db.SaveChangesAsync();

            return Ok(new GlobalResponse<PublicMenuAdDto>
            {
                Data = MapAdDto(ad),
                ErrorStatus = false,
                Message = "ok"
            });
        }

        [AuthorizeSection("publicOrders", Roles = "Commercial,Admin")]
        [HttpDelete("{commercialUserId:int}/ads/{adId:int}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteMenuAd(int commercialUserId, int adId)
        {
            if (!await StaffCanAccessAsync(commercialUserId))
                return Forbid();

            var ad = await _db.PublicMenuAds.FirstOrDefaultAsync(a =>
                a.Id == adId
                && a.CommercialUserId == commercialUserId
                && !a.IsDeleted);

            if (ad == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "adNotFound"
                });
            }

            ad.IsDeleted = true;
            ad.IsActive = false;
            ad.UpdateDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new GlobalResponse<object>
            {
                Data = new { ad.Id },
                ErrorStatus = false,
                Message = "ok"
            });
        }

        [AllowAnonymous]
        [HttpPost("{commercialUserId:int}/order")]
        public async Task<ActionResult<GlobalResponse<object>>> CreatePublicOrder(
            int commercialUserId,
            [FromBody] PublicMenuOrderRequest request)
        {
            var store = await FindCommercialAsync(commercialUserId);
            if (store == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "storeNotFound"
                });
            }

            var customerName = NormalizeCustomerName(request.CustomerName);
            var customerPhone = NormalizeIraqiPhone(request.CustomerPhone);
            if (string.IsNullOrEmpty(customerName) || customerName.Length < 2)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "customerNameRequired"
                });
            }

            if (!IsValidCustomerName(customerName))
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "customerNameInvalid"
                });
            }

            if (string.IsNullOrWhiteSpace(customerPhone))
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "customerPhoneRequired"
                });
            }

            var notes = (request.Notes ?? "").Trim();
            if (notes.Length < 2)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "orderNotesRequired"
                });
            }

            if (notes.Length > 1000)
            {
                notes = notes.Substring(0, 1000);
            }

            if (!IsValidIraqiPhone(customerPhone))
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "customerPhoneInvalid"
                });
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "orderMustContainItems"
                });
            }

            var catalog = await AccessibleItemsQuery(commercialUserId).ToListAsync();
            var lines = new List<(Item Item, int Qty)>();
            foreach (var row in request.Items)
            {
                if (row.Quantity < 1)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "invalidQuantity"
                    });
                }

                var item = catalog.FirstOrDefault(x => x.Id == row.ItemId);
                if (item == null)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "invalidItem"
                    });
                }

                lines.Add((item, row.Quantity));
            }

            var subTotal = lines.Sum(l => ResolvePublicPrice(l.Item) * l.Qty);
            var order = new CustomerOrder
            {
                OrderCode = GenerateOrderCode(),
                PaymentMethod = "Cash",
                PaymentStatus = "Pending",
                OrderSource = "PublicMenu",
                OrderStatus = "Pending",
                InsertByUserId = commercialUserId,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                Notes = notes,
                OrderSubTotal = subTotal,
                OrderTotalAfterDiscount = subTotal
            };

            _db.CustomerOrders.Add(order);
            await _db.SaveChangesAsync();

            var orderItems = lines
                .GroupBy(l => l.Item.Id)
                .Select(g =>
                {
                    var item = g.First().Item;
                    return new CustomerOrderItem
                    {
                        CustomerOrderId = order.Id,
                        ItemId = item.Id,
                        Quantity = g.Sum(x => x.Qty),
                        SellingPrice = ResolvePublicPrice(item),
                        PurchasingPrice = item.PurchasingPrice,
                        InsertByUserId = commercialUserId
                    };
                })
                .ToList();

            _db.CustomerOrderItems.AddRange(orderItems);
            await _db.SaveChangesAsync();

            await NotifyAsync("PublicOrderAdded", commercialUserId, order);

            return Ok(new GlobalResponse<object>
            {
                Data = new
                {
                    order.Id,
                    order.OrderCode,
                    order.OrderStatus,
                    order.PaymentStatus,
                    order.CustomerName,
                    order.CustomerPhone,
                    Total = subTotal
                },
                ErrorStatus = false,
                Message = "orderCreated"
            });
        }

        [AllowAnonymous]
        [HttpGet("{commercialUserId:int}/track")]
        public async Task<ActionResult<GlobalResponse<object>>> TrackPublicOrder(
            int commercialUserId,
            [FromQuery] string? code,
            [FromQuery] string? phone)
        {
            var store = await FindCommercialAsync(commercialUserId);
            if (store == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "storeNotFound"
                });
            }

            var orderCode = (code ?? "").Trim();
            var customerPhone = NormalizeIraqiPhone(phone);
            if (string.IsNullOrWhiteSpace(orderCode) || !IsValidIraqiPhone(customerPhone))
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "orderTrackRequired"
                });
            }

            var order = await _db.CustomerOrders
                .AsNoTracking()
                .Include(o => o.CustomerOrderItem)!
                    .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(o =>
                    !o.IsDeleted
                    && o.InsertByUserId == commercialUserId
                    && o.OrderSource == "PublicMenu"
                    && o.OrderCode == orderCode);

            if (order == null || NormalizeIraqiPhone(order.CustomerPhone) != customerPhone)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "orderNotFound"
                });
            }

            return Ok(new GlobalResponse<object>
            {
                Data = MapPublicTrackDto(order, store),
                ErrorStatus = false,
                Message = "ok"
            });
        }

        [AuthorizeSection("publicOrders", Roles = "Commercial,POS,Admin")]
        [HttpGet("{commercialUserId:int}/orders")]
        public async Task<ActionResult<GlobalResponse<object>>> GetPublicOrders(
            int commercialUserId,
            string? status = null,
            int pageNumber = 0,
            int pageSize = 50)
        {
            if (!await StaffCanAccessAsync(commercialUserId))
            {
                return Forbid();
            }

            if (pageSize < 1) pageSize = 50;
            if (pageSize > 200) pageSize = 200;
            if (pageNumber < 0) pageNumber = 0;

            var query = _db.CustomerOrders
                .AsNoTracking()
                .Include(o => o.CustomerOrderItem)!
                    .ThenInclude(i => i.Item)
                .Where(o => !o.IsDeleted
                    && o.InsertByUserId == commercialUserId
                    && o.OrderSource == "PublicMenu");

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var total = await query.CountAsync();
            var pendingCount = await _db.CustomerOrders.CountAsync(o =>
                !o.IsDeleted
                && o.InsertByUserId == commercialUserId
                && o.OrderSource == "PublicMenu"
                && o.OrderStatus == "Pending");

            var rows = await query
                .OrderByDescending(o => o.InsertDate)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = rows.Select(MapOrderDto).ToList();

            return Ok(new GlobalResponse<object>
            {
                Data = new { items = data, totalItems = total, pendingCount },
                ErrorStatus = false,
                Message = "ok"
            });
        }

        [AuthorizeSection("publicOrders", Roles = "Commercial,POS,Admin")]
        [HttpGet("{commercialUserId:int}/pending-count")]
        public async Task<ActionResult<GlobalResponse<object>>> GetPendingCount(int commercialUserId)
        {
            if (!await StaffCanAccessAsync(commercialUserId))
            {
                return Forbid();
            }

            var count = await _db.CustomerOrders.CountAsync(o =>
                !o.IsDeleted
                && o.InsertByUserId == commercialUserId
                && o.OrderSource == "PublicMenu"
                && o.OrderStatus == "Pending");

            return Ok(new GlobalResponse<object>
            {
                Data = new { count },
                ErrorStatus = false,
                Message = "ok"
            });
        }

        [AuthorizeSection("publicOrders", Roles = "Commercial,POS,Admin")]
        [HttpPut("{commercialUserId:int}/orders/{orderId:int}/approve")]
        public async Task<ActionResult<GlobalResponse<object>>> ApproveOrder(int commercialUserId, int orderId)
        {
            if (!await StaffCanAccessAsync(commercialUserId))
            {
                return Forbid();
            }

            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderItem)!
                    .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId
                    && !o.IsDeleted
                    && o.InsertByUserId == commercialUserId
                    && o.OrderSource == "PublicMenu");

            if (order == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "orderNotFound"
                });
            }

            if (!string.Equals(order.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "orderNotPending"
                });
            }

            var catalog = await AccessibleItemsQuery(commercialUserId).ToListAsync();
            var defaultWh = await _warehouseStock.EnsureDefaultWarehouseAsync(commercialUserId);
            var lines = (order.CustomerOrderItem ?? [])
                .Where(i => !i.IsDeleted)
                .GroupBy(i => i.ItemId)
                .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToList();

            var stockedNeeded = lines
                .Where(l => catalog.FirstOrDefault(x => x.Id == l.ItemId)?.IsNonInventory != true)
                .ToDictionary(l => l.ItemId, l => l.Qty);

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (stockedNeeded.Count > 0)
                {
                    var stockMap = await _warehouseStock.GetStocksForItemsAsync(stockedNeeded.Keys, defaultWh.Id);
                    foreach (var kv in stockedNeeded)
                    {
                        var currentItem = catalog.FirstOrDefault(x => x.Id == kv.Key);
                        stockMap.TryGetValue(kv.Key, out var available);
                        if (available < kv.Value)
                        {
                            await tx.RollbackAsync();
                            return BadRequest(new GlobalResponse<object>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"insufficientInventory|{currentItem?.Name ?? kv.Key.ToString()}|{available}|{kv.Value}"
                            });
                        }
                    }

                    foreach (var kv in stockedNeeded)
                    {
                        await _warehouseStock.DeductAsync(kv.Key, defaultWh.Id, kv.Value);
                    }
                }

                order.OrderStatus = "Approved";
                order.PaymentStatus = "Paid";
                order.PaymentMethod = "Cash";
                order.WarehouseId = defaultWh.Id;
                order.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("insufficientInventory|"))
            {
                await tx.RollbackAsync();
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            await NotifyAsync("PublicOrderUpdated", commercialUserId, order);

            return Ok(new GlobalResponse<object>
            {
                Data = MapOrderDto(order),
                ErrorStatus = false,
                Message = "orderApproved"
            });
        }

        [AuthorizeSection("publicOrders", Roles = "Commercial,POS,Admin")]
        [HttpPut("{commercialUserId:int}/orders/{orderId:int}/cancel")]
        public async Task<ActionResult<GlobalResponse<object>>> CancelOrder(int commercialUserId, int orderId)
        {
            if (!await StaffCanAccessAsync(commercialUserId))
            {
                return Forbid();
            }

            var order = await _db.CustomerOrders
                .Include(o => o.CustomerOrderItem)!
                    .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId
                    && !o.IsDeleted
                    && o.InsertByUserId == commercialUserId
                    && o.OrderSource == "PublicMenu");

            if (order == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "orderNotFound"
                });
            }

            if (!string.Equals(order.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "orderNotPending"
                });
            }

            order.OrderStatus = "Cancelled";
            order.UpdateDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await NotifyAsync("PublicOrderUpdated", commercialUserId, order);

            return Ok(new GlobalResponse<object>
            {
                Data = MapOrderDto(order),
                ErrorStatus = false,
                Message = "orderCancelled"
            });
        }

        private IQueryable<Item> AccessibleItemsQuery(int commercialUserId)
        {
            return _db.Items.Where(x =>
                !x.IsDeleted &&
                (x.InsertByUserId == commercialUserId ||
                 x.User!.Id == commercialUserId ||
                 x.User.InsertByUserId == commercialUserId));
        }

        private Task<User?> FindCommercialAsync(int commercialUserId) =>
            _db.Users.FirstOrDefaultAsync(u =>
                u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);

        private async Task<bool> StaffCanAccessAsync(int commercialUserId)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId))
                return false;

            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return false;

            if (user.Id == commercialUserId && user.Role == "Commercial")
                return true;

            return user.InsertByUserId == commercialUserId;
        }

        private async Task NotifyAsync(string eventName, int commercialUserId, CustomerOrder order)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(eventName, new
                {
                    CommercialUserId = commercialUserId,
                    OrderId = order.Id,
                    OrderCode = order.OrderCode,
                    OrderStatus = order.OrderStatus,
                    PaymentStatus = order.PaymentStatus,
                    CustomerName = order.CustomerName,
                    CustomerPhone = order.CustomerPhone,
                    InsertDate = order.InsertDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR {Event} failed for order {OrderId}", eventName, order.Id);
            }
        }

        private object MapPublicTrackDto(CustomerOrder order, User store)
        {
            var mapped = MapOrderDto(order);
            return new
            {
                storeName = string.IsNullOrWhiteSpace(store.StoreName) ? store.Name : store.StoreName,
                logo = BuildAssetUrl(store.Logo),
                order = mapped
            };
        }

        private object MapOrderDto(CustomerOrder order)
        {
            var lines = (order.CustomerOrderItem ?? [])
                .Where(i => !i.IsDeleted)
                .Select(i => new
                {
                    i.Id,
                    i.ItemId,
                    Name = i.Item?.Name ?? "",
                    Image = BuildAssetUrl(i.Item?.Image),
                    i.Quantity,
                    i.SellingPrice,
                    Total = i.SellingPrice * i.Quantity
                })
                .ToList();

            return new
            {
                order.Id,
                order.OrderCode,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethod,
                order.CustomerName,
                order.CustomerPhone,
                order.Notes,
                order.OrderSubTotal,
                order.OrderTotalAfterDiscount,
                order.InsertDate,
                Items = lines
            };
        }

        private PublicMenuAdDto MapAdDto(PublicMenuAd ad)
        {
            return new PublicMenuAdDto
            {
                Id = ad.Id,
                Image = BuildAssetUrl(ad.Image),
                Title = ad.Title,
                SortOrder = ad.SortOrder,
                IsActive = ad.IsActive
            };
        }

        private async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var validImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName);
            if (!validImageExtensions.Contains(fileExtension.ToLowerInvariant()))
                return "not a valid image extension";

            var fileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(path, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }

        private string? BuildAssetUrl(string? file)
        {
            if (string.IsNullOrWhiteSpace(file) || file == "-" || file == "null")
                return null;
            var name = file.Trim();
            if (name.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("/"))
            {
                return name;
            }

            var imageBase = (_configuration["ApiSettings:ImageBaseUrl"] ?? "/Images/").Trim();
            if (!imageBase.EndsWith('/'))
                imageBase += "/";
            return imageBase + name.TrimStart('/');
        }

        private static decimal ResolvePublicPrice(Item item)
        {
            if (item.DisCountPrice > 0 && item.DisCountPrice < item.SellingPrice)
                return item.DisCountPrice;
            return item.SellingPrice;
        }

        private static string NormalizeCustomerName(string? raw)
        {
            var trimmed = (raw ?? "").Trim();
            if (trimmed.Length == 0) return "";
            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        private static bool IsValidCustomerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 120)
                return false;
            if (name.Any(char.IsDigit))
                return false;
            return name.Count(char.IsLetter) >= 2;
        }

        private static readonly HashSet<string> IraqiMobilePrefixes = new(StringComparer.Ordinal)
        {
            "074", "075", "077", "078"
        };

        private static string NormalizeIraqiPhone(string? raw)
        {
            var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
            if (digits.StartsWith("9640", StringComparison.Ordinal))
                digits = digits[3..];
            else if (digits.StartsWith("964", StringComparison.Ordinal))
            {
                digits = digits[3..];
                if (digits.Length == 10 && digits[0] == '7')
                    digits = "0" + digits;
            }
            else if (digits.Length == 10 && digits.StartsWith("7", StringComparison.Ordinal))
            {
                digits = "0" + digits;
            }

            return digits;
        }

        private static bool IsValidIraqiPhone(string phone)
        {
            return phone.Length == 11
                && IraqiMobilePrefixes.Contains(phone[..3])
                && phone.All(char.IsDigit);
        }

        private static string GenerateOrderCode()
        {
            var random = Random.Shared.Next(100000, 999999);
            var timestamp = DateTime.UtcNow.Ticks % 1000000000;
            var code = (timestamp + random) % 1000000000;
            return code.ToString().PadLeft(9, '0');
        }
    }

    public class PublicMenuDto
    {
        public string StoreName { get; set; } = "";
        public string? Logo { get; set; }
        public string? DefaultProductImage { get; set; }
        public List<PublicMenuItemDto> Items { get; set; } = new();
        public List<PublicMenuAdDto> Ads { get; set; } = new();
    }

    public class PublicMenuAdDto
    {
        public int Id { get; set; }
        public string? Image { get; set; }
        public string? Title { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class PublicMenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Image { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? Tags { get; set; }
        public string? Code { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int? AvailableQuantity { get; set; }
    }

    public class PublicMenuOrderRequest
    {
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string? Notes { get; set; }
        public List<CustomerOrderItemRequest> Items { get; set; } = new();
    }
}

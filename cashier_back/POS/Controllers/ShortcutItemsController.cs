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
    [Authorize]
    public class ShortcutItemsController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<ShortcutItemsController> _logger;

        public ShortcutItemsController(ILogger<ShortcutItemsController> logger, DbConfig dbConfig)
        {
            _logger = logger;
            _dbConfig = dbConfig;
        }

        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId && !x.IsDeleted);

            if (user != null && user.Role == "Commercial")
                return userId;

            return user?.InsertByUserId ?? userId;
        }

        private IQueryable<Item> AccessibleShortcutsQuery(int commercialUserId)
        {
            return _dbConfig.Items.Where(x =>
                !x.IsDeleted &&
                x.IsNonInventory &&
                (x.InsertByUserId == commercialUserId ||
                 x.User!.Id == commercialUserId ||
                 x.User.InsertByUserId == commercialUserId));
        }

        [AuthorizeSection("shortcutItems", Roles = "Commercial,POS")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<PagedList<Item>>>> GetShortcutItems(
            int pageNumber = 0,
            int pageSize = 20,
            string? search = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var query = AccessibleShortcutsQuery(commercialUserId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(x =>
                        x.Name.Contains(term) ||
                        (x.Description != null && x.Description.Contains(term)) ||
                        (x.Code != null && x.Code.Contains(term)));
                }

                var total = await query.CountAsync();
                var items = await query
                    .OrderBy(x => x.Name)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new GlobalResponse<PagedList<Item>>
                {
                    Data = new PagedList<Item>(items, total, pageNumber, pageSize),
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetShortcutItems failed");
                return BadRequest(new GlobalResponse<PagedList<Item>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpGet("ForPos")]
        public async Task<ActionResult<GlobalResponse<List<Item>>>> GetShortcutItemsForPos()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var items = await AccessibleShortcutsQuery(commercialUserId)
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<Item>>
                {
                    Data = items,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetShortcutItemsForPos failed");
                return BadRequest(new GlobalResponse<List<Item>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("shortcutItems", Roles = "Commercial,POS")]
        [HttpPost]
        [HttpPost("Add")]
        public async Task<ActionResult<GlobalResponse<Item>>> AddShortcutItem([FromBody] ShortcutItemRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var commercialUserId = GetCommercialUserId();
                var name = request.Name.Trim();

                var exists = await AccessibleShortcutsQuery(commercialUserId)
                    .AnyAsync(x => x.Name == name);
                if (exists)
                {
                    return BadRequest(new GlobalResponse<Item>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "shortcutItemExists"
                    });
                }

                var item = new Item
                {
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(request.Description)
                        ? null
                        : request.Description.Trim(),
                    SellingPrice = request.SellingPrice,
                    WholesalePrice = request.WholesalePrice,
                    PurchasingPrice = 0,
                    DisCountPrice = 0,
                    Quantity = 0,
                    IsNonInventory = true,
                    Code = await NextShortcutCodeAsync(commercialUserId),
                    InsertByUserId = commercialUserId,
                    IsDeleted = false,
                };

                _dbConfig.Items.Add(item);
                await _dbConfig.SaveChangesAsync();

                _logger.LogInformation("Shortcut item {ItemId} created by {UserId}", item.Id, userId);

                return Ok(new GlobalResponse<Item>
                {
                    Data = item,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddShortcutItem failed");
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("shortcutItems", Roles = "Commercial,POS")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<GlobalResponse<Item>>> UpdateShortcutItem(int id, [FromBody] ShortcutItemRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var item = await AccessibleShortcutsQuery(commercialUserId)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (item == null)
                {
                    return NotFound(new GlobalResponse<Item>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "shortcutItemNotFound"
                    });
                }

                var name = request.Name.Trim();
                var nameTaken = await AccessibleShortcutsQuery(commercialUserId)
                    .AnyAsync(x => x.Name == name && x.Id != id);
                if (nameTaken)
                {
                    return BadRequest(new GlobalResponse<Item>
                    {
                        Data = item,
                        ErrorStatus = true,
                        Message = "shortcutItemExists"
                    });
                }

                item.Name = name;
                item.Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim();
                item.SellingPrice = request.SellingPrice;
                item.WholesalePrice = request.WholesalePrice;

                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<Item>
                {
                    Data = item,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateShortcutItem failed");
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("shortcutItems", Roles = "Commercial,POS")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<GlobalResponse<bool>>> DeleteShortcutItem(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var item = await AccessibleShortcutsQuery(commercialUserId)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (item == null)
                {
                    return NotFound(new GlobalResponse<bool>
                    {
                        Data = false,
                        ErrorStatus = true,
                        Message = "shortcutItemNotFound"
                    });
                }

                item.IsDeleted = true;
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<bool>
                {
                    Data = true,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteShortcutItem failed");
                return BadRequest(new GlobalResponse<bool>
                {
                    Data = false,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        private async Task<string> NextShortcutCodeAsync(int commercialUserId)
        {
            for (var i = 0; i < 8; i++)
            {
                var code = $"SC{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}";
                var taken = await _dbConfig.Items.AnyAsync(x =>
                    !x.IsDeleted &&
                    x.Code == code &&
                    x.InsertByUserId == commercialUserId);
                if (!taken) return code;
            }

            return $"SC{Guid.NewGuid():N}"[..16];
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Authorization;
using POS.Db;
using POS.Models;
using POS.Models.Dtos;
using POS.Models.Response;
using POS.Services;
using System.Security.Claims;

namespace POS.Controllers;

[ApiController]
[Route("[controller]")]
[EnableCors("CorsPolicy")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly DbConfig _db;
    private readonly IWarehouseStockService _stock;
    private readonly ILogger<WarehousesController> _logger;

    public WarehousesController(DbConfig db, IWarehouseStockService stock, ILogger<WarehousesController> logger)
    {
        _db = db;
        _stock = stock;
        _logger = logger;
    }

    private int GetCommercialUserId()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var user = _db.Users.FirstOrDefault(x => x.Id == userId && !x.IsDeleted);
        if (user != null && user.Role == "Commercial")
            return userId;
        return user?.InsertByUserId ?? userId;
    }

    [HttpGet]
    [AuthorizeSection("warehouses", Roles = "Commercial,POS,Admin")]
    public async Task<ActionResult<GlobalResponse<List<Warehouse>>>> GetWarehouses(bool activeOnly = false)
    {
        var commercialUserId = GetCommercialUserId();
        await _stock.EnsureDefaultWarehouseAsync(commercialUserId);

        var q = _db.Warehouses.AsNoTracking()
            .Where(w => !w.IsDeleted && w.InsertByUserId == commercialUserId);
        if (activeOnly)
            q = q.Where(w => w.IsActive);

        var list = await q
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .ToListAsync();

        return Ok(new GlobalResponse<List<Warehouse>>
        {
            Data = list,
            ErrorStatus = false,
            Message = "ok"
        });
    }

    /// <summary>Lightweight list for POS (any cashier role; no warehouses section required).</summary>
    [HttpGet("ForPos")]
    [Authorize(Roles = "Commercial,POS,Manager,Admin")]
    public async Task<ActionResult<GlobalResponse<List<object>>>> GetWarehousesForPos()
    {
        var commercialUserId = GetCommercialUserId();
        await _stock.EnsureDefaultWarehouseAsync(commercialUserId);

        var list = await _db.Warehouses.AsNoTracking()
            .Where(w => !w.IsDeleted && w.IsActive && w.InsertByUserId == commercialUserId)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .Select(w => new { w.Id, w.Name, w.IsDefault })
            .ToListAsync();

        return Ok(new GlobalResponse<List<object>>
        {
            Data = list.Cast<object>().ToList(),
            ErrorStatus = false,
            Message = "ok"
        });
    }

    [HttpPost]
    [AuthorizeSection("warehouses", Roles = "Commercial,Admin")]
    public async Task<ActionResult<GlobalResponse<Warehouse>>> Create([FromBody] WarehouseRequest request)
    {
        var commercialUserId = GetCommercialUserId();
        var name = (request.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new GlobalResponse<Warehouse>
            {
                ErrorStatus = true,
                Message = "warehouseNameRequired"
            });
        }

        await _stock.EnsureDefaultWarehouseAsync(commercialUserId);

        var exists = await _db.Warehouses.AnyAsync(w =>
            !w.IsDeleted && w.InsertByUserId == commercialUserId && w.Name == name);
        if (exists)
        {
            return BadRequest(new GlobalResponse<Warehouse>
            {
                ErrorStatus = true,
                Message = "warehouseNameExists"
            });
        }

        if (request.IsDefault)
        {
            var defaults = await _db.Warehouses
                .Where(w => !w.IsDeleted && w.InsertByUserId == commercialUserId && w.IsDefault)
                .ToListAsync();
            foreach (var d in defaults)
                d.IsDefault = false;
        }

        var wh = new Warehouse
        {
            Name = name,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            InsertByUserId = commercialUserId
        };
        _db.Warehouses.Add(wh);
        await _db.SaveChangesAsync();

        if (!await _db.Warehouses.AnyAsync(w => !w.IsDeleted && w.InsertByUserId == commercialUserId && w.IsDefault))
        {
            wh.IsDefault = true;
            await _db.SaveChangesAsync();
        }

        return Ok(new GlobalResponse<Warehouse> { Data = wh, ErrorStatus = false, Message = "ok" });
    }

    [HttpPut("{id:int}")]
    [AuthorizeSection("warehouses", Roles = "Commercial,Admin")]
    public async Task<ActionResult<GlobalResponse<Warehouse>>> Update(int id, [FromBody] WarehouseRequest request)
    {
        var commercialUserId = GetCommercialUserId();
        var wh = await _db.Warehouses.FirstOrDefaultAsync(w =>
            !w.IsDeleted && w.Id == id && w.InsertByUserId == commercialUserId);
        if (wh == null)
        {
            return NotFound(new GlobalResponse<Warehouse>
            {
                ErrorStatus = true,
                Message = "warehouseNotFound"
            });
        }

        var name = (request.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new GlobalResponse<Warehouse>
            {
                ErrorStatus = true,
                Message = "warehouseNameRequired"
            });
        }

        var exists = await _db.Warehouses.AnyAsync(w =>
            !w.IsDeleted && w.InsertByUserId == commercialUserId && w.Name == name && w.Id != id);
        if (exists)
        {
            return BadRequest(new GlobalResponse<Warehouse>
            {
                ErrorStatus = true,
                Message = "warehouseNameExists"
            });
        }

        if (request.IsDefault && !wh.IsDefault)
        {
            var defaults = await _db.Warehouses
                .Where(w => !w.IsDeleted && w.InsertByUserId == commercialUserId && w.IsDefault)
                .ToListAsync();
            foreach (var d in defaults)
                d.IsDefault = false;
            wh.IsDefault = true;
        }
        else if (!request.IsDefault && wh.IsDefault)
        {
            // Keep at least one default
            wh.IsDefault = true;
        }

        wh.Name = name;
        wh.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new GlobalResponse<Warehouse> { Data = wh, ErrorStatus = false, Message = "ok" });
    }

    [HttpDelete("{id:int}")]
    [AuthorizeSection("warehouses", Roles = "Commercial,Admin")]
    public async Task<ActionResult<GlobalResponse<object>>> Delete(int id)
    {
        var commercialUserId = GetCommercialUserId();
        var wh = await _db.Warehouses.FirstOrDefaultAsync(w =>
            !w.IsDeleted && w.Id == id && w.InsertByUserId == commercialUserId);
        if (wh == null)
        {
            return NotFound(new GlobalResponse<object>
            {
                ErrorStatus = true,
                Message = "warehouseNotFound"
            });
        }

        var count = await _db.Warehouses.CountAsync(w =>
            !w.IsDeleted && w.InsertByUserId == commercialUserId);
        if (count <= 1 || wh.IsDefault)
        {
            return BadRequest(new GlobalResponse<object>
            {
                ErrorStatus = true,
                Message = "cannotDeleteDefaultWarehouse"
            });
        }

        var stock = await _db.ItemWarehouseStocks
            .Where(s => !s.IsDeleted && s.WarehouseId == id)
            .SumAsync(s => (int?)s.Quantity) ?? 0;
        if (stock > 0)
        {
            return BadRequest(new GlobalResponse<object>
            {
                ErrorStatus = true,
                Message = "warehouseHasStock"
            });
        }

        wh.IsDeleted = true;
        wh.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new GlobalResponse<object> { ErrorStatus = false, Message = "ok" });
    }

    [HttpPost("Transfer")]
    [AuthorizeSection("warehouses", Roles = "Commercial,Admin")]
    public async Task<ActionResult<GlobalResponse<object>>> Transfer([FromBody] TransferStockRequest request)
    {
        try
        {
            var commercialUserId = GetCommercialUserId();
            await _stock.TransferAsync(
                request.ItemId,
                commercialUserId,
                request.FromWarehouseId,
                request.ToWarehouseId,
                request.Quantity);

            var qty = await _stock.GetStockAsync(request.ItemId, request.ToWarehouseId);
            return Ok(new GlobalResponse<object>
            {
                Data = new { request.ItemId, toQuantity = qty },
                ErrorStatus = false,
                Message = "ok"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new GlobalResponse<object>
            {
                ErrorStatus = true,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer stock failed");
            return StatusCode(500, new GlobalResponse<object>
            {
                ErrorStatus = true,
                Message = "transferFailed"
            });
        }
    }
}

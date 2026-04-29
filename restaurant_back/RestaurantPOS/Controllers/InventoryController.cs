using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [Authorize(Roles = "Commercial,POS,Admin")]
    public class InventoryController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<InventoryController> _logger;
        private readonly IConfiguration _configuration;

        public InventoryController(ILogger<InventoryController> logger, DbConfig dbConfig, IConfiguration configuration)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _configuration = configuration;
        }

        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            if (user != null && user.Role == "Commercial")
                return userId;
            return user?.InsertByUserId ?? userId;
        }

        /// <summary>جلب أرصدة المخزن حسب اسم المادة (كتابة حرة، لا علاقة بالأطباق/المشروبات)</summary>
        [HttpGet("GetInventory")]
        public async Task<ActionResult<GlobalResponse<object>>> GetInventory(int pageNumber = 0, int pageSize = 500, string? info = null)
        {
            var commercialUserId = GetCommercialUserId();
            var movements = await _dbConfig.StockMovements
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId)
                .OrderByDescending(x => x.InsertDate)
                .ToListAsync();

            var byName = movements
                .GroupBy(x => x.MaterialName.Trim())
                .Select(g =>
                {
                    var adds = g.Where(m => m.MovementType == "Add").Sum(m => m.Quantity);
                    var withdraws = g.Where(m => m.MovementType == "Withdraw").Sum(m => m.Quantity);
                    var lastAdd = g.Where(m => m.MovementType == "Add").OrderByDescending(m => m.InsertDate).FirstOrDefault();
                    return new
                    {
                        materialName = g.Key,
                        currentQuantity = adds - withdraws,
                        unitType = lastAdd?.UnitType ?? ""
                    };
                })
                .Where(x => string.IsNullOrWhiteSpace(info) || x.materialName.Contains(info!.Trim()))
                .OrderBy(x => x.materialName)
                .ToList();

            var total = byName.Count;
            var items = byName.Skip(pageNumber * pageSize).Take(pageSize).ToList();

            return Ok(new GlobalResponse<object>
            {
                Data = new { items, totalItems = total },
                ErrorStatus = false,
                Message = "Success"
            });
        }

        /// <summary>جلب سجل حركات المخزن (إضافة وسحب)</summary>
        [HttpGet("GetStockMovements")]
        public async Task<ActionResult<GlobalResponse<object>>> GetStockMovements(
            int pageNumber = 0,
            int pageSize = 50,
            string? materialName = null,
            string? movementType = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var commercialUserId = GetCommercialUserId();
            var query = _dbConfig.StockMovements
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId);

            if (!string.IsNullOrWhiteSpace(materialName))
                query = query.Where(x => x.MaterialName.Contains(materialName.Trim()));
            if (!string.IsNullOrWhiteSpace(movementType) && (movementType == "Add" || movementType == "Withdraw"))
                query = query.Where(x => x.MovementType == movementType);
            if (startDate.HasValue)
                query = query.Where(x => x.InsertDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(x => x.InsertDate <= endDate.Value.AddDays(1));

            var total = await query.CountAsync();
            var list = await query
                .OrderByDescending(x => x.InsertDate)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    id = x.Id,
                    materialName = x.MaterialName,
                    movementType = x.MovementType,
                    quantity = x.Quantity,
                    supplierName = x.SupplierName,
                    amount = x.Amount,
                    unitType = x.UnitType,
                    notes = x.Notes,
                    receiptAttachmentPath = x.ReceiptAttachmentPath,
                    insertDate = x.InsertDate
                })
                .ToListAsync();

            return Ok(new GlobalResponse<object>
            {
                Data = new { items = list, totalItems = total },
                ErrorStatus = false,
                Message = "Success"
            });
        }

        /// <summary>جلب قائمة الموردين</summary>
        [HttpGet("GetSuppliers")]
        public async Task<ActionResult<GlobalResponse<object>>> GetSuppliers(int pageNumber = 0, int pageSize = 500, string? info = null)
        {
            var commercialUserId = GetCommercialUserId();
            var query = _dbConfig.Suppliers
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId);
            if (!string.IsNullOrWhiteSpace(info))
                query = query.Where(x => x.Name.Contains(info.Trim()));
            var total = await query.CountAsync();
            var list = await query.OrderBy(x => x.Name).Skip(pageNumber * pageSize).Take(pageSize)
                .Select(x => new { id = x.Id, name = x.Name, notes = x.Notes })
                .ToListAsync();
            return Ok(new GlobalResponse<object> { Data = new { items = list, totalItems = total }, ErrorStatus = false, Message = "Success" });
        }

        /// <summary>إضافة مورد</summary>
        [HttpPost("AddSupplier")]
        public async Task<ActionResult<GlobalResponse<object>>> AddSupplier([FromBody] SupplierRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "اسم المورد مطلوب" });
            var commercialUserId = GetCommercialUserId();
            var supplier = new Supplier
            {
                Name = request.Name.Trim(),
                Notes = request.Notes?.Trim(),
                InsertByUserId = commercialUserId
            };
            _dbConfig.Suppliers.Add(supplier);
            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<object> { Data = new { id = supplier.Id, name = supplier.Name, notes = supplier.Notes }, ErrorStatus = false, Message = "تمت إضافة المورد بنجاح" });
        }

        /// <summary>تعديل مورد</summary>
        [HttpPut("UpdateSupplier/{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> UpdateSupplier(int id, [FromBody] SupplierRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "اسم المورد مطلوب" });
            var commercialUserId = GetCommercialUserId();
            var supplier = await _dbConfig.Suppliers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.InsertByUserId == commercialUserId);
            if (supplier == null)
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "المورد غير موجود" });
            supplier.Name = request.Name.Trim();
            supplier.Notes = request.Notes?.Trim();
            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<object> { Data = new { id = supplier.Id, name = supplier.Name, notes = supplier.Notes }, ErrorStatus = false, Message = "تم تعديل المورد بنجاح" });
        }

        /// <summary>حذف مورد (حذف منطقي)</summary>
        [HttpDelete("DeleteSupplier/{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteSupplier(int id)
        {
            var commercialUserId = GetCommercialUserId();
            var supplier = await _dbConfig.Suppliers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.InsertByUserId == commercialUserId);
            if (supplier == null)
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "المورد غير موجود" });
            supplier.IsDeleted = true;
            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<object> { Data = null, ErrorStatus = false, Message = "تم حذف المورد بنجاح" });
        }

        /// <summary>إضافة دخول مخزون: اسم المادة (كتابة)، مورد، مبلغ، وحدة، كمية، مرفق وصل — لا علاقة بالأطباق/المشروبات</summary>
        [HttpPost("AddStock")]
        public async Task<ActionResult<GlobalResponse<object>>> AddStock(
            [FromForm] string materialName,
            [FromForm] decimal quantity,
            [FromForm] string? supplierName,
            [FromForm] decimal amount,
            [FromForm] string? unitType,
            [FromForm] string? notes,
            [FromForm] IFormFile? receiptFile)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "اسم المادة مطلوب" });
            if (quantity <= 0)
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الكمية يجب أن تكون أكبر من الصفر" });

            var commercialUserId = GetCommercialUserId();

            string? receiptPath = null;
            if (receiptFile != null && receiptFile.Length > 0)
            {
                try
                {
                    receiptPath = await UploadReceiptAsync(receiptFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Receipt upload failed");
                    return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "فشل رفع مرفق الوصل: " + ex.Message });
                }
            }

            var movement = new StockMovement
            {
                MaterialName = materialName.Trim(),
                MovementType = "Add",
                Quantity = quantity,
                SupplierName = supplierName?.Trim(),
                Amount = amount,
                UnitType = unitType?.Trim(),
                ReceiptAttachmentPath = receiptPath,
                Notes = notes?.Trim(),
                InsertByUserId = commercialUserId
            };
            _dbConfig.StockMovements.Add(movement);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<object>
            {
                Data = new { movementId = movement.Id },
                ErrorStatus = false,
                Message = "تمت إضافة الكمية إلى المخزن بنجاح"
            });
        }

        /// <summary>سحب كمية من المخزن حسب اسم المادة</summary>
        [HttpPost("WithdrawStock")]
        public async Task<ActionResult<GlobalResponse<object>>> WithdrawStock([FromBody] WithdrawStockRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            var name = request.MaterialName?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "اسم المادة مطلوب" });

            var movements = await _dbConfig.StockMovements
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId && x.MaterialName == name)
                .ToListAsync();
            var adds = movements.Where(m => m.MovementType == "Add").Sum(m => m.Quantity);
            var withdraws = movements.Where(m => m.MovementType == "Withdraw").Sum(m => m.Quantity);
            var currentBalance = adds - withdraws;

            if (currentBalance < request.Quantity)
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الكمية المتاحة في المخزن غير كافية" });

            var movement = new StockMovement
            {
                MaterialName = name,
                MovementType = "Withdraw",
                Quantity = request.Quantity,
                Notes = request.Notes?.Trim(),
                InsertByUserId = commercialUserId
            };
            _dbConfig.StockMovements.Add(movement);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<object>
            {
                Data = new { newStockQuantity = currentBalance - request.Quantity },
                ErrorStatus = false,
                Message = "تم سحب الكمية بنجاح"
            });
        }

        private async Task<string> UploadReceiptAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Receipts");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                throw new ArgumentException("نوع الملف غير مسموح. المسموح: jpg, jpeg, png, gif, pdf");

            var uniqueName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(path, uniqueName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);
            return uniqueName;
        }
    }
}

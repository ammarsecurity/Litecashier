using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Response;
using System.Security.Claims;
using System.Text.Json;

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
                .GroupBy(x => new { Mat = x.MaterialName.Trim(), Rec = InventoryReceiptKey(x) })
                .Select(g =>
                {
                    var adds = g.Where(m => m.MovementType == "Add").Sum(m => m.Quantity);
                    var withdraws = g.Where(m => m.MovementType == "Withdraw").Sum(m => m.Quantity);
                    var lastAdd = g.Where(m => m.MovementType == "Add").OrderByDescending(m => m.InsertDate).FirstOrDefault();
                    var lastMovement = g.OrderByDescending(m => m.InsertDate).FirstOrDefault();
                    var receiptDisplay = string.IsNullOrEmpty(g.Key.Rec) ? null : g.Key.Rec;
                    var latestAddWithAttachment = g
                        .Where(m => m.MovementType == "Add" && !string.IsNullOrWhiteSpace(m.ReceiptAttachmentPath))
                        .OrderByDescending(m => m.InsertDate)
                        .FirstOrDefault();
                    return new
                    {
                        materialName = g.Key.Mat,
                        stockReceiptKey = g.Key.Rec,
                        currentQuantity = adds - withdraws,
                        totalAdded = adds,
                        totalWithdrawn = withdraws,
                        unitType = lastAdd?.UnitType ?? "",
                        lastSupplierName = lastAdd?.SupplierName ?? "",
                        lastReceiptNumber = receiptDisplay,
                        lastReceiptAttachmentPath = BuildReceiptPublicUrl(latestAddWithAttachment?.ReceiptAttachmentPath),
                        lastMovementDate = lastMovement?.InsertDate
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
            string? receiptNumber = null,
            string? receivedByEmployeeName = null,
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
            if (!string.IsNullOrWhiteSpace(receiptNumber))
            {
                var rn = receiptNumber.Trim();
                query = query.Where(x =>
                    (x.Notes != null && x.Notes.Contains($"ReceiptNumber:{rn}")) ||
                    (x.ReceiptNumber != null && x.ReceiptNumber == rn));
            }
            // نطاق تواريخ شامل ليوم البداية والنهاية (حسب التقويم المحلي للخادم)
            if (startDate.HasValue)
                query = query.Where(x => x.InsertDate >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(x => x.InsertDate < endDate.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(receivedByEmployeeName))
            {
                var recv = receivedByEmployeeName.Trim();
                query = query.Where(x =>
                    x.ReceivedByEmployeeName != null && x.ReceivedByEmployeeName.Contains(recv));
            }

            var total = await query.CountAsync();
            const int enrichedSumRowCap = 5000;

            List<StockMovement> pageRows;
            IReadOnlyList<StockMovement> addsPool;
            decimal totalFilteredAmount;

            if (total <= enrichedSumRowCap && total > 0)
            {
                var allRows = await query
                    .AsNoTracking()
                    .OrderByDescending(x => x.InsertDate)
                    .ToListAsync();
                var namesForSum = allRows.Select(x => x.MaterialName).Distinct().ToList();
                var addsForSum = await _dbConfig.StockMovements
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId && x.MovementType == "Add"
                        && namesForSum.Contains(x.MaterialName))
                    .ToListAsync();
                totalFilteredAmount = allRows
                    .Sum(x => ResolveDisplayAmount(x, FindEnrichingAddForWithdraw(x, addsForSum)) ?? 0m);
                pageRows = allRows.Skip(pageNumber * pageSize).Take(pageSize).ToList();
                var namesOnPage = pageRows.Select(x => x.MaterialName).Distinct().ToList();
                addsPool = addsForSum.Where(a => namesOnPage.Contains(a.MaterialName)).ToList();
            }
            else
            {
                totalFilteredAmount = total > 0
                    ? await query.SumAsync(x => x.Amount ?? 0m)
                    : 0m;
                pageRows = await query
                    .AsNoTracking()
                    .OrderByDescending(x => x.InsertDate)
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                var namesOnPage = pageRows.Select(x => x.MaterialName).Distinct().ToList();
                addsPool = await _dbConfig.StockMovements
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId && x.MovementType == "Add"
                        && namesOnPage.Contains(x.MaterialName))
                    .ToListAsync();
            }

            var list = pageRows.Select(x =>
            {
                StockMovement? enrich = null;
                if (x.MovementType == "Withdraw")
                    enrich = FindEnrichingAddForWithdraw(x, addsPool);

                var supplierOut = x.SupplierName ?? enrich?.SupplierName;
                var unitOut = x.UnitType ?? enrich?.UnitType;
                var attachFile = x.ReceiptAttachmentPath ?? enrich?.ReceiptAttachmentPath;
                var attachUrl = BuildReceiptPublicUrl(attachFile);

                var receiptNum = DisplayReceiptNumber(x);
                if (x.MovementType == "Withdraw" && string.IsNullOrWhiteSpace(receiptNum))
                    receiptNum = enrich != null ? DisplayReceiptNumber(enrich) : null;

                var amountOut = ResolveDisplayAmount(x, enrich);

                return new
                {
                    id = x.Id,
                    materialName = x.MaterialName,
                    movementType = x.MovementType,
                    quantity = x.Quantity,
                    supplierName = supplierOut,
                    amount = amountOut,
                    unitType = unitOut,
                    receiptNumber = receiptNum,
                    notes = CleanNotesFromReceiptNumber(x.Notes),
                    receivedByEmployeeName = x.MovementType == "Withdraw" ? x.ReceivedByEmployeeName : null,
                    receiptFileName = attachFile,
                    receiptAttachmentUrl = attachUrl,
                    receiptAttachmentPath = attachUrl,
                    insertDate = x.InsertDate
                };
            }).ToList();

            return Ok(new GlobalResponse<object>
            {
                Data = new { items = list, totalItems = total, totalFilteredAmount },
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
            [FromForm] string? receiptNumber,
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

            var normalizedReceipt = NormalizeReceiptNumber(receiptNumber);
            var movement = new StockMovement
            {
                MaterialName = materialName.Trim(),
                MovementType = "Add",
                Quantity = quantity,
                SupplierName = supplierName?.Trim(),
                Amount = amount,
                UnitType = unitType?.Trim(),
                ReceiptAttachmentPath = receiptPath,
                ReceiptNumber = normalizedReceipt,
                Notes = BuildNotesWithReceiptNumber(receiptNumber, notes),
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

        /// <summary>إضافة دخول مخزون متعددة ضمن وصل واحد</summary>
        [HttpPost("AddStockBatch")]
        public async Task<ActionResult<GlobalResponse<object>>> AddStockBatch(
            [FromForm] string? supplierName,
            [FromForm] string? receiptNumber,
            [FromForm] string? notes,
            [FromForm] string itemsJson,
            [FromForm] IFormFile? receiptFile)
        {
            if (string.IsNullOrWhiteSpace(itemsJson))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "قائمة المواد مطلوبة" });

            List<AddStockBatchItem>? parsedItems;
            try
            {
                parsedItems = JsonSerializer.Deserialize<List<AddStockBatchItem>>(itemsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = $"تنسيق قائمة المواد غير صحيح: {ex.Message}" });
            }

            var validItems = (parsedItems ?? new List<AddStockBatchItem>())
                .Where(x => !string.IsNullOrWhiteSpace(x.MaterialName) && x.Quantity > 0)
                .ToList();

            if (!validItems.Any())
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "يجب إدخال مادة واحدة على الأقل مع كمية صحيحة" });

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

            var normalizedBatchReceipt = NormalizeReceiptNumber(receiptNumber);
            var movements = validItems.Select(item => new StockMovement
            {
                MaterialName = item.MaterialName.Trim(),
                MovementType = "Add",
                Quantity = item.Quantity,
                SupplierName = supplierName?.Trim(),
                Amount = item.Amount ?? (item.UnitPrice * item.Quantity),
                UnitType = item.UnitType?.Trim(),
                ReceiptAttachmentPath = receiptPath,
                ReceiptNumber = normalizedBatchReceipt,
                Notes = BuildNotesWithReceiptNumber(receiptNumber, notes),
                InsertByUserId = commercialUserId
            }).ToList();

            _dbConfig.StockMovements.AddRange(movements);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<object>
            {
                Data = new { count = movements.Count },
                ErrorStatus = false,
                Message = "تمت إضافة قائمة المواد إلى المخزن بنجاح"
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

            var receiptKey = NormalizeReceiptNumber(request.ReceiptNumber) ?? "";

            var movements = await _dbConfig.StockMovements
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId && x.MaterialName.Trim() == name)
                .ToListAsync();
            var slotMovements = movements.Where(x => InventoryReceiptKey(x) == receiptKey).ToList();
            var adds = slotMovements.Where(m => m.MovementType == "Add").Sum(m => m.Quantity);
            var withdraws = slotMovements.Where(m => m.MovementType == "Withdraw").Sum(m => m.Quantity);
            var currentBalance = adds - withdraws;

            if (currentBalance < request.Quantity)
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الكمية المتاحة في المخزن غير كافية" });

            var receivedBy = request.ReceivedByEmployeeName?.Trim() ?? "";
            if (string.IsNullOrEmpty(receivedBy))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "يجب تحديد الموظف الذي استلم السحب" });

            var movement = new StockMovement
            {
                MaterialName = name,
                MovementType = "Withdraw",
                Quantity = request.Quantity,
                ReceiptNumber = string.IsNullOrEmpty(receiptKey) ? null : receiptKey,
                Notes = request.Notes?.Trim(),
                ReceivedByEmployeeName = receivedBy,
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

        /// <summary>رابط عام كامل لمرفق الوصل (للاستخدام من الواجهة أو تطبيقات أخرى).</summary>
        private string? BuildReceiptPublicUrl(string? storedFileName)
        {
            if (string.IsNullOrWhiteSpace(storedFileName))
                return null;
            var raw = storedFileName.Trim();
            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return raw;

            var name = Path.GetFileName(raw.Replace('\\', '/'));
            if (string.IsNullOrEmpty(name) || name.Contains("..", StringComparison.Ordinal))
                return null;

            var root = $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
            return $"{root}/Receipts/{Uri.EscapeDataString(name)}";
        }

        private static string? BuildNotesWithReceiptNumber(string? receiptNumber, string? notes)
        {
            var normalizedReceiptNumber = receiptNumber?.Trim();
            var normalizedNotes = notes?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedReceiptNumber))
            {
                return string.IsNullOrWhiteSpace(normalizedNotes) ? null : normalizedNotes;
            }

            if (string.IsNullOrWhiteSpace(normalizedNotes))
            {
                return $"ReceiptNumber:{normalizedReceiptNumber}";
            }

            return $"ReceiptNumber:{normalizedReceiptNumber}\n{normalizedNotes}";
        }

        private static string? ExtractReceiptNumber(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            const string prefix = "ReceiptNumber:";
            if (!notes.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var firstLine = notes.Split('\n', StringSplitOptions.None)[0];
            var number = firstLine.Substring(prefix.Length).Trim();
            return string.IsNullOrWhiteSpace(number) ? null : number;
        }

        private static string? CleanNotesFromReceiptNumber(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            const string prefix = "ReceiptNumber:";
            if (!notes.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return notes;
            }

            var parts = notes.Split('\n', 2, StringSplitOptions.None);
            if (parts.Length < 2)
            {
                return null;
            }

            var cleaned = parts[1].Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        /// <summary>تطبيع رقم الوصل للتخزين (فارغ = null).</summary>
        private static string? NormalizeReceiptNumber(string? receiptNumber)
        {
            if (string.IsNullOrWhiteSpace(receiptNumber))
                return null;
            return receiptNumber.Trim();
        }

        /// <summary>مفتاح دفعة المخزن: اسم المادة نفسه مع رقم وصل مختلف = صف مستقل.</summary>
        private static string InventoryReceiptKey(StockMovement x)
        {
            if (!string.IsNullOrWhiteSpace(x.ReceiptNumber))
                return x.ReceiptNumber.Trim();
            if (x.MovementType == "Add")
                return ExtractReceiptNumber(x.Notes)?.Trim() ?? "";
            return "";
        }

        /// <summary>رقم الوصل للعرض: العمود المخصص أو السطر الأول في الملاحظات.</summary>
        private static string? DisplayReceiptNumber(StockMovement x)
        {
            if (!string.IsNullOrWhiteSpace(x.ReceiptNumber))
                return x.ReceiptNumber.Trim();
            return ExtractReceiptNumber(x.Notes);
        }

        /// <summary>مبلغ العرض في الجدول: الإضافة كما هي؛ السحب يُقدَّر تناسبياً من آخر إضافة لنفس الدفعة عند غياب المبلغ.</summary>
        private static decimal? ResolveDisplayAmount(StockMovement x, StockMovement? enrich)
        {
            if (x.MovementType == "Add")
                return x.Amount;
            if (x.Amount.HasValue && x.Amount.Value != 0)
                return x.Amount;
            if (enrich != null && enrich.Quantity > 0 && enrich.Amount.HasValue && enrich.Amount.Value != 0)
                return Math.Round(enrich.Amount.Value * (x.Quantity / enrich.Quantity), 2, MidpointRounding.AwayFromZero);
            return x.Amount;
        }

        /// <summary>آخر إضافة لنفس الدفعة قبل تاريخ السحب — لعرض مورد/وحدة/مرفق في سجل السحب.</summary>
        private static StockMovement? FindEnrichingAddForWithdraw(StockMovement withdraw, IReadOnlyList<StockMovement> addsPool)
        {
            var name = withdraw.MaterialName.Trim();
            var rk = InventoryReceiptKey(withdraw);
            return addsPool
                .Where(a => a.MaterialName.Trim() == name
                            && InventoryReceiptKey(a) == rk
                            && a.InsertDate <= withdraw.InsertDate)
                .OrderByDescending(a => a.InsertDate)
                .FirstOrDefault();
        }

        private class AddStockBatchItem
        {
            public string MaterialName { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal? Amount { get; set; }
            public string? UnitType { get; set; }
        }
    }
}

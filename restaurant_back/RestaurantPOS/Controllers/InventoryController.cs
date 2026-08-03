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
using System.Text.Json;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    [Authorize]
    [AuthorizeSection("inventory", Roles = "Commercial,POS,Admin")]
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
                    receivedByDepartmentName = x.MovementType == "Withdraw" ? x.ReceivedByEmployeeName : null,
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

        /// <summary>سحب كمية من المخزن لقسم أو قسم فرعي</summary>
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

            var receivedBy = await ResolveWithdrawDepartmentNameAsync(commercialUserId, request);
            if (string.IsNullOrEmpty(receivedBy))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "يجب تحديد القسم أو القسم الفرعي للسحب" });

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

        /// <summary>أقسام السحب (رئيسي وفرعي) ضمن نطاق المستخدم التجاري</summary>
        [HttpGet("GetWithdrawDepartments")]
        public async Task<ActionResult<GlobalResponse<object>>> GetWithdrawDepartments()
        {
            var commercialUserId = GetCommercialUserId();
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var tags = await _dbConfig.Tags
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => !x.IsDeleted && (
                    x.InsertByUserId == commercialUserId ||
                    x.InsertByUserId == userId ||
                    (x.User != null && (x.User.Id == commercialUserId || x.User.InsertByUserId == userId || x.User.InsertByUserId == commercialUserId))
                ))
                .OrderBy(x => x.Name)
                .Select(x => new { id = x.Id, name = x.Name, parentTagId = x.ParentTagId })
                .ToListAsync();

            return Ok(new GlobalResponse<object>
            {
                Data = tags,
                ErrorStatus = false,
                Message = "Success"
            });
        }

        /// <summary>تعديل مادة في دفعة مخزن (اسم / وحدة / إجمالي الداخل)</summary>
        [HttpPut("UpdateStockLine")]
        public async Task<ActionResult<GlobalResponse<object>>> UpdateStockLine([FromBody] UpdateStockLineRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            var originalName = request.MaterialName?.Trim() ?? "";
            var newName = request.NewMaterialName?.Trim() ?? "";
            if (string.IsNullOrEmpty(originalName) || string.IsNullOrEmpty(newName))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "اسم المادة مطلوب" });

            var receiptKey = NormalizeReceiptNumber(request.ReceiptNumber) ?? "";
            var slotMovements = await LoadSlotMovementsAsync(commercialUserId, originalName, receiptKey);
            if (slotMovements.Count == 0)
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "المادة غير موجودة في المخزن" });

            var totalWithdrawn = slotMovements.Where(m => m.MovementType == "Withdraw").Sum(m => m.Quantity);
            var addMovements = slotMovements.Where(m => m.MovementType == "Add").OrderBy(m => m.InsertDate).ToList();
            if (addMovements.Count == 0)
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "لا توجد إدخالات لهذه المادة" });

            if (request.TotalAddedQuantity.HasValue)
            {
                if (request.TotalAddedQuantity.Value < totalWithdrawn)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = $"لا يمكن تقليل الكمية المدخلة عن المسحوب ({totalWithdrawn})"
                    });
                }

                var currentAdded = addMovements.Sum(m => m.Quantity);
                var target = request.TotalAddedQuantity.Value;
                if (target != currentAdded)
                {
                    var lastAdd = addMovements.Last();
                    var othersSum = currentAdded - lastAdd.Quantity;
                    var newLastQty = target - othersSum;
                    if (newLastQty <= 0)
                    {
                        return BadRequest(new GlobalResponse<object>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "تعذر تعديل الكمية بهذه الطريقة؛ راجع حركات الإضافة"
                        });
                    }

                    if (lastAdd.Amount.HasValue && lastAdd.Quantity > 0)
                    {
                        var unit = lastAdd.Amount.Value / lastAdd.Quantity;
                        lastAdd.Amount = Math.Round(unit * newLastQty, 2, MidpointRounding.AwayFromZero);
                    }
                    lastAdd.Quantity = newLastQty;
                    lastAdd.UpdateDate = DateTime.Now;
                }
            }

            var renamed = !string.Equals(originalName, newName, StringComparison.Ordinal);
            if (renamed)
            {
                var conflict = await _dbConfig.StockMovements
                    .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId && x.MaterialName.Trim() == newName)
                    .ToListAsync();
                if (conflict.Any(x => InventoryReceiptKey(x) == receiptKey))
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يوجد مادة بنفس الاسم ورقم الوصل بالفعل"
                    });
                }
            }

            foreach (var m in slotMovements)
            {
                if (renamed)
                    m.MaterialName = newName;
                if (m.MovementType == "Add" && request.UnitType != null)
                    m.UnitType = string.IsNullOrWhiteSpace(request.UnitType) ? null : request.UnitType.Trim();
                m.UpdateDate = DateTime.Now;
            }

            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<object> { Data = null, ErrorStatus = false, Message = "تم تعديل المادة بنجاح" });
        }

        /// <summary>حذف مادة من المخزن (كل حركات الدفعة)</summary>
        [HttpPost("DeleteStockLine")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteStockLine([FromBody] DeleteStockLineRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            var name = request.MaterialName?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "اسم المادة مطلوب" });

            var receiptKey = NormalizeReceiptNumber(request.ReceiptNumber) ?? "";
            var slotMovements = await LoadSlotMovementsAsync(commercialUserId, name, receiptKey);
            if (slotMovements.Count == 0)
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "المادة غير موجودة في المخزن" });

            foreach (var m in slotMovements)
            {
                m.IsDeleted = true;
                m.UpdateDate = DateTime.Now;
            }

            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<object> { Data = null, ErrorStatus = false, Message = "تم حذف المادة من المخزن بنجاح" });
        }

        /// <summary>جلب فاتورة مخزون كاملة برقم الوصل</summary>
        [HttpGet("GetStockInvoice")]
        public async Task<ActionResult<GlobalResponse<object>>> GetStockInvoice([FromQuery] string receiptNumber)
        {
            var commercialUserId = GetCommercialUserId();
            var receiptKey = NormalizeReceiptNumber(receiptNumber);
            if (string.IsNullOrEmpty(receiptKey))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "رقم الوصل مطلوب" });

            var movements = await _dbConfig.StockMovements
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId)
                .ToListAsync();
            var invoiceMoves = movements.Where(x => InventoryReceiptKey(x) == receiptKey).ToList();
            if (invoiceMoves.Count == 0)
                return Ok(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الفاتورة غير موجودة" });

            var adds = invoiceMoves.Where(m => m.MovementType == "Add").OrderBy(m => m.InsertDate).ToList();
            if (adds.Count == 0)
                return Ok(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "لا توجد مواد إدخال لهذه الفاتورة" });

            var firstAdd = adds.First();
            var latestAttach = adds
                .Where(m => !string.IsNullOrWhiteSpace(m.ReceiptAttachmentPath))
                .OrderByDescending(m => m.InsertDate)
                .FirstOrDefault();

            var items = adds.Select(a =>
            {
                var withdrawn = invoiceMoves
                    .Where(w => w.MovementType == "Withdraw" && w.MaterialName.Trim() == a.MaterialName.Trim())
                    .Sum(w => w.Quantity);
                var unitPrice = a.Quantity > 0 && a.Amount.HasValue
                    ? Math.Round(a.Amount.Value / a.Quantity, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                return new
                {
                    id = a.Id,
                    materialName = a.MaterialName,
                    quantity = a.Quantity,
                    amount = a.Amount,
                    unitPrice,
                    unitType = a.UnitType,
                    withdrawnQuantity = withdrawn,
                    canDelete = withdrawn <= 0
                };
            }).ToList();

            return Ok(new GlobalResponse<object>
            {
                Data = new
                {
                    receiptNumber = receiptKey,
                    supplierName = firstAdd.SupplierName,
                    notes = CleanNotesFromReceiptNumber(firstAdd.Notes),
                    receiptAttachmentPath = BuildReceiptPublicUrl(latestAttach?.ReceiptAttachmentPath),
                    receiptFileName = latestAttach?.ReceiptAttachmentPath,
                    items
                },
                ErrorStatus = false,
                Message = "Success"
            });
        }

        /// <summary>تعديل فاتورة مخزون كاملة (رأس + أسطر)</summary>
        [HttpPut("UpdateStockBatch")]
        public async Task<ActionResult<GlobalResponse<object>>> UpdateStockBatch(
            [FromForm] string originalReceiptNumber,
            [FromForm] string? supplierName,
            [FromForm] string? receiptNumber,
            [FromForm] string? notes,
            [FromForm] string itemsJson,
            [FromForm] IFormFile? receiptFile)
        {
            var commercialUserId = GetCommercialUserId();
            var originalKey = NormalizeReceiptNumber(originalReceiptNumber);
            if (string.IsNullOrEmpty(originalKey))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "رقم الوصل الأصلي مطلوب" });

            if (string.IsNullOrWhiteSpace(itemsJson))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "قائمة المواد مطلوبة" });

            List<UpdateStockBatchItem>? parsedItems;
            try
            {
                parsedItems = JsonSerializer.Deserialize<List<UpdateStockBatchItem>>(itemsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = $"تنسيق قائمة المواد غير صحيح: {ex.Message}" });
            }

            var validItems = (parsedItems ?? new List<UpdateStockBatchItem>())
                .Where(x => !string.IsNullOrWhiteSpace(x.MaterialName) && x.Quantity > 0)
                .ToList();
            if (!validItems.Any())
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "يجب إدخال مادة واحدة على الأقل مع كمية صحيحة" });

            var allMoves = await _dbConfig.StockMovements
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId)
                .ToListAsync();
            var invoiceMoves = allMoves.Where(x => InventoryReceiptKey(x) == originalKey).ToList();
            if (invoiceMoves.Count == 0)
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الفاتورة غير موجودة" });

            var existingAdds = invoiceMoves.Where(m => m.MovementType == "Add").ToList();
            var newKey = NormalizeReceiptNumber(receiptNumber) ?? originalKey;

            if (!string.Equals(newKey, originalKey, StringComparison.Ordinal))
            {
                if (allMoves.Any(x => InventoryReceiptKey(x) == newKey && !invoiceMoves.Contains(x)))
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رقم الوصل الجديد مستخدم بالفعل في فاتورة أخرى"
                    });
                }
            }

            foreach (var item in validItems)
            {
                var mat = item.MaterialName.Trim();
                var withdrawn = invoiceMoves
                    .Where(w => w.MovementType == "Withdraw" && w.MaterialName.Trim() == mat)
                    .Sum(w => w.Quantity);
                if (item.Id.HasValue)
                {
                    var add = existingAdds.FirstOrDefault(a => a.Id == item.Id.Value);
                    if (add != null)
                    {
                        var withdrawnForOld = invoiceMoves
                            .Where(w => w.MovementType == "Withdraw" && w.MaterialName.Trim() == add.MaterialName.Trim())
                            .Sum(w => w.Quantity);
                        if (item.Quantity < withdrawnForOld)
                        {
                            return BadRequest(new GlobalResponse<object>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"كمية المادة «{add.MaterialName}» لا يمكن أن تقل عن المسحوب ({withdrawnForOld})"
                            });
                        }
                    }
                }
                else if (item.Quantity < withdrawn && existingAdds.Any(a => a.MaterialName.Trim() == mat))
                {
                    // new row replacing same name — still check
                    if (item.Quantity < withdrawn)
                    {
                        return BadRequest(new GlobalResponse<object>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = $"كمية المادة «{mat}» لا يمكن أن تقل عن المسحوب ({withdrawn})"
                        });
                    }
                }
            }

            var keepIds = validItems.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
            foreach (var add in existingAdds)
            {
                if (keepIds.Contains(add.Id)) continue;
                var withdrawn = invoiceMoves
                    .Where(w => w.MovementType == "Withdraw" && w.MaterialName.Trim() == add.MaterialName.Trim())
                    .Sum(w => w.Quantity);
                if (withdrawn > 0)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = $"لا يمكن حذف المادة «{add.MaterialName}» لوجود سحوبات عليها"
                    });
                }
                add.IsDeleted = true;
                add.UpdateDate = DateTime.Now;
            }

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

            var supplier = supplierName?.Trim();
            var builtNotes = BuildNotesWithReceiptNumber(newKey, notes);
            var existingAttachment = existingAdds
                .Where(a => !a.IsDeleted && !string.IsNullOrWhiteSpace(a.ReceiptAttachmentPath))
                .OrderByDescending(a => a.InsertDate)
                .Select(a => a.ReceiptAttachmentPath)
                .FirstOrDefault();
            var attachmentToUse = receiptPath ?? existingAttachment;

            foreach (var item in validItems)
            {
                var amount = item.Amount ?? (item.UnitPrice * item.Quantity);
                if (item.Id.HasValue)
                {
                    var add = existingAdds.FirstOrDefault(a => a.Id == item.Id.Value && !a.IsDeleted);
                    if (add == null) continue;

                    var oldName = add.MaterialName.Trim();
                    var newMatName = item.MaterialName.Trim();
                    add.MaterialName = newMatName;
                    add.Quantity = item.Quantity;
                    add.Amount = amount;
                    add.UnitType = item.UnitType?.Trim();
                    add.SupplierName = supplier;
                    add.ReceiptNumber = newKey;
                    add.Notes = builtNotes;
                    if (attachmentToUse != null)
                        add.ReceiptAttachmentPath = attachmentToUse;
                    add.UpdateDate = DateTime.Now;

                    if (!string.Equals(oldName, newMatName, StringComparison.Ordinal))
                    {
                        foreach (var w in invoiceMoves.Where(x => x.MovementType == "Withdraw" && x.MaterialName.Trim() == oldName))
                        {
                            w.MaterialName = newMatName;
                            w.ReceiptNumber = newKey;
                            w.UpdateDate = DateTime.Now;
                        }
                    }
                    else
                    {
                        foreach (var w in invoiceMoves.Where(x => x.MovementType == "Withdraw" && x.MaterialName.Trim() == newMatName))
                        {
                            w.ReceiptNumber = newKey;
                            w.UpdateDate = DateTime.Now;
                        }
                    }
                }
                else
                {
                    _dbConfig.StockMovements.Add(new StockMovement
                    {
                        MaterialName = item.MaterialName.Trim(),
                        MovementType = "Add",
                        Quantity = item.Quantity,
                        SupplierName = supplier,
                        Amount = amount,
                        UnitType = item.UnitType?.Trim(),
                        ReceiptAttachmentPath = attachmentToUse,
                        ReceiptNumber = newKey,
                        Notes = builtNotes,
                        InsertByUserId = commercialUserId
                    });
                }
            }

            // تحديث رقم الوصل على أي سحوبات متبقية لم تُحدَّث أعلاه
            foreach (var w in invoiceMoves.Where(x => x.MovementType == "Withdraw" && !x.IsDeleted))
            {
                w.ReceiptNumber = newKey;
                w.UpdateDate = DateTime.Now;
            }

            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<object> { Data = new { receiptNumber = newKey }, ErrorStatus = false, Message = "تم تعديل الفاتورة بنجاح" });
        }

        /// <summary>حذف فاتورة مخزون كاملة برقم الوصل</summary>
        [HttpDelete("DeleteStockInvoice")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteStockInvoice([FromQuery] string receiptNumber)
        {
            var commercialUserId = GetCommercialUserId();
            var receiptKey = NormalizeReceiptNumber(receiptNumber);
            if (string.IsNullOrEmpty(receiptKey))
                return BadRequest(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "رقم الوصل مطلوب" });

            var movements = await _dbConfig.StockMovements
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId)
                .ToListAsync();
            var invoiceMoves = movements.Where(x => InventoryReceiptKey(x) == receiptKey).ToList();
            if (invoiceMoves.Count == 0)
                return Ok(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "الفاتورة غير موجودة" });

            foreach (var m in invoiceMoves)
            {
                m.IsDeleted = true;
                m.UpdateDate = DateTime.Now;
            }

            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<object>
            {
                Data = new { deletedCount = invoiceMoves.Count },
                ErrorStatus = false,
                Message = "تم حذف الفاتورة بالكامل بنجاح"
            });
        }

        private async Task<List<StockMovement>> LoadSlotMovementsAsync(int commercialUserId, string materialName, string receiptKey)
        {
            var movements = await _dbConfig.StockMovements
                .Where(x => !x.IsDeleted && x.InsertByUserId == commercialUserId && x.MaterialName.Trim() == materialName)
                .ToListAsync();
            return movements.Where(x => InventoryReceiptKey(x) == receiptKey).ToList();
        }

        private async Task<string?> ResolveWithdrawDepartmentNameAsync(int commercialUserId, WithdrawStockRequest request)
        {
            if (request.TagId.HasValue && request.TagId.Value > 0)
            {
                var tag = await _dbConfig.Tags.AsNoTracking()
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == request.TagId.Value && !t.IsDeleted && (
                        t.InsertByUserId == commercialUserId ||
                        (t.User != null && (t.User.Id == commercialUserId || t.User.InsertByUserId == commercialUserId))
                    ));
                if (tag == null)
                    return null;

                if (tag.ParentTagId.HasValue)
                {
                    var parent = await _dbConfig.Tags.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == tag.ParentTagId.Value && !t.IsDeleted);
                    if (parent != null && !string.IsNullOrWhiteSpace(parent.Name))
                        return $"{parent.Name.Trim()} › {tag.Name?.Trim()}".Trim();
                }

                return tag.Name?.Trim();
            }

            var direct = request.ReceivedByDepartmentName?.Trim()
                ?? request.ReceivedByEmployeeName?.Trim();
            return string.IsNullOrWhiteSpace(direct) ? null : direct;
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

        private class UpdateStockBatchItem
        {
            public int? Id { get; set; }
            public string MaterialName { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal? Amount { get; set; }
            public string? UnitType { get; set; }
        }
    }
}

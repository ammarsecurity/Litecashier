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
    public class TagPrintersController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<TagPrintersController> _logger;

        public TagPrintersController(ILogger<TagPrintersController> logger, DbConfig dbConfig)
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

        // GET: api/TagPrinters
        [AuthorizeSection("printServer", Roles = "Commercial,Admin,POS,POS")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<TagPrinter>>>> GetTagPrinters()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var tagPrinters = await _dbConfig.TagPrinters
                    .Include(tp => tp.Tag)
                    .Include(tp => tp.Printer)
                    .Where(tp => !tp.IsDeleted && tp.InsertByUserId == commercialUserId)
                    .OrderBy(tp => tp.Tag != null ? tp.Tag.Name : "")
                    .ToListAsync();

                return Ok(new GlobalResponse<List<TagPrinter>>
                {
                    Data = tagPrinters,
                    ErrorStatus = false,
                    Message = "تم جلب إعدادات طباعة الأقسام بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag printers");
                return StatusCode(500, new GlobalResponse<List<TagPrinter>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب إعدادات طباعة الأقسام: {ex.Message}"
                });
            }
        }

        // GET: api/TagPrinters/{id}
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<TagPrinter>>> GetTagPrinter(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var tagPrinter = await _dbConfig.TagPrinters
                    .Include(tp => tp.Tag)
                    .Include(tp => tp.Printer)
                    .FirstOrDefaultAsync(tp => tp.Id == id && !tp.IsDeleted && tp.InsertByUserId == commercialUserId);

                if (tagPrinter == null)
                {
                    return NotFound(new GlobalResponse<TagPrinter>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "إعدادات طباعة القسم غير موجودة"
                    });
                }

                return Ok(new GlobalResponse<TagPrinter>
                {
                    Data = tagPrinter,
                    ErrorStatus = false,
                    Message = "تم جلب إعدادات طباعة القسم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag printer {TagPrinterId}", id);
                return StatusCode(500, new GlobalResponse<TagPrinter>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب إعدادات طباعة القسم: {ex.Message}"
                });
            }
        }

        // POST: api/TagPrinters
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<TagPrinter>>> AddTagPrinter([FromBody] TagPrinterRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var commercialUserId = GetCommercialUserId();

                // Validate Tag exists
                var tag = await _dbConfig.Tags
                    .FirstOrDefaultAsync(t => t.Id == request.TagId && !t.IsDeleted);
                
                if (tag == null)
                {
                    return BadRequest(new GlobalResponse<TagPrinter>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم غير موجود"
                    });
                }

                // Validate Printer exists and belongs to user
                var printer = await _dbConfig.Printers
                    .FirstOrDefaultAsync(p => p.Id == request.PrinterId && !p.IsDeleted && p.InsertByUserId == commercialUserId);
                
                if (printer == null)
                {
                    return BadRequest(new GlobalResponse<TagPrinter>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطابعة غير موجودة أو غير مسموح لك بالوصول إليها"
                    });
                }

                // Check if TagPrinter already exists for this tag and printer
                var existingTagPrinter = await _dbConfig.TagPrinters
                    .FirstOrDefaultAsync(tp => tp.TagId == request.TagId 
                        && tp.PrinterId == request.PrinterId 
                        && !tp.IsDeleted 
                        && tp.InsertByUserId == commercialUserId);

                if (existingTagPrinter != null)
                {
                    return BadRequest(new GlobalResponse<TagPrinter>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "هذا القسم مرتبط بالفعل بهذه الطابعة"
                    });
                }

                var tagPrinter = new TagPrinter
                {
                    TagId = request.TagId,
                    PrinterId = request.PrinterId,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _dbConfig.TagPrinters.Add(tagPrinter);
                await _dbConfig.SaveChangesAsync();

                // Load related entities for response
                await _dbConfig.Entry(tagPrinter)
                    .Reference(tp => tp.Tag)
                    .LoadAsync();
                await _dbConfig.Entry(tagPrinter)
                    .Reference(tp => tp.Printer)
                    .LoadAsync();

                return Ok(new GlobalResponse<TagPrinter>
                {
                    Data = tagPrinter,
                    ErrorStatus = false,
                    Message = "تم إضافة إعدادات طباعة القسم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tag printer: {Exception}", ex);
                return StatusCode(500, new GlobalResponse<TagPrinter>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة إعدادات طباعة القسم: {ex.Message}"
                });
            }
        }

        // PUT: api/TagPrinters/{id}
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<TagPrinter>>> UpdateTagPrinter(int id, [FromBody] TagPrinterRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var tagPrinter = await _dbConfig.TagPrinters
                    .FirstOrDefaultAsync(tp => tp.Id == id && !tp.IsDeleted && tp.InsertByUserId == commercialUserId);

                if (tagPrinter == null)
                {
                    return NotFound(new GlobalResponse<TagPrinter>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "إعدادات طباعة القسم غير موجودة"
                    });
                }

                // Validate Tag exists
                var tag = await _dbConfig.Tags
                    .FirstOrDefaultAsync(t => t.Id == request.TagId && !t.IsDeleted);
                
                if (tag == null)
                {
                    return BadRequest(new GlobalResponse<TagPrinter>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم غير موجود"
                    });
                }

                // Validate Printer exists and belongs to user
                var printer = await _dbConfig.Printers
                    .FirstOrDefaultAsync(p => p.Id == request.PrinterId && !p.IsDeleted && p.InsertByUserId == commercialUserId);
                
                if (printer == null)
                {
                    return BadRequest(new GlobalResponse<TagPrinter>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطابعة غير موجودة أو غير مسموح لك بالوصول إليها"
                    });
                }

                // Check if another TagPrinter already exists for this tag and printer
                var existingTagPrinter = await _dbConfig.TagPrinters
                    .FirstOrDefaultAsync(tp => tp.TagId == request.TagId 
                        && tp.PrinterId == request.PrinterId 
                        && tp.Id != id
                        && !tp.IsDeleted 
                        && tp.InsertByUserId == commercialUserId);

                if (existingTagPrinter != null)
                {
                    return BadRequest(new GlobalResponse<TagPrinter>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "هذا القسم مرتبط بالفعل بهذه الطابعة"
                    });
                }

                // Store old values for audit log
                var oldTagId = tagPrinter.TagId;
                var oldPrinterId = tagPrinter.PrinterId;
                var oldValues = new
                {
                    TagId = oldTagId,
                    PrinterId = oldPrinterId
                };

                tagPrinter.TagId = request.TagId;
                tagPrinter.PrinterId = request.PrinterId;
                tagPrinter.UpdateDate = DateTime.UtcNow;

                // Store new values for audit log
                var newValues = new
                {
                    TagId = tagPrinter.TagId,
                    PrinterId = tagPrinter.PrinterId
                };

                _dbConfig.TagPrinters.Update(tagPrinter);
                await _dbConfig.SaveChangesAsync();

                // Load related entities for response
                await _dbConfig.Entry(tagPrinter)
                    .Reference(tp => tp.Tag)
                    .LoadAsync();
                await _dbConfig.Entry(tagPrinter)
                    .Reference(tp => tp.Printer)
                    .LoadAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Update",
                    "TagPrinter",
                    tagPrinter.Id,
                    $"ربط قسم {tagPrinter.Tag?.Name} بطابعة {tagPrinter.Printer?.Name}",
                    userId,
                    commercialUserId,
                    oldValues,
                    newValues,
                    $"تم تعديل ربط القسم بالطابعة: {tagPrinter.Tag?.Name} → {tagPrinter.Printer?.Name}"
                );

                return Ok(new GlobalResponse<TagPrinter>
                {
                    Data = tagPrinter,
                    ErrorStatus = false,
                    Message = "تم تحديث إعدادات طباعة القسم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag printer {TagPrinterId}", id);
                return StatusCode(500, new GlobalResponse<TagPrinter>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث إعدادات طباعة القسم: {ex.Message}"
                });
            }
        }

        // DELETE: api/TagPrinters/{id}
        [AuthorizeSection("printServer", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteTagPrinter(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                var tagPrinter = await _dbConfig.TagPrinters
                    .FirstOrDefaultAsync(tp => tp.Id == id && !tp.IsDeleted && tp.InsertByUserId == commercialUserId);

                if (tagPrinter == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "إعدادات طباعة القسم غير موجودة"
                    });
                }

                // Load related entities for audit log
                await _dbConfig.Entry(tagPrinter)
                    .Reference(tp => tp.Tag)
                    .LoadAsync();
                await _dbConfig.Entry(tagPrinter)
                    .Reference(tp => tp.Printer)
                    .LoadAsync();

                var tagPrinterName = $"ربط قسم {tagPrinter.Tag?.Name} بطابعة {tagPrinter.Printer?.Name}";
                tagPrinter.IsDeleted = true;
                tagPrinter.UpdateDate = DateTime.UtcNow;
                _dbConfig.TagPrinters.Update(tagPrinter);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _dbConfig.LogAuditAsync(
                    "Delete",
                    "TagPrinter",
                    tagPrinter.Id,
                    tagPrinterName,
                    userId,
                    commercialUserId,
                    null,
                    null,
                    $"تم حذف ربط القسم بالطابعة: {tagPrinter.Tag?.Name} → {tagPrinter.Printer?.Name}"
                );

                return Ok(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "تم حذف إعدادات طباعة القسم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag printer {TagPrinterId}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حذف إعدادات طباعة القسم: {ex.Message}"
                });
            }
        }
    }
}

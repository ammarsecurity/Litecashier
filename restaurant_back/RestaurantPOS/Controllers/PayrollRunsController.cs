using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Services;
using System.Security.Claims;
using System.Text;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class PayrollRunsController : ControllerBase
    {
        private readonly DbConfig _db;
        private readonly PayrollService _payroll;
        private readonly ILogger<PayrollRunsController> _logger;

        public PayrollRunsController(DbConfig db, PayrollService payroll, ILogger<PayrollRunsController> logger)
        {
            _db = db;
            _payroll = payroll;
            _logger = logger;
        }

        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _db.Users.FirstOrDefault(x => x.Id == userId);
            if (user != null && user.Role == "Commercial") return userId;
            var commercialId = user?.InsertByUserId ?? userId;
            return commercialId == 0 ? userId : commercialId;
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<PayrollRun>>>> List()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var runs = await _db.PayrollRuns
                    .Where(r => !r.IsDeleted && r.InsertByUserId == commercialUserId)
                    .OrderByDescending(r => r.Year)
                    .ThenByDescending(r => r.Month)
                    .ToListAsync();

                return Ok(new GlobalResponse<List<PayrollRun>>
                {
                    Data = runs,
                    ErrorStatus = false,
                    Message = "تم جلب دورات الرواتب"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing payroll runs");
                return StatusCode(500, new GlobalResponse<List<PayrollRun>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> Get(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await _db.PayrollRuns
                    .Include(r => r.Lines!.Where(l => !l.IsDeleted))
                    .ThenInclude(l => l.Employee)
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);

                if (run == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = run,
                    ErrorStatus = false,
                    Message = "تم جلب الدورة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payroll run {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<object>>> Create([FromBody] CreatePayrollRunRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                if (request.Month < 1 || request.Month > 12 || request.Year < 2000)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "سنة/شهر غير صالح"
                    });
                }

                var exists = await _db.PayrollRuns.AnyAsync(r =>
                    !r.IsDeleted
                    && r.InsertByUserId == commercialUserId
                    && r.Year == request.Year
                    && r.Month == request.Month
                    && r.Status != PayrollRunStatus.Cancelled);

                if (exists)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "توجد دورة رواتب لهذا الشهر مسبقاً"
                    });
                }

                var (start, end) = PayrollService.GetPeriodBounds(request.Year, request.Month);
                var run = new PayrollRun
                {
                    Year = request.Year,
                    Month = request.Month,
                    Status = PayrollRunStatus.Draft,
                    PeriodStart = start,
                    PeriodEnd = end,
                    Notes = request.Notes?.Trim(),
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };
                _db.PayrollRuns.Add(run);
                await _db.SaveChangesAsync();

                await GenerateLinesInternalAsync(run, commercialUserId);

                var created = await LoadRunAsync(run.Id, commercialUserId);
                return Ok(new GlobalResponse<object>
                {
                    Data = created,
                    ErrorStatus = false,
                    Message = "تم إنشاء دورة الرواتب"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payroll run");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost("{id}/regenerate")]
        public async Task<ActionResult<GlobalResponse<object>>> Regenerate(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await _db.PayrollRuns
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);
                if (run == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }
                if (run.Status != PayrollRunStatus.Draft)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "إعادة التوليد متاحة للمسودة فقط"
                    });
                }

                var oldLines = await _db.PayrollLines
                    .Where(l => l.PayrollRunId == id && !l.IsDeleted)
                    .ToListAsync();
                foreach (var l in oldLines)
                {
                    l.IsDeleted = true;
                    l.UpdateDate = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync();

                await GenerateLinesInternalAsync(run, commercialUserId);
                var refreshed = await LoadRunAsync(id, commercialUserId);
                return Ok(new GlobalResponse<object>
                {
                    Data = refreshed,
                    ErrorStatus = false,
                    Message = "تمت إعادة توليد الأسطر"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating payroll {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPut("{runId}/lines/{lineId}")]
        public async Task<ActionResult<GlobalResponse<PayrollLine>>> UpdateLine(
            int runId,
            int lineId,
            [FromBody] UpdatePayrollLineRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await _db.PayrollRuns
                    .FirstOrDefaultAsync(r => r.Id == runId && !r.IsDeleted && r.InsertByUserId == commercialUserId);
                if (run == null)
                {
                    return NotFound(new GlobalResponse<PayrollLine>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }
                if (run.Status != PayrollRunStatus.Draft)
                {
                    return BadRequest(new GlobalResponse<PayrollLine>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "تعديل الأسطر متاح في المسودة فقط"
                    });
                }

                var line = await _db.PayrollLines
                    .Include(l => l.Employee)
                    .FirstOrDefaultAsync(l => l.Id == lineId && l.PayrollRunId == runId && !l.IsDeleted);
                if (line == null)
                {
                    return NotFound(new GlobalResponse<PayrollLine>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "سطر الراتب غير موجود"
                    });
                }

                if (request.WorkDays.HasValue) line.WorkDays = request.WorkDays.Value;
                if (request.BaseAmount.HasValue) line.BaseAmount = PayrollService.Round2(request.BaseAmount.Value);
                if (request.OvertimeAmount.HasValue) line.OvertimeAmount = PayrollService.Round2(request.OvertimeAmount.Value);
                if (request.DeductionAmount.HasValue) line.DeductionAmount = PayrollService.Round2(request.DeductionAmount.Value);
                if (request.AbsenceAmount.HasValue) line.AbsenceAmount = PayrollService.Round2(request.AbsenceAmount.Value);
                if (request.AdvanceDeducted.HasValue) line.AdvanceDeducted = PayrollService.Round2(request.AdvanceDeducted.Value);
                if (request.Notes != null) line.Notes = request.Notes.Trim();

                PayrollService.RecalculateNet(line);
                line.UpdateDate = DateTime.UtcNow;
                run.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new GlobalResponse<PayrollLine>
                {
                    Data = line,
                    ErrorStatus = false,
                    Message = "تم تحديث السطر"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payroll line");
                return StatusCode(500, new GlobalResponse<PayrollLine>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost("{id}/approve")]
        public async Task<ActionResult<GlobalResponse<object>>> Approve(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await _db.PayrollRuns
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);
                if (run == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }
                if (run.Status != PayrollRunStatus.Draft)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يمكن اعتماد المسودة فقط"
                    });
                }

                run.Status = PayrollRunStatus.Approved;
                run.ApprovedAt = DateTime.UtcNow;
                run.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = await LoadRunAsync(id, commercialUserId),
                    ErrorStatus = false,
                    Message = "تم اعتماد الدورة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving payroll {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost("{id}/unapprove")]
        public async Task<ActionResult<GlobalResponse<object>>> Unapprove(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await _db.PayrollRuns
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);
                if (run == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }
                if (run.Status != PayrollRunStatus.Approved)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "التراجع متاح قبل الدفع فقط"
                    });
                }

                run.Status = PayrollRunStatus.Draft;
                run.ApprovedAt = null;
                run.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = await LoadRunAsync(id, commercialUserId),
                    ErrorStatus = false,
                    Message = "تم إرجاع الدورة لمسودة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unapproving payroll {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost("{runId}/lines/{lineId}/handover")]
        public async Task<ActionResult<GlobalResponse<object>>> HandoverLine(int runId, int lineId)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await _db.PayrollRuns
                    .FirstOrDefaultAsync(r => r.Id == runId && !r.IsDeleted && r.InsertByUserId == commercialUserId);
                if (run == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }
                if (run.Status != PayrollRunStatus.Paid)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "التسليم متاح بعد صرف الدورة فقط"
                    });
                }

                var line = await _db.PayrollLines
                    .Include(l => l.Employee)
                    .FirstOrDefaultAsync(l => l.Id == lineId && l.PayrollRunId == runId && !l.IsDeleted);
                if (line == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "سطر الراتب غير موجود"
                    });
                }

                if (!line.IsHandedOver)
                {
                    line.IsHandedOver = true;
                    line.HandedOverAt = DateTime.UtcNow;
                    line.UpdateDate = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = new
                    {
                        line,
                        run = new { run.Id, run.Year, run.Month, run.Status, run.PaidAt }
                    },
                    ErrorStatus = false,
                    Message = line.IsHandedOver ? "تم تسجيل التسليم" : "تم التسليم"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handing over payroll line");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", "reports", Roles = "Commercial,Admin")]
        [HttpGet("handovers")]
        public async Task<ActionResult<GlobalResponse<object>>> ListHandovers(
            int? year = null,
            int? month = null,
            int? employeeId = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var query = _db.PayrollLines
                    .Include(l => l.Employee)
                    .Include(l => l.PayrollRun)
                    .Where(l => !l.IsDeleted
                        && l.IsHandedOver
                        && l.PayrollRun != null
                        && !l.PayrollRun.IsDeleted
                        && l.PayrollRun.InsertByUserId == commercialUserId);

                if (year.HasValue) query = query.Where(l => l.PayrollRun!.Year == year.Value);
                if (month.HasValue) query = query.Where(l => l.PayrollRun!.Month == month.Value);
                if (employeeId.HasValue) query = query.Where(l => l.EmployeeId == employeeId.Value);

                var list = await query
                    .OrderByDescending(l => l.HandedOverAt)
                    .ThenByDescending(l => l.Id)
                    .ToListAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = new
                    {
                        count = list.Count,
                        totalNet = list.Sum(l => l.NetAmount),
                        items = list
                    },
                    ErrorStatus = false,
                    Message = "تسليمات الرواتب"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing payroll handovers");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost("{id}/pay")]
        public async Task<ActionResult<GlobalResponse<object>>> Pay(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await _db.PayrollRuns
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);
                if (run == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }
                if (run.Status != PayrollRunStatus.Approved)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يجب اعتماد الدورة قبل الدفع"
                    });
                }

                var lines = await _db.PayrollLines
                    .Where(l => l.PayrollRunId == id && !l.IsDeleted)
                    .ToListAsync();

                await using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    await _payroll.ApplyAdvanceDeductionsAsync(commercialUserId, lines);
                    run.Status = PayrollRunStatus.Paid;
                    run.PaidAt = DateTime.UtcNow;
                    run.UpdateDate = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    await _payroll.CreateSalaryExpensesAsync(commercialUserId, run, lines);
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = await LoadRunAsync(id, commercialUserId),
                    ErrorStatus = false,
                    Message = "تم صرف الرواتب وخصم السلف"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error paying payroll {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", Roles = "Commercial,Admin")]
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<GlobalResponse<object>>> Cancel(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await _db.PayrollRuns
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);
                if (run == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }
                if (run.Status == PayrollRunStatus.Paid)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لا يمكن إلغاء دورة مدفوعة"
                    });
                }

                run.Status = PayrollRunStatus.Cancelled;
                run.UpdateDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = await LoadRunAsync(id, commercialUserId),
                    ErrorStatus = false,
                    Message = "تم إلغاء الدورة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payroll {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", "reports", Roles = "Commercial,Admin")]
        [HttpGet("{id}/report")]
        public async Task<ActionResult<GlobalResponse<object>>> RunReport(int id)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var run = await LoadRunAsync(id, commercialUserId);
                if (run == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "دورة الرواتب غير موجودة"
                    });
                }

                var lines = (run.Lines ?? new List<PayrollLine>()).Where(l => !l.IsDeleted).ToList();
                var summary = new
                {
                    run.Id,
                    run.Year,
                    run.Month,
                    run.Status,
                    run.PeriodStart,
                    run.PeriodEnd,
                    run.ApprovedAt,
                    run.PaidAt,
                    employeeCount = lines.Count,
                    totalBase = lines.Sum(l => l.BaseAmount),
                    totalOvertime = lines.Sum(l => l.OvertimeAmount),
                    totalDeductions = lines.Sum(l => l.DeductionAmount),
                    totalAbsence = lines.Sum(l => l.AbsenceAmount),
                    totalAdvanceDeducted = lines.Sum(l => l.AdvanceDeducted),
                    totalNet = lines.Sum(l => l.NetAmount),
                    lines
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = summary,
                    ErrorStatus = false,
                    Message = "تقرير الدورة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error payroll report {Id}", id);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", "reports", Roles = "Commercial,Admin")]
        [HttpGet("reports/employee/{employeeId}")]
        public async Task<ActionResult<GlobalResponse<object>>> EmployeeLedger(int employeeId)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var employee = await _db.Employees.FirstOrDefaultAsync(e =>
                    e.Id == employeeId && !e.IsDeleted && e.InsertByUserId == commercialUserId);
                if (employee == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الموظف غير موجود"
                    });
                }

                var advances = await _db.EmployeeAdvances
                    .Where(a => !a.IsDeleted && a.InsertByUserId == commercialUserId && a.EmployeeId == employeeId)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                var adjustments = await _db.SalaryAdjustments
                    .Where(a => !a.IsDeleted && a.InsertByUserId == commercialUserId && a.EmployeeId == employeeId)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                var lines = await _db.PayrollLines
                    .Include(l => l.PayrollRun)
                    .Where(l => !l.IsDeleted
                        && l.EmployeeId == employeeId
                        && l.PayrollRun != null
                        && !l.PayrollRun.IsDeleted
                        && l.PayrollRun.InsertByUserId == commercialUserId)
                    .OrderByDescending(l => l.PayrollRun!.Year)
                    .ThenByDescending(l => l.PayrollRun!.Month)
                    .ToListAsync();

                return Ok(new GlobalResponse<object>
                {
                    Data = new
                    {
                        employee,
                        openAdvanceBalance = advances.Where(a => !a.IsClosed).Sum(a => a.RemainingAmount),
                        advances,
                        adjustments,
                        payrollLines = lines
                    },
                    ErrorStatus = false,
                    Message = "كشف الموظف"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error employee payroll ledger {EmployeeId}", employeeId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [AuthorizeSection("payroll", "employees", "reports", Roles = "Commercial,Admin")]
        [HttpGet("{id}/export")]
        public async Task<IActionResult> ExportCsv(int id)
        {
            var commercialUserId = GetCommercialUserId();
            var run = await LoadRunAsync(id, commercialUserId);
            if (run == null) return NotFound();

            var sb = new StringBuilder();
            sb.AppendLine("Employee,Base,Overtime,Deduction,Absence,Advance,Net");
            foreach (var l in (run.Lines ?? new List<PayrollLine>()).Where(x => !x.IsDeleted))
            {
                var name = (l.Employee?.Name ?? $"#{l.EmployeeId}").Replace(",", " ");
                sb.AppendLine($"{name},{l.BaseAmount},{l.OvertimeAmount},{l.DeductionAmount},{l.AbsenceAmount},{l.AdvanceDeducted},{l.NetAmount}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"payroll-{run.Year}-{run.Month:D2}.csv");
        }

        private async Task GenerateLinesInternalAsync(PayrollRun run, int commercialUserId)
        {
            var employees = await _db.Employees
                .Where(e => !e.IsDeleted && e.InsertByUserId == commercialUserId && e.IsActive)
                .ToListAsync();

            foreach (var emp in employees)
            {
                var line = await _payroll.BuildLineAsync(
                    emp,
                    commercialUserId,
                    run.Year,
                    run.Month,
                    run.PeriodStart,
                    run.PeriodEnd);
                line.PayrollRunId = run.Id;
                _db.PayrollLines.Add(line);
            }

            run.UpdateDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        private Task<PayrollRun?> LoadRunAsync(int id, int commercialUserId) =>
            _db.PayrollRuns
                .Include(r => r.Lines!.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Employee)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);
    }
}

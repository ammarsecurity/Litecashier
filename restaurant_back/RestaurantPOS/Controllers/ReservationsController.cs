using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Requests.Restaurant;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Models.Restaurant;
using RestaurantPOS.Services;
using System.Security.Claims;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class ReservationsController : ControllerBase
    {
        private const int DefaultConflictWindowHours = 2;

        private readonly DbConfig _dbConfig;
        private readonly ILogger<ReservationsController> _logger;
        private readonly IMapper _mapper;
        private readonly IReservationTableSyncService _reservationTableSync;

        public ReservationsController(
            ILogger<ReservationsController> logger,
            DbConfig dbConfig,
            IMapper mapper,
            IReservationTableSyncService reservationTableSync)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
            _reservationTableSync = reservationTableSync;
        }

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

        private int GetActingUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        private static bool IsActiveReservationStatus(string? status) =>
            string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Seated", StringComparison.OrdinalIgnoreCase);

        private static (DateTime Start, DateTime End) GetConflictWindow(DateTime reservationDateTime) =>
            (reservationDateTime.AddHours(-DefaultConflictWindowHours),
             reservationDateTime.AddHours(DefaultConflictWindowHours));

        private static bool WindowsOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) =>
            aStart < bEnd && bStart < aEnd;

        // GET: api/Reservations
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<PagedList<Reservation>>>> GetReservations(
            int pageNumber = 0,
            int pageSize = 10,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            DateTime? reservationDate = null,
            TimeSpan? fromTime = null,
            TimeSpan? toTime = null,
            int? tableId = null,
            string? status = null,
            string? search = null)
        {
            var commercialUserId = GetCommercialUserId();
            var query = _dbConfig.Reservations
                .Include(r => r.Table)
                .Include(r => r.Order)
                .Where(r => !r.IsDeleted && r.InsertByUserId == commercialUserId)
                .AsQueryable();

            if (reservationDate.HasValue)
            {
                var day = reservationDate.Value.Date;
                query = query.Where(r => r.ReservationDateTime.Date == day);
            }
            else
            {
                if (fromDate.HasValue)
                {
                    query = query.Where(r => r.ReservationDateTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(r => r.ReservationDateTime <= toDate.Value);
                }
            }

            if (fromTime.HasValue)
            {
                query = query.Where(r => r.ReservationDateTime.TimeOfDay >= fromTime.Value);
            }

            if (toTime.HasValue)
            {
                query = query.Where(r => r.ReservationDateTime.TimeOfDay <= toTime.Value);
            }

            if (tableId.HasValue)
            {
                query = query.Where(r => r.TableId == tableId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r =>
                    r.CustomerName.Contains(search) ||
                    r.PhoneNumber.Contains(search) ||
                    (r.Email != null && r.Email.Contains(search)));
            }

            var reservations = query.OrderBy(r => r.ReservationDateTime);

            var totalItems = await reservations.CountAsync();
            var reservationsList = await reservations
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResult = new PagedList<Reservation>(reservationsList, totalItems, pageNumber, pageSize);

            return Ok(new GlobalResponse<PagedList<Reservation>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "تم جلب الحجوزات بنجاح"
            });
        }

        // GET: api/Reservations/summary
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpGet("summary")]
        public async Task<ActionResult<GlobalResponse<ReservationSummaryStatsDto>>> GetSummary(
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var commercialUserId = GetCommercialUserId();
            var today = DateTime.Today;
            var rangeStart = fromDate?.Date ?? today;
            var rangeEnd = toDate?.Date ?? today;

            var list = await _dbConfig.Reservations
                .Where(r => !r.IsDeleted
                    && r.InsertByUserId == commercialUserId
                    && r.ReservationDateTime.Date >= rangeStart
                    && r.ReservationDateTime.Date <= rangeEnd)
                .ToListAsync();

            var todayList = list.Where(r => r.ReservationDateTime.Date == today).ToList();
            var reservedTableIds = await _dbConfig.Tables
                .Where(t => !t.IsDeleted
                    && t.InsertByUserId == commercialUserId
                    && t.Status == "Reserved")
                .CountAsync();

            var stats = new ReservationSummaryStatsDto
            {
                TodayCount = todayList.Count,
                PendingCount = list.Count(r => r.Status == "Pending"),
                ConfirmedCount = list.Count(r => r.Status == "Confirmed"),
                ReservedTablesCount = reservedTableIds,
            };

            return Ok(new GlobalResponse<ReservationSummaryStatsDto>
            {
                Data = stats,
                ErrorStatus = false,
            });
        }

        // GET: api/Reservations/customers
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpGet("customers")]
        public async Task<ActionResult<GlobalResponse<List<ReservationCustomerOptionDto>>>> GetReservationCustomers(
            [FromQuery] string? search = null)
        {
            var commercialUserId = GetCommercialUserId();
            var result = new List<ReservationCustomerOptionDto>();
            var seenPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var customers = await _dbConfig.Customers
                .Where(c => !c.IsDeleted && c.InsertByUserId == commercialUserId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            foreach (var c in customers)
            {
                var phone = c.PhoneNumber.Trim();
                if (string.IsNullOrWhiteSpace(phone)) continue;
                seenPhones.Add(phone);
                result.Add(new ReservationCustomerOptionDto
                {
                    CustomerId = c.Id,
                    Name = c.Name.Trim(),
                    PhoneNumber = phone,
                    Source = "customer",
                });
            }

            var reservationGuests = await _dbConfig.Reservations
                .Where(r => !r.IsDeleted && r.InsertByUserId == commercialUserId)
                .Select(r => new { r.CustomerName, r.PhoneNumber })
                .Distinct()
                .ToListAsync();

            foreach (var r in reservationGuests)
            {
                var phone = (r.PhoneNumber ?? "").Trim();
                var name = (r.CustomerName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(name)) continue;
                if (seenPhones.Contains(phone)) continue;
                seenPhones.Add(phone);
                result.Add(new ReservationCustomerOptionDto
                {
                    Name = name,
                    PhoneNumber = phone,
                    Source = "reservation",
                });
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                result = result
                    .Where(c =>
                        c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            result = result.OrderBy(c => c.Name).ToList();

            return Ok(new GlobalResponse<List<ReservationCustomerOptionDto>>
            {
                Data = result,
                ErrorStatus = false,
                Message = "تم جلب العملاء بنجاح",
            });
        }

        // POST: api/Reservations/customers
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpPost("customers")]
        public async Task<ActionResult<GlobalResponse<ReservationCustomerOptionDto>>> AddReservationCustomer(
            [FromBody] CustomerRequest request)
        {
            var commercialUserId = GetCommercialUserId();

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return BadRequest(new GlobalResponse<ReservationCustomerOptionDto>
                {
                    ErrorStatus = true,
                    Message = "اسم العميل ورقم الهاتف مطلوبان",
                });
            }

            var phone = request.PhoneNumber.Trim();
            var existing = await _dbConfig.Customers
                .FirstOrDefaultAsync(c =>
                    !c.IsDeleted &&
                    c.InsertByUserId == commercialUserId &&
                    c.PhoneNumber == phone);

            if (existing != null)
            {
                return Ok(new GlobalResponse<ReservationCustomerOptionDto>
                {
                    Data = new ReservationCustomerOptionDto
                    {
                        CustomerId = existing.Id,
                        Name = existing.Name,
                        PhoneNumber = existing.PhoneNumber,
                        Source = "customer",
                    },
                    ErrorStatus = false,
                    Message = "العميل موجود مسبقاً",
                });
            }

            var customer = new Customer
            {
                Name = request.Name.Trim(),
                PhoneNumber = phone,
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                IsActive = request.IsActive ?? true,
                InsertByUserId = commercialUserId,
                InsertDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow,
                IsDeleted = false,
            };

            _dbConfig.Customers.Add(customer);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<ReservationCustomerOptionDto>
            {
                Data = new ReservationCustomerOptionDto
                {
                    CustomerId = customer.Id,
                    Name = customer.Name,
                    PhoneNumber = customer.PhoneNumber,
                    Source = "customer",
                },
                ErrorStatus = false,
                Message = "تم إضافة العميل بنجاح",
            });
        }

        // POST: api/Reservations/reconcile-tables
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpPost("reconcile-tables")]
        public async Task<ActionResult<GlobalResponse<int>>> ReconcileTables()
        {
            var commercialUserId = GetCommercialUserId();
            var actingUserId = GetActingUserId();
            await _reservationTableSync.ReconcileAllTablesAsync(commercialUserId, actingUserId);

            return Ok(new GlobalResponse<int>
            {
                Data = 1,
                ErrorStatus = false,
                Message = "تمت مزامنة حالات الطاولات مع الحجوزات",
            });
        }

        // GET: api/Reservations/availability
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpGet("availability")]
        public async Task<ActionResult<GlobalResponse<ReservationAvailabilityResponseDto>>> GetAvailability(
            [FromQuery] DateTime date,
            TimeSpan? time = null,
            DateTime? toDate = null,
            bool calendarView = false,
            int durationMinutes = 120,
            int? excludeReservationId = null)
        {
            var commercialUserId = GetCommercialUserId();
            var useCalendarView = calendarView || !time.HasValue && toDate.HasValue;

            var tables = await _dbConfig.Tables
                .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId)
                .OrderBy(t => t.Zone)
                .ThenBy(t => t.TableNumber)
                .ToListAsync();

            List<Reservation> reservations;
            DateTime slotStart;
            DateTime slotEnd;

            if (useCalendarView)
            {
                slotStart = date.Date;
                var rangeEndDate = (toDate?.Date ?? date.Date);
                slotEnd = rangeEndDate.AddDays(1).AddTicks(-1);

                reservations = await _dbConfig.Reservations
                    .Where(r => !r.IsDeleted
                        && r.InsertByUserId == commercialUserId
                        && r.TableId != null
                        && r.ReservationDateTime >= slotStart
                        && r.ReservationDateTime <= slotEnd
                        && (r.Status == "Pending" || r.Status == "Confirmed" || r.Status == "Seated"))
                    .ToListAsync();
            }
            else
            {
                slotStart = date.Date + (time ?? new TimeSpan(12, 0, 0));
                slotEnd = slotStart.AddMinutes(Math.Max(30, durationMinutes));
                var (windowStart, windowEnd) = (slotStart.AddHours(-DefaultConflictWindowHours), slotEnd.AddHours(DefaultConflictWindowHours));

                reservations = await _dbConfig.Reservations
                    .Where(r => !r.IsDeleted
                        && r.InsertByUserId == commercialUserId
                        && r.TableId != null
                        && r.ReservationDateTime >= windowStart
                        && r.ReservationDateTime <= windowEnd
                        && (r.Status == "Pending" || r.Status == "Confirmed" || r.Status == "Seated"))
                    .ToListAsync();
            }

            if (excludeReservationId.HasValue)
            {
                reservations = reservations.Where(r => r.Id != excludeReservationId.Value).ToList();
            }

            var result = new ReservationAvailabilityResponseDto
            {
                SlotStart = slotStart,
                SlotEnd = slotEnd,
                Tables = tables.Select(t =>
                {
                    Reservation? conflict;
                    if (useCalendarView)
                    {
                        conflict = reservations
                            .Where(r => r.TableId == t.Id)
                            .OrderBy(r => r.ReservationDateTime)
                            .FirstOrDefault();
                    }
                    else
                    {
                        conflict = reservations
                            .Where(r => r.TableId == t.Id)
                            .OrderBy(r => r.ReservationDateTime)
                            .FirstOrDefault(r =>
                            {
                                var (rStart, rEnd) = GetConflictWindow(r.ReservationDateTime);
                                return WindowsOverlap(slotStart, slotEnd, rStart, rEnd);
                            });
                    }

                    var occupied = string.Equals(t.Status, "Occupied", StringComparison.OrdinalIgnoreCase);
                    var outOfService = string.Equals(t.Status, "OutOfService", StringComparison.OrdinalIgnoreCase);
                    var hasConflict = conflict != null || occupied || outOfService;

                    return new TableReservationAvailabilityDto
                    {
                        TableId = t.Id,
                        TableNumber = t.TableNumber,
                        Zone = t.Zone,
                        Capacity = t.Capacity,
                        TableStatus = t.Status,
                        HasConflict = hasConflict,
                        ReservationId = conflict?.Id,
                        ReservationStatus = conflict?.Status,
                        CustomerName = conflict?.CustomerName,
                        ReservationDateTime = conflict?.ReservationDateTime,
                        IsAvailableForSlot = !hasConflict,
                    };
                }).ToList(),
            };

            return Ok(new GlobalResponse<ReservationAvailabilityResponseDto>
            {
                Data = result,
                ErrorStatus = false,
            });
        }

        // GET: api/Reservations/{id}
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<Reservation>>> GetReservation(int id)
        {
            var commercialUserId = GetCommercialUserId();
            var reservation = await _dbConfig.Reservations
                .Include(r => r.Table)
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);

            if (reservation == null)
            {
                return NotFound(new GlobalResponse<Reservation>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "الحجز غير موجود"
                });
            }

            return Ok(new GlobalResponse<Reservation>
            {
                Data = reservation,
                ErrorStatus = false,
                Message = "تم جلب الحجز بنجاح"
            });
        }

        // GET: api/Reservations/upcoming
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpGet("upcoming")]
        public async Task<ActionResult<GlobalResponse<List<Reservation>>>> GetUpcomingReservations()
        {
            var commercialUserId = GetCommercialUserId();
            var now = DateTime.UtcNow;
            var reservations = await _dbConfig.Reservations
                .Include(r => r.Table)
                .Where(r => !r.IsDeleted
                    && r.InsertByUserId == commercialUserId
                    && r.ReservationDateTime >= now
                    && (r.Status == "Pending" || r.Status == "Confirmed"))
                .OrderBy(r => r.ReservationDateTime)
                .Take(20)
                .ToListAsync();

            return Ok(new GlobalResponse<List<Reservation>>
            {
                Data = reservations,
                ErrorStatus = false,
                Message = "تم جلب الحجوزات القادمة بنجاح"
            });
        }

        // POST: api/Reservations
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<Reservation>>> AddReservation(ReservationRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var actingUserId = GetActingUserId();

                if (request.TableId.HasValue)
                {
                    var table = await _dbConfig.Tables
                        .FirstOrDefaultAsync(t => t.Id == request.TableId.Value && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                    if (table == null)
                    {
                        return BadRequest(new GlobalResponse<Reservation>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "الطاولة غير موجودة"
                        });
                    }

                    if (await HasConflictingReservationAsync(
                            request.TableId.Value,
                            request.ReservationDateTime,
                            commercialUserId,
                            null))
                    {
                        return BadRequest(new GlobalResponse<Reservation>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "الطاولة محجوزة في هذا الوقت"
                        });
                    }
                }

                var reservation = _mapper.Map<Reservation>(request);
                reservation.InsertByUserId = commercialUserId;
                _dbConfig.Reservations.Add(reservation);
                await _dbConfig.SaveChangesAsync();

                await _dbConfig.Entry(reservation).Reference(r => r.Table).LoadAsync();

                await _reservationTableSync.SyncTableForReservationAsync(
                    reservation,
                    null,
                    null,
                    commercialUserId,
                    actingUserId);

                return Ok(new GlobalResponse<Reservation>
                {
                    Data = reservation,
                    ErrorStatus = false,
                    Message = "تم إضافة الحجز بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reservation");
                return StatusCode(500, new GlobalResponse<Reservation>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة الحجز: {ex.Message}"
                });
            }
        }

        // PUT: api/Reservations/{id}
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<Reservation>>> UpdateReservation(int id, ReservationRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            var actingUserId = GetActingUserId();
            var reservation = await _dbConfig.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);

            if (reservation == null)
            {
                return NotFound(new GlobalResponse<Reservation>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "الحجز غير موجود"
                });
            }

            var previousStatus = reservation.Status;
            var previousTableId = reservation.TableId;

            if (request.TableId.HasValue)
            {
                var table = await _dbConfig.Tables
                    .FirstOrDefaultAsync(t => t.Id == request.TableId.Value && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                if (table == null)
                {
                    return BadRequest(new GlobalResponse<Reservation>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة غير موجودة"
                    });
                }

                var tableOrTimeChanged = request.TableId != reservation.TableId
                    || request.ReservationDateTime != reservation.ReservationDateTime;

                if (tableOrTimeChanged && await HasConflictingReservationAsync(
                        request.TableId.Value,
                        request.ReservationDateTime,
                        commercialUserId,
                        id))
                {
                    return BadRequest(new GlobalResponse<Reservation>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة محجوزة في هذا الوقت"
                    });
                }
            }

            var oldValues = new
            {
                reservation.TableId,
                reservation.ReservationDateTime,
                reservation.CustomerName,
                reservation.PhoneNumber,
                reservation.NumberOfGuests,
                reservation.Status,
                reservation.Notes
            };

            _mapper.Map(request, reservation);
            _dbConfig.Reservations.Update(reservation);
            await _dbConfig.SaveChangesAsync();

            await _dbConfig.Entry(reservation).Reference(r => r.Table).LoadAsync();

            await _reservationTableSync.SyncTableForReservationAsync(
                reservation,
                previousStatus,
                previousTableId,
                commercialUserId,
                actingUserId);

            var newValues = new
            {
                reservation.TableId,
                reservation.ReservationDateTime,
                reservation.CustomerName,
                reservation.PhoneNumber,
                reservation.NumberOfGuests,
                reservation.Status,
                reservation.Notes
            };

            await _dbConfig.LogAuditAsync(
                "Update",
                "Reservation",
                reservation.Id,
                $"حجز {reservation.CustomerName}",
                actingUserId,
                commercialUserId,
                oldValues,
                newValues,
                $"تم تعديل الحجز: {reservation.CustomerName}");

            return Ok(new GlobalResponse<Reservation>
            {
                Data = reservation,
                ErrorStatus = false,
                Message = "تم تحديث الحجز بنجاح"
            });
        }

        // PUT: api/Reservations/{id}/status
        [AuthorizeSection("reservations", Roles = "Commercial,POS,Admin")]
        [HttpPut("{id}/status")]
        public async Task<ActionResult<GlobalResponse<Reservation>>> UpdateReservationStatus(
            int id,
            [FromBody] UpdateReservationStatusRequest body)
        {
            var commercialUserId = GetCommercialUserId();
            var actingUserId = GetActingUserId();
            var status = body?.Status ?? "";

            var reservation = await _dbConfig.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);

            if (reservation == null)
            {
                return NotFound(new GlobalResponse<Reservation>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "الحجز غير موجود"
                });
            }

            var validStatuses = new[] { "Pending", "Confirmed", "Seated", "Completed", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                return BadRequest(new GlobalResponse<Reservation>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حالة غير صحيحة"
                });
            }

            var oldStatus = reservation.Status;
            var previousTableId = reservation.TableId;
            reservation.Status = status;
            _dbConfig.Reservations.Update(reservation);
            await _dbConfig.SaveChangesAsync();

            await _reservationTableSync.SyncTableForReservationAsync(
                reservation,
                oldStatus,
                previousTableId,
                commercialUserId,
                actingUserId);

            await _dbConfig.LogAuditAsync(
                "Update",
                "Reservation",
                reservation.Id,
                $"حجز {reservation.CustomerName}",
                actingUserId,
                commercialUserId,
                new { Status = oldStatus },
                new { Status = status },
                $"تم تعديل حالة الحجز: {oldStatus} → {status}");

            return Ok(new GlobalResponse<Reservation>
            {
                Data = reservation,
                ErrorStatus = false,
                Message = "تم تحديث حالة الحجز بنجاح"
            });
        }

        // DELETE: api/Reservations/{id}
        [AuthorizeSection("reservations", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteReservation(int id)
        {
            var commercialUserId = GetCommercialUserId();
            var actingUserId = GetActingUserId();
            var reservation = await _dbConfig.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.InsertByUserId == commercialUserId);

            if (reservation == null)
            {
                return NotFound(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "الحجز غير موجود"
                });
            }

            var reservationName = $"حجز {reservation.CustomerName}";
            var previousTableId = reservation.TableId;
            var previousStatus = reservation.Status;

            reservation.IsDeleted = true;
            reservation.Status = "Cancelled";
            _dbConfig.Reservations.Update(reservation);
            await _dbConfig.SaveChangesAsync();

            await _reservationTableSync.SyncTableForReservationAsync(
                reservation,
                previousStatus,
                previousTableId,
                commercialUserId,
                actingUserId);

            await _dbConfig.LogAuditAsync(
                "Delete",
                "Reservation",
                reservation.Id,
                reservationName,
                actingUserId,
                commercialUserId,
                null,
                null,
                $"تم حذف الحجز: {reservation.CustomerName}");

            return Ok(new GlobalResponse<int>
            {
                Data = id,
                ErrorStatus = false,
                Message = "تم حذف الحجز بنجاح"
            });
        }

        private async Task<bool> HasConflictingReservationAsync(
            int tableId,
            DateTime reservationDateTime,
            int commercialUserId,
            int? excludeReservationId)
        {
            var (windowStart, windowEnd) = GetConflictWindow(reservationDateTime);

            var query = _dbConfig.Reservations.Where(r =>
                r.TableId == tableId
                && !r.IsDeleted
                && r.InsertByUserId == commercialUserId
                && r.Status != "Cancelled"
                && r.Status != "Completed"
                && r.ReservationDateTime >= windowStart
                && r.ReservationDateTime <= windowEnd);

            if (excludeReservationId.HasValue)
            {
                query = query.Where(r => r.Id != excludeReservationId.Value);
            }

            return await query.AnyAsync();
        }
    }
}

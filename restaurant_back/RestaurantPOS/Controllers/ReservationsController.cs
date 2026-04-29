using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using RestaurantPOS.Models.Requests.Restaurant;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Models.Restaurant;
using System;
using System.Security.Claims;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class ReservationsController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<ReservationsController> _logger;
        private readonly IMapper _mapper;

        public ReservationsController(ILogger<ReservationsController> logger, DbConfig dbConfig, IMapper mapper)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
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

        // GET: api/Reservations
        [Authorize(Roles = "Commercial,POS,Admin,ReservationsManager")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<PagedList<Reservation>>>> GetReservations(
            int pageNumber = 0,
            int pageSize = 10,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? status = null,
            string? search = null)
        {
            var commercialUserId = GetCommercialUserId();
            var query = _dbConfig.Reservations
                .Include(r => r.Table)
                .Include(r => r.Order)
                .Where(r => !r.IsDeleted && r.InsertByUserId == commercialUserId)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.ReservationDateTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.ReservationDateTime <= toDate.Value);
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

            var reservations = query
                .OrderBy(r => r.ReservationDateTime);

            var totalItems = await reservations.CountAsync();
            var reservationsList = await reservations.ToListAsync();

            var pagedResult = new PagedList<Reservation>(reservationsList, totalItems, pageNumber, pageSize);

            return Ok(new GlobalResponse<PagedList<Reservation>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "تم جلب الحجوزات بنجاح"
            });
        }

        // GET: api/Reservations/{id}
        [Authorize(Roles = "Commercial,POS,Admin,ReservationsManager")]
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
        [Authorize(Roles = "Commercial,POS,Admin,ReservationsManager")]
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
        [Authorize(Roles = "Commercial,POS,Admin,ReservationsManager")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<Reservation>>> AddReservation(ReservationRequest request)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                // Validate table if provided
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

                    // Check if table is available at the requested time
                    // Calculate time range: 2 hours before and 2 hours after requested time
                    var timeWindowStart = request.ReservationDateTime.AddHours(-2);
                    var timeWindowEnd = request.ReservationDateTime.AddHours(2);
                    
                    var conflictingReservation = await _dbConfig.Reservations
                        .FirstOrDefaultAsync(r => r.TableId == request.TableId.Value
                            && !r.IsDeleted
                            && r.InsertByUserId == commercialUserId
                            && r.Status != "Cancelled"
                            && r.Status != "Completed"
                            && r.ReservationDateTime >= timeWindowStart
                            && r.ReservationDateTime <= timeWindowEnd);

                    if (conflictingReservation != null)
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

                // Load related data
                await _dbConfig.Entry(reservation)
                    .Reference(r => r.Table)
                    .LoadAsync();

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
        [Authorize(Roles = "Commercial,POS,Admin,ReservationsManager")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<Reservation>>> UpdateReservation(int id, ReservationRequest request)
        {
            var commercialUserId = GetCommercialUserId();
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

            // Validate table if changed
            if (request.TableId.HasValue && request.TableId != reservation.TableId)
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

                // Check if table is available at the requested time (excluding current reservation)
                var timeWindowStart = request.ReservationDateTime.AddHours(-2);
                var timeWindowEnd = request.ReservationDateTime.AddHours(2);
                
                var conflictingReservation = await _dbConfig.Reservations
                    .FirstOrDefaultAsync(r => r.Id != id
                        && r.TableId == request.TableId.Value
                        && !r.IsDeleted
                        && r.InsertByUserId == commercialUserId
                        && r.Status != "Cancelled"
                        && r.Status != "Completed"
                        && r.ReservationDateTime >= timeWindowStart
                        && r.ReservationDateTime <= timeWindowEnd);

                if (conflictingReservation != null)
                {
                    return BadRequest(new GlobalResponse<Reservation>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة محجوزة في هذا الوقت"
                    });
                }
            }
            // Also check for conflicts if only the time changed
            else if (request.TableId.HasValue && request.ReservationDateTime != reservation.ReservationDateTime)
            {
                var timeWindowStart = request.ReservationDateTime.AddHours(-2);
                var timeWindowEnd = request.ReservationDateTime.AddHours(2);
                
                var conflictingReservation = await _dbConfig.Reservations
                    .FirstOrDefaultAsync(r => r.Id != id
                        && r.TableId == request.TableId.Value
                        && !r.IsDeleted
                        && r.InsertByUserId == commercialUserId
                        && r.Status != "Cancelled"
                        && r.Status != "Completed"
                        && r.ReservationDateTime >= timeWindowStart
                        && r.ReservationDateTime <= timeWindowEnd);

                if (conflictingReservation != null)
                {
                    return BadRequest(new GlobalResponse<Reservation>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة محجوزة في هذا الوقت"
                    });
                }
            }

            // Store old values for audit log
            var oldValues = new
            {
                TableId = reservation.TableId,
                ReservationDateTime = reservation.ReservationDateTime,
                CustomerName = reservation.CustomerName,
                PhoneNumber = reservation.PhoneNumber,
                NumberOfGuests = reservation.NumberOfGuests,
                Status = reservation.Status,
                Notes = reservation.Notes
            };

            _mapper.Map(request, reservation);
            _dbConfig.Reservations.Update(reservation);
            await _dbConfig.SaveChangesAsync();

            // Load related data
            await _dbConfig.Entry(reservation)
                .Reference(r => r.Table)
                .LoadAsync();

            // Store new values for audit log
            var newValues = new
            {
                TableId = reservation.TableId,
                ReservationDateTime = reservation.ReservationDateTime,
                CustomerName = reservation.CustomerName,
                PhoneNumber = reservation.PhoneNumber,
                NumberOfGuests = reservation.NumberOfGuests,
                Status = reservation.Status,
                Notes = reservation.Notes
            };

            // Log audit
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _dbConfig.LogAuditAsync(
                "Update",
                "Reservation",
                reservation.Id,
                $"حجز {reservation.CustomerName}",
                userId,
                commercialUserId,
                oldValues,
                newValues,
                $"تم تعديل الحجز: {reservation.CustomerName}"
            );

            return Ok(new GlobalResponse<Reservation>
            {
                Data = reservation,
                ErrorStatus = false,
                Message = "تم تحديث الحجز بنجاح"
            });
        }

        // PUT: api/Reservations/{id}/status
        [Authorize(Roles = "Commercial,POS,Admin,ReservationsManager")]
        [HttpPut("{id}/status")]
        public async Task<ActionResult<GlobalResponse<Reservation>>> UpdateReservationStatus(int id, [FromBody] string status)
        {
            var commercialUserId = GetCommercialUserId();
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

            // Store old status for audit log
            var oldStatus = reservation.Status;
            reservation.Status = status;
            _dbConfig.Reservations.Update(reservation);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _dbConfig.LogAuditAsync(
                "Update",
                "Reservation",
                reservation.Id,
                $"حجز {reservation.CustomerName}",
                userId,
                commercialUserId,
                new { Status = oldStatus },
                new { Status = status },
                $"تم تعديل حالة الحجز: {oldStatus} → {status}"
            );

            return Ok(new GlobalResponse<Reservation>
            {
                Data = reservation,
                ErrorStatus = false,
                Message = "تم تحديث حالة الحجز بنجاح"
            });
        }

        // DELETE: api/Reservations/{id}
        [Authorize(Roles = "Commercial,Admin,ReservationsManager")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteReservation(int id)
        {
            var commercialUserId = GetCommercialUserId();
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

            // Store reservation name for audit log
            var reservationName = $"حجز {reservation.CustomerName}";
            
            // Soft delete
            reservation.IsDeleted = true;
            _dbConfig.Reservations.Update(reservation);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _dbConfig.LogAuditAsync(
                "Delete",
                "Reservation",
                reservation.Id,
                reservationName,
                userId,
                commercialUserId,
                null,
                null,
                $"تم حذف الحجز: {reservation.CustomerName}"
            );

            return Ok(new GlobalResponse<int>
            {
                Data = id,
                ErrorStatus = false,
                Message = "تم حذف الحجز بنجاح"
            });
        }
    }
}


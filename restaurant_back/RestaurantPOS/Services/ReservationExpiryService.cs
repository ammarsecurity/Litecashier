using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestaurantPOS.Configuration;
using RestaurantPOS.Db;
using RestaurantPOS.Models.Restaurant;

namespace RestaurantPOS.Services;

public interface IReservationExpiryService
{
    Task<int> ExpireOverdueForCommercialAsync(int commercialUserId, int actingUserId);

    Task<int> ExpireAllOverdueAsync(CancellationToken cancellationToken = default);
}

public class ReservationExpiryService : IReservationExpiryService
{
    private readonly DbConfig _db;
    private readonly IReservationTableSyncService _reservationTableSync;
    private readonly IOptions<ReservationSettingsOptions> _settings;
    private readonly ILogger<ReservationExpiryService> _logger;

    public ReservationExpiryService(
        DbConfig db,
        IReservationTableSyncService reservationTableSync,
        IOptions<ReservationSettingsOptions> settings,
        ILogger<ReservationExpiryService> logger)
    {
        _db = db;
        _reservationTableSync = reservationTableSync;
        _settings = settings;
        _logger = logger;
    }

    public async Task<int> ExpireOverdueForCommercialAsync(int commercialUserId, int actingUserId)
    {
        if (!_settings.Value.AutoCancelWhenDue)
        {
            return 0;
        }

        var now = DateTime.Now;
        var overdue = await _db.Reservations
            .Where(r => !r.IsDeleted
                && r.InsertByUserId == commercialUserId
                && (r.Status == "Pending" || r.Status == "Confirmed")
                && r.ReservationDateTime <= now
                && r.OrderId == null)
            .ToListAsync();

        var expiredCount = 0;
        foreach (var reservation in overdue)
        {
            if (reservation.TableId.HasValue
                && await TableHasActiveDineInOrderAsync(reservation.TableId.Value, commercialUserId))
            {
                continue;
            }

            var oldStatus = reservation.Status;
            var previousTableId = reservation.TableId;
            reservation.Status = "Cancelled";
            _db.Reservations.Update(reservation);
            await _db.SaveChangesAsync();

            await _reservationTableSync.SyncTableForReservationAsync(
                reservation,
                oldStatus,
                previousTableId,
                commercialUserId,
                actingUserId);

            await _db.LogAuditAsync(
                "Update",
                "Reservation",
                reservation.Id,
                $"حجز {reservation.CustomerName}",
                actingUserId,
                commercialUserId,
                new { Status = oldStatus, ReservationDateTime = reservation.ReservationDateTime },
                new { Status = reservation.Status },
                "إلغاء تلقائي: تجاوز موعد الحجز");

            expiredCount++;
            _logger.LogInformation(
                "Auto-cancelled overdue reservation {ReservationId} for table {TableId} (commercial {CommercialUserId})",
                reservation.Id,
                reservation.TableId,
                commercialUserId);
        }

        return expiredCount;
    }

    public async Task<int> ExpireAllOverdueAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Value.AutoCancelWhenDue)
        {
            return 0;
        }

        var commercialUserIds = await _db.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role == "Commercial")
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var total = 0;
        foreach (var commercialUserId in commercialUserIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += await ExpireOverdueForCommercialAsync(commercialUserId, commercialUserId);
        }

        return total;
    }

    private async Task<bool> TableHasActiveDineInOrderAsync(int tableId, int commercialUserId)
    {
        var table = await _db.Tables
            .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);

        if (table?.CurrentOrderId is int currentOrderId)
        {
            var fromCurrent = await _db.CustomerOrders.AnyAsync(o =>
                o.Id == currentOrderId
                && !o.IsDeleted
                && o.OrderType == "DineIn"
                && o.PaymentStatus != "Paid");

            if (fromCurrent)
            {
                return true;
            }
        }

        var orderIdsFromLinks = await _db.OrderTables
            .Where(ot => ot.TableId == tableId && !ot.IsDeleted)
            .Select(ot => ot.OrderId)
            .Distinct()
            .ToListAsync();

        if (orderIdsFromLinks.Count > 0)
        {
            var fromLinks = await _db.CustomerOrders.AnyAsync(o =>
                orderIdsFromLinks.Contains(o.Id)
                && !o.IsDeleted
                && o.OrderType == "DineIn"
                && o.PaymentStatus != "Paid");

            if (fromLinks)
            {
                return true;
            }
        }

        return await _db.CustomerOrders.AnyAsync(o =>
            !o.IsDeleted
            && o.OrderType == "DineIn"
            && o.PaymentStatus != "Paid"
            && o.TableId == tableId
            && o.InsertByUserId == commercialUserId);
    }
}

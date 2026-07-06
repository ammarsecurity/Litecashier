using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Hubs;
using POS.Models;
using POS.Models.Response;

namespace POS.Services
{
    public interface ICardPaymentProcessingService
    {
        Task<(CardPaymentTransaction? Transaction, PaymentDevice? Device, string? ErrorKey)> PrepareTransactionAsync(
            int userId,
            int commercialUserId,
            decimal amount,
            decimal tipAmount,
            string currencyCode,
            int? paymentDeviceId);

        void EnqueueSaleProcessing(int transactionId, int requestedByUserId);

        CardPaymentStatusDto ToStatusDto(CardPaymentTransaction tx, PaymentDevice? device = null);

        Task<(bool Success, string? ErrorKey)> CancelTransactionAsync(
            int transactionId,
            int userId,
            int commercialUserId);
    }

    public class CardPaymentProcessingService : ICardPaymentProcessingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly ILogger<CardPaymentProcessingService> _logger;

        public CardPaymentProcessingService(
            IServiceScopeFactory scopeFactory,
            IHubContext<OrderHub> hubContext,
            ILogger<CardPaymentProcessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<(CardPaymentTransaction? Transaction, PaymentDevice? Device, string? ErrorKey)> PrepareTransactionAsync(
            int userId,
            int commercialUserId,
            decimal amount,
            decimal tipAmount,
            string currencyCode,
            int? paymentDeviceId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbConfig>();
            var nebula = scope.ServiceProvider.GetRequiredService<INebulaPaymentService>();

            var device = await ResolveDeviceAsync(db, commercialUserId, paymentDeviceId);
            if (device == null)
            {
                return (null, null, "noPaymentDeviceConfigured");
            }

            var connectionStatus = await nebula.IsConnectedAsync(device.BaseUrl);
            if (!IsNebulaConnected(connectionStatus))
            {
                var (connected, connectMessage) = await ConnectDeviceAsync(nebula, device);
                if (!connected)
                {
                    return (null, device, connectMessage ?? "connectionFailed");
                }
            }

            var amountLong = (long)Math.Round(amount, MidpointRounding.AwayFromZero);
            var tipLong = (long)Math.Round(tipAmount, MidpointRounding.AwayFromZero);
            if (amountLong <= 0)
            {
                return (null, device, "invalidPaymentAmount");
            }

            var tx = new CardPaymentTransaction
            {
                PaymentDeviceId = device.Id,
                InsertByUserId = commercialUserId,
                RequestedByUserId = userId,
                Amount = amountLong,
                TipAmount = tipLong,
                CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "IQD" : currencyCode,
                Status = "Pending"
            };

            db.CardPaymentTransactions.Add(tx);
            await db.SaveChangesAsync();

            return (tx, device, null);
        }

        public void EnqueueSaleProcessing(int transactionId, int requestedByUserId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessSaleAsync(transactionId, requestedByUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background card payment failed for transaction {TransactionId}", transactionId);
                }
            });
        }

        public CardPaymentStatusDto ToStatusDto(CardPaymentTransaction tx, PaymentDevice? device = null)
        {
            var status = tx.Status ?? "Pending";
            var isTerminal = string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase);

            return new CardPaymentStatusDto
            {
                TransactionId = tx.Id,
                Status = status,
                Message = tx.Message,
                AuthCode = tx.AuthCode,
                RefNo = tx.RefNo,
                CardNo = tx.CardNo,
                IsTerminal = isTerminal,
                Amount = tx.Amount,
                CurrencyCode = tx.CurrencyCode,
                DeviceName = device?.Name
            };
        }

        public async Task<(bool Success, string? ErrorKey)> CancelTransactionAsync(
            int transactionId,
            int userId,
            int commercialUserId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbConfig>();
            var nebula = scope.ServiceProvider.GetRequiredService<INebulaPaymentService>();

            var tx = await db.CardPaymentTransactions
                .Include(t => t.PaymentDevice)
                .FirstOrDefaultAsync(t =>
                    t.Id == transactionId &&
                    !t.IsDeleted &&
                    t.InsertByUserId == commercialUserId &&
                    t.RequestedByUserId == userId);

            if (tx == null)
            {
                return (false, "cardPaymentTransactionNotFound");
            }

            if (string.Equals(tx.Status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "cardPaymentTransactionNotSuccessful");
            }

            if (string.Equals(tx.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                await NotifyStatusChangedAsync(tx.RequestedByUserId, ToStatusDto(tx, tx.PaymentDevice));
                return (true, null);
            }

            if (tx.PaymentDevice != null)
            {
                await nebula.CancelTransAsync(tx.PaymentDevice.BaseUrl, CancellationToken.None);
            }

            tx.Status = "Failed";
            tx.Message = "cardPaymentCancelled";
            tx.UpdateDate = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await NotifyStatusChangedAsync(tx.RequestedByUserId, ToStatusDto(tx, tx.PaymentDevice));
            return (true, null);
        }

        private async Task ProcessSaleAsync(int transactionId, int requestedByUserId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbConfig>();
            var nebula = scope.ServiceProvider.GetRequiredService<INebulaPaymentService>();

            var tx = await db.CardPaymentTransactions
                .Include(t => t.PaymentDevice)
                .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted);

            if (tx == null || tx.PaymentDevice == null)
            {
                return;
            }

            if (string.Equals(tx.Status, "Success", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tx.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            tx.Status = "Processing";
            tx.UpdateDate = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await NotifyStatusChangedAsync(requestedByUserId, ToStatusDto(tx, tx.PaymentDevice));

            var device = tx.PaymentDevice;
            var amount = (long)Math.Round(tx.Amount, MidpointRounding.AwayFromZero);
            var tip = (long)Math.Round(tx.TipAmount, MidpointRounding.AwayFromZero);

            var saleResult = await nebula.CreateSaleAsync(
                device.BaseUrl,
                amount,
                tip,
                tx.CurrencyCode,
                CancellationToken.None);

            tx.ResultCode = saleResult.ResultCode;
            tx.Message = saleResult.Message ?? saleResult.ErrorMessage;
            tx.RawResponse = saleResult.RawOuterResponse;

            ApplySaleDetails(tx, saleResult.Details);

            if (!saleResult.Success && !string.IsNullOrWhiteSpace(tx.RawResponse))
            {
                ReconcileTransactionFromRaw(tx);
            }

            tx.Status = saleResult.Success || IsTransactionSuccessful(tx) ? "Success" : "Failed";
            tx.UpdateDate = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await NotifyStatusChangedAsync(requestedByUserId, ToStatusDto(tx, device));
        }

        private async Task NotifyStatusChangedAsync(int requestedByUserId, CardPaymentStatusDto payload)
        {
            try
            {
                await _hubContext.Clients
                    .User(requestedByUserId.ToString())
                    .SendAsync("CardPaymentStatusChanged", payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send CardPaymentStatusChanged for transaction {TransactionId}", payload.TransactionId);
            }
        }

        private static async Task<PaymentDevice?> ResolveDeviceAsync(DbConfig db, int commercialUserId, int? deviceId)
        {
            if (deviceId.HasValue)
            {
                return await db.PaymentDevices
                    .FirstOrDefaultAsync(d => d.Id == deviceId.Value && !d.IsDeleted && d.IsActive && d.InsertByUserId == commercialUserId);
            }

            return await db.PaymentDevices
                .Where(d => !d.IsDeleted && d.IsActive && d.InsertByUserId == commercialUserId)
                .OrderByDescending(d => d.IsDefault)
                .ThenBy(d => d.Id)
                .FirstOrDefaultAsync();
        }

        private static bool IsNebulaConnected(string? status)
        {
            var raw = (status ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            return raw.Contains("connected") || raw.Contains("\"resultcode\":\"200\"") || raw == "true";
        }

        private static async Task<(bool Success, string? Message)> ConnectDeviceAsync(INebulaPaymentService nebula, PaymentDevice device)
        {
            switch (device.ConnectionType?.ToLowerInvariant())
            {
                case "wifi":
                    return await nebula.ConnectWifiAsync(
                        device.BaseUrl,
                        device.WifiHost ?? "localhost",
                        device.WifiPort ?? 0,
                        device.WifiConfigJson ?? "{}");
                case "cloud":
                    return await nebula.ConnectCloudAsync(
                        device.BaseUrl,
                        device.CloudConfigJson ?? "{}");
                default:
                    return await nebula.ConnectUsbAsync(
                        device.BaseUrl,
                        device.ComPort ?? "COM6");
            }
        }

        private static bool IsTransactionSuccessful(CardPaymentTransaction tx)
        {
            if (string.Equals(tx.Status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(tx.AuthCode) ||
                   NebulaResponseParser.TryEvaluateSaleSuccess(tx.RawResponse, out _, out _, out _);
        }

        private static void ApplySaleDetails(CardPaymentTransaction tx, NebulaTransDetails? details)
        {
            if (details == null)
            {
                return;
            }

            tx.AuthCode = details.AuthCode;
            tx.RefNo = details.RefNo;
            tx.CardNo = details.CardNo;
            tx.CardType = details.CardType;
            tx.IssuerName = details.IssuerName;
            tx.AcquirerName = details.AcquirerName;
            tx.TerminalId = details.TerminalId;
            tx.MerchantId = details.MerchantId;
            tx.MerchantName = details.MerchantName;
            tx.VoucherNo = details.VoucherNo;
            tx.BatchNo = details.BatchNo;
            tx.TransTime = details.TransTime;
            tx.TotalAmount = details.TotalAmount;
        }

        private static void ReconcileTransactionFromRaw(CardPaymentTransaction tx)
        {
            if (string.IsNullOrWhiteSpace(tx.RawResponse))
            {
                return;
            }

            if (!NebulaResponseParser.TryEvaluateSaleSuccess(tx.RawResponse, out var details, out var resultCode, out _))
            {
                return;
            }

            tx.ResultCode = resultCode ?? tx.ResultCode;
            ApplySaleDetails(tx, details);
        }
    }
}

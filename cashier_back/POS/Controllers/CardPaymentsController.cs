using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Authorization;
using POS.Db;
using POS.Models;
using POS.Models.Requests;
using POS.Models.Response;
using POS.Services;
using System.Security.Claims;

namespace POS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class CardPaymentsController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly INebulaPaymentService _nebula;
        private readonly ICardPaymentProcessingService _cardPaymentProcessing;
        private readonly ILogger<CardPaymentsController> _logger;

        public CardPaymentsController(
            DbConfig dbConfig,
            INebulaPaymentService nebula,
            ICardPaymentProcessingService cardPaymentProcessing,
            ILogger<CardPaymentsController> logger)
        {
            _dbConfig = dbConfig;
            _nebula = nebula;
            _cardPaymentProcessing = cardPaymentProcessing;
            _logger = logger;
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

        private async Task<PaymentDevice?> ResolveDeviceAsync(int commercialUserId, int? deviceId)
        {
            if (deviceId.HasValue)
            {
                return await _dbConfig.PaymentDevices
                    .FirstOrDefaultAsync(d => d.Id == deviceId.Value && !d.IsDeleted && d.IsActive && d.InsertByUserId == commercialUserId);
            }

            return await _dbConfig.PaymentDevices
                .Where(d => !d.IsDeleted && d.IsActive && d.InsertByUserId == commercialUserId)
                .OrderByDescending(d => d.IsDefault)
                .ThenBy(d => d.Id)
                .FirstOrDefaultAsync();
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("sale/start")]
        public async Task<ActionResult<GlobalResponse<object>>> StartSale([FromBody] CardPaymentSaleRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var commercialUserId = GetCommercialUserId();

                var (tx, device, errorKey) = await _cardPaymentProcessing.PrepareTransactionAsync(
                    userId,
                    commercialUserId,
                    request.Amount,
                    request.TipAmount,
                    request.CurrencyCode ?? "IQD",
                    request.PaymentDeviceId);

                if (tx == null || errorKey != null)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = errorKey ?? "cardPaymentFailed"
                    });
                }

                _cardPaymentProcessing.EnqueueSaleProcessing(tx.Id, userId);

                var statusDto = _cardPaymentProcessing.ToStatusDto(tx, device);

                return Ok(new GlobalResponse<object>
                {
                    Data = new
                    {
                        transactionId = tx.Id,
                        status = statusDto.Status,
                        amount = statusDto.Amount,
                        currencyCode = statusDto.CurrencyCode,
                        deviceName = statusDto.DeviceName,
                        isTerminal = statusDto.IsTerminal
                    },
                    ErrorStatus = false,
                    Message = "cardPaymentProcessing"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Card payment sale/start failed");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpGet("{id}/status")]
        public async Task<ActionResult<GlobalResponse<CardPaymentStatusDto>>> GetStatus(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var commercialUserId = GetCommercialUserId();

            var tx = await _dbConfig.CardPaymentTransactions
                .Include(t => t.PaymentDevice)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    !t.IsDeleted &&
                    t.InsertByUserId == commercialUserId &&
                    t.RequestedByUserId == userId);

            if (tx == null)
            {
                return NotFound(new GlobalResponse<CardPaymentStatusDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "cardPaymentTransactionNotFound"
                });
            }

            if (tx.Status != "Success")
            {
                ReconcileTransactionFromRaw(tx);
                if (IsTransactionSuccessful(tx))
                {
                    tx.Status = "Success";
                    tx.UpdateDate = DateTime.UtcNow;
                    await _dbConfig.SaveChangesAsync();
                }
            }

            return Ok(new GlobalResponse<CardPaymentStatusDto>
            {
                Data = _cardPaymentProcessing.ToStatusDto(tx, tx.PaymentDevice),
                ErrorStatus = false
            });
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<GlobalResponse<CardPaymentStatusDto>>> CancelSale(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var commercialUserId = GetCommercialUserId();

            var (success, errorKey) = await _cardPaymentProcessing.CancelTransactionAsync(
                id, userId, commercialUserId);

            if (!success)
            {
                return BadRequest(new GlobalResponse<CardPaymentStatusDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = errorKey ?? "cardPaymentFailed"
                });
            }

            var tx = await _dbConfig.CardPaymentTransactions
                .Include(t => t.PaymentDevice)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            return Ok(new GlobalResponse<CardPaymentStatusDto>
            {
                Data = tx != null ? _cardPaymentProcessing.ToStatusDto(tx, tx.PaymentDevice) : null,
                ErrorStatus = false,
                Message = "cardPaymentCancelled"
            });
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("sale")]
        public async Task<ActionResult<GlobalResponse<object>>> Sale([FromBody] CardPaymentSaleRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var commercialUserId = GetCommercialUserId();
                var device = await ResolveDeviceAsync(commercialUserId, request.PaymentDeviceId);

                if (device == null)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "noPaymentDeviceConfigured"
                    });
                }

                var connectionStatus = await _nebula.IsConnectedAsync(device.BaseUrl);
                if (!IsNebulaConnected(connectionStatus))
                {
                    var (connected, connectMessage) = await ConnectDeviceAsync(device);
                    if (!connected)
                    {
                        return BadRequest(new GlobalResponse<object>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = connectMessage ?? "connectionFailed"
                        });
                    }
                }

                var amount = (long)Math.Round(request.Amount, MidpointRounding.AwayFromZero);
                var tip = (long)Math.Round(request.TipAmount, MidpointRounding.AwayFromZero);
                if (amount <= 0)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "invalidPaymentAmount"
                    });
                }

                var tx = new CardPaymentTransaction
                {
                    PaymentDeviceId = device.Id,
                    InsertByUserId = commercialUserId,
                    RequestedByUserId = userId,
                    Amount = amount,
                    TipAmount = tip,
                    CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "IQD" : request.CurrencyCode,
                    Status = "Pending"
                };

                _dbConfig.CardPaymentTransactions.Add(tx);
                await _dbConfig.SaveChangesAsync();

                // Do not tie Nebula wait to HTTP client disconnect — POS may wait minutes on device.
                var saleResult = await _nebula.CreateSaleAsync(
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
                await _dbConfig.SaveChangesAsync();

                if (tx.Status != "Success")
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = new { transactionId = tx.Id, success = false },
                        ErrorStatus = true,
                        Message = saleResult.ErrorMessage ?? saleResult.Message ?? "cardPaymentFailed"
                    });
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = new
                    {
                        transactionId = tx.Id,
                        success = true,
                        authCode = tx.AuthCode,
                        refNo = tx.RefNo,
                        cardNo = tx.CardNo
                    },
                    ErrorStatus = false,
                    Message = "cardPaymentSuccess"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Card payment sale failed");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = ex.Message
                });
            }
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpGet("verify/{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> VerifySale(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var commercialUserId = GetCommercialUserId();

            var tx = await _dbConfig.CardPaymentTransactions
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    !t.IsDeleted &&
                    t.InsertByUserId == commercialUserId &&
                    t.RequestedByUserId == userId);

            if (tx == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "cardPaymentTransactionNotFound"
                });
            }

            if (tx.Status != "Success")
            {
                ReconcileTransactionFromRaw(tx);
                if (IsTransactionSuccessful(tx))
                {
                    tx.Status = "Success";
                    tx.UpdateDate = DateTime.UtcNow;
                    await _dbConfig.SaveChangesAsync();
                }
            }

            if (tx.Status != "Success")
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = new { transactionId = tx.Id, success = false, status = tx.Status },
                    ErrorStatus = true,
                    Message = tx.Message ?? "cardPaymentTransactionNotSuccessful"
                });
            }

            if (tx.CustomerOrderId.HasValue)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = new { transactionId = tx.Id, success = false },
                    ErrorStatus = true,
                    Message = "cardPaymentTransactionAlreadyLinked"
                });
            }

            return Ok(new GlobalResponse<object>
            {
                Data = new
                {
                    transactionId = tx.Id,
                    success = true,
                    authCode = tx.AuthCode,
                    refNo = tx.RefNo,
                    cardNo = tx.CardNo
                },
                ErrorStatus = false,
                Message = "cardPaymentSuccess"
            });
        }

        [Authorize(Roles = "Commercial,POS")]
        [HttpGet("recover")]
        public async Task<ActionResult<GlobalResponse<object>>> RecoverRecentSale(
            [FromQuery] decimal amount,
            [FromQuery] int? transactionId = null,
            [FromQuery] int withinMinutes = 15)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var commercialUserId = GetCommercialUserId();
            var since = DateTime.UtcNow.AddMinutes(-Math.Clamp(withinMinutes, 1, 60));
            var amountRounded = (long)Math.Round(amount, MidpointRounding.AwayFromZero);

            if (transactionId.HasValue)
            {
                var specific = await _dbConfig.CardPaymentTransactions
                    .FirstOrDefaultAsync(t =>
                        t.Id == transactionId.Value &&
                        !t.IsDeleted &&
                        t.InsertByUserId == commercialUserId &&
                        t.RequestedByUserId == userId &&
                        t.CustomerOrderId == null);

                if (specific != null)
                {
                    var recovered = await TryRecoverCandidateAsync(specific);
                    if (recovered != null)
                    {
                        return Ok(recovered);
                    }
                }
            }

            var tx = await _dbConfig.CardPaymentTransactions
                .Where(t =>
                    !t.IsDeleted &&
                    t.InsertByUserId == commercialUserId &&
                    t.RequestedByUserId == userId &&
                    t.CustomerOrderId == null &&
                    t.InsertDate >= since)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            tx = tx
                .Where(t => (long)Math.Round(t.Amount, MidpointRounding.AwayFromZero) == amountRounded)
                .ToList();

            foreach (var candidate in tx)
            {
                var recovered = await TryRecoverCandidateAsync(candidate);
                if (recovered != null)
                {
                    return Ok(recovered);
                }
            }

            if (tx.Count == 0)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "cardPaymentTransactionNotFound"
                });
            }

            return NotFound(new GlobalResponse<object>
            {
                Data = null,
                ErrorStatus = true,
                Message = "cardPaymentTransactionNotSuccessful"
            });
        }

        private async Task<GlobalResponse<object>?> TryRecoverCandidateAsync(CardPaymentTransaction candidate)
        {
            if (candidate.Status != "Success")
            {
                ReconcileTransactionFromRaw(candidate);
                if (IsTransactionSuccessful(candidate))
                {
                    candidate.Status = "Success";
                    candidate.UpdateDate = DateTime.UtcNow;
                    await _dbConfig.SaveChangesAsync();
                }
            }

            if (candidate.Status != "Success")
            {
                return null;
            }

            return new GlobalResponse<object>
            {
                Data = new
                {
                    transactionId = candidate.Id,
                    success = true,
                    authCode = candidate.AuthCode,
                    refNo = candidate.RefNo,
                    cardNo = candidate.CardNo
                },
                ErrorStatus = false,
                Message = "cardPaymentSuccess"
            };
        }

        [AuthorizeSection("cardPayments", "reports", Roles = "Commercial,Admin")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<object>>> List(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? status,
            [FromQuery] string? linkStatus,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var commercialUserId = GetCommercialUserId();
            var query = _dbConfig.CardPaymentTransactions
                .Include(t => t.PaymentDevice)
                .Include(t => t.CustomerOrder)
                .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (startDate.HasValue)
            {
                query = query.Where(t => t.InsertDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => t.InsertDate <= endDate.Value);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }
            if (!string.IsNullOrWhiteSpace(linkStatus))
            {
                query = ApplyLinkStatusFilter(query, linkStatus);
            }

            var total = await query.CountAsync();
            var rows = await query
                .OrderByDescending(t => t.InsertDate)
                .Skip((Math.Max(pageNumber, 1) - 1) * Math.Max(pageSize, 1))
                .Take(Math.Max(pageSize, 1))
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
                    t.TipAmount,
                    t.CurrencyCode,
                    t.Status,
                    t.AuthCode,
                    t.RefNo,
                    t.CardNo,
                    t.TerminalId,
                    t.MerchantName,
                    t.TransTime,
                    t.InsertDate,
                    OrderCode = t.CustomerOrder != null ? t.CustomerOrder.OrderCode : null,
                    CustomerOrderId = t.CustomerOrderId,
                    DeviceName = t.PaymentDevice != null ? t.PaymentDevice.Name : null
                })
                .ToListAsync();

            var items = rows.Select(t => new
            {
                t.Id,
                t.Amount,
                t.TipAmount,
                t.CurrencyCode,
                t.Status,
                t.AuthCode,
                t.RefNo,
                t.CardNo,
                t.TerminalId,
                t.MerchantName,
                t.TransTime,
                t.InsertDate,
                t.OrderCode,
                t.CustomerOrderId,
                t.DeviceName,
                LinkStatus = ResolveLinkStatus(t.Status, t.CustomerOrderId)
            }).ToList();

            return Ok(new GlobalResponse<object>
            {
                Data = new { items, total, pageNumber, pageSize },
                ErrorStatus = false
            });
        }

        [AuthorizeSection("cardPayments", "reports", Roles = "Commercial,Admin")]
        [HttpPost("recheck")]
        public async Task<ActionResult<GlobalResponse<object>>> RecheckLinkResults(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? status,
            [FromQuery] bool onlyNotMatched = true)
        {
            var commercialUserId = GetCommercialUserId();
            var query = _dbConfig.CardPaymentTransactions
                .Where(t => !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (startDate.HasValue)
            {
                query = query.Where(t => t.InsertDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => t.InsertDate <= endDate.Value);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }
            if (onlyNotMatched)
            {
                query = query.Where(t =>
                    t.CustomerOrderId == null ||
                    t.Status != "Success");
            }

            var transactions = await query
                .OrderByDescending(t => t.InsertDate)
                .Take(500)
                .ToListAsync();

            var results = new List<object>();
            var matchedCount = 0;
            var updatedCount = 0;

            foreach (var tx in transactions)
            {
                var previousStatus = tx.Status;
                var previousLink = ResolveLinkStatus(tx.Status, tx.CustomerOrderId);
                var changed = await RecheckTransactionAsync(tx, commercialUserId);
                var currentLink = ResolveLinkStatus(tx.Status, tx.CustomerOrderId);

                if (changed)
                {
                    updatedCount++;
                }
                if (currentLink == "Matched")
                {
                    matchedCount++;
                }

                if (changed || currentLink != "Matched")
                {
                    results.Add(new
                    {
                        id = tx.Id,
                        previousStatus,
                        status = tx.Status,
                        previousLink,
                        linkStatus = currentLink,
                        customerOrderId = tx.CustomerOrderId,
                        changed
                    });
                }
            }

            return Ok(new GlobalResponse<object>
            {
                Data = new
                {
                    totalChecked = transactions.Count,
                    updatedCount,
                    matchedCount,
                    stillNotMatched = transactions.Count(t =>
                        ResolveLinkStatus(t.Status, t.CustomerOrderId) != "Matched"),
                    results
                },
                ErrorStatus = false,
                Message = "cardPaymentRecheckCompleted"
            });
        }

        [AuthorizeSection("cardPayments", "reports", Roles = "Commercial,Admin")]
        [HttpPost("{id}/recheck")]
        public async Task<ActionResult<GlobalResponse<object>>> RecheckSingle(int id)
        {
            var commercialUserId = GetCommercialUserId();
            var tx = await _dbConfig.CardPaymentTransactions
                .Include(t => t.CustomerOrder)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (tx == null)
            {
                return NotFound(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "notFound"
                });
            }

            var previousStatus = tx.Status;
            var previousLink = ResolveLinkStatus(tx.Status, tx.CustomerOrderId);
            var changed = await RecheckTransactionAsync(tx, commercialUserId);
            var linkStatus = ResolveLinkStatus(tx.Status, tx.CustomerOrderId);

            return Ok(new GlobalResponse<object>
            {
                Data = new
                {
                    id = tx.Id,
                    previousStatus,
                    status = tx.Status,
                    previousLink,
                    linkStatus,
                    customerOrderId = tx.CustomerOrderId,
                    orderCode = tx.CustomerOrder?.OrderCode,
                    changed
                },
                ErrorStatus = false,
                Message = linkStatus == "Matched" ? "cardPaymentLinkMatched" : "cardPaymentRecheckCompleted"
            });
        }

        [AuthorizeSection("cardPayments", "reports", Roles = "Commercial,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<CardPaymentTransaction>>> Get(int id)
        {
            var commercialUserId = GetCommercialUserId();
            var tx = await _dbConfig.CardPaymentTransactions
                .Include(t => t.PaymentDevice)
                .Include(t => t.CustomerOrder)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.InsertByUserId == commercialUserId);

            if (tx == null)
            {
                return NotFound(new GlobalResponse<CardPaymentTransaction>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "notFound"
                });
            }

            return Ok(new GlobalResponse<CardPaymentTransaction> { Data = tx, ErrorStatus = false });
        }

        private static string ResolveLinkStatus(string? status, int? customerOrderId)
        {
            if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase) && customerOrderId.HasValue)
            {
                return "Matched";
            }

            if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return "Unmatched";
            }

            if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed";
            }

            return "Pending";
        }

        private static IQueryable<CardPaymentTransaction> ApplyLinkStatusFilter(
            IQueryable<CardPaymentTransaction> query,
            string linkStatus)
        {
            return linkStatus.Trim().ToLowerInvariant() switch
            {
                "matched" => query.Where(t => t.Status == "Success" && t.CustomerOrderId != null),
                "unmatched" => query.Where(t => t.Status == "Success" && t.CustomerOrderId == null),
                "failed" => query.Where(t => t.Status == "Failed"),
                "pending" => query.Where(t => t.Status != "Success" && t.Status != "Failed"),
                _ => query.Where(t =>
                    t.CustomerOrderId == null ||
                    t.Status != "Success")
            };
        }

        private async Task<bool> RecheckTransactionAsync(CardPaymentTransaction tx, int commercialUserId)
        {
            var changed = false;
            var previousStatus = tx.Status;
            var previousOrderId = tx.CustomerOrderId;

            if (tx.Status != "Success")
            {
                ReconcileTransactionFromRaw(tx);
                if (IsTransactionSuccessful(tx))
                {
                    tx.Status = "Success";
                    changed = true;
                }
            }

            if (tx.Status == "Success" && !tx.CustomerOrderId.HasValue)
            {
                if (await TryAutoLinkOrderAsync(tx, commercialUserId))
                {
                    changed = true;
                }
            }

            if (changed || previousStatus != tx.Status || previousOrderId != tx.CustomerOrderId)
            {
                tx.UpdateDate = DateTime.UtcNow;
                await _dbConfig.SaveChangesAsync();
                return true;
            }

            return false;
        }

        private async Task<bool> TryAutoLinkOrderAsync(CardPaymentTransaction tx, int commercialUserId)
        {
            if (tx.CustomerOrderId.HasValue || !string.Equals(tx.Status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var windowStart = tx.InsertDate.AddMinutes(-90);
            var windowEnd = tx.InsertDate.AddMinutes(90);
            var txAmount = (long)Math.Round(tx.Amount, MidpointRounding.AwayFromZero);

            var linkedOrderIds = await _dbConfig.CardPaymentTransactions
                .Where(c => !c.IsDeleted && c.CustomerOrderId != null)
                .Select(c => c.CustomerOrderId!.Value)
                .ToListAsync();

            var candidateOrders = await _dbConfig.CustomerOrders
                .Where(o =>
                    !o.IsDeleted &&
                    o.InsertByUserId == commercialUserId &&
                    o.PaymentMethod == "Card" &&
                    o.InsertDate >= windowStart &&
                    o.InsertDate <= windowEnd &&
                    !linkedOrderIds.Contains(o.Id))
                .OrderByDescending(o => o.InsertDate)
                .ToListAsync();

            var matchedOrder = candidateOrders
                .Select(o => new
                {
                    Order = o,
                    Amount = (long)Math.Round(
                        o.OrderTotalAfterDiscount ?? o.OrderSubTotal ?? 0,
                        MidpointRounding.AwayFromZero)
                })
                .Where(x => x.Amount == txAmount)
                .OrderBy(x => Math.Abs((x.Order.InsertDate - tx.InsertDate).TotalSeconds))
                .Select(x => x.Order)
                .FirstOrDefault();

            if (matchedOrder == null)
            {
                return false;
            }

            tx.CustomerOrderId = matchedOrder.Id;
            return true;
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

        private async Task<(bool Success, string? Message)> ConnectDeviceAsync(PaymentDevice device)
        {
            switch (device.ConnectionType?.ToLowerInvariant())
            {
                case "wifi":
                    return await _nebula.ConnectWifiAsync(
                        device.BaseUrl,
                        device.WifiHost ?? "localhost",
                        device.WifiPort ?? 0,
                        device.WifiConfigJson ?? "{}");
                case "cloud":
                    return await _nebula.ConnectCloudAsync(
                        device.BaseUrl,
                        device.CloudConfigJson ?? "{}");
                default:
                    return await _nebula.ConnectUsbAsync(
                        device.BaseUrl,
                        device.ComPort ?? "COM6");
            }
        }
    }
}

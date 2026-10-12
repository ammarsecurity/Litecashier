using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Services;
using System.Security.Claims;

namespace RestaurantPOS.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
[EnableCors("CorsPolicy")]
public class CreditAccountsController : ControllerBase
{
    private readonly ICreditAccountService _creditAccountService;
    private readonly DbConfig _db;

    public CreditAccountsController(ICreditAccountService creditAccountService, DbConfig db)
    {
        _creditAccountService = creditAccountService;
        _db = db;
    }

    private int GetCommercialUserId()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var user = _db.Users.FirstOrDefault(x => x.Id == userId);
        if (user != null && user.Role == "Commercial")
        {
            return userId;
        }

        return user?.InsertByUserId ?? userId;
    }

    private int GetActingUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

    [AuthorizeSection("deferredPayments", Roles = "Commercial,Admin")]
    [HttpGet("summary")]
    public async Task<ActionResult<GlobalResponse<CreditAccountsOverviewDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        var data = await _creditAccountService.GetOverviewAsync(commercialUserId, cancellationToken);
        return Ok(new GlobalResponse<CreditAccountsOverviewDto> { Data = data, ErrorStatus = false });
    }

    [AuthorizeSection("deferredPayments", Roles = "Commercial,Admin")]
    [HttpGet("{accountType}/{accountId:int}/orders")]
    public async Task<ActionResult<GlobalResponse<CreditAccountDetailDto>>> GetAccountOrders(
        string accountType,
        int accountId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        var data = await _creditAccountService.GetAccountDetailAsync(
            commercialUserId,
            accountType,
            accountId,
            status,
            cancellationToken);

        if (data == null)
        {
            return NotFound(new GlobalResponse<CreditAccountDetailDto>
            {
                Data = null,
                ErrorStatus = true,
                Message = "accountNotFound",
            });
        }

        return Ok(new GlobalResponse<CreditAccountDetailDto> { Data = data, ErrorStatus = false });
    }

    [AuthorizeSection("deferredPayments", Roles = "Commercial,Admin")]
    [HttpPost("settle")]
    public async Task<ActionResult<GlobalResponse<SettleCreditOrderResultDto>>> SettleOrder(
        [FromBody] SettleCreditOrderRequest request,
        CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        var (ok, error, result) = await _creditAccountService.SettleOrderAsync(
            commercialUserId,
            GetActingUserId(),
            request,
            cancellationToken);

        if (!ok)
        {
            return BadRequest(new GlobalResponse<SettleCreditOrderResultDto>
            {
                Data = null,
                ErrorStatus = true,
                Message = error,
            });
        }

        return Ok(new GlobalResponse<SettleCreditOrderResultDto>
        {
            Data = result,
            ErrorStatus = false,
            Message = "settleSuccess",
        });
    }

    [AuthorizeSection("deferredPayments", Roles = "Commercial,Admin")]
    [HttpPost("orders")]
    public async Task<ActionResult<GlobalResponse<CreditAccountOrderDto>>> CreateOrder(
        [FromBody] CreateCreditOrderRequest request,
        CancellationToken cancellationToken)
    {
        var (ok, error, result) = await _creditAccountService.CreateOrderAsync(
            GetCommercialUserId(),
            GetActingUserId(),
            request,
            cancellationToken);

        if (!ok)
        {
            return BadRequest(new GlobalResponse<CreditAccountOrderDto>
            {
                Data = null,
                ErrorStatus = true,
                Message = error,
            });
        }

        return Ok(new GlobalResponse<CreditAccountOrderDto>
        {
            Data = result,
            ErrorStatus = false,
            Message = "creditOrderCreated",
        });
    }

    [AuthorizeSection("deferredPayments", Roles = "Commercial,Admin")]
    [HttpPut("orders/{orderId:int}")]
    public async Task<ActionResult<GlobalResponse<CreditAccountOrderDto>>> UpdateOrder(
        int orderId,
        [FromBody] UpdateCreditOrderRequest request,
        CancellationToken cancellationToken)
    {
        var (ok, error, result) = await _creditAccountService.UpdateOrderAsync(
            GetCommercialUserId(),
            GetActingUserId(),
            orderId,
            request,
            cancellationToken);

        if (!ok)
        {
            return BadRequest(new GlobalResponse<CreditAccountOrderDto>
            {
                Data = null,
                ErrorStatus = true,
                Message = error,
            });
        }

        return Ok(new GlobalResponse<CreditAccountOrderDto>
        {
            Data = result,
            ErrorStatus = false,
            Message = "creditOrderUpdated",
        });
    }

    [AuthorizeSection("deferredPayments", Roles = "Commercial,Admin")]
    [HttpDelete("orders/{orderId:int}")]
    public async Task<ActionResult<GlobalResponse<object>>> DeleteOrder(
        int orderId,
        CancellationToken cancellationToken)
    {
        var (ok, error) = await _creditAccountService.DeleteOrderAsync(
            GetCommercialUserId(),
            GetActingUserId(),
            orderId,
            cancellationToken);

        if (!ok)
        {
            return BadRequest(new GlobalResponse<object>
            {
                Data = null,
                ErrorStatus = true,
                Message = error,
            });
        }

        return Ok(new GlobalResponse<object>
        {
            Data = null,
            ErrorStatus = false,
            Message = "creditOrderDeleted",
        });
    }

    [AuthorizeSection("deferredPayments", Roles = "Commercial,Admin")]
    [HttpDelete("{accountType}/{accountId:int}")]
    public async Task<ActionResult<GlobalResponse<object>>> DeleteAccount(
        string accountType,
        int accountId,
        CancellationToken cancellationToken)
    {
        var (ok, error) = await _creditAccountService.DeleteAccountAsync(
            GetCommercialUserId(),
            GetActingUserId(),
            accountType,
            accountId,
            cancellationToken);

        if (!ok)
        {
            return BadRequest(new GlobalResponse<object>
            {
                Data = null,
                ErrorStatus = true,
                Message = error,
            });
        }

        return Ok(new GlobalResponse<object>
        {
            Data = null,
            ErrorStatus = false,
            Message = "creditAccountDeleted",
        });
    }
}

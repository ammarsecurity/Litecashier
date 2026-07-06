using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using POS.Authorization;
using POS.Db;
using POS.Models.Response;
using POS.Services;
using System.Security.Claims;

namespace POS.Controllers;

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
}

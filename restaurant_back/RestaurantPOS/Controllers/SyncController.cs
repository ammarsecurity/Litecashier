using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RestaurantPOS.Authorization;
using RestaurantPOS.Db;
using RestaurantPOS.Models.Response;
using RestaurantPOS.Services.Sync;
using System.Security.Claims;

namespace RestaurantPOS.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
[EnableCors("CorsPolicy")]
public class SyncController : ControllerBase
{
    private readonly IDatabaseSyncService _syncService;
    private readonly DbConfig _db;

    public SyncController(IDatabaseSyncService syncService, DbConfig db)
    {
        _syncService = syncService;
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

    [AuthorizeSection("databaseSync", Roles = "Commercial,Admin")]
    [HttpGet("status")]
    public async Task<ActionResult<GlobalResponse<SyncStatusDto>>> GetStatus(CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        var status = await _syncService.GetStatusAsync(commercialUserId, cancellationToken);
        return Ok(new GlobalResponse<SyncStatusDto> { Data = status, ErrorStatus = false });
    }

    [AuthorizeSection("databaseSync", Roles = "Commercial,Admin")]
    [HttpPost("push")]
    public async Task<ActionResult<GlobalResponse<SyncPushResultDto>>> Push(CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        var result = await _syncService.PushAsync(commercialUserId, SyncTriggers.Manual, cancellationToken);
        return Ok(new GlobalResponse<SyncPushResultDto>
        {
            Data = result,
            ErrorStatus = !result.Success,
            Message = result.Message,
        });
    }

    [AuthorizeSection("databaseSync", Roles = "Commercial,Admin")]
    [HttpGet("history")]
    public async Task<ActionResult<GlobalResponse<List<SyncRunDto>>>> GetHistory(CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        var history = await _syncService.GetHistoryAsync(commercialUserId, 30, cancellationToken);
        return Ok(new GlobalResponse<List<SyncRunDto>> { Data = history.ToList(), ErrorStatus = false });
    }

    [AuthorizeSection("databaseSync", Roles = "Commercial,Admin")]
    [HttpDelete("history")]
    public async Task<ActionResult<GlobalResponse<int>>> ClearHistory(CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        try
        {
            var deleted = await _syncService.ClearHistoryAsync(commercialUserId, cancellationToken);
            return Ok(new GlobalResponse<int> { Data = deleted, ErrorStatus = false, Message = "cleared" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "syncInProgress")
        {
            return BadRequest(new GlobalResponse<int>
            {
                Data = 0,
                ErrorStatus = true,
                Message = "syncInProgress",
            });
        }
    }

    [AuthorizeSection("databaseSync", Roles = "Commercial,Admin")]
    [HttpGet("settings")]
    public async Task<ActionResult<GlobalResponse<SyncSettingsDto>>> GetSettings(CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        var settings = await _syncService.GetSettingsAsync(commercialUserId, cancellationToken);
        return Ok(new GlobalResponse<SyncSettingsDto> { Data = settings, ErrorStatus = false });
    }

    [AuthorizeSection("databaseSync", Roles = "Commercial,Admin")]
    [HttpPut("settings")]
    public async Task<ActionResult<GlobalResponse<SyncSettingsDto>>> UpdateSettings(
        [FromBody] UpdateSyncSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var commercialUserId = GetCommercialUserId();
        var settings = await _syncService.UpdateSettingsAsync(commercialUserId, request, cancellationToken);
        return Ok(new GlobalResponse<SyncSettingsDto> { Data = settings, ErrorStatus = false, Message = "updated" });
    }

    [AuthorizeSection("databaseSync", Roles = "Commercial,Admin")]
    [HttpPost("test-connection")]
    public async Task<ActionResult<GlobalResponse<SyncConnectionTestDto>>> TestConnection(CancellationToken cancellationToken)
    {
        var result = await _syncService.TestConnectionsAsync(cancellationToken);
        return Ok(new GlobalResponse<SyncConnectionTestDto> { Data = result, ErrorStatus = false });
    }
}

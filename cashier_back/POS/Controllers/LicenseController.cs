using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Services;

namespace POS.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class LicenseController : ControllerBase
{
    private readonly ILicenseService _licenseService;

    public LicenseController(ILicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpGet("status")]
    public IActionResult Status() => Ok(_licenseService.GetStatus());

    [HttpGet("machine-id")]
    public IActionResult MachineId() => Ok(new { machineId = _licenseService.GetMachineId() });

    [HttpGet("connectivity")]
    public async Task<IActionResult> Connectivity(CancellationToken ct)
    {
        var online = await _licenseService.CanReachLicenseServerAsync(ct);
        return Ok(new
        {
            online,
            enforcementEnabled = _licenseService.EnforcementEnabled
        });
    }

    public class ActivateBody
    {
        public string? Code { get; set; }
    }

    [HttpPost("activate")]
    public async Task<IActionResult> Activate([FromBody] ActivateBody body, CancellationToken ct)
    {
        try
        {
            var status = await _licenseService.ActivateAsync(body?.Code ?? "", ct);
            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message, status = _licenseService.GetStatus() });
        }
    }
}

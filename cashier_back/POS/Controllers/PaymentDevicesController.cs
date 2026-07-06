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
    public class PaymentDevicesController : ControllerBase
    {
        private readonly DbConfig _dbConfig;
        private readonly INebulaPaymentService _nebula;
        private readonly ILogger<PaymentDevicesController> _logger;

        public PaymentDevicesController(
            DbConfig dbConfig,
            INebulaPaymentService nebula,
            ILogger<PaymentDevicesController> logger)
        {
            _dbConfig = dbConfig;
            _nebula = nebula;
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

        [AuthorizeSection("paymentDevices", Roles = "Commercial,Admin")]
        [HttpGet]
        public async Task<ActionResult<GlobalResponse<List<PaymentDevice>>>> GetDevices()
        {
            var commercialUserId = GetCommercialUserId();
            var devices = await _dbConfig.PaymentDevices
                .Where(d => !d.IsDeleted && d.InsertByUserId == commercialUserId)
                .OrderByDescending(d => d.IsDefault)
                .ThenBy(d => d.Name)
                .ToListAsync();

            return Ok(new GlobalResponse<List<PaymentDevice>>
            {
                Data = devices,
                ErrorStatus = false,
                Message = "success"
            });
        }

        [AuthorizeSection("paymentDevices", Roles = "Commercial,Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GlobalResponse<PaymentDevice>>> GetDevice(int id)
        {
            var commercialUserId = GetCommercialUserId();
            var device = await _dbConfig.PaymentDevices
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.InsertByUserId == commercialUserId);

            if (device == null)
            {
                return NotFound(new GlobalResponse<PaymentDevice>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "notFound"
                });
            }

            return Ok(new GlobalResponse<PaymentDevice> { Data = device, ErrorStatus = false });
        }

        [AuthorizeSection("paymentDevices", Roles = "Commercial,Admin")]
        [HttpPost]
        public async Task<ActionResult<GlobalResponse<PaymentDevice>>> CreateDevice([FromBody] PaymentDeviceRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            if (request.IsDefault)
            {
                var existing = await _dbConfig.PaymentDevices
                    .Where(d => !d.IsDeleted && d.InsertByUserId == commercialUserId && d.IsDefault)
                    .ToListAsync();
                foreach (var d in existing)
                {
                    d.IsDefault = false;
                }
            }

            var device = MapRequest(request, commercialUserId);
            _dbConfig.PaymentDevices.Add(device);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<PaymentDevice> { Data = device, ErrorStatus = false, Message = "created" });
        }

        [AuthorizeSection("paymentDevices", Roles = "Commercial,Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<GlobalResponse<PaymentDevice>>> UpdateDevice(int id, [FromBody] PaymentDeviceRequest request)
        {
            var commercialUserId = GetCommercialUserId();
            var device = await _dbConfig.PaymentDevices
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.InsertByUserId == commercialUserId);

            if (device == null)
            {
                return NotFound(new GlobalResponse<PaymentDevice> { Data = null, ErrorStatus = true, Message = "notFound" });
            }

            if (request.IsDefault)
            {
                var others = await _dbConfig.PaymentDevices
                    .Where(d => !d.IsDeleted && d.InsertByUserId == commercialUserId && d.IsDefault && d.Id != id)
                    .ToListAsync();
                foreach (var d in others)
                {
                    d.IsDefault = false;
                }
            }

            ApplyRequest(device, request);
            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<PaymentDevice> { Data = device, ErrorStatus = false, Message = "updated" });
        }

        [AuthorizeSection("paymentDevices", Roles = "Commercial,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<GlobalResponse<object>>> DeleteDevice(int id)
        {
            var commercialUserId = GetCommercialUserId();
            var device = await _dbConfig.PaymentDevices
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.InsertByUserId == commercialUserId);

            if (device == null)
            {
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "notFound" });
            }

            device.IsDeleted = true;
            device.IsDefault = false;
            await _dbConfig.SaveChangesAsync();
            return Ok(new GlobalResponse<object> { Data = null, ErrorStatus = false, Message = "deleted" });
        }

        [AuthorizeSection("paymentDevices", Roles = "Commercial,Admin")]
        [HttpGet("{id}/status")]
        public async Task<ActionResult<GlobalResponse<object>>> GetStatus(int id)
        {
            var device = await GetOwnedDevice(id);
            if (device == null)
            {
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "notFound" });
            }

            var status = await _nebula.IsConnectedAsync(device.BaseUrl);
            return Ok(new GlobalResponse<object>
            {
                Data = new { connected = status, raw = status },
                ErrorStatus = false
            });
        }

        [AuthorizeSection("paymentDevices", Roles = "Commercial,Admin")]
        [HttpPost("{id}/connect")]
        public async Task<ActionResult<GlobalResponse<object>>> Connect(int id, [FromBody] PaymentDeviceConnectRequest? request)
        {
            var device = await GetOwnedDevice(id);
            if (device == null)
            {
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "notFound" });
            }

            request ??= new PaymentDeviceConnectRequest();
            bool success;
            string? message;

            switch (device.ConnectionType?.ToLowerInvariant())
            {
                case "wifi":
                    (success, message) = await _nebula.ConnectWifiAsync(
                        device.BaseUrl,
                        request.WifiHost ?? device.WifiHost ?? "localhost",
                        request.WifiPort ?? device.WifiPort ?? 0,
                        request.WifiConfigJson ?? device.WifiConfigJson ?? "{}");
                    break;
                case "cloud":
                    (success, message) = await _nebula.ConnectCloudAsync(
                        device.BaseUrl,
                        request.CloudConfigJson ?? device.CloudConfigJson ?? "{}");
                    break;
                default:
                    (success, message) = await _nebula.ConnectUsbAsync(
                        device.BaseUrl,
                        request.ComPort ?? device.ComPort ?? "COM6");
                    break;
            }

            return Ok(new GlobalResponse<object>
            {
                Data = new { success, message },
                ErrorStatus = !success,
                Message = message
            });
        }

        [AuthorizeSection("paymentDevices", Roles = "Commercial,Admin")]
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<GlobalResponse<object>>> CancelOngoing(int id)
        {
            var device = await GetOwnedDevice(id);
            if (device == null)
            {
                return NotFound(new GlobalResponse<object> { Data = null, ErrorStatus = true, Message = "notFound" });
            }

            var (success, message) = await _nebula.CancelTransAsync(device.BaseUrl);
            return Ok(new GlobalResponse<object>
            {
                Data = new { success, message },
                ErrorStatus = !success,
                Message = message
            });
        }

        private async Task<PaymentDevice?> GetOwnedDevice(int id)
        {
            var commercialUserId = GetCommercialUserId();
            return await _dbConfig.PaymentDevices
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.InsertByUserId == commercialUserId);
        }

        private static PaymentDevice MapRequest(PaymentDeviceRequest request, int commercialUserId)
        {
            var device = new PaymentDevice { InsertByUserId = commercialUserId };
            ApplyRequest(device, request);
            return device;
        }

        private static void ApplyRequest(PaymentDevice device, PaymentDeviceRequest request)
        {
            device.Name = request.Name?.Trim() ?? device.Name;
            device.BaseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? "http://localhost:9092" : request.BaseUrl.Trim();
            device.ConnectionType = string.IsNullOrWhiteSpace(request.ConnectionType) ? "Usb" : request.ConnectionType.Trim();
            device.ComPort = request.ComPort;
            device.WifiHost = request.WifiHost;
            device.WifiPort = request.WifiPort;
            device.WifiConfigJson = request.WifiConfigJson;
            device.CloudConfigJson = request.CloudConfigJson;
            device.IsDefault = request.IsDefault;
            device.IsActive = request.IsActive;
        }
    }
}

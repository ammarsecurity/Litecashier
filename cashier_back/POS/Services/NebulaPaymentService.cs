using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POS.Services
{
    public class NebulaSaleResult
    {
        public bool Success { get; set; }
        public string? ResultCode { get; set; }
        public string? Message { get; set; }
        public string? RawOuterResponse { get; set; }
        public string? InnerResponseJson { get; set; }
        public NebulaTransDetails? Details { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class NebulaTransDetails
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("authCode")]
        public string? AuthCode { get; set; }

        [JsonPropertyName("refNo")]
        public string? RefNo { get; set; }

        [JsonPropertyName("cardNo")]
        public string? CardNo { get; set; }

        [JsonPropertyName("cardType")]
        public string? CardType { get; set; }

        [JsonPropertyName("issuerName")]
        public string? IssuerName { get; set; }

        [JsonPropertyName("acquirerName")]
        public string? AcquirerName { get; set; }

        [JsonPropertyName("terminalId")]
        public string? TerminalId { get; set; }

        [JsonPropertyName("merchantId")]
        public string? MerchantId { get; set; }

        [JsonPropertyName("merchantName")]
        public string? MerchantName { get; set; }

        [JsonPropertyName("voucherNo")]
        public long? VoucherNo { get; set; }

        [JsonPropertyName("batchNo")]
        public long? BatchNo { get; set; }

        [JsonPropertyName("transTime")]
        public string? TransTime { get; set; }

        [JsonPropertyName("totalAmount")]
        public string? TotalAmount { get; set; }

        [JsonPropertyName("amount")]
        public string? Amount { get; set; }
    }

    public class NebulaApiResponse
    {
        [JsonPropertyName("resultCode")]
        public string? ResultCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }

    public static class NebulaResponseParser
    {
        private static readonly JsonSerializerOptions InnerJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string? NormalizeResultCode(string? rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                if (!doc.RootElement.TryGetProperty("resultCode", out var codeEl))
                {
                    return null;
                }

                return codeEl.ValueKind switch
                {
                    JsonValueKind.String => codeEl.GetString(),
                    JsonValueKind.Number => codeEl.GetRawText(),
                    JsonValueKind.True => "200",
                    JsonValueKind.False => "0",
                    _ => codeEl.ToString()
                };
            }
            catch
            {
                return null;
            }
        }

        public static string? ExtractInnerResponse(string? rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                if (!doc.RootElement.TryGetProperty("response", out var responseEl))
                {
                    return null;
                }

                if (responseEl.ValueKind == JsonValueKind.String)
                {
                    return responseEl.GetString();
                }

                return responseEl.GetRawText();
            }
            catch
            {
                return null;
            }
        }

        public static NebulaTransDetails? TryParseInnerDetails(string? innerJson)
        {
            if (string.IsNullOrWhiteSpace(innerJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<NebulaTransDetails>(innerJson, InnerJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsOuterResultSuccessful(string? resultCode)
        {
            return string.Equals(resultCode?.Trim(), "200", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsInnerPaymentSuccessful(string? innerJson, NebulaTransDetails? details = null)
        {
            details ??= TryParseInnerDetails(innerJson);

            if (details?.IsSuccess == true)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(details?.AuthCode))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(innerJson))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(innerJson);
                var root = doc.RootElement;

                foreach (var propertyName in new[] { "isSuccess", "IsSuccess", "success", "Success" })
                {
                    if (!root.TryGetProperty(propertyName, out var prop))
                    {
                        continue;
                    }

                    if (prop.ValueKind == JsonValueKind.True)
                    {
                        return true;
                    }

                    if (prop.ValueKind == JsonValueKind.String &&
                        string.Equals(prop.GetString(), "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                foreach (var authName in new[] { "authCode", "AuthCode" })
                {
                    if (root.TryGetProperty(authName, out var authProp) &&
                        authProp.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(authProp.GetString()))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // ignore parse errors
            }

            return false;
        }

        public static bool TryEvaluateSaleSuccess(string? rawOuterBody, out NebulaTransDetails? details, out string? resultCode, out string? innerJson)
        {
            details = null;
            innerJson = null;
            resultCode = NormalizeResultCode(rawOuterBody);

            if (!IsOuterResultSuccessful(resultCode))
            {
                return false;
            }

            innerJson = ExtractInnerResponse(rawOuterBody);
            if (string.IsNullOrWhiteSpace(innerJson))
            {
                return false;
            }

            details = TryParseInnerDetails(innerJson);
            return IsInnerPaymentSuccessful(innerJson, details);
        }
    }

    public interface INebulaPaymentService
    {
        Task<string?> IsConnectedAsync(string baseUrl, CancellationToken cancellationToken = default);
        Task<NebulaSaleResult> CreateSaleAsync(string baseUrl, long amount, long tipAmount, string currencyCode, CancellationToken cancellationToken = default);
        Task<(bool Success, string? Message)> ConnectUsbAsync(string baseUrl, string comPort, CancellationToken cancellationToken = default);
        Task<(bool Success, string? Message)> ConnectWifiAsync(string baseUrl, string host, int port, string jsonBody, CancellationToken cancellationToken = default);
        Task<(bool Success, string? Message)> ConnectCloudAsync(string baseUrl, string jsonBody, CancellationToken cancellationToken = default);
        Task<(bool Success, string? Message)> CancelTransAsync(string baseUrl, CancellationToken cancellationToken = default);
    }

    public class NebulaPaymentService : INebulaPaymentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NebulaPaymentService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions NebulaRequestJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public NebulaPaymentService(IHttpClientFactory httpClientFactory, ILogger<NebulaPaymentService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private HttpClient CreateClient(string baseUrl)
        {
            var client = _httpClientFactory.CreateClient("NebulaPayment");
            client.BaseAddress = new Uri(NormalizeBaseUrl(baseUrl));
            client.Timeout = TimeSpan.FromSeconds(180);
            return client;
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            var url = (baseUrl ?? "http://localhost:9092").Trim().TrimEnd('/');
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }
            return url + "/";
        }

        /// <summary>
        /// PAX Nebula expects IQD amounts with 3 implicit decimal places (e.g. 2000 IQD → 2000000).
        /// </summary>
        private const int IqdDeviceAmountFactor = 1000;

        private static long ToDeviceAmount(long displayAmount, string? currencyCode)
        {
            if (displayAmount <= 0)
            {
                return displayAmount;
            }

            if (string.IsNullOrWhiteSpace(currencyCode) ||
                string.Equals(currencyCode.Trim(), "IQD", StringComparison.OrdinalIgnoreCase))
            {
                return checked(displayAmount * IqdDeviceAmountFactor);
            }

            return displayAmount;
        }

        public async Task<string?> IsConnectedAsync(string baseUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl);
                var response = await client.GetAsync("isConnected", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return body;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nebula isConnected failed for {BaseUrl}", baseUrl);
                return null;
            }
        }

        public async Task<NebulaSaleResult> CreateSaleAsync(string baseUrl, long amount, long tipAmount, string currencyCode, CancellationToken cancellationToken = default)
        {
            var result = new NebulaSaleResult();
            try
            {
                var currency = string.IsNullOrWhiteSpace(currencyCode) ? "IQD" : currencyCode;
                var deviceAmount = ToDeviceAmount(amount, currency);
                var deviceTip = ToDeviceAmount(tipAmount, currency);

                // Nebula requires exact key "CATEGORY" (not camelCase "cATEGORY").
                var payload = new Dictionary<string, object>
                {
                    ["CATEGORY"] = "com.pax.payment.Sale",
                    ["parm"] = new Dictionary<string, object>
                    {
                        ["amount"] = deviceAmount,
                        ["tipAmount"] = deviceTip,
                        ["currencyCode"] = currency
                    }
                };

                var client = CreateClient(baseUrl);
                var json = JsonSerializer.Serialize(payload, NebulaRequestJsonOptions);
                _logger.LogDebug(
                    "Nebula createRequest payload (display amount {DisplayAmount} → device amount {DeviceAmount}): {Payload}",
                    amount,
                    deviceAmount,
                    json);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("createRequest", content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                result.RawOuterResponse = body;

                NebulaApiResponse? apiResponse;
                try
                {
                    apiResponse = JsonSerializer.Deserialize<NebulaApiResponse>(body, JsonOptions);
                }
                catch
                {
                    apiResponse = JsonSerializer.Deserialize<NebulaApiResponse>(body);
                }

                result.ResultCode = NebulaResponseParser.NormalizeResultCode(body) ?? apiResponse?.ResultCode;
                result.Message = apiResponse?.Message;
                result.InnerResponseJson = NebulaResponseParser.ExtractInnerResponse(body) ?? apiResponse?.Response;

                if (!NebulaResponseParser.IsOuterResultSuccessful(result.ResultCode))
                {
                    result.Success = false;
                    result.ErrorMessage = apiResponse?.Message ?? "Payment failed";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(result.InnerResponseJson))
                {
                    result.Success = false;
                    result.ErrorMessage = "Empty payment response";
                    return result;
                }

                result.Details = NebulaResponseParser.TryParseInnerDetails(result.InnerResponseJson);
                if (!NebulaResponseParser.IsInnerPaymentSuccessful(result.InnerResponseJson, result.Details))
                {
                    result.Success = false;
                    result.ErrorMessage = apiResponse?.Message ?? "Payment was not successful";
                    return result;
                }

                result.Success = true;
                return result;
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.ErrorMessage = "Payment device timed out";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nebula createRequest failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public async Task<(bool Success, string? Message)> ConnectUsbAsync(string baseUrl, string comPort, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl);
                var encoded = Uri.EscapeDataString(comPort);
                var response = await client.PostAsync($"connectDeviceByUsb?comPort={encoded}", null, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return (response.IsSuccessStatusCode, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nebula USB connect failed");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Message)> ConnectWifiAsync(string baseUrl, string host, int port, string jsonBody, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl);
                var url = $"connectDeviceByWifi?host={Uri.EscapeDataString(host)}&port={port}";
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return (response.IsSuccessStatusCode, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nebula WiFi connect failed");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Message)> ConnectCloudAsync(string baseUrl, string jsonBody, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl);
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("connectDeviceByCloud", content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return (response.IsSuccessStatusCode, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nebula Cloud connect failed");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Message)> CancelTransAsync(string baseUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl);
                var response = await client.GetAsync("cancelTrans", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return (response.IsSuccessStatusCode, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nebula cancelTrans failed");
                return (false, ex.Message);
            }
        }
    }
}

using System.Text.Json.Serialization;

namespace PrintServer.Models;

public class PrintRequest
{
    [JsonPropertyName("htmlContent")]
    public string? HtmlContent { get; set; }

    [JsonPropertyName("printerName")]
    public string? PrinterName { get; set; }

    [JsonPropertyName("printerType")]
    public string? PrinterType { get; set; } = "windows";

    [JsonPropertyName("storeName")]
    public string? StoreName { get; set; }

    [JsonPropertyName("storeAddress")]
    public string? StoreAddress { get; set; }

    [JsonPropertyName("storePhone")]
    public string? StorePhone { get; set; }

    [JsonPropertyName("orderCode")]
    public string? OrderCode { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("tableNumber")]
    public string? TableNumber { get; set; }

    [JsonPropertyName("employeeName")]
    public string? EmployeeName { get; set; }

    [JsonPropertyName("items")]
    public List<PrintItem>? Items { get; set; }

    [JsonPropertyName("subtotal")]
    public string? Subtotal { get; set; }

    [JsonPropertyName("discount")]
    public string? Discount { get; set; }

    [JsonPropertyName("tax")]
    public string? Tax { get; set; }

    [JsonPropertyName("total")]
    public string? Total { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("qrCode")]
    public string? QrCode { get; set; }
}

public class PrintItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("discount")]
    public decimal? Discount { get; set; }
}


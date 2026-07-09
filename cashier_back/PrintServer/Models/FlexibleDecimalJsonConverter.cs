using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrintServer.Models;

/// <summary>
/// Accepts decimal values as JSON numbers or formatted strings (e.g. "15,000" from frontend).
/// </summary>
public sealed class FlexibleDecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadDecimal(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

    internal static decimal ReadDecimal(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetDecimal();
            case JsonTokenType.String:
                var raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    return 0m;
                raw = raw.Trim()
                    .Replace(",", "")
                    .Replace("٬", "")
                    .Replace(" ", "")
                    .Replace("د.ع", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("IQD", "", StringComparison.OrdinalIgnoreCase);
                if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
                if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
                    return parsed;
                return 0m;
            case JsonTokenType.Null:
                return 0m;
            default:
                throw new JsonException($"Cannot convert token {reader.TokenType} to decimal.");
        }
    }
}

public sealed class FlexibleNullableDecimalJsonConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        return FlexibleDecimalJsonConverter.ReadDecimal(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

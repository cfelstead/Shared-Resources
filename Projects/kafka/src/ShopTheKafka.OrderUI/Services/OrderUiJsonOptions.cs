using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopTheKafka.OrderUI.Services;

/// <summary>camelCase + camelCase-string-enum options matching every other service's wire format (SCHEMA.md).</summary>
public static class OrderUiJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}

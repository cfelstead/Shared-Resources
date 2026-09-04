using System.Text.Json;

namespace ShopTheKafka.Contracts;

/// <summary>The <see cref="JsonSerializerOptions"/> every service must use to (de)serialize Event payloads, so all topics carry camelCase JSON.</summary>
public static class EventJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

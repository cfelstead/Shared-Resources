using System.Text.Json;

namespace McpSidecarServer.Services;

public sealed class GeocodingService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GeocodingService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(double Latitude, double Longitude)?> ResolveAsync(string location, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("geocoding");
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1&language=en&format=json";

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            return null;
        }

        var first = results[0];
        var latitude = first.GetProperty("latitude").GetDouble();
        var longitude = first.GetProperty("longitude").GetDouble();

        return (latitude, longitude);
    }
}

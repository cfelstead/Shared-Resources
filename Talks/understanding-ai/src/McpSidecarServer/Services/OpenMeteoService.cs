using System.Text.Json;

namespace McpSidecarServer.Services;

public sealed class OpenMeteoService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenMeteoService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetCurrentConditionsAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("openmeteo");
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m&timezone=auto";

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty("current", out var current))
        {
            return "Current weather data unavailable.";
        }

        var temp = current.GetProperty("temperature_2m").GetDouble();
        var feelsLike = current.GetProperty("apparent_temperature").GetDouble();
        var precipitation = current.GetProperty("precipitation").GetDouble();
        var wind = current.GetProperty("wind_speed_10m").GetDouble();

        return $"Temperature {temp:0.#} C, feels like {feelsLike:0.#} C, precipitation {precipitation:0.#} mm, wind {wind:0.#} km/h.";
    }
}

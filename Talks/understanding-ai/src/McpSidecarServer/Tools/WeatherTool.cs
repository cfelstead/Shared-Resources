using McpSidecarServer.Services;
using ModelContextProtocol.Server;

namespace McpSidecarServer.Tools;

public sealed class WeatherTool
{
    private readonly GeocodingService _geocodingService;
    private readonly OpenMeteoService _openMeteoService;

    public WeatherTool(GeocodingService geocodingService, OpenMeteoService openMeteoService)
    {
        _geocodingService = geocodingService;
        _openMeteoService = openMeteoService;
    }

    [McpServerTool]
    public async Task<string> GetWeather(string location, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "Please provide a location, for example 'London'.";
        }

        try
        {
            var coordinates = await _geocodingService.ResolveAsync(location, cancellationToken);
            if (coordinates is null)
            {
                return $"Could not resolve location '{location}'.";
            }

            var (latitude, longitude) = coordinates.Value;
            var conditions = await _openMeteoService.GetCurrentConditionsAsync(latitude, longitude, cancellationToken);

            return $"Weather for {location}: {conditions}";
        }
        catch (HttpRequestException)
        {
            return "Live weather is temporarily unavailable due to a network or certificate issue. Please try again in a moment.";
        }
        catch (TaskCanceledException)
        {
            return "Live weather request timed out. Please try again.";
        }
        catch (Exception)
        {
            return "Live weather is temporarily unavailable due to a tool error. Please try again.";
        }
    }
}

using Microsoft.Extensions.Configuration;

namespace Talk.Core;

public sealed class AppConfigLoader : IAppConfigLoader
{
    public AppConfig Load(IConfiguration configuration)
    {
        var defaults = new AppConfig();

        return new AppConfig
        {
            AiProvider = ParseProvider(configuration["AI_PROVIDER"]),
            AiEndpoint = NullIfBlank(configuration["AI_ENDPOINT"]) ?? defaults.AiEndpoint,
            AiModel = NullIfBlank(configuration["AI_MODEL"]) ?? defaults.AiModel,
            AiApiKey = NullIfBlank(configuration["AI_API_KEY"]) ?? defaults.AiApiKey,
            McpServerCommand = NullIfBlank(configuration["MCP_SERVER_COMMAND"]) ?? defaults.McpServerCommand,
            McpServerArguments = NullIfBlank(configuration["MCP_SERVER_ARGS"]) ?? defaults.McpServerArguments
        };
    }

    private static AiProvider ParseProvider(string? rawValue)
    {
        var value = NullIfBlank(rawValue);
        if (value is not null)
        {
            foreach (var name in Enum.GetNames<AiProvider>())
            {
                if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                {
                    return Enum.Parse<AiProvider>(name);
                }
            }
        }

        throw new InvalidOperationException(
            $"AI_PROVIDER must be set to one of: {string.Join(", ", Enum.GetNames<AiProvider>())}. " +
            $"Got: '{rawValue}'.");
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

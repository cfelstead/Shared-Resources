using Microsoft.Extensions.Configuration;
using Talk.Core;

namespace Talk.Core.Tests;

public class AppConfigLoaderTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void Load_WithNoProviderSet_ThrowsAClearError()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        var exception = Assert.Throws<InvalidOperationException>(() => new AppConfigLoader().Load(configuration));

        Assert.Contains("AI_PROVIDER", exception.Message);
        Assert.Contains("OpenAI", exception.Message);
        Assert.Contains("Ollama", exception.Message);
    }

    [Fact]
    public void Load_WithBlankProvider_ThrowsAClearError()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["AI_PROVIDER"] = "   " });

        Assert.Throws<InvalidOperationException>(() => new AppConfigLoader().Load(configuration));
    }

    [Fact]
    public void Load_WithInvalidProvider_ThrowsAClearError()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["AI_PROVIDER"] = "not-a-provider" });

        var exception = Assert.Throws<InvalidOperationException>(() => new AppConfigLoader().Load(configuration));

        Assert.Contains("not-a-provider", exception.Message);
    }

    [Fact]
    public void Load_WithNumericProvider_ThrowsAClearErrorInsteadOfResolvingByEnumIndex()
    {
        // AiProvider.Anthropic is index 2 - Enum.TryParse would silently accept "2" without this guard.
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["AI_PROVIDER"] = "2" });

        var exception = Assert.Throws<InvalidOperationException>(() => new AppConfigLoader().Load(configuration));

        Assert.Contains("2", exception.Message);
    }

    [Theory]
    [InlineData("OpenAI", AiProvider.OpenAI)]
    [InlineData("azureopenai", AiProvider.AzureOpenAI)]
    [InlineData("ANTHROPIC", AiProvider.Anthropic)]
    [InlineData("Gemini", AiProvider.Gemini)]
    [InlineData("ollama", AiProvider.Ollama)]
    public void Load_WithEachSupportedProvider_ParsesCaseInsensitively(string rawValue, AiProvider expected)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["AI_PROVIDER"] = rawValue });

        var config = new AppConfigLoader().Load(configuration);

        Assert.Equal(expected, config.AiProvider);
    }

    [Fact]
    public void Load_WithOnlyProviderSet_ReturnsBlankDefaultsForTheRest()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["AI_PROVIDER"] = "Ollama" });

        var config = new AppConfigLoader().Load(configuration);

        Assert.Equal("", config.AiEndpoint);
        Assert.Equal("", config.AiModel);
        Assert.Equal("", config.AiApiKey);
        Assert.Equal("dotnet", config.McpServerCommand);
        Assert.Equal("run --project ../McpSidecarServer", config.McpServerArguments);
    }

    [Fact]
    public void Load_WithAllValuesSet_UsesConfiguredValues()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AI_PROVIDER"] = "OpenAI",
            ["AI_ENDPOINT"] = "https://example.test/v1",
            ["AI_MODEL"] = "gpt-test",
            ["AI_API_KEY"] = "sk-test",
            ["MCP_SERVER_COMMAND"] = "dotnet",
            ["MCP_SERVER_ARGS"] = "/mcp/McpSidecarServer.dll"
        });

        var config = new AppConfigLoader().Load(configuration);

        Assert.Equal(AiProvider.OpenAI, config.AiProvider);
        Assert.Equal("https://example.test/v1", config.AiEndpoint);
        Assert.Equal("gpt-test", config.AiModel);
        Assert.Equal("sk-test", config.AiApiKey);
        Assert.Equal("dotnet", config.McpServerCommand);
        Assert.Equal("/mcp/McpSidecarServer.dll", config.McpServerArguments);
    }

    [Fact]
    public void Load_WithBlankOptionalValue_FallsBackToDefault()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AI_PROVIDER"] = "Ollama",
            ["MCP_SERVER_COMMAND"] = ""
        });

        var config = new AppConfigLoader().Load(configuration);

        Assert.Equal("dotnet", config.McpServerCommand);
    }
}

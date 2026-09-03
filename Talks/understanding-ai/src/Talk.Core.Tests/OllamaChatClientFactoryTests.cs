using Microsoft.Extensions.Logging.Abstractions;
using Talk.Core;

namespace Talk.Core.Tests;

public class OllamaChatClientFactoryTests
{
    [Fact]
    public void Config_ReturnsTheConfigPassedToTheConstructor()
    {
        var config = new AppConfig { AiProvider = AiProvider.Ollama, AiEndpoint = "http://example:11434", AiModel = "test-model" };
        using var factory = new OllamaChatClientFactory(config, NullLoggerFactory.Instance);

        Assert.Same(config, factory.Config);
    }

    [Fact]
    public void CreateBaseClient_ReturnsAChatClient()
    {
        var config = new AppConfig { AiProvider = AiProvider.Ollama, AiEndpoint = "http://example:11434", AiModel = "test-model" };
        using var factory = new OllamaChatClientFactory(config, NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateBaseClient_WithBlankEndpointAndModel_FallsBackToDefaults()
    {
        var config = new AppConfig { AiProvider = AiProvider.Ollama };
        using var factory = new OllamaChatClientFactory(config, NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateFunctionInvokingClient_ReturnsAChatClient()
    {
        var config = new AppConfig { AiProvider = AiProvider.Ollama, AiEndpoint = "http://example:11434", AiModel = "test-model" };
        using var factory = new OllamaChatClientFactory(config, NullLoggerFactory.Instance);

        using var client = factory.CreateFunctionInvokingClient();

        Assert.NotNull(client);
    }
}

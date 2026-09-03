using Microsoft.Extensions.Logging.Abstractions;
using Talk.Core;

namespace Talk.Core.Tests;

public class GeminiChatClientFactoryTests
{
    private static AppConfig FakeConfig() => new()
    {
        AiProvider = AiProvider.Gemini,
        AiModel = "gemini-test",
        AiApiKey = "fake-api-key"
    };

    [Fact]
    public void Config_ReturnsTheConfigPassedToTheConstructor()
    {
        var config = FakeConfig();
        using var factory = new GeminiChatClientFactory(config, NullLoggerFactory.Instance);

        Assert.Same(config, factory.Config);
    }

    [Fact]
    public void CreateBaseClient_ReturnsAChatClient()
    {
        using var factory = new GeminiChatClientFactory(FakeConfig(), NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateBaseClient_WithCustomEndpoint_ReturnsAChatClient()
    {
        var config = new AppConfig
        {
            AiProvider = AiProvider.Gemini,
            AiEndpoint = "https://example.test",
            AiModel = "gemini-test",
            AiApiKey = "fake-api-key"
        };
        using var factory = new GeminiChatClientFactory(config, NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateFunctionInvokingClient_ReturnsAChatClient()
    {
        using var factory = new GeminiChatClientFactory(FakeConfig(), NullLoggerFactory.Instance);

        using var client = factory.CreateFunctionInvokingClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithBlankApiKey_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.Gemini, AiModel = "gemini-test" };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new GeminiChatClientFactory(config, NullLoggerFactory.Instance));

        Assert.Contains("AI_API_KEY", exception.Message);
    }

    [Fact]
    public void CreateBaseClient_WithBlankModel_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.Gemini, AiApiKey = "fake-api-key" };
        using var factory = new GeminiChatClientFactory(config, NullLoggerFactory.Instance);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBaseClient());

        Assert.Contains("AI_MODEL", exception.Message);
    }
}

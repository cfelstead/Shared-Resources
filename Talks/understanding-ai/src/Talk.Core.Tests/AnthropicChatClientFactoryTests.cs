using Microsoft.Extensions.Logging.Abstractions;
using Talk.Core;

namespace Talk.Core.Tests;

public class AnthropicChatClientFactoryTests
{
    private static AppConfig FakeConfig() => new()
    {
        AiProvider = AiProvider.Anthropic,
        AiModel = "claude-test",
        AiApiKey = "fake-api-key"
    };

    [Fact]
    public void Config_ReturnsTheConfigPassedToTheConstructor()
    {
        var config = FakeConfig();
        using var factory = new AnthropicChatClientFactory(config, NullLoggerFactory.Instance);

        Assert.Same(config, factory.Config);
    }

    [Fact]
    public void CreateBaseClient_ReturnsAChatClient()
    {
        using var factory = new AnthropicChatClientFactory(FakeConfig(), NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateBaseClient_WithCustomEndpoint_ReturnsAChatClient()
    {
        var config = new AppConfig
        {
            AiProvider = AiProvider.Anthropic,
            AiEndpoint = "https://example.test",
            AiModel = "claude-test",
            AiApiKey = "fake-api-key"
        };
        using var factory = new AnthropicChatClientFactory(config, NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateFunctionInvokingClient_ReturnsAChatClient()
    {
        using var factory = new AnthropicChatClientFactory(FakeConfig(), NullLoggerFactory.Instance);

        using var client = factory.CreateFunctionInvokingClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithBlankApiKey_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.Anthropic, AiModel = "claude-test" };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new AnthropicChatClientFactory(config, NullLoggerFactory.Instance));

        Assert.Contains("AI_API_KEY", exception.Message);
    }

    [Fact]
    public void CreateBaseClient_WithBlankModel_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.Anthropic, AiApiKey = "fake-api-key" };
        using var factory = new AnthropicChatClientFactory(config, NullLoggerFactory.Instance);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBaseClient());

        Assert.Contains("AI_MODEL", exception.Message);
    }
}

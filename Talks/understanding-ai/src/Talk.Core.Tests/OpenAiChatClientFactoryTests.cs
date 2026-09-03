using Microsoft.Extensions.Logging.Abstractions;
using Talk.Core;

namespace Talk.Core.Tests;

public class OpenAiChatClientFactoryTests
{
    private static AppConfig FakeConfig() => new()
    {
        AiProvider = AiProvider.OpenAI,
        AiEndpoint = "https://example.test/v1",
        AiModel = "gpt-test",
        AiApiKey = "fake-api-key"
    };

    [Fact]
    public void Config_ReturnsTheConfigPassedToTheConstructor()
    {
        var config = FakeConfig();
        using var factory = new OpenAiChatClientFactory(config, NullLoggerFactory.Instance);

        Assert.Same(config, factory.Config);
    }

    [Fact]
    public void CreateBaseClient_ReturnsAChatClient()
    {
        using var factory = new OpenAiChatClientFactory(FakeConfig(), NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateBaseClient_WithBlankEndpoint_UsesTheDefaultOpenAiEndpoint()
    {
        var config = new AppConfig { AiProvider = AiProvider.OpenAI, AiModel = "gpt-test", AiApiKey = "fake-api-key" };
        using var factory = new OpenAiChatClientFactory(config, NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateFunctionInvokingClient_ReturnsAChatClient()
    {
        using var factory = new OpenAiChatClientFactory(FakeConfig(), NullLoggerFactory.Instance);

        using var client = factory.CreateFunctionInvokingClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateBaseClient_WithBlankApiKey_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.OpenAI, AiModel = "gpt-test" };
        using var factory = new OpenAiChatClientFactory(config, NullLoggerFactory.Instance);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBaseClient());

        Assert.Contains("AI_API_KEY", exception.Message);
    }

    [Fact]
    public void CreateBaseClient_WithBlankModel_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.OpenAI, AiApiKey = "fake-api-key" };
        using var factory = new OpenAiChatClientFactory(config, NullLoggerFactory.Instance);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBaseClient());

        Assert.Contains("AI_MODEL", exception.Message);
    }
}

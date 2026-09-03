using Microsoft.Extensions.Logging.Abstractions;
using Talk.Core;

namespace Talk.Core.Tests;

public class AzureOpenAiChatClientFactoryTests
{
    private static AppConfig FakeConfig() => new()
    {
        AiProvider = AiProvider.AzureOpenAI,
        AiEndpoint = "https://example.openai.azure.com",
        AiModel = "gpt-deployment",
        AiApiKey = "fake-api-key"
    };

    [Fact]
    public void Config_ReturnsTheConfigPassedToTheConstructor()
    {
        var config = FakeConfig();
        using var factory = new AzureOpenAiChatClientFactory(config, NullLoggerFactory.Instance);

        Assert.Same(config, factory.Config);
    }

    [Fact]
    public void CreateBaseClient_ReturnsAChatClient()
    {
        using var factory = new AzureOpenAiChatClientFactory(FakeConfig(), NullLoggerFactory.Instance);

        using var client = factory.CreateBaseClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateBaseClient_WithBlankEndpoint_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.AzureOpenAI, AiModel = "gpt-deployment", AiApiKey = "fake-api-key" };
        using var factory = new AzureOpenAiChatClientFactory(config, NullLoggerFactory.Instance);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBaseClient());

        Assert.Contains("AI_ENDPOINT", exception.Message);
    }

    [Fact]
    public void CreateBaseClient_WithBlankApiKey_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.AzureOpenAI, AiEndpoint = "https://example.openai.azure.com", AiModel = "gpt-deployment" };
        using var factory = new AzureOpenAiChatClientFactory(config, NullLoggerFactory.Instance);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBaseClient());

        Assert.Contains("AI_API_KEY", exception.Message);
    }

    [Fact]
    public void CreateBaseClient_WithBlankModel_ThrowsAClearError()
    {
        var config = new AppConfig { AiProvider = AiProvider.AzureOpenAI, AiEndpoint = "https://example.openai.azure.com", AiApiKey = "fake-api-key" };
        using var factory = new AzureOpenAiChatClientFactory(config, NullLoggerFactory.Instance);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBaseClient());

        Assert.Contains("AI_MODEL", exception.Message);
    }

    [Fact]
    public void CreateFunctionInvokingClient_ReturnsAChatClient()
    {
        using var factory = new AzureOpenAiChatClientFactory(FakeConfig(), NullLoggerFactory.Instance);

        using var client = factory.CreateFunctionInvokingClient();

        Assert.NotNull(client);
    }
}

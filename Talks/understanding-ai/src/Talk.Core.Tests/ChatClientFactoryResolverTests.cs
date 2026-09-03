using Microsoft.Extensions.Logging.Abstractions;
using Talk.Core;

namespace Talk.Core.Tests;

public class ChatClientFactoryResolverTests
{
    [Theory]
    [InlineData(AiProvider.OpenAI, typeof(OpenAiChatClientFactory))]
    [InlineData(AiProvider.AzureOpenAI, typeof(AzureOpenAiChatClientFactory))]
    [InlineData(AiProvider.Anthropic, typeof(AnthropicChatClientFactory))]
    [InlineData(AiProvider.Gemini, typeof(GeminiChatClientFactory))]
    [InlineData(AiProvider.Ollama, typeof(OllamaChatClientFactory))]
    public void Create_ForEachProvider_ReturnsTheMatchingFactoryType(AiProvider provider, Type expectedFactoryType)
    {
        var config = new AppConfig
        {
            AiProvider = provider,
            AiEndpoint = "https://example.test",
            AiModel = "test-model",
            AiApiKey = "fake-api-key"
        };

        using var factory = ChatClientFactoryResolver.Create(config, NullLoggerFactory.Instance);

        Assert.IsType(expectedFactoryType, factory);
        Assert.Same(config, factory.Config);
    }
}

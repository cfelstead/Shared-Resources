using Microsoft.Extensions.Logging;

namespace Talk.Core;

/// <summary>
/// Resolves the <see cref="IChatClientFactory"/> implementation for the configured
/// <see cref="AppConfig.AiProvider"/>, so step projects depend only on <c>IChatClient</c>.
/// </summary>
public static class ChatClientFactoryResolver
{
    public static IChatClientFactory Create(AppConfig config, ILoggerFactory loggerFactory) => config.AiProvider switch
    {
        AiProvider.OpenAI => new OpenAiChatClientFactory(config, loggerFactory),
        AiProvider.AzureOpenAI => new AzureOpenAiChatClientFactory(config, loggerFactory),
        AiProvider.Anthropic => new AnthropicChatClientFactory(config, loggerFactory),
        AiProvider.Gemini => new GeminiChatClientFactory(config, loggerFactory),
        AiProvider.Ollama => new OllamaChatClientFactory(config, loggerFactory),
        _ => throw new ArgumentOutOfRangeException(nameof(config), config.AiProvider, "Unsupported AI provider.")
    };
}

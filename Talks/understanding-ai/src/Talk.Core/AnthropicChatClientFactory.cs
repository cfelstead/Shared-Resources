using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Talk.Core;

public sealed class AnthropicChatClientFactory : IChatClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly AnthropicClient _client;
    private bool _disposed;

    public AnthropicChatClientFactory(AppConfig config, ILoggerFactory loggerFactory)
    {
        Config = config;
        _loggerFactory = loggerFactory;

        var apiKey = RequiredConfig.Require(config.AiApiKey, "AI_API_KEY", config.AiProvider);
        _client = string.IsNullOrWhiteSpace(config.AiEndpoint)
            ? new AnthropicClient { ApiKey = apiKey }
            : new AnthropicClient { ApiKey = apiKey, BaseUrl = config.AiEndpoint };
    }

    public AppConfig Config { get; }

    public IChatClient CreateBaseClient()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var modelId = RequiredConfig.Require(Config.AiModel, "AI_MODEL", Config.AiProvider);
        return _client.AsIChatClient(modelId);
    }

    public IChatClient CreateFunctionInvokingClient()
    {
        return CreateBaseClient()
            .AsBuilder()
            .UseFunctionInvocation(_loggerFactory)
            .Build();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client.Dispose();
        _disposed = true;
    }
}

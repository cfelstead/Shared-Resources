using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Talk.Core;

public sealed class GeminiChatClientFactory : IChatClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Client _client;
    private bool _disposed;

    public GeminiChatClientFactory(AppConfig config, ILoggerFactory loggerFactory)
    {
        Config = config;
        _loggerFactory = loggerFactory;

        var apiKey = RequiredConfig.Require(config.AiApiKey, "AI_API_KEY", config.AiProvider);
        var httpOptions = string.IsNullOrWhiteSpace(config.AiEndpoint)
            ? null
            : new HttpOptions { BaseUrl = config.AiEndpoint };
        _client = new Client(apiKey: apiKey, httpOptions: httpOptions);
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

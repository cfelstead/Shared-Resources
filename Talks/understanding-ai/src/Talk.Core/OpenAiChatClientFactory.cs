using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Talk.Core;

public sealed class OpenAiChatClientFactory : IChatClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;

    public OpenAiChatClientFactory(AppConfig config, ILoggerFactory loggerFactory)
    {
        Config = config;
        _loggerFactory = loggerFactory;
    }

    public AppConfig Config { get; }

    public IChatClient CreateBaseClient()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var apiKey = RequiredConfig.Require(Config.AiApiKey, "AI_API_KEY", Config.AiProvider);
        var model = RequiredConfig.Require(Config.AiModel, "AI_MODEL", Config.AiProvider);

        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(Config.AiEndpoint))
        {
            options.Endpoint = new Uri(Config.AiEndpoint);
        }

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(model).AsIChatClient();
    }

    public IChatClient CreateFunctionInvokingClient()
    {
        return CreateBaseClient()
            .AsBuilder()
            .UseFunctionInvocation(_loggerFactory)
            .Build();
    }

    public void Dispose() => _disposed = true;
}

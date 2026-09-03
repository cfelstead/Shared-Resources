using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Talk.Core;

public sealed class AzureOpenAiChatClientFactory : IChatClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;

    public AzureOpenAiChatClientFactory(AppConfig config, ILoggerFactory loggerFactory)
    {
        Config = config;
        _loggerFactory = loggerFactory;
    }

    public AppConfig Config { get; }

    public IChatClient CreateBaseClient()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var endpoint = RequiredConfig.Require(Config.AiEndpoint, "AI_ENDPOINT", Config.AiProvider);
        var apiKey = RequiredConfig.Require(Config.AiApiKey, "AI_API_KEY", Config.AiProvider);
        var model = RequiredConfig.Require(Config.AiModel, "AI_MODEL", Config.AiProvider);

        var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
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

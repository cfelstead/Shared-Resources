using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Talk.Core;

public sealed class OllamaChatClientFactory : IChatClientFactory
{
    private const string DefaultEndpoint = "http://localhost:11434";
    private const string DefaultModel = "llama3.1:8b";

    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public OllamaChatClientFactory(AppConfig config, ILoggerFactory loggerFactory)
    {
        Config = config;
        _loggerFactory = loggerFactory;
        _httpClient = new HttpClient();
    }

    public AppConfig Config { get; }

    public IChatClient CreateBaseClient()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var endpoint = new Uri(string.IsNullOrWhiteSpace(Config.AiEndpoint) ? DefaultEndpoint : Config.AiEndpoint);
        var model = string.IsNullOrWhiteSpace(Config.AiModel) ? DefaultModel : Config.AiModel;
        return new OllamaChatClient(endpoint, model, _httpClient);
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

        _httpClient.Dispose();
        _disposed = true;
    }
}

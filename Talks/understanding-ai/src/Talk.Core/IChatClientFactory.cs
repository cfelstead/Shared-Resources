using Microsoft.Extensions.AI;

namespace Talk.Core;

public interface IChatClientFactory : IDisposable
{
    AppConfig Config { get; }

    IChatClient CreateBaseClient();

    IChatClient CreateFunctionInvokingClient();
}

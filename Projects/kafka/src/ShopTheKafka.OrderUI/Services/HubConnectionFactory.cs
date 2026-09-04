using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace ShopTheKafka.OrderUI.Services;

/// <summary>
/// Builds the (unstarted) HubConnection to OrderStatusService's SignalR hub. Always pins the LongPolling
/// transport and routes every request through an injectable HttpMessageHandler - that keeps this one code path
/// identical between production (where the handler resolves Aspire's "http://orderstatusservice" logical
/// hostname via service discovery) and tests (where the handler points straight at a WebApplicationFactory's
/// TestServer, which doesn't support raw WebSocket upgrades).
/// </summary>
public static class HubConnectionFactory
{
    public static HubConnection Create(Uri hubUri, Func<HttpMessageHandler>? handlerFactory = null) =>
        new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                if (handlerFactory is not null)
                {
                    options.HttpMessageHandlerFactory = _ => handlerFactory();
                }
            })
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)))
            .Build();
}

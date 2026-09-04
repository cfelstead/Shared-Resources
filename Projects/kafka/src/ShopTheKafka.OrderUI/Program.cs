using ShopTheKafka.OrderUI.Components;
using ShopTheKafka.OrderUI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<OrderServiceClient>(client => client.BaseAddress = new Uri("http://orderservice"));
builder.Services.AddHttpClient<OrderStatusServiceClient>(client => client.BaseAddress = new Uri("http://orderstatusservice"));

// Named (not typed) so the SignalR connection below can pull the same service-discovery-aware handler
// out of IHttpMessageHandlerFactory without needing an HttpClient instance of its own.
builder.Services.AddHttpClient("OrderStatusServiceHub", client => client.BaseAddress = new Uri("http://orderstatusservice"));

builder.Services.AddSingleton(sp =>
{
    var handlerFactory = sp.GetRequiredService<IHttpMessageHandlerFactory>();
    return HubConnectionFactory.Create(
        new Uri("http://orderstatusservice/hubs/order-status"),
        () => handlerFactory.CreateHandler("OrderStatusServiceHub"));
});

builder.Services.AddSingleton<OrderBoardState>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;

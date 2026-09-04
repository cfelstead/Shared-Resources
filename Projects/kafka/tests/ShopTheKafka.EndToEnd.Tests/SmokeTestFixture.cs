using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ShopTheKafka.EndToEnd.Tests;

/// <summary>
/// Boots the whole Aspire app graph (all six services plus a real Kafka broker) once and shares it across every
/// test in the class, since standing the graph up is expensive and these tests only need it running, not fresh.
/// </summary>
public sealed class SmokeTestFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public HttpClient OrderServiceClient { get; private set; } = null!;

    public HttpClient OrderStatusServiceClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ShopTheKafka_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("orderservice", startupTimeout.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("orderstatusservice", startupTimeout.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("paymentservice", startupTimeout.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("inventoryservice", startupTimeout.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("shippingservice", startupTimeout.Token);

        OrderServiceClient = _app.CreateHttpClient("orderservice");
        OrderStatusServiceClient = _app.CreateHttpClient("orderstatusservice");
    }

    public async Task DisposeAsync()
    {
        OrderServiceClient?.Dispose();
        OrderStatusServiceClient?.Dispose();

        // _app may still be null if InitializeAsync threw before assigning it (e.g. the app graph failed to
        // build) - guard so that failure surfaces on its own rather than being masked by a NullReferenceException
        // here.
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

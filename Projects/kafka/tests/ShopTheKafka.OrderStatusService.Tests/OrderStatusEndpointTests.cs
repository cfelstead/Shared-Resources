using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using ShopTheKafka.Contracts;
using Testcontainers.Kafka;

namespace ShopTheKafka.OrderStatusService.Tests;

public sealed class OrderStatusEndpointTests : IAsyncLifetime
{
#pragma warning disable CS0618 // parameterless KafkaBuilder is obsolete but the recommended image parameter isn't published on any tag yet
    private readonly KafkaContainer _kafka = new KafkaBuilder().Build();
#pragma warning restore CS0618
    private readonly CapturingLoggerProvider _capturingLogger = new();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private IProducer<string, string> _producer = null!;

    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();

        // AddKafkaConsumer reads the connection string while Program.cs's top-level statements run, which happens
        // before WebApplicationFactory's ConfigureAppConfiguration hook can inject config - so the bootstrap
        // address has to arrive via an environment variable, per OrderService.Tests' established pattern.
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", _kafka.GetBootstrapAddress());

        _factory = new TestFactory(_capturingLogger);
        _client = _factory.CreateClient();

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
        }).Build();
    }

    public async Task DisposeAsync()
    {
        _producer.Dispose();
        _client.Dispose();
        await _factory.DisposeAsync();
        await _kafka.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
    }

    private async Task PublishAsync<TEvent>(string topic, Guid orderId, TEvent evt)
    {
        var payload = JsonSerializer.Serialize(evt, EventJsonOptions.Default);
        await _producer.ProduceAsync(topic, new Message<string, string> { Key = orderId.ToString(), Value = payload });
    }

    private static OrderPlacedEvent MakeOrderPlaced(Guid orderId, IReadOnlyList<Item>? items = null)
    {
        var actualItems = items ?? [new Item("Widget", 2, 9.99m)];
        return new OrderPlacedEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            CustomerId: Guid.NewGuid(),
            Items: actualItems,
            TotalAmount: actualItems.Sum(i => i.UnitPrice * i.Quantity));
    }

    /// <summary>Polls <c>GET /orders/{id}</c> until the predicate matches or the timeout elapses, since consumption is async.</summary>
    private async Task<JsonElement> WaitForOrderAsync(Guid orderId, Func<JsonElement, bool> predicate, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            var response = await _client.GetAsync($"/orders/{orderId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (predicate(body))
                {
                    return body;
                }
            }
            await Task.Delay(250);
        }

        throw new TimeoutException($"Order {orderId} never reached the expected state within the timeout");
    }

    [Fact]
    public async Task OrderPlaced_ReturnsPlacedStatus_WithItemsTotalAmountAndOneTimelineEntry()
    {
        var orderId = Guid.NewGuid();
        var items = new List<Item> { new("Widget", 2, 9.99m), new("Gadget", 1, 4.99m) };
        var orderPlaced = MakeOrderPlaced(orderId, items);

        await PublishAsync(OrderPlacedEvent.Topic, orderId, orderPlaced);

        var body = await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "placed");

        Assert.Equal("placed", body.GetProperty("currentStatus").GetString());
        Assert.Equal(orderPlaced.TotalAmount, body.GetProperty("totalAmount").GetDecimal());
        var returnedItems = body.GetProperty("items");
        Assert.Equal(2, returnedItems.GetArrayLength());
        Assert.Contains(returnedItems.EnumerateArray(), i => i.GetProperty("itemName").GetString() == "Widget" && i.GetProperty("quantity").GetInt32() == 2);
        Assert.Contains(returnedItems.EnumerateArray(), i => i.GetProperty("itemName").GetString() == "Gadget" && i.GetProperty("quantity").GetInt32() == 1);

        var timeline = body.GetProperty("timeline");
        Assert.Equal(1, timeline.GetArrayLength());
        Assert.Equal("placed", timeline[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task UnknownOrderId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PaymentApproved_AfterOrderPlaced_UpdatesStatus_AndAppendsTimelineEntry()
    {
        var orderId = Guid.NewGuid();
        await PublishAsync(OrderPlacedEvent.Topic, orderId, MakeOrderPlaced(orderId));

        await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "placed");

        var paymentApproved = new PaymentApprovedEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            PaymentId: Guid.NewGuid(),
            AmountCharged: 19.98m,
            Items: [new Item("Widget", 2, 9.99m)]);
        await PublishAsync(PaymentApprovedEvent.Topic, orderId, paymentApproved);

        var body = await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "paymentApproved");

        var timeline = body.GetProperty("timeline");
        Assert.Equal(2, timeline.GetArrayLength());
        Assert.Equal("placed", timeline[0].GetProperty("status").GetString());
        Assert.Equal("paymentApproved", timeline[1].GetProperty("status").GetString());
        Assert.True(timeline[1].GetProperty("detail").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public async Task PaymentFailed_AfterOrderPlaced_UpdatesStatus_WithNonNullReasonDetail()
    {
        var orderId = Guid.NewGuid();
        await PublishAsync(OrderPlacedEvent.Topic, orderId, MakeOrderPlaced(orderId));
        await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "placed");

        var paymentFailed = new PaymentFailedEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow, "Card declined");
        await PublishAsync(PaymentFailedEvent.Topic, orderId, paymentFailed);

        var body = await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "paymentFailed");

        var lastEntry = body.GetProperty("timeline").EnumerateArray().Last();
        Assert.Equal("paymentFailed", lastEntry.GetProperty("status").GetString());
        Assert.Equal("Card declined", lastEntry.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InventoryBackorder_AfterOrderPlaced_UpdatesStatus_WithUnavailableItemsDetail()
    {
        var orderId = Guid.NewGuid();
        await PublishAsync(OrderPlacedEvent.Topic, orderId, MakeOrderPlaced(orderId));
        await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "placed");

        var backorder = new InventoryBackorderEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow, ["Gizmo"]);
        await PublishAsync(InventoryBackorderEvent.Topic, orderId, backorder);

        var body = await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "inventoryBackorder");

        var lastEntry = body.GetProperty("timeline").EnumerateArray().Last();
        Assert.Equal("inventoryBackorder", lastEntry.GetProperty("status").GetString());
        Assert.Contains("Gizmo", lastEntry.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InventoryReserved_ThenShipped_UpdateStatusInTurn()
    {
        var orderId = Guid.NewGuid();
        await PublishAsync(OrderPlacedEvent.Topic, orderId, MakeOrderPlaced(orderId));
        await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "placed");

        await PublishAsync(InventoryReservedEvent.Topic, orderId, new InventoryReservedEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow));
        await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "inventoryReserved");

        var shipped = new OrderShippedEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3));
        await PublishAsync(OrderShippedEvent.Topic, orderId, shipped);

        var body = await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "shipped");
        Assert.Equal(3, body.GetProperty("timeline").GetArrayLength());
    }

    [Fact]
    public async Task EventOtherThanOrderPlaced_ArrivingFirst_CreatesPlaceholder_ThenBackfillsOnOrderPlaced()
    {
        var orderId = Guid.NewGuid();

        var paymentApproved = new PaymentApprovedEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            PaymentId: Guid.NewGuid(),
            AmountCharged: 19.98m,
            Items: [new Item("Widget", 2, 9.99m)]);
        await PublishAsync(PaymentApprovedEvent.Topic, orderId, paymentApproved);

        var placeholder = await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "paymentApproved");
        Assert.Equal(0, placeholder.GetProperty("items").GetArrayLength());
        Assert.Equal(0m, placeholder.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(1, placeholder.GetProperty("timeline").GetArrayLength());

        var orderPlaced = MakeOrderPlaced(orderId, [new Item("Widget", 2, 9.99m)]);
        await PublishAsync(OrderPlacedEvent.Topic, orderId, orderPlaced);

        var backfilled = await WaitForOrderAsync(orderId, b => b.GetProperty("items").GetArrayLength() > 0);
        Assert.Equal(orderPlaced.TotalAmount, backfilled.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(2, backfilled.GetProperty("timeline").GetArrayLength());
        // currentStatus never regresses: paymentApproved is further along the pipeline than the placed entry this
        // late-arriving OrderPlacedEvent contributes, so it stays paymentApproved rather than reverting to placed.
        Assert.Equal("paymentApproved", backfilled.GetProperty("currentStatus").GetString());
    }

    [Fact]
    public async Task OrderStatusChange_IsPushedToConnectedSignalRClients()
    {
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_client.BaseAddress!, "/hubs/order-status"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var received = new TaskCompletionSource<JsonElement>();
        connection.On<JsonElement>("OrderStatusChanged", status => received.TrySetResult(status));
        await connection.StartAsync();

        var orderId = Guid.NewGuid();
        await PublishAsync(OrderPlacedEvent.Topic, orderId, MakeOrderPlaced(orderId));

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        Assert.Same(received.Task, completed);

        var pushed = await received.Task;
        Assert.Equal(orderId, pushed.GetProperty("orderId").GetGuid());
        Assert.Equal("placed", pushed.GetProperty("currentStatus").GetString());
    }

    [Fact]
    public async Task MalformedMessage_IsSkipped_AndSubsequentValidMessageIsStillProcessed()
    {
        await _producer.ProduceAsync(OrderPlacedEvent.Topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = "{ this is not valid JSON for an OrderPlacedEvent",
        });

        var orderId = Guid.NewGuid();
        await PublishAsync(OrderPlacedEvent.Topic, orderId, MakeOrderPlaced(orderId));

        // proves the malformed message didn't crash or block the consume loop
        await WaitForOrderAsync(orderId, b => b.GetProperty("currentStatus").GetString() == "placed");

        // per ADR 0007: the processing failure is logged, not silently swallowed
        Assert.Contains(_capturingLogger.Messages, m => m.Contains("Failed to process message", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A single-instance factory subclass, so the logging provider hook survives for the whole test - unlike
    /// chaining <c>WithWebHostBuilder</c> off a discarded base factory, which can be GC'd mid-test and tear down
    /// the TestServer both factories share.</summary>
    private sealed class TestFactory(ILoggerProvider loggerProvider) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.AddProvider(loggerProvider));
        }
    }

    /// <summary>Captures formatted log messages at Error level or above, so a test can assert something was logged.</summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public readonly ConcurrentQueue<string> Messages = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel))
                {
                    messages.Enqueue(formatter(state, exception));
                }
            }
        }
    }
}

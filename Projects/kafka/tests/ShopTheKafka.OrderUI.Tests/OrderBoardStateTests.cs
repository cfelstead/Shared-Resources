extern alias OrderServiceHost;
extern alias OrderStatusServiceHost;

using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc.Testing;
using ShopTheKafka.Contracts;
using ShopTheKafka.KafkaTestSupport;
using ShopTheKafka.OrderUI.Services;
using Testcontainers.Kafka;
using OrderServiceProgram = OrderServiceHost::Program;
using OrderStatusServiceProgram = OrderStatusServiceHost::Program;

namespace ShopTheKafka.OrderUI.Tests;

/// <summary>
/// Exercises the UI's HTTP + SignalR seam (SPEC.md's Testing Decisions) against the real OrderService and
/// OrderStatusService, both running in-process via WebApplicationFactory over one shared real Kafka broker -
/// proving the board's calls to <c>POST /orders</c>, <c>GET /orders/{id}</c>, and the SignalR hub produce
/// correct results, exactly as ticket 07 requires.
/// </summary>
public sealed class OrderBoardStateTests : IAsyncLifetime
{
#pragma warning disable CS0618 // parameterless KafkaBuilder is obsolete but the recommended image parameter isn't published on any tag yet
    private readonly KafkaContainer _kafka = new KafkaBuilder().Build();
#pragma warning restore CS0618
    private WebApplicationFactory<OrderServiceProgram> _orderServiceFactory = null!;
    private WebApplicationFactory<OrderStatusServiceProgram> _orderStatusServiceFactory = null!;
    private IProducer<string, string> _producer = null!;
    private OrderBoardState _board = null!;

    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();

        // Both Program.cs top-level statements read the bootstrap address while building, before
        // WebApplicationFactory's ConfigureAppConfiguration hook can inject it - so it has to arrive via an
        // environment variable, per OrderService.Tests' and OrderStatusService.Tests' established pattern.
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", _kafka.GetBootstrapAddress());

        _orderServiceFactory = new WebApplicationFactory<OrderServiceProgram>();
        _orderStatusServiceFactory = new WebApplicationFactory<OrderStatusServiceProgram>();

        var orderHttp = _orderServiceFactory.CreateClient();
        var orderStatusHttp = _orderStatusServiceFactory.CreateClient();

        var hubConnection = HubConnectionFactory.Create(
            new Uri(orderStatusHttp.BaseAddress!, "/hubs/order-status"),
            () => _orderStatusServiceFactory.Server.CreateHandler());

        _board = new OrderBoardState(
            new OrderServiceClient(orderHttp),
            new OrderStatusServiceClient(orderStatusHttp),
            hubConnection);
        await _board.ConnectAsync();

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
        }).Build();
    }

    public async Task DisposeAsync()
    {
        _producer.Dispose();
        await _board.DisposeAsync();
        await _orderStatusServiceFactory.DisposeAsync();
        await _orderServiceFactory.DisposeAsync();
        await _kafka.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
    }

    private IConsumer<string, string> CreateOrdersPlacedConsumer()
    {
        var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        consumer.Subscribe(OrderPlacedEvent.Topic);
        return consumer;
    }

    private async Task<bool> WaitUntilAsync(Func<bool> predicate, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
            {
                return true;
            }
            await Task.Delay(200);
        }
        return predicate();
    }

    [Fact]
    public async Task PlaceOrderAsync_CallsRealPostOrders_PublishingTheEnteredItems()
    {
        using var consumer = CreateOrdersPlacedConsumer();
        var customerId = Guid.NewGuid();
        var items = new List<OrderLineItem> { new("Widget", 2), new("Gadget", 1) };

        var orderId = await _board.PlaceOrderAsync(customerId, items);

        Assert.NotEqual(Guid.Empty, orderId);

        var published = KafkaTestConsumer.TryConsume(consumer, TimeSpan.FromSeconds(15));
        Assert.NotNull(published);
        var evt = System.Text.Json.JsonSerializer.Deserialize<OrderPlacedEvent>(published!.Message.Value, EventJsonOptions.Default)!;
        Assert.Equal(orderId, evt.OrderId);
        Assert.Equal(customerId, evt.CustomerId);
        Assert.Equal(2, evt.Items.Count);
        Assert.Contains(evt.Items, i => i.ItemName == "Widget" && i.Quantity == 2);
        Assert.Contains(evt.Items, i => i.ItemName == "Gadget" && i.Quantity == 1);
    }

    [Fact]
    public async Task PlaceOrderAsync_NewOrderAppearsInPlacedColumn_WithoutAnyManualRefresh()
    {
        var items = new List<OrderLineItem> { new("Widget", 1) };

        var orderId = await _board.PlaceOrderAsync(Guid.NewGuid(), items);

        // No call back into the board other than reading its already-updated state - proving the Order showed
        // up as a side effect of PlaceOrderAsync/the hub push, not because the test forced a re-fetch.
        var placedColumn = _board.GetByStatus(OrderStatusValue.Placed);
        Assert.Contains(placedColumn, o => o.OrderId == orderId);
    }

    [Fact]
    public async Task HubPush_ForAnAlreadyKnownOrder_UpdatesItIntoTheCorrectColumn()
    {
        var orderId = await _board.PlaceOrderAsync(Guid.NewGuid(), [new OrderLineItem("Widget", 1)]);
        Assert.Contains(_board.GetByStatus(OrderStatusValue.Placed), o => o.OrderId == orderId);

        // Produced directly onto payment-approved, per SPEC.md's per-service Kafka-hop seam - bypasses
        // PaymentService entirely, isolating this test to the UI's SignalR subscription itself.
        var paymentApproved = new PaymentApprovedEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            PaymentId: Guid.NewGuid(),
            AmountCharged: 9.99m,
            Items: [new Item("Widget", 1, 9.99m)]);
        var payload = System.Text.Json.JsonSerializer.Serialize(paymentApproved, EventJsonOptions.Default);
        await _producer.ProduceAsync(PaymentApprovedEvent.Topic, new Message<string, string> { Key = orderId.ToString(), Value = payload });

        var moved = await WaitUntilAsync(() => _board.GetByStatus(OrderStatusValue.PaymentApproved).Any(o => o.OrderId == orderId));

        Assert.True(moved);
        Assert.DoesNotContain(_board.GetByStatus(OrderStatusValue.Placed), o => o.OrderId == orderId);
    }
}

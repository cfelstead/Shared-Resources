using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopTheKafka.Contracts;
using ShopTheKafka.KafkaTestSupport;
using Testcontainers.Kafka;
using static ShopTheKafka.KafkaTestSupport.KafkaTestConsumer;

namespace ShopTheKafka.PaymentService.Tests;

public sealed class PaymentServiceTests : IAsyncLifetime
{
#pragma warning disable CS0618 // parameterless KafkaBuilder is obsolete but the recommended image parameter isn't published on any tag yet
    private readonly KafkaContainer _kafka = new KafkaBuilder().Build();
#pragma warning restore CS0618
    private readonly CapturingLoggerProvider _capturingLogger = new();
    private IHost _host = null!;
    private IProducer<string, string> _testProducer = null!;

    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();

        _host = global::Program.CreateHost(
            ["--ConnectionStrings:kafka=" + _kafka.GetBootstrapAddress(), "--Processing:DelaySeconds=0"],
            logging => logging.AddProvider(_capturingLogger));
        await _host.StartAsync();

        _testProducer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
        }).Build();
    }

    public async Task DisposeAsync()
    {
        _testProducer.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        await _kafka.DisposeAsync();
    }

    private IConsumer<string, string> CreateOutputConsumer()
    {
        var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        consumer.Subscribe([PaymentApprovedEvent.Topic, PaymentFailedEvent.Topic]);
        return consumer;
    }

    private async Task PublishOrderPlacedAsync(OrderPlacedEvent orderPlaced)
    {
        var payload = JsonSerializer.Serialize(orderPlaced, EventJsonOptions.Default);
        await _testProducer.ProduceAsync(OrderPlacedEvent.Topic, new Message<string, string>
        {
            Key = orderPlaced.OrderId.ToKafkaKey(),
            Value = payload,
        });
    }

    private static OrderPlacedEvent MakeOrderPlaced(Guid? orderId = null) => new(
        EventId: Guid.NewGuid(),
        OrderId: orderId ?? Guid.NewGuid(),
        OccurredAtUtc: DateTimeOffset.UtcNow,
        CustomerId: Guid.NewGuid(),
        Items: [new Item("Widget", 2, 9.99m)],
        TotalAmount: 19.98m);

    [Fact]
    public async Task OrderPlaced_ProducesExactlyOneEvent_KeyedByOrderId()
    {
        using var consumer = CreateOutputConsumer();
        var orderPlaced = MakeOrderPlaced();

        await PublishOrderPlacedAsync(orderPlaced);

        var first = TryConsume(consumer, TimeSpan.FromSeconds(30));
        Assert.NotNull(first);
        Assert.Equal(orderPlaced.OrderId.ToString(), first.Message.Key);
        Assert.True(first.Topic == PaymentApprovedEvent.Topic || first.Topic == PaymentFailedEvent.Topic);

        var second = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(second);
    }

    [Fact]
    public async Task ManyOrders_SplitApprovedAndFailed_RoughlyNinetyTen_WithCorrectFieldsOnEach()
    {
        const int orderCount = 200;
        using var consumer = CreateOutputConsumer();

        var orders = Enumerable.Range(0, orderCount)
            .Select(i => MakeOrderPlaced() with
            {
                Items = [new Item("Widget", i % 9 + 1, 9.99m)],
                TotalAmount = (i % 9 + 1) * 9.99m,
            })
            .ToList();
        var ordersById = orders.ToDictionary(o => o.OrderId);

        foreach (var order in orders)
        {
            await PublishOrderPlacedAsync(order);
        }

        var results = new List<ConsumeResult<string, string>>();
        while (results.Count < orderCount)
        {
            var result = TryConsume(consumer, TimeSpan.FromSeconds(60));
            Assert.NotNull(result);
            results.Add(result);
        }

        var approvedCount = 0;
        var failedCount = 0;
        var seenPaymentIds = new HashSet<Guid>();

        foreach (var result in results)
        {
            var orderId = Guid.Parse(result.Message.Key);
            var order = ordersById[orderId];

            if (result.Topic == PaymentApprovedEvent.Topic)
            {
                approvedCount++;
                var approved = JsonSerializer.Deserialize<PaymentApprovedEvent>(result.Message.Value, EventJsonOptions.Default)!;
                Assert.Equal(orderId, approved.OrderId);
                Assert.NotEqual(Guid.Empty, approved.PaymentId);
                Assert.True(seenPaymentIds.Add(approved.PaymentId), "paymentId should be freshly generated per event");
                Assert.Equal(order.TotalAmount, approved.AmountCharged);
                Assert.Equal(order.Items, approved.Items);
            }
            else
            {
                failedCount++;
                var failed = JsonSerializer.Deserialize<PaymentFailedEvent>(result.Message.Value, EventJsonOptions.Default)!;
                Assert.Equal(orderId, failed.OrderId);
                Assert.Equal("Card declined", failed.Reason);
            }
        }

        Assert.Equal(orderCount, approvedCount + failedCount);
        // ~90/10 split; generous tolerance to avoid flakiness while still catching a badly-skewed distribution.
        Assert.InRange(approvedCount, orderCount * 0.75, orderCount * 0.99);
        Assert.InRange(failedCount, orderCount * 0.01, orderCount * 0.25);
    }

    [Fact]
    public async Task MalformedMessage_IsSkipped_AndSubsequentValidMessageIsStillProcessed()
    {
        using var consumer = CreateOutputConsumer();

        await _testProducer.ProduceAsync(OrderPlacedEvent.Topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = "{ this is not valid JSON for an OrderPlacedEvent",
        });

        var validOrder = MakeOrderPlaced();
        await PublishOrderPlacedAsync(validOrder);

        var result = TryConsume(consumer, TimeSpan.FromSeconds(30));

        Assert.NotNull(result);
        Assert.Equal(validOrder.OrderId.ToString(), result.Message.Key);

        // the malformed message never produces an output of its own
        var second = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(second);

        // per ADR 0007: the processing failure is logged, not silently swallowed. The single-threaded consume
        // loop processes the malformed message before the valid one, so by the time its output was consumed above
        // the log entry has already been written.
        Assert.Contains(_capturingLogger.Messages, m => m.Contains("Failed to process message", StringComparison.OrdinalIgnoreCase));
    }
}

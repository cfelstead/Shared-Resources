using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopTheKafka.Contracts;
using ShopTheKafka.KafkaTestSupport;
using Testcontainers.Kafka;
using static ShopTheKafka.KafkaTestSupport.KafkaTestConsumer;

namespace ShopTheKafka.InventoryService.Tests;

public sealed class InventoryServiceTests : IAsyncLifetime
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
        consumer.Subscribe([InventoryReservedEvent.Topic, InventoryBackorderEvent.Topic]);
        return consumer;
    }

    private async Task PublishPaymentApprovedAsync(PaymentApprovedEvent paymentApproved)
    {
        var payload = JsonSerializer.Serialize(paymentApproved, EventJsonOptions.Default);
        await _testProducer.ProduceAsync(PaymentApprovedEvent.Topic, new Message<string, string>
        {
            Key = paymentApproved.OrderId.ToKafkaKey(),
            Value = payload,
        });
    }

    private static PaymentApprovedEvent MakePaymentApproved(Guid? orderId = null, IReadOnlyList<Item>? items = null) => new(
        EventId: Guid.NewGuid(),
        OrderId: orderId ?? Guid.NewGuid(),
        OccurredAtUtc: DateTimeOffset.UtcNow,
        PaymentId: Guid.NewGuid(),
        AmountCharged: 19.98m,
        Items: items ?? [new Item("Widget", 2, 9.99m)]);

    [Fact]
    public async Task OrderWithNoGizmoLine_ProducesInventoryReserved_AndNothingOnBackorder()
    {
        using var consumer = CreateOutputConsumer();
        var paymentApproved = MakePaymentApproved(items: [new Item("Widget", 2, 9.99m), new Item("Gadget", 1, 4.99m)]);

        await PublishPaymentApprovedAsync(paymentApproved);

        var first = TryConsume(consumer, TimeSpan.FromSeconds(30));
        Assert.NotNull(first);
        Assert.Equal(InventoryReservedEvent.Topic, first.Topic);
        Assert.Equal(paymentApproved.OrderId.ToString(), first.Message.Key);

        var second = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(second);
    }

    [Fact]
    public async Task OrderWithGizmoLine_ProducesInventoryBackorder_ListingGizmo_AndNothingOnReserved()
    {
        using var consumer = CreateOutputConsumer();
        var paymentApproved = MakePaymentApproved(items: [new Item("Widget", 2, 9.99m), new Item("Gizmo", 1, 29.99m)]);

        await PublishPaymentApprovedAsync(paymentApproved);

        var first = TryConsume(consumer, TimeSpan.FromSeconds(30));
        Assert.NotNull(first);
        Assert.Equal(InventoryBackorderEvent.Topic, first.Topic);
        Assert.Equal(paymentApproved.OrderId.ToString(), first.Message.Key);

        var backorder = JsonSerializer.Deserialize<InventoryBackorderEvent>(first.Message.Value, EventJsonOptions.Default)!;
        Assert.Contains("Gizmo", backorder.UnavailableItemNames);

        var second = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(second);
    }

    [Fact]
    public async Task MalformedMessage_IsSkipped_AndSubsequentValidMessageIsStillProcessed()
    {
        using var consumer = CreateOutputConsumer();

        await _testProducer.ProduceAsync(PaymentApprovedEvent.Topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = "{ this is not valid JSON for a PaymentApprovedEvent",
        });

        var validPayment = MakePaymentApproved();
        await PublishPaymentApprovedAsync(validPayment);

        var result = TryConsume(consumer, TimeSpan.FromSeconds(30));

        Assert.NotNull(result);
        Assert.Equal(validPayment.OrderId.ToString(), result.Message.Key);

        // the malformed message never produces an output of its own
        var second = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(second);

        // per ADR 0007: the processing failure is logged, not silently swallowed. The single-threaded consume
        // loop processes the malformed message before the valid one, so by the time its output was consumed above
        // the log entry has already been written.
        Assert.Contains(_capturingLogger.Messages, m => m.Contains("Failed to process message", StringComparison.OrdinalIgnoreCase));
    }
}

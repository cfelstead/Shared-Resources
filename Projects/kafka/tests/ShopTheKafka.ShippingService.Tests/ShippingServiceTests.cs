using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopTheKafka.Contracts;
using ShopTheKafka.KafkaTestSupport;
using Testcontainers.Kafka;
using static ShopTheKafka.KafkaTestSupport.KafkaTestConsumer;

namespace ShopTheKafka.ShippingService.Tests;

public sealed class ShippingServiceTests : IAsyncLifetime
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
        consumer.Subscribe(OrderShippedEvent.Topic);
        return consumer;
    }

    private async Task PublishInventoryReservedAsync(InventoryReservedEvent inventoryReserved)
    {
        var payload = JsonSerializer.Serialize(inventoryReserved, EventJsonOptions.Default);
        await _testProducer.ProduceAsync(InventoryReservedEvent.Topic, new Message<string, string>
        {
            Key = inventoryReserved.OrderId.ToKafkaKey(),
            Value = payload,
        });
    }

    private static InventoryReservedEvent MakeInventoryReserved(Guid? orderId = null) => new(
        EventId: Guid.NewGuid(),
        OrderId: orderId ?? Guid.NewGuid(),
        OccurredAtUtc: DateTimeOffset.UtcNow);

    [Fact]
    public async Task InventoryReserved_ProducesExactlyOneOrderShippedEvent()
    {
        using var consumer = CreateOutputConsumer();
        var inventoryReserved = MakeInventoryReserved();

        await PublishInventoryReservedAsync(inventoryReserved);

        var first = TryConsume(consumer, TimeSpan.FromSeconds(30));
        Assert.NotNull(first);
        Assert.Equal(OrderShippedEvent.Topic, first.Topic);

        var second = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(second);
    }

    [Fact]
    public async Task OrderShippedEvent_CarriesFreshShipmentId_AndEstimatedDeliveryDateThreeDaysOut()
    {
        using var consumer = CreateOutputConsumer();
        var inventoryReserved = MakeInventoryReserved();

        await PublishInventoryReservedAsync(inventoryReserved);

        var result = TryConsume(consumer, TimeSpan.FromSeconds(30));
        Assert.NotNull(result);

        var shipped = JsonSerializer.Deserialize<OrderShippedEvent>(result.Message.Value, EventJsonOptions.Default)!;
        Assert.NotEqual(Guid.Empty, shipped.ShipmentId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3), shipped.EstimatedDeliveryDate);
    }

    [Fact]
    public async Task OrderShippedEvent_IsKeyedBySameOrderId_AsConsumedInventoryReservedEvent()
    {
        using var consumer = CreateOutputConsumer();
        var inventoryReserved = MakeInventoryReserved();

        await PublishInventoryReservedAsync(inventoryReserved);

        var result = TryConsume(consumer, TimeSpan.FromSeconds(30));
        Assert.NotNull(result);
        Assert.Equal(inventoryReserved.OrderId.ToString(), result.Message.Key);

        var shipped = JsonSerializer.Deserialize<OrderShippedEvent>(result.Message.Value, EventJsonOptions.Default)!;
        Assert.Equal(inventoryReserved.OrderId, shipped.OrderId);
    }

    [Fact]
    public async Task MalformedMessage_IsSkipped_AndSubsequentValidMessageIsStillProcessed()
    {
        using var consumer = CreateOutputConsumer();

        await _testProducer.ProduceAsync(InventoryReservedEvent.Topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = "{ this is not valid JSON for an InventoryReservedEvent",
        });

        var validReserved = MakeInventoryReserved();
        await PublishInventoryReservedAsync(validReserved);

        var result = TryConsume(consumer, TimeSpan.FromSeconds(30));

        Assert.NotNull(result);
        Assert.Equal(validReserved.OrderId.ToString(), result.Message.Key);

        // the malformed message never produces an output of its own
        var second = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(second);

        // per ADR 0007: the processing failure is logged, not silently swallowed. The single-threaded consume
        // loop processes the malformed message before the valid one, so by the time its output was consumed above
        // the log entry has already been written.
        Assert.Contains(_capturingLogger.Messages, m => m.Contains("Failed to process message", StringComparison.OrdinalIgnoreCase));
    }
}

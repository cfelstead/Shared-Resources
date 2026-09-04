using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopTheKafka.Contracts;
using Testcontainers.Kafka;

namespace ShopTheKafka.NotificationService.Tests;

public sealed class NotificationServiceTests : IAsyncLifetime
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
            ["--ConnectionStrings:kafka=" + _kafka.GetBootstrapAddress()],
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

    private async Task PublishAsync<TEvent>(string topic, Guid orderId, TEvent evt)
    {
        var payload = JsonSerializer.Serialize(evt, EventJsonOptions.Default);
        await _testProducer.ProduceAsync(topic, new Message<string, string>
        {
            Key = orderId.ToString(),
            Value = payload,
        });
    }

    private static async Task<bool> WaitForLogAsync(ConcurrentQueue<string> messages, Func<string, bool> predicate, TimeSpan overallTimeout)
    {
        var deadline = DateTime.UtcNow + overallTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (messages.Any(predicate))
            {
                return true;
            }
            await Task.Delay(250);
        }
        return false;
    }

    [Fact]
    public async Task ConsumingPaymentFailedEvent_LogsOrderIdAndReason()
    {
        var orderId = Guid.NewGuid();
        var paymentFailed = new PaymentFailedEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Reason: "Card declined");

        await PublishAsync(PaymentFailedEvent.Topic, orderId, paymentFailed);

        var found = await WaitForLogAsync(
            _capturingLogger.Messages,
            m => m.Contains(orderId.ToString(), StringComparison.Ordinal) && m.Contains("Card declined", StringComparison.Ordinal),
            TimeSpan.FromSeconds(30));

        Assert.True(found, "Expected a log message identifying the orderId and failure reason");
    }

    [Fact]
    public async Task ConsumingInventoryBackorderEvent_LogsOrderIdAndUnavailableItems()
    {
        var orderId = Guid.NewGuid();
        var backorder = new InventoryBackorderEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            UnavailableItemNames: ["Gizmo"]);

        await PublishAsync(InventoryBackorderEvent.Topic, orderId, backorder);

        var found = await WaitForLogAsync(
            _capturingLogger.Messages,
            m => m.Contains(orderId.ToString(), StringComparison.Ordinal) && m.Contains("Gizmo", StringComparison.Ordinal),
            TimeSpan.FromSeconds(30));

        Assert.True(found, "Expected a log message identifying the orderId and unavailable item(s)");
    }

    [Fact]
    public async Task ConsumingOrderShippedEvent_LogsOrderIdAndEstimatedDeliveryDate()
    {
        var orderId = Guid.NewGuid();
        var estimatedDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(5));
        var shipped = new OrderShippedEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            ShipmentId: Guid.NewGuid(),
            EstimatedDeliveryDate: estimatedDeliveryDate);

        await PublishAsync(OrderShippedEvent.Topic, orderId, shipped);

        var found = await WaitForLogAsync(
            _capturingLogger.Messages,
            m => m.Contains(orderId.ToString(), StringComparison.Ordinal) && m.Contains(estimatedDeliveryDate.ToString(), StringComparison.Ordinal),
            TimeSpan.FromSeconds(30));

        Assert.True(found, "Expected a log message identifying the orderId and estimatedDeliveryDate");
    }

    [Fact]
    public async Task ProcessingAllThreeInputs_PublishesNoEventToAnyTopic()
    {
        var orderId = Guid.NewGuid();
        var paymentFailed = new PaymentFailedEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow, "Card declined");
        var backorder = new InventoryBackorderEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow, ["Gizmo"]);
        var shipped = new OrderShippedEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));

        await PublishAsync(PaymentFailedEvent.Topic, orderId, paymentFailed);
        await PublishAsync(InventoryBackorderEvent.Topic, orderId, backorder);
        await PublishAsync(OrderShippedEvent.Topic, orderId, shipped);

        // wait for all three to be processed (logged) before checking what actually landed on the topics
        await WaitForLogAsync(
            _capturingLogger.Messages,
            _ => _capturingLogger.Messages.Count(m => m.Contains(orderId.ToString(), StringComparison.Ordinal)) >= 3,
            TimeSpan.FromSeconds(30));

        // Consume every message that actually exists on the three topics NotificationService reads from - the
        // only topics it could conceivably have published to, since it never wires up a producer. If processing
        // had published anything, it would show up here alongside the three messages this test itself produced.
        using var verificationConsumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        verificationConsumer.Subscribe([PaymentFailedEvent.Topic, InventoryBackorderEvent.Topic, OrderShippedEvent.Topic]);

        var observed = new List<ConsumeResult<string, string>>();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var result = verificationConsumer.Consume(TimeSpan.FromMilliseconds(500));
                if (result?.Message is not null)
                {
                    observed.Add(result);
                }
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
            }
        }

        // exactly the 3 messages this test itself produced above - proves NotificationService produced nothing extra
        Assert.Equal(3, observed.Count);
        Assert.All(observed, r => Assert.Equal(orderId.ToString(), r.Message.Key));
    }

    [Fact]
    public async Task SubscribedTopicNotYetCreated_LogsWarning()
    {
        // Nothing has been published yet in this test, so at least one of the three subscribed topics
        // (payment-failed, inventory-backorder, order-shipped) doesn't exist on the broker - the worker
        // should say so rather than looping silently.
        var found = await WaitForLogAsync(
            _capturingLogger.Messages,
            m => m.Contains("not yet available", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(10));

        Assert.True(found, "Expected a warning that a subscribed topic isn't available yet");
    }

    [Fact]
    public async Task MalformedMessage_IsSkipped_AndSubsequentValidMessageIsStillProcessed()
    {
        await _testProducer.ProduceAsync(PaymentFailedEvent.Topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = "{ this is not valid JSON for a PaymentFailedEvent",
        });

        var orderId = Guid.NewGuid();
        await PublishAsync(PaymentFailedEvent.Topic, orderId, new PaymentFailedEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow, "Card declined"));

        var found = await WaitForLogAsync(
            _capturingLogger.Messages,
            m => m.Contains(orderId.ToString(), StringComparison.Ordinal) && m.Contains("Card declined", StringComparison.Ordinal),
            TimeSpan.FromSeconds(30));

        Assert.True(found, "The valid message published after the malformed one should still be processed");

        // per ADR 0007: the processing failure is logged, not silently swallowed.
        Assert.Contains(_capturingLogger.Messages, m => m.Contains("Failed to process message", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Captures formatted log messages at Information level or above, so a test can assert something was logged.</summary>
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

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

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

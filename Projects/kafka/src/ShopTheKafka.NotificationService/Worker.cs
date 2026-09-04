using System.Text.Json;
using Confluent.Kafka;
using ShopTheKafka.Contracts;

namespace ShopTheKafka.NotificationService;

public sealed class Worker : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<Worker> _logger;
    private readonly Dictionary<string, Action<string>> _handlers;

    public Worker(IConsumer<string, string> consumer, ILogger<Worker> logger)
    {
        _consumer = consumer;
        _logger = logger;
        _handlers = new Dictionary<string, Action<string>>
        {
            [PaymentFailedEvent.Topic] = ProcessPaymentFailed,
            [InventoryBackorderEvent.Topic] = ProcessInventoryBackorder,
            [OrderShippedEvent.Topic] = ProcessOrderShipped,
        };
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe([PaymentFailedEvent.Topic, InventoryBackorderEvent.Topic, OrderShippedEvent.Topic]);
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;
            try
            {
                result = _consumer.Consume(TimeSpan.FromSeconds(1));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                // One of the three subscribed input topics may not exist yet (nothing has published to it yet) -
                // this is not a processing failure, so it doesn't go through the ADR 0007 log-and-skip path below,
                // but it's still worth a trace: otherwise a startup wait looks identical to a hung consumer.
                _logger.LogWarning(ex, "Subscribed topic not yet available (will retry)");
                continue;
            }

            if (result?.Message is null)
            {
                continue;
            }

            try
            {
                Process(result.Topic, result.Message.Value);
            }
            catch (Exception ex)
            {
                // Per ADR 0007: log and skip - the offset is auto-committed regardless of processing outcome.
                _logger.LogError(ex, "Failed to process message at {TopicPartitionOffset}; skipping", result.TopicPartitionOffset);
            }
        }
    }

    private void Process(string topic, string payload)
    {
        if (!_handlers.TryGetValue(topic, out var handler))
        {
            throw new InvalidOperationException($"Unexpected topic: {topic}");
        }

        handler(payload);
    }

    private void ProcessPaymentFailed(string payload)
    {
        var paymentFailed = Deserialize<PaymentFailedEvent>(payload);
        _logger.LogInformation(
            "Payment failed for order {OrderId}: {Reason}",
            paymentFailed.OrderId,
            paymentFailed.Reason);
    }

    private void ProcessInventoryBackorder(string payload)
    {
        var backorder = Deserialize<InventoryBackorderEvent>(payload);
        _logger.LogInformation(
            "Order {OrderId} backordered; unavailable items: {UnavailableItemNames}",
            backorder.OrderId,
            string.Join(", ", backorder.UnavailableItemNames));
    }

    private void ProcessOrderShipped(string payload)
    {
        var shipped = Deserialize<OrderShippedEvent>(payload);
        _logger.LogInformation(
            "Order {OrderId} shipped; estimated delivery {EstimatedDeliveryDate}",
            shipped.OrderId,
            shipped.EstimatedDeliveryDate);
    }

    private static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, EventJsonOptions.Default)
            ?? throw new InvalidOperationException($"Deserialized {typeof(T).Name} was null");
}

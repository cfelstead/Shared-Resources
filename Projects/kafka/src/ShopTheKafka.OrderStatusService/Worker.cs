using System.Text.Json;
using Confluent.Kafka;
using ShopTheKafka.Contracts;

namespace ShopTheKafka.OrderStatusService;

/// <summary>
/// Fans in on all 6 event topics to keep <see cref="OrderStatusStore"/> up to date - the only consumer with more
/// than one input topic in the pipeline.
/// </summary>
public sealed class Worker(
    IConsumer<string, string> consumer,
    OrderStatusStore store,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly Dictionary<string, Action<string>> _handlersByTopic = new()
    {
        [OrderPlacedEvent.Topic] = payload => store.ApplyOrderPlaced(Deserialize<OrderPlacedEvent>(payload)),
        [PaymentApprovedEvent.Topic] = payload => store.ApplyPaymentApproved(Deserialize<PaymentApprovedEvent>(payload)),
        [PaymentFailedEvent.Topic] = payload => store.ApplyPaymentFailed(Deserialize<PaymentFailedEvent>(payload)),
        [InventoryReservedEvent.Topic] = payload => store.ApplyInventoryReserved(Deserialize<InventoryReservedEvent>(payload)),
        [InventoryBackorderEvent.Topic] = payload => store.ApplyInventoryBackorder(Deserialize<InventoryBackorderEvent>(payload)),
        [OrderShippedEvent.Topic] = payload => store.ApplyOrderShipped(Deserialize<OrderShippedEvent>(payload)),
    };

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(_handlersByTopic.Keys);
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;
            try
            {
                result = consumer.Consume(TimeSpan.FromSeconds(1));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                // Fanning in on 6 topics means startup routinely races topics that don't exist yet - because
                // nothing has published to them - which throws here instead of just returning no message; transient.
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
                logger.LogError(ex, "Failed to process message at {TopicPartitionOffset}; skipping", result.TopicPartitionOffset);
            }
        }
    }

    private void Process(string topic, string payload)
    {
        if (!_handlersByTopic.TryGetValue(topic, out var handler))
        {
            throw new InvalidOperationException($"Unrecognized topic '{topic}'");
        }

        handler(payload);
    }

    private static TEvent Deserialize<TEvent>(string payload) =>
        JsonSerializer.Deserialize<TEvent>(payload, EventJsonOptions.Default)
            ?? throw new InvalidOperationException($"Deserialized {typeof(TEvent).Name} was null");
}

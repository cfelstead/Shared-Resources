using Confluent.Kafka;
using ShopTheKafka.Contracts;

namespace ShopTheKafka.PaymentService;

public sealed class Worker(
    IConsumer<string, string> consumer,
    IProducer<string, string> producer,
    ILogger<Worker> logger,
    IConfiguration configuration) : KafkaConsumeWorker<OrderPlacedEvent>(consumer, logger, configuration)
{
    private const string DeclinedReason = "Card declined";

    /// <summary>Independent per-Order chance a simulated charge is declined, per SPEC.md's "Payment simulation" decision.</summary>
    private const double DeclineProbability = 0.10;

    protected override string InputTopic => OrderPlacedEvent.Topic;

    protected override void Process(OrderPlacedEvent orderPlaced)
    {
        var key = orderPlaced.OrderId.ToKafkaKey();

        if (Random.Shared.NextDouble() < DeclineProbability)
        {
            var failed = new PaymentFailedEvent(
                EventId: Guid.NewGuid(),
                OrderId: orderPlaced.OrderId,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Reason: DeclinedReason);
            producer.PublishEvent(PaymentFailedEvent.Topic, key, failed);
        }
        else
        {
            var approved = new PaymentApprovedEvent(
                EventId: Guid.NewGuid(),
                OrderId: orderPlaced.OrderId,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                PaymentId: Guid.NewGuid(),
                AmountCharged: orderPlaced.TotalAmount,
                Items: orderPlaced.Items);
            producer.PublishEvent(PaymentApprovedEvent.Topic, key, approved);
        }
    }
}

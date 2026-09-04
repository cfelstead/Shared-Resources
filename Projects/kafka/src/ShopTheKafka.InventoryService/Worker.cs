using Confluent.Kafka;
using ShopTheKafka.Contracts;

namespace ShopTheKafka.InventoryService;

public sealed class Worker(
    IConsumer<string, string> consumer,
    IProducer<string, string> producer,
    ILogger<Worker> logger,
    IConfiguration configuration) : KafkaConsumeWorker<PaymentApprovedEvent>(consumer, logger, configuration)
{
    /// <summary>Per SPEC.md's "Inventory simulation" decision: permanently out of stock, backordering the whole Order per ADR 0004.</summary>
    private const string OutOfStockItemName = "Gizmo";

    protected override string InputTopic => PaymentApprovedEvent.Topic;

    protected override void Process(PaymentApprovedEvent paymentApproved)
    {
        var key = paymentApproved.OrderId.ToKafkaKey();

        var unavailableItemNames = paymentApproved.Items
            .Where(item => item.ItemName == OutOfStockItemName)
            .Select(item => item.ItemName)
            .ToList();

        if (unavailableItemNames.Count > 0)
        {
            var backorder = new InventoryBackorderEvent(
                EventId: Guid.NewGuid(),
                OrderId: paymentApproved.OrderId,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                UnavailableItemNames: unavailableItemNames);
            producer.PublishEvent(InventoryBackorderEvent.Topic, key, backorder);
        }
        else
        {
            var reserved = new InventoryReservedEvent(
                EventId: Guid.NewGuid(),
                OrderId: paymentApproved.OrderId,
                OccurredAtUtc: DateTimeOffset.UtcNow);
            producer.PublishEvent(InventoryReservedEvent.Topic, key, reserved);
        }
    }
}

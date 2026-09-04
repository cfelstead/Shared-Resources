using Confluent.Kafka;
using ShopTheKafka.Contracts;

namespace ShopTheKafka.ShippingService;

public sealed class Worker(
    IConsumer<string, string> consumer,
    IProducer<string, string> producer,
    ILogger<Worker> logger,
    IConfiguration configuration) : KafkaConsumeWorker<InventoryReservedEvent>(consumer, logger, configuration)
{
    protected override string InputTopic => InventoryReservedEvent.Topic;

    protected override void Process(InventoryReservedEvent inventoryReserved)
    {
        var key = inventoryReserved.OrderId.ToKafkaKey();

        var shipped = new OrderShippedEvent(
            EventId: Guid.NewGuid(),
            OrderId: inventoryReserved.OrderId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            ShipmentId: Guid.NewGuid(),
            EstimatedDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3));
        producer.PublishEvent(OrderShippedEvent.Topic, key, shipped);
    }
}

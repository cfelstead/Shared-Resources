namespace ShopTheKafka.OrderStatusService;

/// <summary>Where an Order currently sits in the pipeline, per SCHEMA.md's <c>OrderStatus.currentStatus</c>.</summary>
public enum OrderStatusValue
{
    Placed,
    PaymentApproved,
    PaymentFailed,
    InventoryReserved,
    InventoryBackorder,
    Shipped,
}

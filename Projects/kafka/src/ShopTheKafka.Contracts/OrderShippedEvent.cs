namespace ShopTheKafka.Contracts;

/// <summary>Published by ShippingService to <c>order-shipped</c> once a Shipment is created for a reserved Order.</summary>
public sealed record OrderShippedEvent(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid ShipmentId,
    DateOnly EstimatedDeliveryDate) : EventBase(EventId, OrderId, OccurredAtUtc)
{
    public const string Topic = "order-shipped";
}

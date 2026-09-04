namespace ShopTheKafka.Contracts;

/// <summary>Published by OrderService to <c>orders-placed</c> when a customer places an Order.</summary>
public sealed record OrderPlacedEvent(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid CustomerId,
    IReadOnlyList<Item> Items,
    decimal TotalAmount) : EventBase(EventId, OrderId, OccurredAtUtc)
{
    public const string Topic = "orders-placed";
}

namespace ShopTheKafka.Contracts;

/// <summary>Published by InventoryService to <c>inventory-reserved</c> when every Item in the Order is available.</summary>
public sealed record InventoryReservedEvent(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc) : EventBase(EventId, OrderId, OccurredAtUtc)
{
    public const string Topic = "inventory-reserved";
}

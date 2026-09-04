namespace ShopTheKafka.Contracts;

/// <summary>
/// Published by InventoryService to <c>inventory-backorder</c> when at least one Item is unavailable.
/// Backorder is all-or-nothing: any unavailable Item backorders the whole Order.
/// </summary>
public sealed record InventoryBackorderEvent(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyList<string> UnavailableItemNames) : EventBase(EventId, OrderId, OccurredAtUtc)
{
    public const string Topic = "inventory-backorder";
}

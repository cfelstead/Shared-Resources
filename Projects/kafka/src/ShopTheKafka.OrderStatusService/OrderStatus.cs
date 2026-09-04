using ShopTheKafka.Contracts;

namespace ShopTheKafka.OrderStatusService;

/// <summary>
/// The in-memory materialized view of a single Order, per SCHEMA.md. Rebuilt entirely from the 6 Event topics -
/// not persisted anywhere else.
/// </summary>
public sealed record OrderStatus(
    Guid OrderId,
    OrderStatusValue CurrentStatus,
    IReadOnlyList<Item> Items,
    decimal TotalAmount,
    IReadOnlyList<TimelineEntry> Timeline);

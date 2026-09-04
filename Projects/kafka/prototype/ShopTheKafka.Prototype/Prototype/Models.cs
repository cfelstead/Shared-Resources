namespace ShopTheKafka.Prototype.Prototype;

// Prototype-only models shaped to match SCHEMA.md's OrderStatus record.
// Not the real domain model — throwaway, for UI-shape exploration only.

public enum OrderStatusValue
{
    Placed,
    PaymentApproved,
    PaymentFailed,
    InventoryReserved,
    InventoryBackorder,
    Shipped
}

public record Item(string ItemName, int Quantity, decimal UnitPrice);

public record TimelineEntry(OrderStatusValue Status, DateTimeOffset OccurredAtUtc, string? Detail);

public class OrderStatusRecord
{
    public required Guid OrderId { get; init; }
    public required OrderStatusValue CurrentStatus { get; set; }
    public required IReadOnlyList<Item> Items { get; init; }
    public required decimal TotalAmount { get; init; }
    public List<TimelineEntry> Timeline { get; init; } = [];
}

namespace ShopTheKafka.OrderStatusService;

/// <summary>One entry per Event consumed for an Order, per SCHEMA.md's embedded <c>TimelineEntry</c> shape.</summary>
public sealed record TimelineEntry(
    OrderStatusValue Status,
    DateTimeOffset OccurredAtUtc,
    string? Detail);

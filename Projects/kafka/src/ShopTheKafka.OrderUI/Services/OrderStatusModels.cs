namespace ShopTheKafka.OrderUI.Services;

/// <summary>Mirrors SCHEMA.md's embedded Item shape as returned by OrderStatusService.</summary>
public sealed record OrderItem(string ItemName, int Quantity, decimal UnitPrice);

/// <summary>Mirrors SCHEMA.md's embedded TimelineEntry shape.</summary>
public sealed record TimelineEntry(OrderStatusValue Status, DateTimeOffset OccurredAtUtc, string? Detail);

/// <summary>The UI's read-only view of OrderStatusService's OrderStatus record (SCHEMA.md), as returned by
/// <c>GET /orders/{id}</c> and pushed by the <c>OrderStatusChanged</c> SignalR event.</summary>
public sealed record OrderStatusRecord(
    Guid OrderId,
    OrderStatusValue CurrentStatus,
    IReadOnlyList<OrderItem> Items,
    decimal TotalAmount,
    IReadOnlyList<TimelineEntry> Timeline);

/// <summary>One line of a Order being placed through the form: an Item name and quantity. Price is not sent -
/// OrderService's Catalog is the sole source of truth for pricing.</summary>
public sealed record OrderLineItem(string ItemName, int Quantity);

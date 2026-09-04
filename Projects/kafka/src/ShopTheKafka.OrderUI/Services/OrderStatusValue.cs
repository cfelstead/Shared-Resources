namespace ShopTheKafka.OrderUI.Services;

/// <summary>The UI's own copy of SCHEMA.md's <c>OrderStatus.currentStatus</c> shape, read over the HTTP + SignalR seam.</summary>
public enum OrderStatusValue
{
    Placed,
    PaymentApproved,
    PaymentFailed,
    InventoryReserved,
    InventoryBackorder,
    Shipped,
}

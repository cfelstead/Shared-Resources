namespace ShopTheKafka.OrderUI.Services;

/// <summary>Shared label/color lookup for the kanban board's columns (UI-DECISIONS.md's Variant C).</summary>
public static class StatusDisplay
{
    public static readonly OrderStatusValue[] Columns =
    [
        OrderStatusValue.Placed,
        OrderStatusValue.PaymentApproved,
        OrderStatusValue.PaymentFailed,
        OrderStatusValue.InventoryReserved,
        OrderStatusValue.InventoryBackorder,
        OrderStatusValue.Shipped,
    ];

    public static string Label(OrderStatusValue status) => status switch
    {
        OrderStatusValue.Placed => "Placed",
        OrderStatusValue.PaymentApproved => "Payment approved",
        OrderStatusValue.PaymentFailed => "Payment failed",
        OrderStatusValue.InventoryReserved => "Inventory reserved",
        OrderStatusValue.InventoryBackorder => "Backordered",
        OrderStatusValue.Shipped => "Shipped",
        _ => status.ToString(),
    };

    public static string Color(OrderStatusValue status) => status switch
    {
        OrderStatusValue.Placed => "#6c757d",
        OrderStatusValue.PaymentApproved => "#0d6efd",
        OrderStatusValue.PaymentFailed => "#dc3545",
        OrderStatusValue.InventoryReserved => "#6f42c1",
        OrderStatusValue.InventoryBackorder => "#fd7e14",
        OrderStatusValue.Shipped => "#198754",
        _ => "#6c757d",
    };
}

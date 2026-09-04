namespace ShopTheKafka.Prototype.Prototype;

// Shared label/color lookup only — each variant is free to lay this out differently.
public static class StatusDisplay
{
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

    public static bool IsTerminal(OrderStatusValue status) =>
        status is OrderStatusValue.PaymentFailed or OrderStatusValue.InventoryBackorder or OrderStatusValue.Shipped;
}

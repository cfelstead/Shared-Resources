namespace ShopTheKafka.OrderService;

public sealed record PlaceOrderRequest(Guid CustomerId, IReadOnlyList<PlaceOrderItem> Items);

public sealed record PlaceOrderItem(string ItemName, int Quantity);

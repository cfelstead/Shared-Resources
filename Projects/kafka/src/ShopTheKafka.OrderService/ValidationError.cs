namespace ShopTheKafka.OrderService;

/// <summary>One validation failure: which request field it's about, and why.</summary>
public sealed record ValidationError(string Field, string Message);

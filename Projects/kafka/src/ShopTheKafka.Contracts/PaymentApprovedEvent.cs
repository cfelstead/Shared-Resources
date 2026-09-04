namespace ShopTheKafka.Contracts;

/// <summary>Published by PaymentService to <c>payment-approved</c> when the simulated charge succeeds.</summary>
public sealed record PaymentApprovedEvent(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid PaymentId,
    decimal AmountCharged,
    IReadOnlyList<Item> Items) : EventBase(EventId, OrderId, OccurredAtUtc)
{
    public const string Topic = "payment-approved";
}

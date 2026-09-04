namespace ShopTheKafka.Contracts;

/// <summary>Published by PaymentService to <c>payment-failed</c> when the simulated charge fails (~10% of attempts).</summary>
public sealed record PaymentFailedEvent(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    string Reason) : EventBase(EventId, OrderId, OccurredAtUtc)
{
    public const string Topic = "payment-failed";
}

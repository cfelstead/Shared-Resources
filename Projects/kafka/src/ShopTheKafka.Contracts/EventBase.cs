namespace ShopTheKafka.Contracts;

/// <summary>
/// The envelope shared by every Event: <see cref="EventId"/> uniquely identifies the event,
/// <see cref="OrderId"/> is the Kafka partition key correlating it to its Order, and
/// <see cref="OccurredAtUtc"/> is when it happened.
/// </summary>
public abstract record EventBase(Guid EventId, Guid OrderId, DateTimeOffset OccurredAtUtc);

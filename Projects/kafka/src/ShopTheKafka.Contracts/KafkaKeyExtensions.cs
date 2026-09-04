namespace ShopTheKafka.Contracts;

public static class KafkaKeyExtensions
{
    /// <summary>Every service keys its produced messages by the Order's id, in string form.</summary>
    public static string ToKafkaKey(this Guid orderId) => orderId.ToString();
}

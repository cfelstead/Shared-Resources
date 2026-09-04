using Confluent.Kafka;

namespace ShopTheKafka.KafkaTestSupport;

public static class KafkaTestConsumer
{
    /// <summary>
    /// A topic that doesn't exist yet (because nothing has published to it) makes <c>Consume</c> throw
    /// <see cref="ErrorCode.UnknownTopicOrPart"/> immediately rather than waiting out the timeout, so a single
    /// call can't be used to wait for a message on a topic the producer hasn't created yet - this retries until
    /// <paramref name="overallTimeout"/> elapses.
    /// </summary>
    public static ConsumeResult<string, string>? TryConsume(IConsumer<string, string> consumer, TimeSpan overallTimeout)
    {
        var deadline = DateTime.UtcNow + overallTimeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(250));
                if (result is not null)
                {
                    return result;
                }
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                Thread.Sleep(250);
            }
        }
        return null;
    }
}

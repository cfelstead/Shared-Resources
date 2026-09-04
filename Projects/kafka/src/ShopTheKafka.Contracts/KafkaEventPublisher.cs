using System.Text.Json;
using Confluent.Kafka;

namespace ShopTheKafka.Contracts;

/// <summary>Serialize-and-produce shape shared by every service that publishes an Event to Kafka.</summary>
public static class KafkaEventPublisher
{
    public static void PublishEvent<TEvent>(this IProducer<string, string> producer, string topic, string key, TEvent evt)
    {
        var payload = JsonSerializer.Serialize(evt, EventJsonOptions.Default);
        producer.Produce(topic, new Message<string, string> { Key = key, Value = payload });
        producer.Flush(TimeSpan.FromSeconds(10));
    }
}

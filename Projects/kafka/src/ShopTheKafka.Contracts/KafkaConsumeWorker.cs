using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShopTheKafka.Contracts;

/// <summary>
/// Shared consume-loop scaffold for a service that consumes one Event type from one topic: subscribes,
/// deserializes each message, and hands it to <see cref="Process"/>. Per ADR 0007, any unhandled exception -
/// including a failed deserialization - is logged and the message skipped (the offset is committed regardless
/// of processing outcome) rather than retried, crashed on, or dead-lettered.
/// </summary>
public abstract class KafkaConsumeWorker<TIn>(
    IConsumer<string, string> consumer,
    ILogger logger,
    IConfiguration configuration) : BackgroundService
{
    /// <summary>
    /// Simulated processing time applied before every message is handled, so a human watching the UI can see an
    /// order visibly sit at each pipeline stage instead of the whole pipeline completing near-instantly.
    /// Configurable via "Processing:DelaySeconds" (defaults to 5) so tests can zero it out.
    /// </summary>
    private readonly TimeSpan _processingDelay = TimeSpan.FromSeconds(
        int.TryParse(configuration["Processing:DelaySeconds"], out var seconds) ? seconds : 5);

    protected abstract string InputTopic { get; }

    protected abstract void Process(TIn message);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(InputTopic);
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;
            try
            {
                result = consumer.Consume(TimeSpan.FromSeconds(1));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                // On a cold broker, this service's input topic may not exist yet - not until some producer
                // publishes to it for the first time. That's transient, not a bug: per ADR 0007's philosophy that
                // a crash loop is worse than a skipped message, log and keep polling rather than taking the whole
                // host down (BackgroundServiceExceptionBehavior defaults to StopHost). Any other ConsumeException
                // is a genuine, likely non-transient failure (bad credentials, broker misconfiguration, ...) and
                // is deliberately left to propagate and fault the host, rather than retried forever.
                logger.LogWarning(ex, "Input topic {Topic} does not exist yet; will keep retrying", InputTopic);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                continue;
            }

            if (result?.Message is null)
            {
                continue;
            }

            try
            {
                var message = JsonSerializer.Deserialize<TIn>(result.Message.Value, EventJsonOptions.Default)
                    ?? throw new InvalidOperationException($"Deserialized {typeof(TIn).Name} was null");

                if (_processingDelay > TimeSpan.Zero)
                {
                    Thread.Sleep(_processingDelay);
                }

                Process(message);
            }
            catch (Exception ex)
            {
                // Per ADR 0007: log and skip - the offset is auto-committed regardless of processing outcome.
                logger.LogError(ex, "Failed to process message at {TopicPartitionOffset}; skipping", result.TopicPartitionOffset);
            }
        }
    }
}

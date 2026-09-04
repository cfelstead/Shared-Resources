using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ShopTheKafka.Contracts;
using Testcontainers.Kafka;

namespace ShopTheKafka.Contracts.Tests;

/// <summary>
/// Regression coverage for the race a real Aspire cold start hits: every service subscribes to its input topic
/// before anything has ever published to it, so the broker has no such topic yet. Ticket 08's end-to-end smoke
/// tests exposed that <see cref="KafkaConsumeWorker{TIn}"/> used to let that <see cref="ConsumeException"/>
/// propagate out of the consume loop, faulting the BackgroundService and (with the default
/// HostOptions.BackgroundServiceExceptionBehavior) crashing the whole host - which ADR 0007 already establishes
/// as worse than logging and continuing.
/// </summary>
public sealed class KafkaConsumeWorkerTests : IAsyncLifetime
{
#pragma warning disable CS0618 // parameterless KafkaBuilder is obsolete but the recommended image parameter isn't published on any tag yet
    private readonly KafkaContainer _kafka = new KafkaBuilder().Build();
#pragma warning restore CS0618

    public Task InitializeAsync() => _kafka.StartAsync();

    public Task DisposeAsync() => _kafka.DisposeAsync().AsTask();

    private sealed record TestMessage(string Value);

    private static readonly IConfiguration NoDelayConfiguration = new ConfigurationBuilder()
        .AddInMemoryCollection([new("Processing:DelaySeconds", "0")])
        .Build();

    private sealed class RecordingWorker(IConsumer<string, string> consumer, ILogger logger, string inputTopic, ConcurrentQueue<string> received)
        : KafkaConsumeWorker<TestMessage>(consumer, logger, NoDelayConfiguration)
    {
        protected override string InputTopic => inputTopic;

        protected override void Process(TestMessage message) => received.Enqueue(message.Value);
    }

    [Fact]
    public async Task InputTopicDoesNotExistYet_ConsumeLoopSurvives_AndStillProcessesOnceTopicExists()
    {
        var topic = $"not-yet-created-{Guid.NewGuid()}";
        var received = new ConcurrentQueue<string>();

        // AllowAutoCreateTopics = false makes the "topic doesn't exist yet" ConsumeException deterministic,
        // rather than racing against how fast the worker's first Consume() call happens to run.
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = false,
        }).Build();

        var worker = new RecordingWorker(consumer, NullLogger.Instance, topic, received);
        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);

        try
        {
            // Give the consume loop several chances to hit (and survive) the missing-topic error before the
            // topic exists at all.
            await Task.Delay(TimeSpan.FromSeconds(3));

            using var producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = _kafka.GetBootstrapAddress(),
            }).Build();
            await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = JsonSerializer.Serialize(new TestMessage("hello"), EventJsonOptions.Default),
            });

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            while (received.IsEmpty && DateTime.UtcNow < deadline)
            {
                await Task.Delay(250);
            }

            Assert.Contains("hello", received);
        }
        finally
        {
            cts.Cancel();
            await worker.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// A <see cref="ConsumeException"/> whose error isn't "the topic doesn't exist yet" must NOT be swallowed and
    /// retried forever - per ADR 0007, a genuinely broken consumer (bad credentials, broker misconfiguration, ...)
    /// should still fault the BackgroundService rather than silently spin, which is what an unbounded catch-and-
    /// retry on every ConsumeException would do.
    /// </summary>
    [Fact]
    public async Task NonTopicMissingConsumeException_IsNotSwallowed_AndFaultsTheBackgroundService()
    {
        var received = new ConcurrentQueue<string>();
        var consumer = new ThrowingConsumer(new Error(ErrorCode.Local_Authentication, "simulated non-transient failure"));

        var worker = new RecordingWorker(consumer, NullLogger.Instance, "irrelevant-topic", received);
        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);

        try
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            while (worker.ExecuteTask is { IsCompleted: false } && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            Assert.NotNull(worker.ExecuteTask);
            Assert.True(worker.ExecuteTask.IsFaulted, "a non-transient ConsumeException should fault the BackgroundService, not be retried forever");
            Assert.IsType<ConsumeException>(worker.ExecuteTask.Exception!.GetBaseException());
        }
        finally
        {
            cts.Cancel();
            try
            {
                await worker.StopAsync(CancellationToken.None);
            }
            catch (ConsumeException)
            {
                // already faulted with this exception - StopAsync surfaces it again, which we've already asserted on
            }
        }
    }

    /// <summary>Deterministically throws the given <see cref="Error"/> from every <see cref="Consume(TimeSpan)"/>
    /// call, to exercise <see cref="KafkaConsumeWorker{TIn}"/>'s exception handling without depending on a real
    /// broker producing a specific, hard-to-control error code.</summary>
    private sealed class ThrowingConsumer(Error error) : IConsumer<string, string>
    {
        public ConsumeResult<string, string> Consume(int millisecondsTimeout) => throw new ConsumeException(new ConsumeResult<byte[]?, byte[]?>(), error);

        public ConsumeResult<string, string> Consume(CancellationToken cancellationToken = default) => throw new ConsumeException(new ConsumeResult<byte[]?, byte[]?>(), error);

        public ConsumeResult<string, string> Consume(TimeSpan timeout) => throw new ConsumeException(new ConsumeResult<byte[]?, byte[]?>(), error);

        public void Subscribe(IEnumerable<string> topics)
        {
        }

        public void Subscribe(string topic)
        {
        }

        public void Unsubscribe()
        {
        }

        public void Assign(TopicPartition partition)
        {
        }

        public void Assign(TopicPartitionOffset partition)
        {
        }

        public void Assign(IEnumerable<TopicPartitionOffset> partitions)
        {
        }

        public void Assign(IEnumerable<TopicPartition> partitions)
        {
        }

        public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions)
        {
        }

        public void IncrementalAssign(IEnumerable<TopicPartition> partitions)
        {
        }

        public void IncrementalUnassign(IEnumerable<TopicPartition> partitions)
        {
        }

        public void Unassign() => throw new NotImplementedException();

        public void StoreOffset(ConsumeResult<string, string> result) => throw new NotImplementedException();

        public void StoreOffset(TopicPartitionOffset offset) => throw new NotImplementedException();

        public List<TopicPartitionOffset> Commit() => throw new NotImplementedException();

        public void Commit(IEnumerable<TopicPartitionOffset> offsets) => throw new NotImplementedException();

        public void Commit(ConsumeResult<string, string> result) => throw new NotImplementedException();

        public void Seek(TopicPartitionOffset tpo) => throw new NotImplementedException();

        public void Pause(IEnumerable<TopicPartition> partitions) => throw new NotImplementedException();

        public void Resume(IEnumerable<TopicPartition> partitions) => throw new NotImplementedException();

        public List<TopicPartitionOffset> Committed(TimeSpan timeout) => throw new NotImplementedException();

        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout) => throw new NotImplementedException();

        public Offset Position(TopicPartition partition) => throw new NotImplementedException();

        public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout) => throw new NotImplementedException();

        public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) => throw new NotImplementedException();

        public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) => throw new NotImplementedException();

        public void Close()
        {
        }

        public Handle Handle => throw new NotImplementedException();

        public string Name => nameof(ThrowingConsumer);

        public string MemberId => throw new NotImplementedException();

        public List<TopicPartition> Assignment => throw new NotImplementedException();

        public List<string> Subscription => throw new NotImplementedException();

        public IConsumerGroupMetadata ConsumerGroupMetadata => throw new NotImplementedException();

        public int AddBrokers(string brokers) => throw new NotImplementedException();

        public void SetSaslCredentials(string username, string password)
        {
        }

        public void Dispose()
        {
        }
    }
}

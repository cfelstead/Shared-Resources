using Confluent.Kafka;
using ShopTheKafka.NotificationService;

var host = Program.CreateHost(args);
await host.RunAsync();

public partial class Program
{
    // Mirrors InventoryService's CreateHost split (see its Program.cs) so tests can build and start the same
    // host directly, supplying a Testcontainers connection string and an optional log-capturing provider.
    public static IHost CreateHost(string[] args, Action<ILoggingBuilder>? configureLogging = null)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();
        builder.AddKafkaConsumer<string, string>("kafka", settings =>
        {
            settings.Config.GroupId = "notification-service";
            settings.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
            // Explicit per ADR 0007: NotificationService's log-and-skip error policy depends on the offset being
            // committed regardless of processing outcome, so this isn't left to ride on the client library's default.
            settings.Config.EnableAutoCommit = true;
        });
        builder.Services.AddHostedService<Worker>();
        configureLogging?.Invoke(builder.Logging);

        return builder.Build();
    }
}

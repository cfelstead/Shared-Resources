using Confluent.Kafka;
using ShopTheKafka.PaymentService;

var host = Program.CreateHost(args);
await host.RunAsync();

public partial class Program
{
    // Unlike OrderService's plain top-level Program (testable via WebApplicationFactory<Program>), PaymentService
    // is a non-web Worker Service, which has no host-testing equivalent - so CreateHost is factored out here to let
    // tests build and start the exact same host directly, supplying a Testcontainers connection string and
    // (optionally) a log-capturing provider that production startup has no reason to need.
    public static IHost CreateHost(string[] args, Action<ILoggingBuilder>? configureLogging = null)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();
        builder.AddKafkaProducer<string, string>("kafka");
        builder.AddKafkaConsumer<string, string>("kafka", settings =>
        {
            settings.Config.GroupId = "payment-service";
            settings.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
            // Explicit per ADR 0007: PaymentService's log-and-skip error policy depends on the offset being
            // committed regardless of processing outcome, so this isn't left to ride on the client library's default.
            settings.Config.EnableAutoCommit = true;
        });
        builder.Services.AddHostedService<Worker>();
        configureLogging?.Invoke(builder.Logging);

        return builder.Build();
    }
}

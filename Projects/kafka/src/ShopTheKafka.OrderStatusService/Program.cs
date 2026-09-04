using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using ShopTheKafka.OrderStatusService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKafkaConsumer<string, string>("kafka", settings =>
{
    settings.Config.GroupId = "order-status-service";
    settings.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
    // Explicit per ADR 0007: OrderStatusService's log-and-skip error policy depends on the offset being
    // committed regardless of processing outcome, so this isn't left to ride on the client library's default.
    settings.Config.EnableAutoCommit = true;
    // Fanning in on 6 topics means this consumer routinely subscribes before some of them have been created by
    // their owning service - the client library's default 5-minute metadata refresh would leave a newly-created
    // topic undiscovered for far too long, so this is tightened for this consumer specifically.
    settings.Config.TopicMetadataRefreshIntervalMs = 1000;
});

builder.Services.AddSingleton<OrderStatusStore>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSignalR()
    // SignalR's hub protocol has its own JSON options, separate from ConfigureHttpJsonOptions below.
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapHub<OrderStatusHub>("/hubs/order-status");

// Per SPEC.md's "Real-time updates" decision: broadcast every change to every client, no per-client filtering.
var hubContext = app.Services.GetRequiredService<IHubContext<OrderStatusHub>>();
var store = app.Services.GetRequiredService<OrderStatusStore>();
store.Changed += status => hubContext.Clients.All.SendAsync("OrderStatusChanged", status);

app.MapGet("/orders/{id:guid}", (Guid id, OrderStatusStore store) =>
{
    var status = store.TryGet(id);
    return status is null ? Results.NotFound() : Results.Ok(status);
});

app.Run();

public partial class Program;

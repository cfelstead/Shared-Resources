using Confluent.Kafka;
using ShopTheKafka.Contracts;
using ShopTheKafka.OrderService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKafkaProducer<string, string>("kafka");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/orders", (PlaceOrderRequest request, IProducer<string, string> producer) =>
{
    var errors = OrderRequestValidator.Validate(request);
    if (errors.Count > 0)
    {
        var errorsByField = errors
            .GroupBy(e => e.Field)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
        return Results.ValidationProblem(errorsByField);
    }

    var items = request.Items
        .Select(i => new Item(i.ItemName, i.Quantity, Catalog.Prices[i.ItemName]))
        .ToList();
    var totalAmount = items.Sum(i => i.UnitPrice * i.Quantity);

    var orderId = Guid.NewGuid();
    var placedEvent = new OrderPlacedEvent(
        EventId: Guid.NewGuid(),
        OrderId: orderId,
        OccurredAtUtc: DateTimeOffset.UtcNow,
        CustomerId: request.CustomerId,
        Items: items,
        TotalAmount: totalAmount);

    producer.PublishEvent(OrderPlacedEvent.Topic, orderId.ToString(), placedEvent);

    return Results.Created($"/orders/{orderId}", new { orderId });
});

app.Run();

public partial class Program;

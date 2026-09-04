using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc.Testing;
using ShopTheKafka.Contracts;
using Testcontainers.Kafka;

namespace ShopTheKafka.OrderService.Tests;

public sealed class OrdersEndpointTests : IAsyncLifetime
{
#pragma warning disable CS0618 // parameterless KafkaBuilder is obsolete but the recommended image parameter isn't published on any tag yet
    private readonly KafkaContainer _kafka = new KafkaBuilder().Build();
#pragma warning restore CS0618
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();

        // AddKafkaProducer reads the connection string while Program.cs's top-level statements run,
        // which happens before WebApplicationFactory's ConfigureAppConfiguration hook can inject config —
        // so the bootstrap address has to arrive via an environment variable, which the default
        // configuration pipeline picks up when WebApplicationBuilder.CreateBuilder(args) itself runs.
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", _kafka.GetBootstrapAddress());

        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _kafka.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
    }

    private IConsumer<string, string> CreateConsumer()
    {
        var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        consumer.Subscribe(OrderPlacedEvent.Topic);
        return consumer;
    }

    [Fact]
    public async Task ValidOrder_ReturnsCreated_AndPublishesExactlyOneMatchingEvent()
    {
        using var consumer = CreateConsumer();
        var customerId = Guid.NewGuid();
        var request = new
        {
            customerId,
            items = new[]
            {
                new { itemName = "Widget", quantity = 2 },
                new { itemName = "Gadget", quantity = 1 },
            },
        };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = body.GetProperty("orderId").GetGuid();
        Assert.NotEqual(Guid.Empty, orderId);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/orders/{orderId}", response.Headers.Location!.OriginalString);

        var expectedTotal = 2 * Catalog.Prices["Widget"] + 1 * Catalog.Prices["Gadget"];

        var first = consumer.Consume(TimeSpan.FromSeconds(15));
        Assert.NotNull(first);

        // camelCase JSON, per SCHEMA.md
        using var doc = JsonDocument.Parse(first.Message.Value);
        Assert.True(doc.RootElement.TryGetProperty("orderId", out _));
        Assert.True(doc.RootElement.TryGetProperty("totalAmount", out _));

        var evt = JsonSerializer.Deserialize<OrderPlacedEvent>(first.Message.Value, EventJsonOptions.Default)!;
        Assert.Equal(orderId, evt.OrderId);
        Assert.Equal(customerId, evt.CustomerId);
        Assert.Equal(expectedTotal, evt.TotalAmount);
        Assert.Equal(2, evt.Items.Count);
        Assert.Contains(evt.Items, i => i.ItemName == "Widget" && i.Quantity == 2 && i.UnitPrice == Catalog.Prices["Widget"]);
        Assert.Contains(evt.Items, i => i.ItemName == "Gadget" && i.Quantity == 1 && i.UnitPrice == Catalog.Prices["Gadget"]);
        Assert.Equal(orderId.ToString(), first.Message.Key);

        // exactly one event: no second message shows up
        var second = consumer.Consume(TimeSpan.FromSeconds(2));
        Assert.Null(second);
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task InvalidOrder_ReturnsBadRequest_AndPublishesNothing(object request)
    {
        using var consumer = CreateConsumer();

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var message = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(message);
    }

    [Fact]
    public async Task MissingCustomerId_ReturnsBadRequest_AndPublishesNothing()
    {
        using var consumer = CreateConsumer();

        // customerId property is entirely absent, not just an empty Guid
        var request = new { items = new[] { new { itemName = "Widget", quantity = 1 } } };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var message = TryConsume(consumer, TimeSpan.FromSeconds(2));
        Assert.Null(message);
    }

    [Fact]
    public async Task InvalidOrder_ProblemDetails_CarryFieldSpecificMessages()
    {
        using var consumer = CreateConsumer();

        var request = new
        {
            customerId = Guid.Empty,
            items = new[] { new { itemName = "NotACatalogItem", quantity = 0 } },
        };

        var response = await _client.PostAsJsonAsync("/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");

        var customerIdMessage = errors.GetProperty("customerId")[0].GetString();
        var quantityMessage = errors.GetProperty("items[].quantity")[0].GetString();
        var itemNameMessage = errors.GetProperty("items[].itemName")[0].GetString();

        // each field's message describes its own failure, not a shared generic string
        Assert.NotEqual(customerIdMessage, quantityMessage);
        Assert.NotEqual(quantityMessage, itemNameMessage);
        Assert.DoesNotContain("Invalid request.", new[] { customerIdMessage, quantityMessage, itemNameMessage });

        TryConsume(consumer, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// A brand-new broker has no <c>orders-placed</c> topic until something publishes to it, and subscribing to
    /// a nonexistent topic throws rather than just returning no messages — which is itself proof nothing was published.
    /// </summary>
    private static ConsumeResult<string, string>? TryConsume(IConsumer<string, string> consumer, TimeSpan timeout)
    {
        try
        {
            return consumer.Consume(timeout);
        }
        catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
        {
            return null;
        }
    }

    public static IEnumerable<object[]> InvalidRequests()
    {
        // empty items
        yield return new object[]
        {
            new { customerId = Guid.NewGuid(), items = Array.Empty<object>() },
        };

        // quantity out of range (0 and 10)
        yield return new object[]
        {
            new { customerId = Guid.NewGuid(), items = new[] { new { itemName = "Widget", quantity = 0 } } },
        };
        yield return new object[]
        {
            new { customerId = Guid.NewGuid(), items = new[] { new { itemName = "Widget", quantity = 10 } } },
        };

        // unknown item name
        yield return new object[]
        {
            new { customerId = Guid.NewGuid(), items = new[] { new { itemName = "NotACatalogItem", quantity = 1 } } },
        };

        // missing/empty customerId
        yield return new object[]
        {
            new { customerId = Guid.Empty, items = new[] { new { itemName = "Widget", quantity = 1 } } },
        };
    }
}

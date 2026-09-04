using System.Net.Http.Json;
using System.Text.Json;

namespace ShopTheKafka.OrderUI.Services;

/// <summary>Thin wrapper over the real <c>POST /orders</c> call on OrderService (SPEC.md's HTTP seam).</summary>
public sealed class OrderServiceClient(HttpClient http)
{
    public async Task<Guid> PlaceOrderAsync(Guid customerId, IReadOnlyList<OrderLineItem> items, CancellationToken ct = default)
    {
        var request = new
        {
            customerId,
            items = items.Select(i => new { itemName = i.ItemName, quantity = i.Quantity }),
        };

        var response = await http.PostAsJsonAsync("/orders", request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return body.GetProperty("orderId").GetGuid();
    }
}

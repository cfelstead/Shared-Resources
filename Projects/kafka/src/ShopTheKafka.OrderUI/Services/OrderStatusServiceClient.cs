using System.Net;
using System.Net.Http.Json;

namespace ShopTheKafka.OrderUI.Services;

/// <summary>Thin wrapper over the real <c>GET /orders/{id}</c> call on OrderStatusService (SPEC.md's HTTP seam).</summary>
public sealed class OrderStatusServiceClient(HttpClient http)
{
    public async Task<OrderStatusRecord?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/orders/{orderId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderStatusRecord>(OrderUiJsonOptions.Default, ct);
    }
}

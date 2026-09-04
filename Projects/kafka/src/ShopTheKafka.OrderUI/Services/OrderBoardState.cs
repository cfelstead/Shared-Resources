using Microsoft.AspNetCore.SignalR.Client;

namespace ShopTheKafka.OrderUI.Services;

/// <summary>
/// The board's in-memory view of every Order it has received an OrderStatus for (UI-DECISIONS.md's Variant C
/// kanban board), built entirely from the real OrderService/OrderStatusService HTTP + SignalR seam - never from
/// fake/local data.
/// </summary>
public sealed class OrderBoardState(
    OrderServiceClient orderClient,
    OrderStatusServiceClient statusClient,
    HubConnection hubConnection) : IAsyncDisposable
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, OrderStatusRecord> _orders = [];
    private bool _connected;

    /// <summary>Raised after the board's state changes, so a subscriber (the Razor page) can re-render.</summary>
    public event Action? Changed;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_connected)
        {
            return;
        }

        hubConnection.On<OrderStatusRecord>("OrderStatusChanged", Upsert);
        await hubConnection.StartAsync(ct);
        _connected = true;
    }

    /// <summary>
    /// Places a real Order via OrderService, then seeds the board with its authoritative initial OrderStatus via
    /// a short bounded poll of OrderStatusService - OrderStatusService only learns about the Order once it has
    /// consumed the OrderPlacedEvent off Kafka, an async hop with no fixed latency, so this poll is what lets the
    /// new Order appear on the board deterministically rather than racing the SignalR push.
    /// </summary>
    public async Task<Guid> PlaceOrderAsync(
        Guid customerId, IReadOnlyList<OrderLineItem> items, CancellationToken ct = default)
    {
        var orderId = await orderClient.PlaceOrderAsync(customerId, items, ct);

        for (var attempt = 0; attempt < 25; attempt++)
        {
            var record = await statusClient.GetOrderAsync(orderId, ct);
            if (record is not null)
            {
                Upsert(record);
                break;
            }

            await Task.Delay(200, ct);
        }

        return orderId;
    }

    public IReadOnlyList<OrderStatusRecord> GetAll()
    {
        lock (_lock)
        {
            return [.. _orders.Values];
        }
    }

    public IReadOnlyList<OrderStatusRecord> GetByStatus(OrderStatusValue status) =>
        [.. GetAll().Where(o => o.CurrentStatus == status)];

    private void Upsert(OrderStatusRecord record)
    {
        lock (_lock)
        {
            _orders[record.OrderId] = record;
        }

        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync() => await hubConnection.DisposeAsync();
}

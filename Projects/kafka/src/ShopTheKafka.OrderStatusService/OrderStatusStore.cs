using ShopTheKafka.Contracts;

namespace ShopTheKafka.OrderStatusService;

/// <summary>
/// The in-memory materialized view behind <c>GET /orders/{id}</c> and the SignalR push, built by fanning in on
/// all 6 event topics. Thread-safe: the fan-in consumer writes from a background thread while HTTP requests read
/// concurrently.
/// </summary>
public sealed class OrderStatusStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, OrderStatus> _statuses = [];

    /// <summary>Raised after every change, so the caller can push the updated <see cref="OrderStatus"/> to SignalR clients.</summary>
    public event Action<OrderStatus>? Changed;

    public OrderStatus? TryGet(Guid orderId)
    {
        lock (_lock)
        {
            return _statuses.GetValueOrDefault(orderId);
        }
    }

    public void ApplyOrderPlaced(OrderPlacedEvent evt)
    {
        Update(evt.OrderId, existing => (existing ?? Placeholder(evt.OrderId)) with
        {
            Items = evt.Items,
            TotalAmount = evt.TotalAmount,
        }, OrderStatusValue.Placed, evt.OccurredAtUtc, detail: null);
    }

    public void ApplyPaymentApproved(PaymentApprovedEvent evt) =>
        Update(evt.OrderId, existing => existing ?? Placeholder(evt.OrderId), OrderStatusValue.PaymentApproved, evt.OccurredAtUtc, detail: null);

    public void ApplyPaymentFailed(PaymentFailedEvent evt) =>
        Update(evt.OrderId, existing => existing ?? Placeholder(evt.OrderId), OrderStatusValue.PaymentFailed, evt.OccurredAtUtc, evt.Reason);

    public void ApplyInventoryReserved(InventoryReservedEvent evt) =>
        Update(evt.OrderId, existing => existing ?? Placeholder(evt.OrderId), OrderStatusValue.InventoryReserved, evt.OccurredAtUtc, detail: null);

    public void ApplyInventoryBackorder(InventoryBackorderEvent evt) =>
        Update(evt.OrderId, existing => existing ?? Placeholder(evt.OrderId), OrderStatusValue.InventoryBackorder, evt.OccurredAtUtc,
            string.Join(", ", evt.UnavailableItemNames));

    public void ApplyOrderShipped(OrderShippedEvent evt) =>
        Update(evt.OrderId, existing => existing ?? Placeholder(evt.OrderId), OrderStatusValue.Shipped, evt.OccurredAtUtc, detail: null);

    private static OrderStatus Placeholder(Guid orderId) => new(orderId, OrderStatusValue.Placed, Items: [], TotalAmount: 0, Timeline: []);

    /// <summary>
    /// Where each status sits in the pipeline, so a late-arriving event (e.g. a backfilling <see cref="OrderPlacedEvent"/>
    /// consumed after later stages) never pulls <see cref="OrderStatus.CurrentStatus"/> backward - only the timeline
    /// records arrival order; currentStatus always reflects the furthest pipeline stage seen so far.
    /// </summary>
    private static readonly Dictionary<OrderStatusValue, int> PipelineOrder = new()
    {
        [OrderStatusValue.Placed] = 0,
        [OrderStatusValue.PaymentApproved] = 1,
        [OrderStatusValue.PaymentFailed] = 1,
        [OrderStatusValue.InventoryReserved] = 2,
        [OrderStatusValue.InventoryBackorder] = 2,
        [OrderStatusValue.Shipped] = 3,
    };

    private void Update(
        Guid orderId,
        Func<OrderStatus?, OrderStatus> resolveCurrent,
        OrderStatusValue eventStatus,
        DateTimeOffset occurredAtUtc,
        string? detail)
    {
        OrderStatus updated;
        lock (_lock)
        {
            var current = resolveCurrent(_statuses.GetValueOrDefault(orderId));
            var entry = new TimelineEntry(eventStatus, occurredAtUtc, detail);
            var currentStatus = PipelineOrder[eventStatus] >= PipelineOrder[current.CurrentStatus]
                ? eventStatus
                : current.CurrentStatus;
            updated = current with
            {
                CurrentStatus = currentStatus,
                Timeline = [.. current.Timeline, entry],
            };
            _statuses[orderId] = updated;
        }

        Changed?.Invoke(updated);
    }
}

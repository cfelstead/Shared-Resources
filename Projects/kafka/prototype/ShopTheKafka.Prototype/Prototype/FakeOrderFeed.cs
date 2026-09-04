namespace ShopTheKafka.Prototype.Prototype;

// Stubbed live-data source for the prototype. Simulates the pipeline advancing
// orders through OrderStatusValue so the variants have something "live" to render.
// No real Kafka/SignalR involved — this is prototype-only, read-mostly fake data.
public class FakeOrderFeed : IDisposable
{
    private static readonly string[] SampleItemNames =
        ["Widget", "Gadget", "Gizmo", "Doohickey", "Thingamajig"];

    private readonly Dictionary<Guid, OrderStatusRecord> _orders = [];
    private readonly Random _random = new();
    private readonly Timer _timer;

    public event Action? Changed;

    public FakeOrderFeed()
    {
        for (var i = 0; i < 5; i++)
        {
            SeedOrder();
        }

        _timer = new Timer(_ => Advance(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public IReadOnlyList<OrderStatusRecord> GetAll() =>
        _orders.Values.OrderByDescending(o => o.Timeline[^1].OccurredAtUtc).ToList();

    public OrderStatusRecord? Get(Guid orderId) => _orders.GetValueOrDefault(orderId);

    // Stubbed mutation — a real POST /orders call is out of scope for a read-shape prototype.
    public OrderStatusRecord PlaceOrder(IReadOnlyList<Item> items)
    {
        var record = new OrderStatusRecord
        {
            OrderId = Guid.NewGuid(),
            CurrentStatus = OrderStatusValue.Placed,
            Items = items,
            TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice),
        };
        record.Timeline.Add(new TimelineEntry(OrderStatusValue.Placed, DateTimeOffset.UtcNow, null));
        _orders[record.OrderId] = record;
        Changed?.Invoke();
        return record;
    }

    private void SeedOrder()
    {
        var items = new List<Item> { RandomItem() };
        if (_random.Next(2) == 0) items.Add(RandomItem());

        var record = new OrderStatusRecord
        {
            OrderId = Guid.NewGuid(),
            CurrentStatus = OrderStatusValue.Placed,
            Items = items,
            TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice),
        };
        var placedAt = DateTimeOffset.UtcNow.AddMinutes(-_random.Next(1, 30));
        record.Timeline.Add(new TimelineEntry(OrderStatusValue.Placed, placedAt, null));
        _orders[record.OrderId] = record;

        // Fast-forward some seeded orders so the board isn't all "Placed" on first load.
        var hops = _random.Next(0, 4);
        for (var i = 0; i < hops; i++)
        {
            AdvanceOrder(record, seeding: true);
        }
    }

    private Item RandomItem() =>
        new(SampleItemNames[_random.Next(SampleItemNames.Length)], _random.Next(1, 4), Math.Round((decimal)(_random.NextDouble() * 40 + 5), 2));

    private void Advance()
    {
        var inFlight = _orders.Values.Where(o => o.Timeline.Count < 3 &&
            o.CurrentStatus is OrderStatusValue.Placed or OrderStatusValue.PaymentApproved or OrderStatusValue.InventoryReserved)
            .ToList();

        if (inFlight.Count == 0)
        {
            if (_orders.Count < 12) SeedOrder();
            Changed?.Invoke();
            return;
        }

        var order = inFlight[_random.Next(inFlight.Count)];
        AdvanceOrder(order, seeding: false);
        Changed?.Invoke();
    }

    private void AdvanceOrder(OrderStatusRecord order, bool seeding)
    {
        var now = seeding
            ? order.Timeline[^1].OccurredAtUtc.AddSeconds(_random.Next(5, 60))
            : DateTimeOffset.UtcNow;

        var (next, detail) = order.CurrentStatus switch
        {
            OrderStatusValue.Placed => _random.Next(10) == 0
                ? (OrderStatusValue.PaymentFailed, "Card declined")
                : (OrderStatusValue.PaymentApproved, (string?)null),
            OrderStatusValue.PaymentApproved => _random.Next(5) == 0
                ? (OrderStatusValue.InventoryBackorder, $"{order.Items[0].ItemName} unavailable")
                : (OrderStatusValue.InventoryReserved, (string?)null),
            OrderStatusValue.InventoryReserved => (OrderStatusValue.Shipped, (string?)null),
            _ => (order.CurrentStatus, (string?)null),
        };

        if (next == order.CurrentStatus) return;

        order.CurrentStatus = next;
        order.Timeline.Add(new TimelineEntry(next, now, detail));
    }

    public void Dispose() => _timer.Dispose();
}

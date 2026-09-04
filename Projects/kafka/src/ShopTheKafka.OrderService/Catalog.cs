namespace ShopTheKafka.OrderService;

/// <summary>The fixed 5-item catalog and server-side prices; clients cannot supply a free-text item name or a price.</summary>
public static class Catalog
{
    public static readonly IReadOnlyDictionary<string, decimal> Prices = new Dictionary<string, decimal>
    {
        ["Widget"] = 9.99m,
        ["Gadget"] = 14.99m,
        ["Gizmo"] = 19.99m,
        ["Doohickey"] = 4.99m,
        ["Thingamajig"] = 24.99m,
    };
}

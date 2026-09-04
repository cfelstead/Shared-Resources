namespace ShopTheKafka.OrderService;

public static class OrderRequestValidator
{
    public static IReadOnlyList<ValidationError> Validate(PlaceOrderRequest request)
    {
        var errors = new List<ValidationError>();

        if (request.CustomerId == Guid.Empty)
        {
            errors.Add(new ValidationError("customerId", "customerId is required."));
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors.Add(new ValidationError("items", "items must not be empty."));
        }
        else
        {
            foreach (var item in request.Items)
            {
                if (item.Quantity is < 1 or > 9)
                {
                    errors.Add(new ValidationError("items[].quantity", "quantity must be between 1 and 9."));
                }

                if (!Catalog.Prices.ContainsKey(item.ItemName))
                {
                    errors.Add(new ValidationError("items[].itemName", $"itemName must be one of: {string.Join(", ", Catalog.Prices.Keys)}."));
                }
            }
        }

        return errors;
    }
}

# ShopTheKafka

The domain glossary for ShopTheKafka, a demo e-commerce order pipeline built on .NET Aspire and Kafka. It exists so every service, event, and doc uses the same word for the same concept as an order moves from placement through payment, inventory, and shipping.

## Language

**Order**:
A customer's request to purchase one or more Items, identified by an `orderId` used as the Kafka partition key so every Event for one order stays ordered across topics.
_Avoid_: Purchase, transaction

**Event**:
An immutable fact published to a Kafka topic recording something that happened in the pipeline (e.g. `OrderPlacedEvent`). This is the canonical term for anything a service produces or consumes.
_Avoid_: Message (reserve "message" only for generic talk about the Kafka protocol itself, never for a specific payload)

**OrderStatus**:
The current position of a single Order in the pipeline (e.g. placed, payment-approved, shipped, backordered), derived entirely from the Events consumed for that order. Maintained by OrderStatusService's in-memory materialized view and returned by `GET /orders/{id}`.
_Avoid_: OrderStage, OrderState

**Payment**:
The simulated charge attempt made against a customer for an Order, performed by PaymentService.
_Avoid_: Charge (as a noun), Transaction

**Item**:
The generic unit of stock that InventoryService reserves against an Order. This demo has no catalog or SKU model — an Item is just a name simulated as available or out of stock.
_Avoid_: SKU, Product

**Reserve** (verb):
The action InventoryService takes to hold an Item against an Order. Matches the `inventory-reserved` topic name.
_Avoid_: Allocate

**Shipment**:
The record created by ShippingService once an Order's Items are reserved, representing that the Order has been handed off for delivery.
_Avoid_: Delivery, Fulfillment

**Notification**:
A customer-facing message sent by NotificationService in response to one of three pipeline outcomes: payment failure, inventory backorder, or successful shipment. Distinct from an Event — a Notification is triggered by an Event but is not itself published to a topic.
_Avoid_: Alert

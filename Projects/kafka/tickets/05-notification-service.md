# 05 — NotificationService

**What to build:** A customer gets a Notification logged for each of the pipeline's three terminal outcomes: a failed payment, a backordered Order, or a shipped Order — a fan-in consumer contrasting with the fan-out shape of the rest of the pipeline.

**Blocked by:** 01 — Aspire scaffold, shared event contracts, and OrderService (needs the shared contracts project and the `payment-failed`/`inventory-backorder`/`order-shipped` topics/schemas it defines).

**Status:** done

## Schema touchpoints

- `SCHEMA.md`: `PaymentFailedEvent`, `InventoryBackorderEvent`, `OrderShippedEvent` (all consumed only — this service produces no events)

## UI touchpoints

None.

## Testing requirements

- **Seam:** per-service Kafka-hop seam (`SPEC.md` Testing Decisions), with the NotificationService special case: since it has no output topic, tests assert against captured log output (a test logger), not a downstream topic.
- **Coverage bar:** every acceptance criterion below has at least one test at this seam, written before the code that satisfies it.

## Acceptance criteria

- [x] Consuming a `PaymentFailedEvent` logs a structured message identifying the `orderId` and the failure `reason`
- [x] Consuming an `InventoryBackorderEvent` logs a structured message identifying the `orderId` and the unavailable item(s)
- [x] Consuming an `OrderShippedEvent` logs a structured message identifying the `orderId` and the shipment's `estimatedDeliveryDate`
- [x] No event is published to any topic as a result of processing any of the three inputs
- [x] An unhandled exception while processing a message is logged and the offset is committed (message skipped) per [ADR 0007](../docs/adr/0007-consumer-error-policy.md)

# 04 — ShippingService

**What to build:** Every reserved Order gets a Shipment created for it, with a simulated delivery estimate, completing the happy path of the pipeline.

**Blocked by:** 01 — Aspire scaffold, shared event contracts, and OrderService (needs the shared contracts project and the `inventory-reserved`/`order-shipped` topics/schemas it defines).

**Status:** done

## Schema touchpoints

- `SCHEMA.md`: `InventoryReservedEvent` (consumed)
- `SCHEMA.md`: `OrderShippedEvent` (produced)

## UI touchpoints

None.

## Testing requirements

- **Seam:** per-service Kafka-hop seam (`SPEC.md` Testing Decisions) — produce a test `InventoryReservedEvent` directly onto `inventory-reserved`, assert against `order-shipped`. Do not depend on InventoryService's actual code.
- **Coverage bar:** every acceptance criterion below has at least one test at this seam, written before the code that satisfies it.

## Acceptance criteria

- [x] Consuming an `InventoryReservedEvent` always produces exactly one `OrderShippedEvent` on `order-shipped`
- [x] `OrderShippedEvent` carries a freshly generated `shipmentId` and `estimatedDeliveryDate` equal to today's date plus 3 days
- [x] The produced event is keyed by the same `orderId` as the consumed `InventoryReservedEvent`
- [x] An unhandled exception while processing a message is logged and the offset is committed (message skipped) per [ADR 0007](../docs/adr/0007-consumer-error-policy.md)

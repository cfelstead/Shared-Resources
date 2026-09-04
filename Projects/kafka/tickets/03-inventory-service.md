# 03 — InventoryService

**What to build:** Every approved payment gets its Items checked against simulated stock: an Order with no `Gizmo` line always reserves successfully; an Order with a `Gizmo` line always backorders entirely, listing which Item(s) caused it.

**Blocked by:** 01 — Aspire scaffold, shared event contracts, and OrderService (needs the shared contracts project and the `payment-approved`/`inventory-reserved`/`inventory-backorder` topics/schemas it defines, including `PaymentApprovedEvent.items`).

**Status:** done

## Schema touchpoints

- `SCHEMA.md`: `PaymentApprovedEvent` (consumed, including `items`)
- `SCHEMA.md`: `InventoryReservedEvent`, `InventoryBackorderEvent` (produced)

## UI touchpoints

None.

## Testing requirements

- **Seam:** per-service Kafka-hop seam (`SPEC.md` Testing Decisions) — produce a test `PaymentApprovedEvent` directly onto `payment-approved`, assert against `inventory-reserved`/`inventory-backorder`. Do not depend on PaymentService's actual code.
- **Coverage bar:** every acceptance criterion below has at least one test at this seam, written before the code that satisfies it.

## Acceptance criteria

- [x] An Order whose `items` contain no `Gizmo` line produces an `InventoryReservedEvent` on `inventory-reserved`, and nothing on `inventory-backorder`
- [x] An Order whose `items` contain a `Gizmo` line produces an `InventoryBackorderEvent` on `inventory-backorder` with `unavailableItemNames` containing `"Gizmo"`, and nothing on `inventory-reserved` — per [ADR 0004](../docs/adr/0004-all-or-nothing-backorder.md), the whole Order backorders even if it also contains available items
- [x] Every produced event is keyed by the same `orderId` as the consumed `PaymentApprovedEvent`
- [x] An unhandled exception while processing a message is logged and the offset is committed (message skipped) per [ADR 0007](../docs/adr/0007-consumer-error-policy.md)

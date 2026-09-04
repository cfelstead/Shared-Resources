# 06 — OrderStatusService

**What to build:** A developer (or the UI, in ticket 07) can ask for any Order's current status and full timeline over HTTP, and get live push updates as that status changes — built by fanning in on all 6 topics and keeping an in-memory materialized view that tolerates Kafka's normal cross-topic-ordering behavior rather than breaking under it.

**Blocked by:** 01 — Aspire scaffold, shared event contracts, and OrderService (needs the shared contracts project and all 6 topics/schemas it defines).

**Status:** done

## Schema touchpoints

- `SCHEMA.md`: `OrderStatus` (the in-memory record this ticket builds and serves — `orderId`, `currentStatus`, `items`, `totalAmount`, `timeline`)
- `SCHEMA.md`: reads all 6 event schemas (`OrderPlacedEvent` through `OrderShippedEvent`)

## UI touchpoints

None directly — this ticket backs the UI (ticket 07) but does not render anything itself.

## Testing requirements

- **Seam:** HTTP seam (`SPEC.md` Testing Decisions) — produce test events directly onto the 6 input topics (in whatever order a test needs, including out of order), assert via `GET /orders/{id}`. Never assert by reaching into the in-memory materialized view directly.
- **Coverage bar:** every acceptance criterion below has at least one test at this seam, written before the code that satisfies it.

## Acceptance criteria

- [x] After an `OrderPlacedEvent` is consumed, `GET /orders/{id}` returns `200` with `currentStatus: placed`, the Order's `items`/`totalAmount`, and a `timeline` containing one entry
- [x] Each subsequent event for the same `orderId` (`PaymentApprovedEvent`, `PaymentFailedEvent`, `InventoryReservedEvent`, `InventoryBackorderEvent`, `OrderShippedEvent`) updates `currentStatus` and appends a `timeline` entry; entries for `paymentFailed`/`inventoryBackorder` carry a non-null `detail` with the reason/unavailable items
- [x] `GET /orders/{id}` for an `orderId` with no consumed events at all returns `404 Not Found`
- [x] If an event other than `OrderPlacedEvent` arrives first for an `orderId`, a placeholder `OrderStatus` (`items: []`, `totalAmount: 0`) is created and the event is appended to its timeline rather than dropped or buffered, per [ADR 0006](../docs/adr/0006-eventual-consistency-in-order-status.md); once `OrderPlacedEvent` is later consumed, `items`/`totalAmount` are backfilled onto the existing record
- [x] Every `OrderStatus` change is pushed to all connected SignalR clients, with no per-client filtering
- [x] An unhandled exception while processing a message is logged and the offset is committed (message skipped) per [ADR 0007](../docs/adr/0007-consumer-error-policy.md)

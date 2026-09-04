# 02 — PaymentService

**What to build:** Every placed Order gets a simulated payment attempt: ~90% of the time it's approved, ~10% of the time it's declined — either outcome recorded as its own event, keyed to the same Order, so the pipeline can branch on it downstream.

**Blocked by:** 01 — Aspire scaffold, shared event contracts, and OrderService (needs the shared contracts project and the `orders-placed`/`payment-approved`/`payment-failed` topics/schemas it defines).

**Status:** done

## Schema touchpoints

- `SCHEMA.md`: `OrderPlacedEvent` (consumed)
- `SCHEMA.md`: `PaymentApprovedEvent`, `PaymentFailedEvent` (produced)

## UI touchpoints

None.

## Testing requirements

- **Seam:** per-service Kafka-hop seam (`SPEC.md` Testing Decisions) — produce a test `OrderPlacedEvent` directly onto `orders-placed` via a real Kafka broker (Testcontainers), assert against `payment-approved`/`payment-failed`. Do not depend on OrderService's actual code to produce the input event.
- **Coverage bar:** every acceptance criterion below has at least one test at this seam, written before the code that satisfies it.

## Acceptance criteria

- [x] Consuming an `OrderPlacedEvent` produces, independently at random, either a `PaymentApprovedEvent` or a `PaymentFailedEvent` — never both, never neither
- [x] Across many runs, approvals occur roughly 90% of the time and failures roughly 10% of the time (statistically, not per-message)
- [x] `PaymentApprovedEvent` carries a freshly generated `paymentId`, `amountCharged` equal to the consumed Order's `totalAmount`, and `items` echoed from the Order
- [x] `PaymentFailedEvent` carries `reason` equal to the fixed string `"Card declined"`
- [x] Every produced event is keyed by the same `orderId` as the consumed `OrderPlacedEvent`
- [x] An unhandled exception while processing a message is logged and the offset is committed (message skipped) per [ADR 0007](../docs/adr/0007-consumer-error-policy.md) — the service does not crash or block the partition

# 08 — End-to-end smoke tests

**What to build:** Proof that the six services are actually wired together correctly — not just individually correct — by driving the whole running Aspire app through its public HTTP surface for the three interesting end-to-end paths: happy path to shipped, payment failure, and backorder.

**Blocked by:** 01, 02, 03, 04, 05, 06 — every backend service must be real for these tests to prove anything.

**Status:** done

## Schema touchpoints

- `SCHEMA.md`: the full event chain (`OrderPlacedEvent` through `OrderShippedEvent`) and the final `OrderStatus` state for each path — read indirectly, through the HTTP surface only.

## UI touchpoints

None — these tests drive the backend directly, not through the Blazor UI.

## Testing requirements

- **Seam:** end-to-end seam (`SPEC.md` Testing Decisions) — run the whole Aspire app graph via `Aspire.Hosting.Testing` against a real Kafka broker, driven purely through `POST /orders` and `GET /orders/{id}`. This ticket exists to prove wiring, not to re-test business rules already covered by tickets 01-06's per-service tests — keep it to 2-3 tests, not a broader matrix.
- **Coverage bar:** the three acceptance criteria below are the entire coverage bar for this ticket; do not add more end-to-end cases here.

## Acceptance criteria

- [x] Placing an Order whose payment succeeds and whose items are all available eventually reaches `currentStatus: shipped` when polled via `GET /orders/{id}`, with a `timeline` showing all 4 happy-path stages in order
- [x] Placing an Order that experiences a simulated payment failure eventually reaches `currentStatus: paymentFailed`, with a `timeline` ending in an entry with a non-null `detail`
- [x] Placing an Order containing `Gizmo` eventually reaches `currentStatus: inventoryBackorder`, with a `timeline` ending in an entry whose `detail` mentions `"Gizmo"`

## Notes from implementation

- The happy-path test asserts the timeline contains all 4 stages, not their arrival order: OrderStatusService fans in on 6 independent topic subscriptions, and per ADR 0006 its timeline records arrival order, not pipeline order. A real full-pipeline run occasionally observes `shipped` before `inventoryReserved` even though ShippingService produces `OrderShippedEvent` causally after `InventoryReservedEvent` — this was hit and confirmed as expected, already-reviewed behavior (not a bug) while implementing this ticket. Strict per-event ordering is already covered at OrderStatusService's own seam (ticket 06).
- This ticket exposed a real bug in `ShopTheKafka.Contracts/KafkaConsumeWorker.cs` (from ticket 01): on a cold Kafka broker, a service's very first `Consume()` call throws `ConsumeException` ("Unknown topic or partition") if nothing has published to its input topic yet, and that exception wasn't caught — it faulted the `BackgroundService` and, with .NET's default `BackgroundServiceExceptionBehavior = StopHost`, crashed the whole service process. Every service in a genuinely cold Aspire start hit this, so none of this ticket's acceptance criteria could ever pass without fixing it. Fixed by catching `ConsumeException` in the consume loop and retrying (log and continue), consistent with ADR 0007's existing philosophy that a crash loop is worse than a skipped/delayed message. Covered by a new regression test in `tests/ShopTheKafka.Contracts.Tests`.

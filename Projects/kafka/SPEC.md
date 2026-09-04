# Order Pipeline

## Problem Statement

There's no small, hands-on project that demonstrates the realistic shape of event-driven microservices — a chain of topics, branching failure paths, fan-in consumption, and key-based ordering — using .NET Aspire and Kafka together. This project is that demo: a full order pipeline built as a set of small .NET services orchestrated by Aspire, with a UI to place orders and watch them move through the pipeline live. (See [PROJECT.md](./PROJECT.md).)

## Solution

Six services connected by six Kafka topics, keyed throughout by `orderId`, implement the schemas in [SCHEMA.md](./SCHEMA.md): OrderService accepts an Order and publishes `OrderPlacedEvent`; PaymentService, InventoryService, and ShippingService each consume one topic and produce the next event in the chain (or a branch event on simulated failure); NotificationService fans in on the three terminal-outcome topics and logs a message; OrderStatusService fans in on all six topics to maintain the in-memory `OrderStatus` materialized view behind `GET /orders/{id}` and a SignalR hub. The UI ([UI-DECISIONS.md](./UI-DECISIONS.md)) is a single kanban-board screen — one column per `OrderStatus` value, an order-placement form above the board, cards that move column to column live as the pipeline advances.

## User Stories

1. As a customer, I want to place an order for one or more catalog items, so that the order pipeline begins processing it.
2. As a customer, I want my order request rejected with a clear validation error if it's malformed, so that I don't create an order the pipeline can't process.
3. As a customer, I want my payment to be simulated with a realistic chance of failure, so that I can see how the system handles a failed charge.
4. As a customer, I want my order's items checked against simulated stock, so that I can see how the system handles an unavailable item.
5. As a customer whose order is fully reserved, I want it shipped with an estimated delivery date, so that I know the order completed successfully.
6. As a customer, I want to be notified when my payment fails, my order is backordered, or my order ships, so that I know the outcome of my order.
7. As anyone watching the dashboard, I want to see every order on a kanban board grouped by its current status, so that I can observe the whole pipeline's activity at a glance.
8. As anyone watching the dashboard, I want orders to move between columns live as their status changes, so that I can watch the event-driven pipeline work without refreshing.
9. As a developer, I want to query an order's current status and full timeline over HTTP, so that I can inspect pipeline state without the UI.
10. As a developer, I want every event keyed by `orderId`, so that Kafka preserves per-order ordering within each topic.
11. As a developer, I want each consuming service to skip and log a message it can't process rather than crash or block, so that one bad message doesn't take down the pipeline.
12. As a developer, I want OrderStatusService to behave sensibly when events for one order arrive out of order across topics, so that the dashboard doesn't break under Kafka's normal cross-topic ordering behavior.

## Implementation Decisions

Builds on [SCHEMA.md](./SCHEMA.md) and [UI-DECISIONS.md](./UI-DECISIONS.md); only new, behavioral decisions are recorded here.

**Item catalog**: Items are constrained to a fixed catalog of 5 names — `Widget`, `Gadget`, `Gizmo`, `Doohickey`, `Thingamajig` — each with a fixed `unitPrice` set server-side by OrderService. Clients cannot supply a free-text item name or a price.

**`POST /orders`**: Request body is `{ customerId: Guid, items: [{ itemName, quantity }] }`. Validation (400 Bad Request on failure): `items` non-empty; each `quantity` in `1..9` inclusive; each `itemName` one of the 5 catalog names; `customerId` a non-empty Guid. On success, `201 Created` with a body containing the new `orderId` and a `Location` header pointing at `GET /orders/{id}`.

**Payment simulation**: PaymentService fails a charge with an independent 10% random chance per Order, regardless of items or amount. `PaymentFailedEvent.reason` is always the fixed string `"Card declined"`.

**Inventory simulation**: `Gizmo` is permanently out of stock; the other 4 catalog items always reserve successfully. Per [ADR 0004](./docs/adr/0004-all-or-nothing-backorder.md), an Order containing a `Gizmo` line backorders entirely, regardless of its other items.

**Shipping simulation**: `OrderShippedEvent.estimatedDeliveryDate` is always today + 3 days.

**Notification "sending"**: NotificationService's only effect is a structured console log line per consumed event, e.g. `[Notification] Order {orderId}: payment failed — Card declined`. No email/SMS provider, no outbox, no retry.

**Eventual consistency in OrderStatusService**: see [ADR 0006](./docs/adr/0006-eventual-consistency-in-order-status.md) — placeholder records for out-of-order arrivals, and `GET /orders/{id}` returns `404 Not Found` uniformly for both unknown and not-yet-materialized orders.

**Consumer error policy**: see [ADR 0007](./docs/adr/0007-consumer-error-policy.md) — log and skip (commit offset) on an unhandled processing error, for every consuming service.

**Real-time updates**: OrderStatusService's SignalR hub broadcasts every `OrderStatus` change to every connected client — no per-user filtering, since there's no authentication ([PROJECT.md](./PROJECT.md) non-goals) and the dashboard is meant to show the whole pipeline's activity to anyone watching.

**Permissions**: None. No authentication or authorization exists anywhere in this system (per [PROJECT.md](./PROJECT.md) non-goals) — every endpoint and the UI are fully open.

## Testing Decisions

- **Per-service Kafka-hop seam** (primary): for PaymentService, InventoryService, ShippingService, and NotificationService, tests produce a message directly onto the service's input topic(s) using a real Kafka broker (Testcontainers) — bypassing upstream services — and assert against its output topic(s). NotificationService has no output topic, so its tests assert against captured log output instead.
- **HTTP seam**: OrderService is tested via `POST /orders`, asserting against the real `orders-placed` topic. OrderStatusService is tested by producing events directly onto its 6 input topics and asserting via `GET /orders/{id}` — never by reaching into its in-memory materialized view directly. This seam is also how OrderStatusService's out-of-order-arrival behavior (story 12) gets tested, since it requires precise control over event arrival order that the full pipeline can't guarantee.
- **End-to-end smoke tests** (2-3 total, not more): run the whole Aspire app graph via `Aspire.Hosting.Testing` against a real Kafka broker, driven purely through `POST /orders` and `GET /orders/{id}` — one happy path to `shipped`, one payment-failure path, one backorder path. These exist to prove the services are wired together correctly; they are not where business-rule edge cases get tested (that's the per-service seam above).
- A good test at any of these seams asserts observable behavior only — a topic's payload, an HTTP response body/status, a log line, or the SignalR-observable `OrderStatus` — never a service's internal state or implementation.
- Coverage bar: every user story above should be traceable to at least one test at one of these seams before `cfd-implement` is considered done for that story.
- No prior test code exists in this repository yet — these seams are the first testing convention for the project, established here rather than following existing precedent.

## Out of Scope

Everything already marked as a non-goal in [PROJECT.md](./PROJECT.md): persistence/database, authentication/authorization, multi-broker Kafka, exactly-once/transactional semantics, dead-letter topics and retry, Avro/Schema Registry.

Newly excluded at this step:
- Idempotent/deduplicated message processing — consumers do not detect or dedupe reprocessed messages; combined with the log-and-skip error policy ([ADR 0007](./docs/adr/0007-consumer-error-policy.md)), at-least-once delivery anomalies are an accepted risk for this demo.
- Randomizing the payment-failure or backorder outcomes per environment/configuration — both are hardcoded (10% and "Gizmo," respectively), not configurable at runtime.
- Any admin/operator view distinct from the single customer-facing kanban board.

## Further Notes

The full 3-variant UI prototype (including the two variants not chosen) lives on the `prototype/main-screen-ui-variants` branch for reference, per [UI-DECISIONS.md](./UI-DECISIONS.md). The folded-in Variant C code on `main` is prototype-quality (fake in-memory data, no real Kafka/SignalR wiring yet) and is expected to be rewritten test-first during `cfd-implement`, not extended in place.

# ShopTheKafka

A hands-on demo of event-driven microservices on .NET Aspire + Kafka: a full order pipeline built as six small
backend services connected by six Kafka topics, keyed throughout by `orderId`, with a Blazor UI that shows every
order moving through the pipeline live.

There's no persistence layer — Kafka's log is the only durable state. The "database" is an in-memory materialized
view rebuilt by replaying events (see [ADR 0002](./docs/adr/0002-in-memory-materialized-view.md)).

## What it demonstrates

- A **chain of topics**: `OrderPlaced → PaymentApproved → InventoryReserved → OrderShipped`.
- A **branching failure path** modeled as events, not exceptions: a declined payment or an out-of-stock item
  diverts the order down `PaymentFailed` / `InventoryBackorder` instead of the happy path.
- A true **fan-in consumer**: `OrderStatusService` subscribes to all six topics to build one `OrderStatus`
  read-model per order, pushed to the UI over SignalR.
- **Key-based ordering**: every event for a given order is keyed by `orderId`, so Kafka preserves per-order
  ordering within each topic — while `OrderStatusService`'s cross-topic fan-in deliberately does *not* get an
  ordering guarantee, which is a real, visible characteristic of this kind of design (see
  [ADR 0006](./docs/adr/0006-eventual-consistency-in-order-status.md)).

## Services and topics

| Service | Consumes | Produces |
|---|---|---|
| OrderService | — (HTTP `POST /orders`) | `orders-placed` |
| PaymentService | `orders-placed` | `payment-approved`, `payment-failed` |
| InventoryService | `payment-approved` | `inventory-reserved`, `inventory-backorder` |
| ShippingService | `inventory-reserved` | `order-shipped` |
| NotificationService | `payment-failed`, `inventory-backorder`, `order-shipped` | — (logs only) |
| OrderStatusService | all 6 topics | — (HTTP `GET /orders/{id}` + SignalR hub) |

PaymentService, InventoryService, and ShippingService each pause for a few seconds while "processing" a message
(configurable, see [Simulated processing delay](#simulated-processing-delay) below) so an order visibly lingers at
each stage on the dashboard instead of the whole pipeline completing instantly.

Business rules are simulated, not configurable at runtime: payments fail with an independent 10% chance per order;
the item `Gizmo` is permanently out of stock and backorders the whole order (per
[ADR 0004](./docs/adr/0004-all-or-nothing-backorder.md)); shipped orders always get an estimated delivery date of
today + 3 days.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (running) — the Aspire AppHost starts a real
  Kafka broker and Kafka UI in containers, and the integration tests spin up their own Kafka container per suite
  via Testcontainers

No database, no cloud account, and no manual Kafka setup is needed — the AppHost provisions everything.

## Running it

Clone the repo and run the Aspire AppHost from the repo root:

```bash
git clone https://github.com/cfelstead/Kafka-Demo.git
cd Kafka-Demo
dotnet run --project src/ShopTheKafka.AppHost
```

This launches the **Aspire dashboard** (URL printed in the console, e.g. `https://localhost:17026`) with all seven
resources — the Kafka broker, Kafka UI, and the six .NET services. From the dashboard:

- Open the **orderui** resource's endpoint to reach the Blazor kanban board. Place an order from the form at the
  top and watch its card move across the columns (`Placed → Payment approved → Inventory reserved → Shipped`, or
  down one of the failure branches) as the pipeline processes it.
- Open the **Kafka UI** resource's endpoint to inspect topics, partitions, and messages directly.
- Use the dashboard's structured logs/traces view to watch each service's console output (including
  NotificationService's log-only "notifications").

Aspire assigns ports dynamically per run — always use the URLs shown in the dashboard rather than assuming fixed
ports.

### Simulated processing delay

Each of PaymentService, InventoryService, and ShippingService sleeps for `Processing:DelaySeconds` (default `5`)
before handling each message. Override it per service if you want a snappier or slower demo, e.g.:

```bash
dotnet run --project src/ShopTheKafka.PaymentService -- --Processing:DelaySeconds=0
```

(When running the whole graph via the AppHost, this can be set as an environment variable
`Processing__DelaySeconds` on the relevant project resource in `src/ShopTheKafka.AppHost/AppHost.cs`.)

## Running the tests

```bash
dotnet test ShopTheKafka.slnx
```

Tests run at three seams:

- **Per-service, against a real Kafka broker** (Testcontainers) — each of PaymentService, InventoryService,
  ShippingService, NotificationService, and OrderStatusService is tested by producing directly onto its input
  topic(s) and asserting against its output topic(s)/log output/`GET` endpoint, bypassing upstream services.
- **HTTP seam** — OrderService via `POST /orders`.
- **End-to-end smoke tests** — a handful of tests boot the whole Aspire app graph (`Aspire.Hosting.Testing`)
  against a real broker and drive it purely through `POST /orders` and `GET /orders/{id}`, to catch a broken wire
  between services.

Docker must be running for any of the above, since every seam above the pure-unit level uses a real Kafka broker.

## Project docs

- [PROJECT.md](./PROJECT.md) — problem, goals, and non-goals
- [SPEC.md](./SPEC.md) — user stories and implementation decisions
- [SCHEMA.md](./SCHEMA.md) — event and API payload shapes
- [UI-DECISIONS.md](./UI-DECISIONS.md) — kanban board UI rationale
- [CONTEXT.md](./CONTEXT.md) — domain glossary
- [docs/adr/](./docs/adr/) — architecture decision records

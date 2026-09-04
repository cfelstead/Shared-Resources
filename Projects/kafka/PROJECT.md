# ShopTheKafka

## Problem

There's no small, hands-on project that demonstrates the realistic shape of event-driven microservices — a chain of topics, branching failure paths, fan-in consumption, and key-based ordering — using .NET Aspire and Kafka together. This project is that demo: a full order pipeline built as a set of small .NET services orchestrated by Aspire, with a UI to place orders and watch them move through the pipeline live.

## Goals

- Model an order pipeline as 6 backend services connected by 6 Kafka topics, keyed throughout by `orderId` so all Events for one Order stay ordered.
- Demonstrate a chain of topics (order → payment → inventory → shipping), a failure/branch path modeled as Events rather than exceptions, and a true fan-in consumer.
- Provide a Blazor Server UI to place Orders and see their OrderStatus update live via SignalR as Events flow through the pipeline.
- Be developed test-first (TDD): xUnit for unit tests, Testcontainers for integration tests that exercise real Kafka.
- Ship with a developer-focused README that lets someone else clone the repo and run the whole pipeline locally via the Aspire AppHost, including an architecture diagram.

## Non-goals / out of scope

- Persistence/database — Kafka's log is the only durable state; OrderStatus is served from an in-memory materialized view (see [ADR 0002](./docs/adr/0002-in-memory-materialized-view.md)).
- Authentication/authorization.
- Multi-broker/multi-node Kafka cluster — a single broker is enough to demonstrate partitioning and keying.
- Exactly-once semantics / transactional producers.
- Dead-letter topics and retry — a stretch goal.
- Avro + Schema Registry — a stretch goal (see [ADR 0001](./docs/adr/0001-technology-stack.md)).

## Services and topics

| Service | Consumes | Produces |
|---|---|---|
| OrderService | — (HTTP `POST /orders`) | `orders-placed` |
| PaymentService | `orders-placed` | `payment-approved`, `payment-failed` |
| InventoryService | `payment-approved` | `inventory-reserved`, `inventory-backorder` |
| ShippingService | `inventory-reserved` | `order-shipped` |
| NotificationService | `payment-failed`, `inventory-backorder`, `order-shipped` | — |
| OrderStatusService | all 6 topics | — (HTTP `GET /orders/{id}` + SignalR hub) |

## Technology stack

| Layer | Choice | Why |
|---|---|---|
| Backend | .NET 9 | Current .NET target, matches what Aspire 9.x expects. See [ADR 0001](./docs/adr/0001-technology-stack.md). |
| Messaging | Confluent.Kafka + Aspire.Hosting.Kafka + Aspire.Confluent.Kafka | Standard .NET Kafka client with first-class Aspire integration. See [ADR 0001](./docs/adr/0001-technology-stack.md). |
| Payload format | JSON | Keeps focus on topic/partition/consumer-group concepts; avoids a 6th infra dependency. See [ADR 0001](./docs/adr/0001-technology-stack.md). |
| Status/read model | In-memory materialized view (OrderStatusService) | No database needed; state is derived from the Kafka log. See [ADR 0002](./docs/adr/0002-in-memory-materialized-view.md). |
| Frontend | Blazor Server + SignalR | All-.NET stack; live push updates as pipeline Events occur. |
| Testing | xUnit + Testcontainers | .NET-default test framework; Testcontainers runs real Kafka for integration tests, supporting a TDD workflow. |
| Hosting/orchestration | .NET Aspire AppHost (local only) | Runs the Kafka broker and all 6 services together for local development; no cloud deployment target in v1. |

## References

- Domain glossary: [CONTEXT.md](./CONTEXT.md)
- ADRs: [docs/adr/](./docs/adr/)

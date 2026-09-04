# 01 — Aspire scaffold, shared event contracts, and OrderService

**What to build:** A customer can call `POST /orders` with a set of catalog Items and get back a new `orderId`, with the Order recorded as an `OrderPlacedEvent` on Kafka — the first real hop of the pipeline, running inside a full Aspire app (Kafka broker + all 6 services registered as resources, 5 of them still empty stubs). This ticket also establishes the shared event-contract types and the xUnit + Testcontainers pattern every later ticket reuses.

**Blocked by:** None — can start immediately.

**Status:** done

## Schema touchpoints

- `SCHEMA.md`: `OrderPlacedEvent` (all fields) — produced by this ticket.
- `SCHEMA.md`: the shared envelope (`eventId`, `orderId`, `occurredAtUtc`) and the other 5 event type definitions (`PaymentApprovedEvent`, `PaymentFailedEvent`, `InventoryReservedEvent`, `InventoryBackorderEvent`, `OrderShippedEvent`) — defined here in a shared contracts project so every later ticket has them available, even though nothing produces or consumes them yet.

## UI touchpoints

None — this ticket is API-only.

## Testing requirements

- **Seam:** HTTP seam (`SPEC.md` Testing Decisions) — call `POST /orders` against the running service and assert against the real `orders-placed` topic, using a real Kafka broker via Testcontainers. No mocking of Kafka.
- **Coverage bar:** every acceptance criterion below has at least one test at this seam, written before the code that satisfies it.

## Acceptance criteria

- [x] The Aspire AppHost runs a Kafka broker (via `Aspire.Hosting.Kafka`) and registers all 6 services as resources; the app boots cleanly with `dotnet run` on the AppHost
- [x] A shared contracts project defines the Event envelope and all 6 event payload types exactly as described in `SCHEMA.md`, serialized as camelCase JSON
- [x] `POST /orders` accepts `{ customerId, items: [{ itemName, quantity }] }`
- [x] Request validation returns `400 Bad Request` when: `items` is empty; any `quantity` is outside `1..9`; any `itemName` is not one of the 5 catalog items (`Widget`, `Gadget`, `Gizmo`, `Doohickey`, `Thingamajig`); `customerId` is missing or an empty Guid
- [x] On valid input, the service computes `totalAmount` from server-side catalog prices (not client-supplied), publishes `OrderPlacedEvent` to `orders-placed` keyed by the new `orderId`, and returns `201 Created` with the `orderId` in the body and a `Location` header pointing at `GET /orders/{id}`
- [x] A valid `POST /orders` results in exactly one `OrderPlacedEvent` on `orders-placed`, with `items`/`totalAmount` matching the request
- [x] Each invalid-input case above returns `400` and publishes nothing to `orders-placed`

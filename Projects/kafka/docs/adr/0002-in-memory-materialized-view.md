# Back OrderStatus with an in-memory materialized view, not a database

`GET /orders/{id}` needs somewhere to read an Order's current OrderStatus from, but the project's non-goals explicitly exclude persistence. We resolved this by having OrderStatusService consume all 6 topics and hold OrderStatus in an in-memory dictionary keyed by `orderId`, rebuilt purely from the event stream — Kafka's log is the durable source of truth, and the read model is a disposable projection of it. State is lost on restart (until the service re-consumes from the earliest offset), which is an acceptable trade-off for a demo and is itself a useful illustration of "state is derived from the log" rather than a limitation to work around.

## Consequences

- No database, connection strings, or migrations to stand up — keeps the AppHost and README simple.
- On restart, OrderStatusService must consume its assigned topics from the beginning to rebuild state; this is fine at demo volumes but would not scale as-is to production data volumes.
- Durable persistence (e.g. a KTable-style store, or a real database) is a natural stretch goal if this project is ever extended past a demo.

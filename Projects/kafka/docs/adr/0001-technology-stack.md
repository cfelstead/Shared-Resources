# Use .NET 10, Aspire 13.x, Confluent.Kafka, and JSON payloads

ShopTheKafka is a demo of Kafka concepts (topics, partitioning/keying, consumer groups, fan-in/fan-out) inside a .NET Aspire orchestration. We chose **Confluent.Kafka** as the client (the de facto standard .NET Kafka client), **Aspire.Hosting.Kafka** to run a single-broker KRaft-mode Kafka container from the AppHost, and **Aspire.Confluent.Kafka** for client wiring/service discovery — this is the most idiomatic, well-documented path through Aspire's ecosystem. Event payloads are plain **JSON**, not Avro + Schema Registry: JSON keeps the demo's infrastructure footprint small and its logs human-readable, at the cost of not demonstrating schema evolution. Schema Registry is left as an explicit stretch goal rather than in-scope, since it would add a real piece of infrastructure without changing the story the pipeline itself is meant to tell.

**Update (ticket 01, scaffolding):** this ADR originally specified .NET 9 / Aspire 9.x. At implementation time, only .NET 9/10 SDKs were available and the current `Aspire.ProjectTemplates` on NuGet is 13.5.3, which targets `net10.0` and uses Aspire's newer AppHost format (`AppHost.cs`, CLI-bundle execution). The project was scaffolded against .NET 10 / Aspire 13.5.3 instead — the underlying rationale (Confluent.Kafka, Aspire.Hosting.Kafka, JSON payloads) is unchanged, only the version numbers.

## Considered Options

- Avro + Confluent Schema Registry for payloads — rejected for v1 to avoid a 6th infrastructure dependency and keep focus on topic/partition/consumer-group concepts.
- A different Kafka client (e.g. a raw `librdkafka` wrapper, or KafkaFlow) — rejected in favor of Confluent.Kafka, which has first-class Aspire integration support.

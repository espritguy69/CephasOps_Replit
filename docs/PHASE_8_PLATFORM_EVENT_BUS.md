# Phase 8: Production-Grade Platform Event Bus

## Architecture overview

CephasOps Phase 8 upgrades the event-driven foundation into a **platform event system** with:

- **Canonical platform event envelope** – consistent metadata (EventId, CorrelationId, CausationId, ParentEventId, RootEventId, PartitionKey, SourceService, etc.) across publish and persistence.
- **Event partitioning** – partition key derivation (CompanyId → EntityId → CorrelationId → EventId) and partition-aware claim ordering so events in the same partition are processed in order while different partitions run in parallel.
- **Adaptive backpressure** – dispatcher reduces batch size and adds delay when pending/failed/dead-letter counts or oldest-pending age exceed thresholds (Reduced → Throttled → Paused).
- **Replay pipelines** – existing replay by date range, event type, correlation, company, entity; safety window and dry-run; replay operations and audit.
- **Event correlation trees** – lineage by RootEventId, ParentEventId, CorrelationId; APIs to reconstruct trees for debugging.
- **Parent/child event model** – DomainEvent has ParentEventId and RootEventId; `EventLineageHelper.SetLineageFrom` for handlers that emit child events.
- **Cross-service observability** – structured logs, metrics (counts, latency, backpressure level), and tracing hooks (TraceId/SpanId on envelope).

Domain events remain **domain-first**; infrastructure concerns are carried in the envelope and store, not in entity types.

---

## Event envelope model

- **IDomainEvent** – domain contract (EventId, EventType, Version, OccurredAtUtc, CorrelationId, CompanyId, CausationId, TriggeredByUserId, Source).
- **IHasParentEvent** – optional ParentEventId (child events).
- **IHasRootEvent** – optional RootEventId (origin of causality chain).
- **EventStoreEnvelopeMetadata** – optional metadata passed when appending: PartitionKey, RootEventId, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority.
- **EventStoreEntry** – persistence: all envelope fields plus Status, RetryCount, lease fields, LastError, etc.
- **IPlatformEventEnvelopeBuilder** – builds metadata from IDomainEvent (partition key, root, source, trace) for consistent publish.

Envelopes are **version-tolerant** and **serializable**; new optional fields can be added without breaking existing producers or consumers.

---

## Partitioning strategy

- **Default partition key** (DefaultPartitionKeyResolver): CompanyId (tenant) → EntityId (aggregate) → CorrelationId (workflow) → EventId (no sharing).
- **Claim ordering**: `ORDER BY PartitionKey NULLS LAST, CreatedAtUtc, EventId` so per-partition order is preserved.
- **Concurrency**: different partitions are claimed and processed in parallel (FOR UPDATE SKIP LOCKED).
- **When to partition by**: tenant (CompanyId) for multi-company isolation; by aggregate (EntityId) for per-order or per-job ordering; by CorrelationId for workflow ordering. Events without CompanyId/EntityId still get a key (CorrelationId or EventId) so the store stays consistent.

---

## Backpressure model

- **Signals**: pending count, failed count, dead-letter count, oldest pending event age (from EventBusMetricsSnapshot, updated every 30s).
- **Levels**: None → Reduced (batch/2, +500ms) → Throttled (batch/4, +2s) → Paused (batch=0, +10s). Thresholds configurable via `EventBus:Backpressure`.
- **Recovery**: when counts/age drop below thresholds, suggested batch and delay return to normal automatically.
- **Protection**: avoids retry/replay storms by backing off when the queue is overloaded or failing heavily.
- **Observability**: `eventbus.backpressure.level` gauge (0–3); dispatcher logs when Paused.

---

## Replay architecture

- **Scopes**: by date range, event type, company, entity, correlation; from failed/dead-letter via requeue or bulk reset.
- **Safety**: replay safety window (e.g. 5 min) excludes events newer than cutoff; dry-run preview; idempotency via IEventProcessingLogStore.
- **Replay metadata**: ReplayId column on EventStore for future use; replay operations and ReplayOperationEvent record execution and audit.
- **Safeguards**: max events per run, resume/checkpoint, cancellation; handlers should be idempotent where possible.

---

## Correlation tree model

- **RootEventId** – origin of the full causality chain; set on child events via EventLineageHelper or IHasRootEvent.
- **ParentEventId** – immediate predecessor; set when publishing a child from a handler.
- **CausationId** – event or command that caused this event.
- **CorrelationId** – groups a flow (e.g. workflow run).
- **Reconstruction**: IEventLineageService.GetTreeByEventIdAsync / GetTreeByRootEventIdAsync / GetTreeByCorrelationIdAsync return EventLineageTreeDto (nodes with EventId, EventType, Status, ParentEventId, etc.).

---

## Parent/child event semantics

- When a handler publishes a **child event**, set lineage before publish:  
  `EventLineageHelper.SetLineageFrom(childEvent, causingEvent)`  
  so ParentEventId, RootEventId, CorrelationId, and CausationId are correct.
- Child events are stored with the same envelope metadata (partition, source, etc.) via IPlatformEventEnvelopeBuilder.
- Lineage stays intact across async and replay as long as child events are created with SetLineageFrom.

---

## Observability model

- **Logs**: publish/dispatch/consume/retry/fail/dead-letter with EventId, EventType, CorrelationId, CompanyId, ParentEventId; backpressure Paused warning.
- **Metrics**: eventbus.events.* (persisted, dispatched, succeeded, failed, dead_lettered, etc.); eventbus.pending_count, failed_count, dead_letter_count, oldest_pending_event_age_seconds; eventbus.dispatcher.parallel_workers; eventbus.backpressure.level.
- **Tracing**: TraceId/SpanId on envelope when Activity.Current is set; optional enrichment in logs.
- **Diagnostics**: GET /api/event-store/events/{id}, attempt-history, related-links, dashboard; GET events/{id}/lineage, lineage/by-root/{id}, lineage/by-correlation/{id}.

---

## Operational usage

- **Inspect failures**: event-store/events/dead-letter, event-store/events/{id}, attempt-history, related-links.
- **Trace lineage**: event-store/events/{id}/lineage or lineage/by-correlation/{correlationId}.
- **Replay**: preview then execute via replay APIs; respect safety window and dry-run.
- **Backpressure**: monitor eventbus.backpressure.level and pending/oldest_pending; tune EventBus:Backpressure thresholds if needed.
- **Partitioning**: ensure high-volume flows use consistent CompanyId or EntityId so partition key is stable and ordering is preserved where required.

---

## Limitations / future evolution

- **Scale**: single EventStore table; for very high throughput, consider sharding or an external bus (Kafka, RabbitMQ) with the same envelope contract.
- **ReplayId**: column is in place; full stamping of replayed events (e.g. when re-dispatching) can be added so replayed vs original can be distinguished in the store.
- **Priority**: Priority column and DefaultPriority option exist; dispatcher does not yet sort by priority; can be added for priority-aware claim.
- **IdempotencyKey**: stored; deduplication at append can be implemented when required.

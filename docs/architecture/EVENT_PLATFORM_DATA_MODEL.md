# Event Platform — Data Model

**Date:** Event Platform Layer phase.  
**Purpose:** Document persistence used by the event platform: existing tables, relationships, retention, indexing, and operational impact.  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md, EVENT_PLATFORM_CURRENT_STATE_AUDIT.md.

---

## 1. Scope

This phase **does not add new tables**. The event platform uses existing schema:

- **EventStore** (internal outbox)
- **EventProcessingLog** (handler idempotency)
- **EventStoreAttemptHistory** (optional attempt audit)
- **OutboundIntegrationDeliveries** (outbound delivery records)
- **OutboundIntegrationAttempts** (per-attempt history)
- **InboundWebhookReceipts** (inbound receipt store)
- **ExternalIdempotencyRecords** (inbound idempotency)
- **ConnectorDefinitions**, **ConnectorEndpoints** (connector abstraction)
- **ReplayOperations**, **ReplayOperationEvents** (replay audit)

Migrations that introduced these are under the official migration path (EF Core Designer + snapshot). No undocumented script-only migrations were added in this phase.

---

## 2. Internal event outbox: EventStore

| Column | Type | Purpose |
|--------|------|---------|
| EventId | uuid | PK. |
| EventType | varchar | Handler routing. |
| Payload | jsonb | Serialized event (no secrets). |
| OccurredAtUtc, CreatedAtUtc | timestamptz | Timestamps. |
| Status | varchar | Pending, Processing, Processed, Failed, DeadLetter. |
| ProcessedAtUtc | timestamptz? | When processed. |
| RetryCount | int | Attempt count. |
| NextRetryAtUtc | timestamptz? | Retry backoff. |
| CorrelationId, CompanyId, TriggeredByUserId, Source | — | Tracing and scope. |
| EntityType, EntityId | — | Entity context. |
| ParentEventId, CausationId | uuid? | Lineage. |
| LastError, LastErrorAtUtc, LastHandler | — | Failure observability. |
| ProcessingNodeId, ProcessingLeaseExpiresAtUtc, LastClaimedAtUtc, LastClaimedBy | — | Dispatcher ownership (Phase 7). |
| RootEventId, PartitionKey, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority, PayloadVersion | — | Platform envelope (Phase 8). |

**Indexes:** Status (for claim), (CompanyId, EventType, OccurredAtUtc) for queries, CorrelationId, NextRetryAtUtc for retry. Partition/lease indexes as in Phase 7/8 migrations.

**Retention:** Automated by EventPlatformRetentionWorkerHostedService. Processed and DeadLetter rows older than EventStoreProcessedAndDeadLetterDays (default 90) are deleted in batches. See backend/scripts/EVENT_PLATFORM_RUNBOOK.md §8.

**Growth:** Events per workflow transition and per job; monitor table size and consider partitioning by date if needed later.

---

## 3. Handler idempotency: EventProcessingLog

| Column | Purpose |
|--------|---------|
| EventId, HandlerName | Unique (EventId, Handler) for at-most-once. |
| ProcessedAtUtc | When handler completed. |

**Retention:** Can be purged after ProcessedAtUtc is beyond audit window; keep long enough for replay idempotency.

---

## 4. Outbound integration: OutboundIntegrationDeliveries, OutboundIntegrationAttempts

**OutboundIntegrationDeliveries:** Id, ConnectorEndpointId, CompanyId, SourceEventId, EventType, CorrelationId, RootEventId, IdempotencyKey, Status, PayloadJson, AttemptCount, MaxAttempts, NextRetryAtUtc, DeliveredAtUtc, LastErrorMessage, LastHttpStatusCode, IsReplay, ReplayOperationId, CreatedAtUtc, UpdatedAtUtc.

**OutboundIntegrationAttempts:** Per-attempt record (delivery id, timestamp, HTTP status, error).

**Indexes:** IdempotencyKey (unique), Status, ConnectorEndpointId, CompanyId, EventType, CreatedAtUtc for list/filter and replay.

**Retention:** Delivered rows older than OutboundDeliveredDays (default 60) are deleted by the retention worker; attempts are cascade-deleted. Failed/DeadLetter are never deleted by retention.

---

## 5. Inbound: InboundWebhookReceipts, ExternalIdempotencyRecords

**InboundWebhookReceipts:** Id, ConnectorEndpointId, ConnectorKey, CompanyId, ExternalIdempotencyKey, ExternalEventId, MessageType, Status, PayloadJson, VerificationPassed, ReceivedAtUtc, ProcessedAtUtc, HandlerErrorMessage, HandlerAttemptCount, etc.

**ExternalIdempotencyRecords:** ConnectorKey, ExternalIdempotencyKey, completion state. Unique (ConnectorKey, ExternalIdempotencyKey).

**Indexes:** Unique on (ConnectorKey, ExternalIdempotencyKey) for InboundWebhookReceipts; status and date for queries.

**Retention:** Processed receipts older than InboundProcessedDays (default 90) are deleted by the retention worker. ExternalIdempotencyRecords with CompletedAtUtc older than ExternalIdempotencyCompletedDays (default 7) are deleted. HandlerFailed receipts are never deleted by retention.

---

## 6. Connector abstraction

**ConnectorDefinitions:** ConnectorKey, DisplayName, ConnectorType, Direction.  
**ConnectorEndpoints:** EndpointUrl, AllowedEventTypes, RetryCount, IsPaused, IsActive, etc.  
No schema change in this phase.

---

## 7. Replay audit

**ReplayOperations, ReplayOperationEvents:** Replay run metadata and event lists. Existing migrations. No change in this phase.

---

## 8. Migration governance

- All event-platform-related tables were introduced in prior migrations (EventStore, Phase 4/7/8, AddExternalIntegrationBus, etc.).
- This phase adds **no new migrations**. Code and docs only.
- Future changes (e.g. retention job, new indexes) should use the official migration path and Designer where applicable.

---

## 9. Operational maintenance

- **EventStore:** Monitor Pending/Failed/DeadLetter counts; run stuck-processing reset if needed; consider archiving Processed events by date.
- **OutboundIntegrationDeliveries:** Monitor Failed/DeadLetter; use replay API for retries; consider archiving Delivered by date.
- **InboundWebhookReceipts:** Monitor HandlerFailed; optional receipt replay; consider archiving Processed by date.
- **Indexes:** Ensure indexes on Status, date, and correlation columns for list/filter and replay queries.

This document describes the existing data model used by the event platform; no new tables or migrations were added in this phase.

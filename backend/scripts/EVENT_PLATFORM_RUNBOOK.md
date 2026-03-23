# Event Platform — Operational Runbook

**Date:** Event Platform Layer phase.  
**Purpose:** How to operate and troubleshoot the CephasOps event platform (internal events, outbound delivery, inbound receipts).  
**Audience:** Support, DevOps, and developers.

---

## 1. End-to-end event flow (summary)

1. **Domain events** are raised from application code (e.g. WorkflowEngineService after a transition). They are persisted to **EventStore** in the same transaction as the business change (when using AppendInCurrentTransaction).
2. **EventStoreDispatcherHostedService** (background worker) polls for Pending (and due-retry Failed) events, claims a batch, deserializes, and dispatches to **IDomainEventHandler&lt;T&gt;** in-process. Handlers may create tasks, send notifications, write to ledger, or forward to the **outbound integration bus**.
3. **Outbound integration:** When a handler (or application code) calls **IOutboundIntegrationBus.PublishAsync(PlatformEventEnvelope)**, the bus creates one **OutboundIntegrationDelivery** per connector endpoint, maps payload, and performs HTTP POST. Success → Delivered; failure → Failed with NextRetryAtUtc; after max attempts → DeadLetter. **OutboundIntegrationRetryWorkerHostedService** periodically loads Pending or Failed (due for retry) deliveries and calls DispatchDeliveryAsync so they are retried without manual replay. Configure via `OutboundIntegrationRetryWorker:Enabled`, `PollingIntervalSeconds`, `MaxDeliveriesPerPoll`.
4. **Inbound webhooks:** POST to `/api/integration/webhooks/{connectorKey}`. Request is verified, receipt stored, idempotency claimed, then a handler runs. Duplicate requests (same idempotency key) return 200 with idempotencyReused and do not re-run the handler.

---

## 2. Inspecting failed deliveries (outbound)

- **List failed/DeadLetter:** Use operator API (e.g. GET `/api/integration/outbound/deliveries?status=Failed` or `status=DeadLetter`). Filter by ConnectorEndpointId, CompanyId, EventType, date range.
- **Detail:** GET `/api/integration/outbound/deliveries/{id}` for LastErrorMessage, LastHttpStatusCode, AttemptCount, NextRetryAtUtc.
- **Automatic retry:** The **OutboundIntegrationRetryWorkerHostedService** runs on a timer (default 60s), loads up to MaxDeliveriesPerPoll (default 20) Pending or Failed deliveries with NextRetryAtUtc ≤ now, and dispatches each via IOutboundIntegrationBus.DispatchDeliveryAsync. No manual action needed for Failed deliveries that are due for retry.
- **Manual replay:** POST `/api/integration/outbound/replay` with body: ConnectorEndpointId (optional), CompanyId (optional), EventType (optional), Status = "Failed" or "DeadLetter", FromUtc, ToUtc, MaxCount. This re-dispatches matching deliveries. Use for DeadLetter or when you want to force retry immediately. Endpoints must be idempotent.
- **Logs:** Search logs for delivery Id, SourceEventId, or CorrelationId. No secrets in logs.

---

## 3. Replaying events (domain EventStore)

- **By event ID:** Use replay service/API to re-dispatch specific EventIds. Handlers run again; they must be idempotent (e.g. EventProcessingLog gives at-most-once per EventId + Handler).
- **Bulk reset Failed/DeadLetter to Pending:** Use bulk APIs (e.g. BulkResetDeadLetterToPendingAsync, BulkResetFailedToPendingAsync) with EventStoreBulkFilter (CompanyId, EventType, FromUtc, ToUtc, MaxCount). Then the dispatcher will pick them up on the next poll.
- **Stuck Processing:** If the worker crashed, events may stay in Processing. Use ResetStuckProcessingAsync (or bulk) with a timeout so they move to Failed and get NextRetryAtUtc = now; then they can be re-claimed.
- **Projection-only replay:** When replay target is Projection, only IProjectionEventHandler run; side-effect handlers are skipped. Use for rebuilding projections without duplicate side effects.

---

## 4. Inspecting inbound receipts

- **List receipts:** GET `/api/integration/inbound/receipts`. Filter by connectorKey, company, status, date.
- **Detail:** GET `/api/integration/inbound/receipts/{id}` for Status, VerificationPassed, HandlerErrorMessage, ProcessedAtUtc.
- **VerificationFailed:** Receipt was rejected before handler (e.g. bad signature). Fix verifier or sender; no handler replay.
- **HandlerFailed:** Handler threw. Idempotency key is already claimed; duplicate HTTP retries from sender will get 200 idempotencyReused. To re-run the handler (e.g. after a fix), call **POST `/api/integration/inbound/receipts/{id}/replay`** (JobsAdmin). Only receipts in HandlerFailed status can be replayed. The handler runs again with the stored payload; it must be idempotent by business key.

---

## 5. Troubleshooting idempotency

- **Outbound:** Duplicate PublishAsync with same (EventId, EndpointId) does not create a second delivery; idempotency key is out-{eventId}-{endpointId}. If you see duplicate deliveries, check that EventId is unique per occurrence.
- **Inbound:** Duplicate request with same ExternalIdempotencyKey returns 200 and does not run the handler again. If the handler was never run (e.g. crash before marking completed), the key may remain “claimed”; document recovery (e.g. manual completion or reset) if needed.
- **Domain handlers:** EventProcessingLog ensures at-most-once per (EventId, Handler). If a handler is not idempotent by business key, duplicate delivery (e.g. on replay) can still cause duplicate side effects; fix the handler to be idempotent by business key.

---

## 6. What is safe vs unsafe

- **Safe:** Replaying Failed/DeadLetter outbound deliveries (endpoints idempotent). Replaying HandlerFailed inbound receipts (handler idempotent). Replaying domain events with idempotent handlers. Resetting stuck Processing to Failed. Listing and filtering events/deliveries/receipts.
- **Unsafe:** Re-running “all” events or “all” deliveries without scope (always use filters and MaxCount). Changing payload or event type on replay without contract versioning. Disabling idempotency checks. Exposing secrets in logs or payloads.

---

## 7. Dashboards and logs

- **Logs:** EventStore dispatcher (claim, dispatch, MarkProcessed), OutboundIntegrationBus (delivery created, delivered, failed), InboundWebhookRuntime (received, verified, processed, handler failed). Use CorrelationId, EventId, delivery Id, receipt Id for trace.
- **Metrics (optional):** Event type counts, delivery success/failure rates, receipt verification/handler failure rates. Add if needed.
- **APIs:** Use existing operator APIs for events, deliveries, receipts; no single “event platform dashboard” required in this phase.

---

## 8. Retention and archival (automated)

Retention is automated by **EventPlatformRetentionWorkerHostedService** (runs on interval, default 24h). Config section: **EventPlatformRetention**. Only completed/successful rows older than the retention window are deleted; Pending/Failed/DeadLetter/HandlerFailed are never deleted.

### 8.1 Tables and eligibility

| Table | What is deleted | Cutoff | Default |
|-------|------------------|--------|---------|
| **EventStore** | Processed or DeadLetter only | ProcessedAtUtc or CreatedAtUtc &lt; cutoff | 90 days |
| **EventProcessingLog** | Completed only | CompletedAtUtc &lt; cutoff | 90 days |
| **OutboundIntegrationDeliveries** | Delivered only (attempts cascade) | DeliveredAtUtc &lt; cutoff | 60 days |
| **InboundWebhookReceipts** | Processed only | ProcessedAtUtc &lt; cutoff | 90 days |
| **ExternalIdempotencyRecords** | Completed only | CompletedAtUtc &lt; cutoff | 7 days |

### 8.2 Config

EventPlatformRetention: **Enabled**, **RunIntervalSeconds** (default 86400), **EventStoreProcessedAndDeadLetterDays** (90), **EventProcessingLogCompletedDays** (90), **OutboundDeliveredDays** (60), **InboundProcessedDays** (90), **ExternalIdempotencyCompletedDays** (7), **MaxDeletesPerTablePerRun** (1000). Set a category to 0 to skip it.

### 8.3 Behavior

Order: EventProcessingLog → EventStore → OutboundIntegrationDeliveries → InboundWebhookReceipts → ExternalIdempotencyRecords. Each table: select up to MaxDeletesPerTablePerRun eligible IDs, then delete. Logs per table and a summary (TotalDeleted, errors). Idempotent and restart-safe. Manual run: resolve IEventPlatformRetentionService and call RunRetentionAsync().

- **Policy:** Define retention windows in configuration or runbook (e.g. “EventStore Processed: 90 days; Outbound Delivered: 60 days”). Run archival during low traffic; use transactions and batch deletes to avoid long locks.

---

## 9. References

- Architecture: docs/architecture/EVENT_PLATFORM_ARCHITECTURE_DECISION.md
- Current state: docs/architecture/EVENT_PLATFORM_CURRENT_STATE_AUDIT.md
- Replay: docs/architecture/EVENT_REPLAY_MODEL.md
- Observability: docs/architecture/EVENT_OBSERVABILITY_MODEL.md
- Phase 10: docs/PHASE_10_EXTERNAL_INTEGRATION_BUS.md

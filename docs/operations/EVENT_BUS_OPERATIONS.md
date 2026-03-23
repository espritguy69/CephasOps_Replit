# Event Bus — Operations Guide

**Purpose:** Operational observability, monitoring, and reliability for the Event Bus (Phase 5). Distributed dispatcher (Phase 6) supports horizontal scaling with Postgres row locking. Phase 7 adds lease ownership, attempt history, poison classification, bulk actions, and backpressure.  
**Related:** [EVENT_BUS_PHASE4_PRODUCTION.md](../EVENT_BUS_PHASE4_PRODUCTION.md), [EVENT_STORE_STUCK_PROCESSING.md](EVENT_STORE_STUCK_PROCESSING.md).

---

## 1. Distributed Dispatcher Architecture (Phase 6 & 7)

Multiple CephasOps application nodes can run against the same EventStore (PostgreSQL). The dispatcher is safe for horizontal scaling.

- **Row locking:** The repository claims events with `SELECT ... FOR UPDATE SKIP LOCKED` (and sets `Status = 'Processing'` in the same transaction). Each node claims a disjoint subset of rows; no event is processed by more than one node.
- **Claim criteria:** Rows with `Status = 'Pending'` and (`NextRetryAtUtc` IS NULL or `NextRetryAtUtc` ≤ now), or `Status = 'Failed'` with due retry. Ordered by `CreatedAtUtc`, limited by **MaxEventsPerPoll** / **BatchSize**.
- **Parallel workers:** Within a node, each batch is processed in parallel. Concurrency is capped by **MaxConcurrentDispatchers** (default 8). A **SemaphoreSlim** limits how many events are dispatched at once per batch.
- **Flow per cycle:** Reset stuck Processing (respecting lease expiry) → claim batch (FOR UPDATE SKIP LOCKED; stamp **NodeId** and **ProcessingLeaseExpiresAtUtc** when configured) → process batch → mark processed (clear lease on completion) → record attempt history and metrics → delay (idle/busy). Then repeat.
- **Throughput:** Increasing **MaxEventsPerPoll**, **MaxConcurrentDispatchers**, and running more instances improves throughput. Use **DispatcherBusyDelayMs** / **DispatcherIdleDelayMs** for backpressure.

---

## 2. Event lifecycle

| Status       | Meaning |
|-------------|--------|
| **Pending** | Persisted; not yet claimed by the dispatcher. |
| **Processing** | Claimed by the dispatcher; dispatch in progress. |
| **Processed** | All in-process handlers completed; event complete. |
| **Failed** | At least one handler failed; `NextRetryAtUtc` set; will be re-claimed when due. |
| **DeadLetter** | `RetryCount >= MaxRetriesBeforeDeadLetter` or non-retryable (poison) failure; no further automatic retries. |
| **Cancelled** | Pending event cancelled by operator (bulk cancel; incident control). |

---

## 2a. Distributed Operations (Phase 7)

- **Lease / ownership:** When **NodeId** and **ProcessingLeaseSeconds** are set, each claim stamps **ProcessingNodeId**, **ProcessingLeaseExpiresAtUtc**, **LastClaimedAtUtc**, **LastClaimedBy** on the row. When processing finishes (success or failure), lease fields are cleared.
- **Stuck recovery:** Stuck recovery considers **lease expiry** first: events with `ProcessingLeaseExpiresAtUtc < now` are recovered even if **ProcessingStartedAtUtc** is within the timeout. Then events with **ProcessingStartedAtUtc** (or **CreatedAtUtc**) older than **StuckProcessingTimeoutMinutes** are reset to Failed.
- **Visibility:** List and detail APIs return **ProcessingNodeId**, **ProcessingLeaseExpiresAtUtc**, **LastErrorType**, **LastClaimedAtUtc** so operators can see which node owns an event and when the lease expires.

---

## 3. Retry behavior and poison classification (Phase 7)

- **Backoff (Phase 6):** Exponential-style delays: 1 → +1 min, 2 → +5 min, 3 → +15 min, 4 → +60 min, 5 → dead-letter (configurable via `EventBus:Dispatcher:RetryDelaySeconds`: 60, 300, 900, 3600).
- **NextRetryAtUtc:** Set when marking as Failed; dispatcher only re-claims when `NextRetryAtUtc <= now` and `RetryCount < MaxRetriesBeforeDeadLetter`.
- **Poison (non-retryable):** Failures classified as non-retryable (e.g. validation, deserialization, missing entity, unsupported version) go **directly to DeadLetter** with **LastErrorType** set. They do not use the retry schedule. Retryable failures (transient, timeout, lock) use the normal retry schedule.
- **Dead-letter:** After 5 retryable failures (default), or immediately for poison, status is set to DeadLetter; no further automatic retries.

---

## 4. Dead-letter handling

- **Inspection:** Use `GET /api/events/dead-letter` (or `GET /api/event-store/events?status=DeadLetter`) with optional filters: `eventType`, `companyId`, `fromUtc`, `toUtc`, `retryCountMin`, `retryCountMax`, `page`, `pageSize`.
- **Replay (requeue):** Use `POST /api/events/{eventId}/replay` to move a DeadLetter event back to **Pending** so the dispatcher can retry it. RetryCount is unchanged. Only allowed for DeadLetter; idempotency guard prevents duplicate side effects when handlers run again.
- **Full re-dispatch:** Use `POST /api/event-store/events/{eventId}/retry` for immediate in-process re-dispatch (no status change; runs handlers once).

---

## 5. Stuck processing recovery

- The dispatcher runs **ResetStuckProcessingAsync** at the start of each poll cycle.
- Events in **Processing** with **expired lease** (`ProcessingLeaseExpiresAtUtc < now`) or older than **StuckProcessingTimeoutMinutes** (default 15) are set to **Failed** with **NextRetryAtUtc = now**, so they are re-claimed. Lease fields are cleared. Safe against duplicate processing (idempotency guard).
- See [EVENT_STORE_STUCK_PROCESSING.md](EVENT_STORE_STUCK_PROCESSING.md) for details and manual steps.

---

## 6. Metrics

Structured event processing metrics (System.Diagnostics.Metrics; export via OpenTelemetry or Prometheus if needed):

**Counters:**

- `eventbus.events.persisted` — Events persisted to store  
- `eventbus.events.dispatched` — Events dispatched from store  
- `eventbus.events.succeeded` — Events processed successfully  
- `eventbus.events.failed` — Events failed (will retry or dead-letter)  
- `eventbus.events.retried` — Events scheduled for retry  
- `eventbus.events.dead_lettered` — Events moved to dead-letter  
- `eventbus.events.recovered_from_stuck` — Events recovered from stuck Processing  
- `eventbus.dispatcher.claimed` — Events claimed per batch (Phase 7)  
- `eventbus.events.non_retryable_failed` — Events failed as poison (Phase 7)  
- `eventbus.events.bulk_replayed` — Events replayed via bulk action (Phase 7)  
- `eventbus.events.bulk_cancelled` — Events cancelled via bulk action (Phase 7)  

**Gauges (updated periodically by EventBusMetricsCollectorHostedService or dispatcher):**

- `eventbus.dispatcher.parallel_workers` — Number of events currently being processed in the current batch (Phase 6).  
- `eventbus.dispatcher.inflight` — Same as parallel_workers (Phase 7).  
- `eventbus.pending_count` — Current pending event count  
- `eventbus.failed_count` — Current failed event count  
- `eventbus.dead_letter_count` — Current dead-letter event count  
- `eventbus.oldest_pending_event_age_seconds` — Age in seconds of oldest pending event  

**Histograms:**

- `eventbus.event.processing_latency_seconds` — Time from CreatedAtUtc to ProcessedAtUtc  
- `eventbus.event.attempt_duration_seconds` — Duration of a single dispatch attempt (Phase 7)  

Labels: `event_type`, `company_id`, `handler_name` (when applicable).

---

## 7. Monitoring

- **Event lag:** When oldest pending event age exceeds **OldestPendingEventAgeWarningMinutes** (default 30), a warning is logged. Gauges expose `oldest_pending_event_age_seconds` for dashboards.
- **Health:** `GET /health` runs **EventBusHealthCheck** (Phase 7 extended):
  - **Healthy:** Dispatcher running; no expired leases; pending and dead-letter within thresholds; oldest pending age within warning.
  - **Degraded:** Expired leases present; or pending above `PendingCountDegradedThreshold` (default 5000); or dead-letter above `DeadLetterDegradedThreshold` (default 100); or oldest pending event age above `OldestPendingEventAgeWarningMinutes`.
  - **Unhealthy:** Dispatcher not running or dead-letter above `DeadLetterUnhealthyThreshold` (default 500).

---

## 8. Replay procedures

**Requeue dead-letter (safe for dispatcher to retry):**

1. List dead-letter events: `GET /api/events/dead-letter`.
2. Optionally inspect: `GET /api/event-store/events/{eventId}` for full detail and payload.
3. Requeue: `POST /api/events/{eventId}/replay`. Event moves to Pending; dispatcher will pick it up; idempotency guard prevents duplicate handler side effects.

**Immediate retry (in-process):**

- `POST /api/event-store/events/{eventId}/retry` — Re-dispatches the event through current handlers once (Failed or DeadLetter). Does not change status to Pending.

**Bulk actions (Phase 7; JobsAdmin; support dry-run and filters):**

- `POST /api/events/bulk/replay-dead-letter` — Requeue dead-letter events matching filter to Pending. Query params: `dryRun`, `companyId`, `eventType`, `fromUtc`, `toUtc`, `retryCountMin`, `retryCountMax`, `maxCount` (default 1000).
- `POST /api/events/bulk/replay-failed` — Requeue failed events (due for retry) matching filter to Pending. Same query params.
- `POST /api/events/bulk/reset-stuck` — Reset stuck Processing events matching filter to Failed. Same query params.
- `POST /api/events/bulk/cancel-pending` — Cancel pending events matching filter (Status = Cancelled). Same query params. Use for incident control.

All bulk actions return `{ success, countAffected, errorMessage, dryRun }`. Use `dryRun=true` to see how many would be affected.

---

## 8a. Runbooks (Phase 7)

**Inspect backlog:** Use `GET /api/events/pending` and `GET /api/event-store/dashboard`. Check **oldest_pending_event_age_seconds** in metrics. List items now include **ProcessingNodeId**, **ProcessingLeaseExpiresAtUtc**, **LastErrorType**.

**Replay dead-letter safely:** Use `GET /api/events/dead-letter` with filters; inspect events; run `POST /api/events/bulk/replay-dead-letter?dryRun=true` to see count; then run without `dryRun` or replay single events with `POST /api/events/{eventId}/replay`.

**Identify poison events:** In dead-letter or failed lists, check **LastErrorType** (e.g. Validation, Deserialization, MissingEntity). Poison events moved directly to DeadLetter without retries. Use `GET /api/event-store/events/{eventId}/attempt-history` to see attempt history.

**Handle stuck leases:** If health reports expired leases, the dispatcher’s stuck recovery will reclaim them on the next cycle. To force reset by filter: `POST /api/events/bulk/reset-stuck` with optional filters (or rely on automatic recovery). Ensure **StuckProcessingTimeoutMinutes** and **ProcessingLeaseSeconds** are set appropriately.

**Scale dispatcher nodes:** Set **NodeId** (e.g. hostname or instance id) and **ProcessingLeaseSeconds** (e.g. 300) per node. Each node will stamp leases on claimed events; if a node crashes, events become recoverable after lease expiry or stuck timeout. Run multiple instances; they will claim disjoint sets via SKIP LOCKED.

---

## 9. API summary

| Method | Path | Purpose |
|--------|------|--------|
| GET | /api/events/dead-letter | List dead-letter events (filters: eventType, companyId, fromUtc, toUtc, retryCountMin, retryCountMax) |
| GET | /api/events/failed | List failed events (same filters) |
| GET | /api/events/pending | List pending events (same filters) |
| POST | /api/events/{eventId}/replay | Requeue DeadLetter → Pending (JobsAdmin) |
| GET | /api/event-store/events | List events with status filter |
| GET | /api/event-store/events/{eventId} | Event detail (including payload) |
| POST | /api/event-store/events/{eventId}/retry | Immediate retry (re-dispatch) |
| GET | /api/event-store/events/{eventId}/attempt-history | Execution attempt history for event (Phase 7) |
| POST | /api/events/bulk/replay-dead-letter | Bulk requeue dead-letter by filter (Phase 7) |
| POST | /api/events/bulk/replay-failed | Bulk requeue failed (due retry) by filter (Phase 7) |
| POST | /api/events/bulk/reset-stuck | Bulk reset stuck Processing by filter (Phase 7) |
| POST | /api/events/bulk/cancel-pending | Bulk cancel pending by filter (Phase 7) |
| GET | /health | Health (includes Event Bus) |

---

## 10. Configuration (EventBus:Dispatcher)

| Key | Default | Description |
|-----|---------|-------------|
| NodeId | (none) | Identifier for this dispatcher node (e.g. hostname). When set, claims stamp lease ownership (Phase 7). |
| ProcessingLeaseSeconds | 300 | Lease duration in seconds; after this, event can be recovered by another node (Phase 7). |
| PollingIntervalSeconds | 15 | Seconds between dispatcher poll cycles. |
| MaxEventsPerPoll | (BatchSize) | Max events to claim per cycle (Phase 7; alias for BatchSize). |
| MaxConcurrentDispatchers | 8 | Max concurrent event processors per batch (parallel workers). |
| BatchSize | 20 | Max events to claim per cycle (capped 1–100). |
| DispatcherIdleDelayMs | 0 | Delay in ms when no work found; 0 = use PollingIntervalSeconds (Phase 7). |
| DispatcherBusyDelayMs | 0 | Delay in ms when work was processed (backpressure); 0 = use PollingIntervalSeconds (Phase 7). |
| MaxRetriesBeforeDeadLetter | 5 | After this many failures, status set to DeadLetter. |
| RetryDelaySeconds | [60,300,900,3600] | Delay in seconds per retry (1→+1min, 2→+5min, 3→+15min, 4→+60min, 5→dead-letter). |
| StuckProcessingTimeoutMinutes | 15 | Events in Processing (or with expired lease) older than this are reset to Failed. |
| OldestPendingEventAgeWarningMinutes | 30 | Log warning and health Degraded when oldest pending event is older than this (0 = disabled). |
| PendingCountDegradedThreshold | 5000 | Health: pending above this → Degraded. |
| DeadLetterDegradedThreshold | 100 | Health: dead-letter above this → Degraded. |
| DeadLetterUnhealthyThreshold | 500 | Health: dead-letter above this → Unhealthy. |

---

## 11. Architecture flow (Phase 7)

```
claim (FOR UPDATE SKIP LOCKED) → stamp NodeId + lease expiry
  → dispatch attempt → success → clear lease, write attempt history (Success), metrics
  → retryable failure → schedule retry, clear lease, write attempt history (Retry), metrics
  → non-retryable failure → DeadLetter, clear lease, write attempt history (DeadLetter), metrics
  → stuck/expired → reset to Failed, write attempt history (RecoveredFromStuck), metrics
  → health uses: dispatcher running, pending/dead-letter thresholds, expired leases count, oldest pending age
```

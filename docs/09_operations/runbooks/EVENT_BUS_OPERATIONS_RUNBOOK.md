# Event Bus Operations Runbook

## How to inspect event failures

1. **List failed events**  
   `GET /api/event-store/events/failed?page=1&pageSize=50`  
   Returns events with Status = Failed (will retry).

2. **List dead-letter events**  
   `GET /api/event-store/events/dead-letter?page=1&pageSize=50`  
   Returns events that exceeded max retries or were marked non-retryable.

3. **Event detail**  
   `GET /api/event-store/events/{eventId}`  
   Payload, Status, LastError, LastHandler, LastErrorType, RetryCount, NextRetryAtUtc, CorrelationId, RootEventId, PartitionKey, etc.

4. **Attempt history**  
   `GET /api/event-store/events/{eventId}/attempt-history`  
   Per-attempt status, handler, duration, error type/message, retry/dead-letter flags.

5. **Related links**  
   `GET /api/event-store/events/{eventId}/related-links`  
   JobRuns and WorkflowJobs with same EventId or CorrelationId.

6. **Dashboard**  
   `GET /api/event-store/dashboard?fromUtc=&toUtc=`  
   Counts (processed, failed, dead-letter), percentages, top failing event types and companies.

---

## How to investigate lineage

1. **By event id**  
   `GET /api/event-store/events/{eventId}/lineage`  
   Returns the correlation tree for that event (same root); nodes include EventId, EventType, Status, ParentEventId, CausationId, PartitionKey, ReplayId.

2. **By root event id**  
   `GET /api/event-store/lineage/by-root/{rootEventId}?maxNodes=500`  
   All events with that RootEventId (or the root event itself).

3. **By correlation id**  
   `GET /api/event-store/lineage/by-correlation/{correlationId}?maxNodes=500`  
   All events in that workflow/flow.

Use these to see what caused an event and what events it caused (children/siblings).

---

## How to replay safely

1. **Preview (dry-run)**  
   Use the replay preview API with DryRun so no handlers run; check TotalMatched, EligibleCount, BlockedReasons, SafetyCutoffOccurredAtUtc.

2. **Safety window**  
   Events with OccurredAtUtc newer than the safety cutoff (e.g. 5 min) are excluded to avoid overlap with live traffic.

3. **Execute**  
   Run replay with the desired filters (company, event type, date range, correlation, etc.) and MaxEvents cap.

4. **Idempotency**  
   Handlers that are not idempotent may duplicate side effects on replay; prefer idempotent handlers or restrict replay to projection-only targets where applicable.

5. **Dead-letter requeue**  
   Single event: `POST /api/event-store/events/{eventId}/requeue-dead-letter`.  
   Bulk: use bulk reset APIs with filter; then dispatcher will pick them up.

---

## How backpressure behaves

- **None**: normal batch size and polling delay.
- **Reduced**: pending or failed over threshold → batch size halved, +500 ms delay (configurable).
- **Throttled**: higher pending or dead-letter unhealthy → batch size quartered, +2 s delay.
- **Paused**: pending above pause threshold → no claim (batch=0), +10 s delay; dispatcher logs "backpressure Paused".

Recovery is automatic when metrics drop below thresholds (metrics snapshot updated every 30 s).  
To tune: adjust `EventBus:Backpressure` (ReducedPendingThreshold, ThrottledPendingThreshold, PausedPendingThreshold, *DelayMs, OldestPendingAgeThrottleSeconds).

---

## What metrics to monitor

- **eventbus.pending_count** – backlog size.
- **eventbus.oldest_pending_event_age_seconds** – lag.
- **eventbus.failed_count**, **eventbus.dead_letter_count** – failure state.
- **eventbus.backpressure.level** – 0=None, 1=Reduced, 2=Throttled, 3=Paused.
- **eventbus.events.succeeded** / **eventbus.events.failed** (counters) – throughput and failures.
- **eventbus.event.processing_latency_seconds** – latency from CreatedAtUtc to ProcessedAtUtc.

---

## How to handle dead-letter events

1. Inspect: GET event detail and attempt-history; fix bug or data if possible.
2. Retry single: `POST /api/event-store/events/{eventId}/retry` (re-dispatch once).
3. Requeue: `POST /api/event-store/events/{eventId}/requeue-dead-letter` to set Status back to Pending so the dispatcher picks it up again.
4. Bulk reset: use bulk reset dead-letter API with CompanyId/EventType/date filters and MaxCount.
5. If the event type is permanently invalid, leave in dead-letter and alert; consider excluding that type from replay.

---

## Common failure scenarios

| Scenario | What to check | Action |
|----------|----------------|--------|
| High pending, growing | Throughput vs. intake; handler duration | Scale dispatchers or optimize handlers; check backpressure. |
| Many dead-letters | LastError, LastErrorType, EventType | Fix handler or data; requeue after fix. |
| Stuck Processing | ProcessingLeaseExpiresAtUtc, StuckProcessingTimeoutMinutes | Dispatcher resets stuck to Failed; ensure lease is long enough for slow handlers. |
| Deserialization failures | EventType, payload version | Register new event type or fix payload; event marked non-retryable. |
| Wrong partition ordering | PartitionKey, CompanyId, EntityId | Ensure partition key is set (envelope builder); same key for same flow. |

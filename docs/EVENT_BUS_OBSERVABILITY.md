# Event Bus Observability

**Purpose:** Bounded admin-grade diagnostics for the Event Bus so operators and engineers can inspect recent events, handler execution outcomes, failed handlers, replay-origin processing, and event/handler state. This improves incident debugging and operational trust without altering event processing semantics.

---

## 1. What It Shows

The observability surface exposes:

- **Recent events** — List of events from the event store with filters (already existed; see Event Bus Monitor).
- **Recent handler processing** — Rows from `EventProcessingLog`: which handler ran for which event, state (Processing / Completed / Failed), timings, attempt count, error, replay operation ID, correlation ID.
- **Failed handler-processing** — Same list filtered to `State = Failed` so you can quickly see which handlers failed and why.
- **Event detail with processing** — For a given event: core event metadata plus all related `EventProcessingLog` rows (handler name, state, started/completed time, attempt count, error, replay/correlation context).

Replay-origin processing is identifiable by a non-null **ReplayOperationId** on the processing log row. Correlation is shown where the event or handler flow stored it (**CorrelationId** on event store and/or processing log).

---

## 2. Data Sources

- **Event store** (`EventStore` table / `EventStoreEntry`) — Event list and event detail (event ID, type, occurred/created time, status, entity type/id, correlation ID, company, last error/handler, payload).
- **EventProcessingLog** — Handler-level processing state: EventId, HandlerName, State, StartedAtUtc, CompletedAtUtc, AttemptCount, Error, ReplayOperationId, CorrelationId. No separate “metrics” store; we only use existing persisted data.

We do **not** invent metrics or fake fields. All list/detail shapes use only fields present in the real domain and persistence models.

---

## 3. API Endpoints (Admin)

All under `api/event-store`, same auth as existing event-store endpoints: **Jobs** policy, **JobsView** permission. Bounded and paginated.

| Endpoint | Description |
|----------|-------------|
| `GET api/event-store/observability/processing` | Recent handler processing. Query: `page`, `pageSize` (max 100), `failedOnly`, `eventId`, `replayOperationId`, `correlationId`. Returns `{ items, total, page, pageSize }`. |
| `GET api/event-store/events/{eventId}/observability/processing` | Processing log rows for a single event. 404 if event not found or out of scope. |
| `GET api/event-store/observability/events/{eventId}` | Event detail plus related processing log rows. 404 if event not found or out of scope. |

Company scope is applied for non–global admins: processing list is restricted to events belonging to the user’s company (via join to event store); event detail and processing-by-event respect the same scope.

---

## 4. How to Inspect Failed Handlers

1. **Handler processing tab** — In Admin → Event Bus Monitor, open the **Handler processing** tab. Turn on **Failed only** and click **Apply filters**. The table shows EventId, HandlerName, State, Started/Completed (UTC), AttemptCount, Error, ReplayOperationId, CorrelationId.
2. **Event detail** — Click an event (from Recent events or from a processing row). The detail drawer includes a **Handler processing** section with all handlers that ran for that event; failed rows show State = Failed and the Error message.
3. **Filters** — Optionally filter by Event ID, Replay operation ID, or Correlation ID to narrow to a specific incident or replay run.

---

## 5. How Replay-Related Processing Appears

- When processing is part of a **replay** (operational replay, resume, or rerun-failed), the handler row has **ReplayOperationId** set. In the UI this is shown as “Replay op.” (truncated ID); you can copy or use it to correlate with the Replay Operations list.
- Rows with **State = Completed** and a **ReplayOperationId** indicate the handler was run as part of a replay and completed successfully.
- Rows with **State = Failed** and a **ReplayOperationId** indicate the handler was run as part of a replay and failed; **Error** and **AttemptCount** help with diagnosis.
- **Skipped** handlers (already completed for that event) do not create a new row; the idempotency guard prevents a second run. So you will not see a “Skipped” state in the log—you see only rows where the handler was actually invoked (Processing/Completed/Failed).

---

## 6. UI Location

- **Admin → Event Bus** (Event Bus Monitor page).
- New tab: **Handler processing** — table of recent processing log rows, failed-only toggle, filters (event ID, replay operation ID, correlation ID), pagination.
- **Event detail drawer** — When you open an event (from Recent events, Failed, Dead-letter, or by clicking a row in Handler processing), the drawer shows a **Handler processing** section with all processing log rows for that event, including replay and correlation when present.

---

## 7. Limitations

- **No full distributed trace** — We do not implement or expose a full trace tree across services. Correlation ID and replay operation ID are used where already stored.
- **Correlation only where available** — CorrelationId is shown when the event or handler flow recorded it; not every path sets it.
- **No log viewer** — Observability is based on durable event store and EventProcessingLog data only. Application logs are not aggregated here.
- **Bounded queries** — List endpoints are paginated with a maximum page size (e.g. 100). No unbounded “all events” or “all processing” export.
- **Company scope** — Non–global admins see only events (and thus processing) for their company.

---

## 8. What We Do Not Change

- Replay semantics, idempotency guard, and event processing logic are unchanged.
- No new event types or handler contracts.
- No exposure of raw debug-only data to non-admin users; same admin/Jobs permission as existing event-store endpoints.

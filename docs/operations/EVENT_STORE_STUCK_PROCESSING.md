# Event Store — Stuck “Processing” Events

**Purpose:** How stuck **Processing** events are handled and how to find or recover them if needed.

---

## 1. Automatic recovery (Phase 4 follow-up)

- The **EventStoreDispatcherHostedService** runs **ResetStuckProcessingAsync** at the start of each poll cycle.
- Events in **Processing** longer than **StuckProcessingTimeoutMinutes** (default 15) are set to **Failed** with **NextRetryAtUtc = now**, so they are re-claimed on the next cycle. Safe against duplicate processing (idempotency guard); retry and dead-letter semantics are preserved.
- Config: `EventBus:Dispatcher:StuckProcessingTimeoutMinutes` (see **docs/EVENT_BUS_PHASE4_PRODUCTION.md**).
- Structured logs record each reset (EventId, EventType, CompanyId, CorrelationId, RetryCount, age).

---

## 2. What “Processing” means

- When the dispatcher claims an event, it sets **Processing** and **ProcessingStartedAtUtc** in the event store.
- When all in-process handlers (and, if applicable, the async job) finish, it marks the event **Processed** or **Failed**.
- If the process crashes or is killed after claiming but before marking done, the event remains in **Processing** until the next dispatcher loop runs the stuck recovery (see above) or manual steps below.

---

## 3. Finding stuck events

- **API:** Use the event store list endpoint with `status=Processing`:
  - `GET /api/event-store/events?status=Processing&page=1&pageSize=100`
- **Optional filters:** Add `companyId`, `eventType`, or `toUtc` to narrow results. Events that have been in Processing for a long time (e.g. `toUtc` set to “now minus 15 minutes”) are good candidates for “stuck.”
- **Database:** Query `EventStore` where `Status = 'Processing'` and `ProcessingStartedAtUtc` (or `CreatedAtUtc` for legacy rows) is older than your threshold (e.g. 15–30 minutes).

---

## 4. Manual recovery (if needed)

- **Retry:** Use the retry endpoint for the event: `POST /api/event-store/events/{eventId}/retry`. This re-dispatches the event (in-process only; single-event retry does not enqueue async handlers again). If the handler succeeds, the event will be marked Processed (or Failed if it fails again).
- **Mark as Failed (manual):** There is no dedicated “mark as Failed” API today. To mark an event as Failed without retrying (e.g. to move it out of Processing so it can be handled by a “failed events” workflow or dead-letter process), use one of:
  - A future admin tool or endpoint that sets `Status = 'Failed'` and optionally increments `RetryCount`.
  - Direct DB update (with care and backups) for the specific `EventId`.
- **DeadLetter:** After `RetryCount` reaches the configured max (e.g. 5), the next failure will set status to **DeadLetter**. Automatic recovery resets stuck Processing to Failed so they re-enter the retry flow; no manual step required unless you need to force DeadLetter or abandon.

---

## 5. Operational recommendation

- Rely on **automatic recovery** (dispatcher reset). Ensure `StuckProcessingTimeoutMinutes` is appropriate for your environment (default 15).
- If you need to inspect or manually act: list events in **Processing** via the API; for specific events use the retry endpoint or (with care) a controlled DB update.

---

## 6. Related

- **docs/EVENT_BUS_PHASE4_PRODUCTION.md** — Stuck recovery, configuration.
- **docs/operations/EVENT_BUS_OPERATIONS.md** — Event lifecycle, metrics, health, replay procedures (Phase 5).
- **docs/SYSTEM_HARDENING_AUDIT.md** §1.6 (stuck Processing events), §7 Improvement #8.
- **EventStoreController:** List events (`status=Processing`), Retry endpoint.
- **EventsController:** `GET /api/events/pending`, `failed`, `dead-letter`; `POST /api/events/{eventId}/replay` (requeue dead-letter).
- **IEventStore.ResetStuckProcessingAsync** — Called by EventStoreDispatcherHostedService each loop.
- **Metrics:** `eventbus.events.recovered_from_stuck` counter incremented when stuck events are reset.

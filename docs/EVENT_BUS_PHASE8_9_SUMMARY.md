# Event Bus Phase 8–9 Summary

This document describes the async event subscriber architecture, the Event Bus Admin UI (monitor), operational workflows for retry and replay, and security/permissions.

## Async subscriber architecture (Phase 8)

### Overview

- **In-process handlers**: Any `IDomainEventHandler<TEvent>` that does *not* implement `IAsyncEventSubscriber<TEvent>` runs synchronously during `PublishAsync` / `DispatchToHandlersAsync`. EventStore is updated (e.g. `MarkProcessedAsync`) in the same flow.
- **Async handlers**: Handlers that implement `IAsyncEventSubscriber<TEvent>` are *not* run in-process when `IAsyncEventEnqueuer` is registered. Instead, the dispatcher enqueues a single background job (job type `EventHandlingAsync`) with payload `eventId`, `correlationId`, `companyId`. The event remains in **Processing** until that job runs and calls `MarkProcessedAsync`.

### Flow

1. **Publish**
   - Event is appended to the Event Store and marked **Processing**.
   - Dispatcher resolves all `IDomainEventHandler<TEvent>` and splits them into:
     - **In-process**: handlers that do *not* implement `IAsyncEventSubscriber<TEvent>`.
     - **Async**: handlers that implement `IAsyncEventSubscriber<TEvent>`.
   - In-process handlers run immediately; JobRun records are created when `IJobRunRecorderForEvents` is present.
   - If there is at least one async handler and `IAsyncEventEnqueuer` is present:
     - `EnqueueAsync(eventId, domainEvent)` is called once.
     - The method returns **without** calling `MarkProcessedAsync` (event stays Processing).
   - Otherwise (no async handlers or no enqueuer):
     - `MarkProcessedAsync` is called with success/failure and last handler name.

2. **Async job execution**
   - `BackgroundJobProcessorService` processes jobs with type `EventHandlingAsync`.
   - It loads the event from the store, deserializes it via `IEventTypeRegistry`, resolves handlers that implement `IAsyncEventSubscriber<TEvent>`, and runs each via reflection.
   - For each async handler it uses `IJobRunRecorderForEvents`: `StartHandlerRunAsync` creates a JobRun with **EventId**, **CorrelationId**, and handler name; on success `CompleteHandlerRunAsync`, on failure `FailHandlerRunAsync`.
   - After all async handlers run, it calls `IEventStore.MarkProcessedAsync(eventId, success, errorMessage, lastHandlerName)` so the event moves to **Processed** or **Failed** (and eventually **DeadLetter** after max retries).

### Idempotency and retries

- Async handlers **must be idempotent**: the same event may be retried or replayed, so handling the same event more than once should be safe.
- Retries are handled by the background job infrastructure (e.g. job retry count and dead-letter). The Event Store’s own retry count and DeadLetter transition are driven by `MarkProcessedAsync(success: false)` from the async job.

### Correlation and observability

- **CorrelationId** is carried on the event and in the async job payload, and is stored on **JobRun** when `StartHandlerRunAsync` is used (in-process and async paths).
- **EventId** is stored on **JobRun** so event ↔ job run traceability is available in the Event Bus Monitor and Background Jobs UI.

---

## Event Bus Admin UI (Phase 9)

### Purpose

The Event Bus Monitor provides operational visibility and control over domain events: overview metrics, recent/failed/dead-letter lists, event detail (payload, errors, handler history, correlation), and actions (Retry, Replay).

### Location and access

- **Route**: `/admin/event-bus`
- **Permission**: Same as Background Jobs — `jobs.view` to see the monitor; `jobs.admin` to perform Retry/Replay.
- **Company scoping**: Non–global admins see only events for their company; API uses `ScopeCompanyId()`.

### Sections

1. **Overview**
   - Dashboard metrics: events today, processed %, failed %, dead-letter count, total retry count.
   - Top failing event types and top failing companies (by failed + dead-letter counts).

2. **Recent events**
   - Paginated table with filters: date range (fromUtc / toUtc), event type, status, company, correlation ID, entity type, entity ID.
   - Columns: Event type, Status, Occurred (UTC), Company, Correlation, Retries, Last handler, Entity type, Entity ID.

3. **Failed events**
   - Table of events with status **Failed** (Retry action available for admins).

4. **Dead-letter events**
   - Table of events with status **DeadLetter** (Retry action available for admins).

### Event detail drawer

- **Payload**: JSON payload of the event.
- **Last error / Last error at (UTC)**: From Event Store.
- **Last handler**: Last handler name that ran (in-process or async).
- **Correlation links (Phase 10)**:
  - **Related Job runs**: JobRuns with same **EventId** or same **CorrelationId**, with link to “Background Jobs” for full traceability.
  - **Related Workflow jobs**: WorkflowJobs with same **CorrelationId** (entity type, entity id, state, created).
- **Actions** (when permitted):
  - **Retry event**: Re-dispatch the event to current handlers (in-process and/or enqueue async again).
  - **Replay event**: Allowed only when the event type is allowed by replay policy (e.g. `WorkflowTransitionCompleted`); same dispatch semantics as Retry but intended for “replay” workflows.

### API used

- List: `GET /api/event-store/events` (with query filters).
- Failed: `GET /api/event-store/events/failed`.
- Dead-letter: `GET /api/event-store/events/dead-letter`.
- Detail: `GET /api/event-store/events/{eventId}`.
- Related links: `GET /api/event-store/events/{eventId}/related-links`.
- Dashboard: `GET /api/event-store/dashboard`.
- Retry: `POST /api/event-store/events/{eventId}/retry`.
- Replay: `POST /api/event-store/events/{eventId}/replay`.
- Replay policy: `GET /api/event-store/replay-policy/{eventType}`.

---

## Operational workflow: Retry and Replay

### Retry

- **Use when**: An event is **Failed** or **DeadLetter** and you want to run handlers again (e.g. after fixing a bug or dependency).
- **Effect**: The event is re-dispatched to all current handlers (in-process run immediately; async handlers enqueued again). Event Store processing state is updated when in-process completes and when the async job completes.
- **Permission**: `jobs.admin`.

### Replay

- **Use when**: You want to “replay” an event that is allowed by policy (e.g. for reprocessing or testing). Not all event types are replayable; the UI only shows Replay when the event type is allowed.
- **Effect**: Same as Retry from a dispatch perspective; replay policy only controls which event types are allowed to be replayed.
- **Permission**: `jobs.admin`.

### Best practices

- Prefer **Retry** for failed/dead-letter recovery.
- Use **Replay** only for event types explicitly allowed by policy and when you understand side effects (handlers must be idempotent).

---

## Observability links (Phase 10)

- From an event’s detail, **Related links** show:
  - **Job runs**: Same **EventId** or same **CorrelationId**, so you can see the exact job runs that processed this event (including async handler runs).
  - **Workflow jobs**: Same **CorrelationId**, so you can trace from event to workflow execution.
- This gives full traceability: **Request → Workflow → Event → JobRun** when all layers propagate CorrelationId and EventId.

---

## Security and permissions

| Action / area           | Permission   | Notes                                      |
|-------------------------|-------------|--------------------------------------------|
| View Event Bus Monitor  | `jobs.view` | Same as Background Jobs list/detail        |
| Retry / Replay event    | `jobs.admin`| Required for retry and replay endpoints    |
| Company scope           | Enforced    | Non–global admins see only their company   |

- All event-store endpoints are under the same “Jobs” policy and company scoping as the rest of the jobs/event infrastructure.
- No workflow engine semantics or transition logic are changed by Event Bus Phases 8–9; only event dispatch, async enqueue, and UI/APIs are extended.

---

## Deploy Event Bus Monitor, run with real traffic, and observe

### 2. Deploy Event Bus Monitor

The Event Bus Monitor is part of the existing app: no separate service.

**Backend**

- Deploy the API as you normally do (e.g. same host/container as today).
- Ensure the **AddJobRunEventId** migration is applied in the target database (`dotnet ef database update` or your idempotent migration script).
- No extra config: event-store and dashboard use the same DB and `IEventStore` / `IEventStoreQueryService`.

**Frontend**

- Build and deploy the frontend (e.g. `npm run build` and deploy the `dist/` output).
- The Event Bus Monitor is at **`/admin/event-bus`**. It appears in the sidebar for users with `jobs.view` (same as Background Jobs).
- Ensure the deployed app’s API base URL and auth are correct so `/api/event-store/*` calls work.

**Checklist**

- [ ] Backend deployed with latest migrations applied.
- [ ] Frontend deployed and routing to backend.
- [ ] User with `jobs.view` can open `/admin/event-bus` and see the Overview tab (dashboard may show zeros until events flow).

---

### 3. Let the system run with real traffic

- Use the environment as you normally would (staging or production).
- Trigger workflows and other features that publish domain events (e.g. order status changes, workflow transitions).
- Keep the **Background Job Processor** running so async event handlers (and other jobs) are processed.
- No code or config change is required for “running with real traffic”; the Event Bus and monitor are already wired in.

---

### 4. Observe event volume and failure pattern

Use the Event Bus Monitor to watch behaviour over time.

**Where to look**

| Area | What to observe |
|------|------------------|
| **Overview** | **Events today** – total volume. **Processed %** vs **Failed %** – health. **Dead-letter count** – events that gave up after retries. **Total retries** – retry pressure. **Top failing event types** – which events fail most. **Top failing companies** – which tenants have the most failures. |
| **Recent events** | Use filters (date range, event type, status, company, correlation ID, entity) to inspect specific flows. Check **Status** (Processed / Processing / Failed / DeadLetter) and **Last handler** to see where work is done or stuck. |
| **Failed** | Events in **Failed** status. Open an event to see **Last error**, **Last error at (UTC)**, **Payload**, and **Related links** (Job runs, Workflow jobs). Use **Retry** after fixing issues if needed. |
| **Dead-letter** | Events that have exhausted retries. Same detail and **Retry** as Failed; investigate root cause before retrying. |

**Operational habits**

- Review **Overview** daily (or on a schedule) to spot rising failure % or dead-letter count.
- If **Failed %** or **Dead-letter** grows: use **Top failing event types** and **Top failing companies** to target investigation; use **Recent events** / **Failed** and event detail (error, payload, related Job runs) to find root cause.
- Use **Correlation ID** and **Related links** to trace from an event to the specific Job run(s) and workflow job(s) (and from there to Background Jobs or workflow UIs).
- After fixing a bug or dependency, use **Retry** on failed/dead-letter events as needed; use **Replay** only for allowed event types and when side effects are understood.

**Correlation tracing**

- Open an event in the monitor and use the **Related (same correlation)** section: **Job runs** (same EventId or CorrelationId) and **Workflow jobs** (same CorrelationId). This gives EventStore entry → CorrelationId → JobRun and WorkflowJob. Use the Background Jobs link to inspect JobRun details; use entity type/entity id to find the workflow or order in the app.

**Optional**

- Export or log dashboard metrics (e.g. events today, processed %, failed %, dead-letter count) for trending (e.g. in a spreadsheet or monitoring tool).
- Set alerts (outside the app) on failure rate or dead-letter count if you have a monitoring pipeline.

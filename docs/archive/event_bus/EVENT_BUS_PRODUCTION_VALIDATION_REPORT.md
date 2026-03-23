# Event Bus Monitor — Production Validation Report

**Date:** 2026-03-09  
**Scope:** CephasOps Event Bus, Event Store, replay engine, observability links, and Event Bus Monitor UI.  
**Goal:** Confirm production readiness and operational traceability. No architecture changes were made.

---

## Step 1 — Deployment readiness

| Check | Status | Details |
|-------|--------|---------|
| Migration `AddJobRunEventId` exists and runs after `AddJobRunsTable` | **Pass** | Migration `20260309120000_AddJobRunEventId` runs after `20260309100000_AddJobRunsTable` and `20260309110100_AddJobRunsParentJobRunIdIndex`. Comment in code: "Runs after AddJobRunsTable." |
| `JobRuns.EventId` column and `IX_JobRuns_EventId` index | **Pass** | Migration `Up()` adds column `EventId` (uuid, nullable) and index `IX_JobRuns_EventId` with filter `WHERE "EventId" IS NOT NULL`. Idempotent (only if table exists and column/index missing). |
| EventStore schema (correlation and processing fields) | **Pass** | `EventStoreEntry` and `EventStoreEntryConfiguration`: table `EventStore`; `CorrelationId`, `Status`, `ProcessedAtUtc`, `RetryCount`, `LastError`, `LastErrorAtUtc`, `LastHandler`; indexes on Status, CorrelationId, OccurredAtUtc, (CompanyId, EventType, OccurredAtUtc). |
| API builds | **Pass** | `dotnet build` for CephasOps.Api succeeds with 0 errors, 0 warnings. |
| EventStoreController endpoints reachable | **Pass** | Controller at `[Route("api/event-store")]`, `[Authorize(Policy = "Jobs")]`. Endpoints: `GET events`, `GET events/failed`, `GET events/dead-letter`, `GET events/{eventId}`, `GET events/{eventId}/related-links`, `GET dashboard`, `POST events/{eventId}/retry`, `POST events/{eventId}/replay`, `GET replay-policy/{eventType}`. All use `RequirePermission(JobsView)` or `JobsAdmin` as documented. |

**Step 1 verdict:** Deployment readiness confirmed. Migration order correct; schema and API are in place.

---

## Step 2 — Event Bus Monitor (frontend)

| Check | Status | Details |
|-------|--------|---------|
| Route `/admin/event-bus` exists | **Pass** | `App.tsx`: `<Route path="/admin/event-bus" element={<SettingsProtectedRoute><EventBusMonitorPage /></SettingsProtectedRoute>} />`. |
| `jobs.view` can access monitor | **Pass** | `EventBusMonitorPage` uses `canViewJobs` (SuperAdmin or `jobs.view` or Admin with no permissions). Page renders; without permission shows "You do not have permission to view the event bus." |
| Tabs: Overview, Recent events, Failed, Dead-letter | **Pass** | `tabs` array: `overview`, `recent`, `failed`, `deadletter` with labels "Overview", "Recent events", "Failed", "Dead-letter". Tab content renders per `tab === 'overview'` etc. |
| Dashboard metrics from `/api/event-store/dashboard` | **Pass** | `getEventStoreDashboard(fromUtc, toUtc)` calls `apiClient.get('/event-store/dashboard', { params })`. Overview tab uses `loadDashboard()` and displays `dashboard.eventsToday`, `processedPercent`, `failedPercent`, `deadLetterCount`, `totalRetryCount`, top failing types/companies. |

**Step 2 verdict:** Event Bus Monitor is wired: route, permission, tabs, and dashboard API are correct.

---

## Step 3 — Event generation under real traffic

Events are **generated** (new EventStore entries) only when `IDomainEventDispatcher.PublishAsync` is called. That happens in one place:

- **Workflow transitions:** `WorkflowEngineService.ExecuteTransitionAsync` builds `WorkflowTransitionCompletedEvent` and calls `_domainEventDispatcher.PublishAsync(evt, cancellationToken)` after a successful transition.

Workflow transitions are triggered by:

- **API / UI:** Order status changes, invoice submission, agent actions, etc., via `IWorkflowEngineService.ExecuteTransitionAsync`.
- **Scheduler:** `SchedulerService` uses `IWorkflowEngineService` in multiple job paths (e.g. lines 581, 757, 919, 1068, 1185); scheduled tasks can trigger transitions and thus events.
- **Background jobs:** Any job that calls `IWorkflowEngineService.ExecuteTransitionAsync` (e.g. email ingestion, order processing) will cause events when transitions run.
- **Async event subscribers:** Do not create new events; they consume events already stored (dispatcher enqueues `EventHandlingAsync` job; processor loads event from store and runs async handlers). So events appear in the monitor when workflow transitions run (from UI, API, scheduler, or other jobs).

**Step 3 verdict:** Event flow is correct. With real traffic (workflow transitions from any trigger), events will appear in the Event Bus Monitor. No simulation was run; code paths are verified.

---

## Step 4 — Observability chain

Traceability: **Event → EventStore entry → CorrelationId → JobRun / WorkflowJob**.

| Link | Implementation |
|------|----------------|
| Event → EventStore | Every published event is appended via `IEventStore.AppendAsync`; entry has `EventId`, `CorrelationId`, `Status`, etc. |
| CorrelationId | Set on `WorkflowTransitionCompletedEvent` (and stored in EventStore); propagated to async job payload and to `JobRun` via `JobRunRecorderForEvents.StartHandlerRunAsync` (and in-process handler runs). |
| Event → JobRun | `JobRun.EventId` set in `StartJobRunDto` by `JobRunRecorderForEvents`; `GetRelatedLinksAsync` returns JobRuns where `EventId == eventId` OR `CorrelationId == entry.CorrelationId`. |
| Event → WorkflowJob | WorkflowJob has `CorrelationId`; `GetRelatedLinksAsync` returns WorkflowJobs where `CorrelationId == entry.CorrelationId`. |

**API:** `GET /api/event-store/events/{eventId}/related-links` returns `EventStoreRelatedLinksDto` with `JobRuns` and `WorkflowJobs` (company-scoped). Frontend event detail drawer loads this and shows "Related (same correlation)" with Job runs table and Workflow jobs table.

**Step 4 verdict:** Observability chain is implemented; Event → EventStore → CorrelationId → JobRun and → WorkflowJob is traceable via event detail and related-links.

---

## Step 5 — Failure patterns and dashboard metrics

Dashboard (Overview tab) shows:

- **Events today** — count of events (from EventStore) with `OccurredAtUtc` in the selected day.
- **Processed % / Failed %** — from `ProcessedCount`, `FailedCount`, `DeadLetterCount` vs `EventsToday`.
- **Dead-letter count** — events in status DeadLetter (today’s filter).
- **Total retry count** — sum of `RetryCount` for events in range.
- **Top failing event types** — event types with status Failed or DeadLetter, grouped and ordered by count (top 10).
- **Top failing companies** — companies with failed/dead-letter counts (top 10).

Abnormal patterns to watch (as in runbook): rising Failed % or Dead-letter count; one event type or company dominating failures. Investigation: use Failed/Dead-letter tabs, event detail (Last error, Payload, Related links), and Background Jobs for JobRun details.

**Step 5 verdict:** Dashboard metrics and failure breakdown are implemented and documented; no live metrics were inspected (no real traffic run in this validation).

---

## Step 6 — Operational controls (Retry / Replay)

| Check | Status | Details |
|-------|--------|---------|
| Retry: `POST /api/event-store/events/{id}/retry` | **Pass** | `EventStoreController.Retry(eventId)` calls `_replayService.RetryAsync(eventId, ScopeCompanyId(), _currentUser.UserId)`. `RequirePermission(JobsAdmin)`. |
| Replay: `POST /api/event-store/events/{id}/replay` | **Pass** | `Replay(eventId)` calls `_replayService.ReplayAsync(...)`. Same permission. |
| Replay policy enforced | **Pass** | `ReplayAsync` uses `checkPolicy: true`; `DispatchStoredEventAsync` calls `_replayPolicy.IsReplayAllowed(entry.EventType)` and returns `BlockedReason` when not allowed. `EventReplayPolicy` allows only `WorkflowTransitionCompleted`. |
| Correlation preserved | **Pass** | Retry/Replay do not create a new event; they load stored event and call `_dispatcher.DispatchToHandlersAsync(domainEvent)`. Deserialized event keeps original `CorrelationId`; handlers and JobRuns receive it. |
| JobRun created for handler execution | **Pass** | `DispatchToHandlersAsync` runs in-process handlers with `IJobRunRecorderForEvents.StartHandlerRunAsync` and enqueues async handlers; when the async job runs, `ProcessEventHandlingAsyncJobAsync` uses `jobRunRecorder.StartHandlerRunAsync` per async handler. So each handler run (in-process or async) gets a JobRun with EventId and CorrelationId. |

**Step 6 verdict:** Retry and Replay are implemented, replay policy is enforced, correlation is preserved, and JobRuns are created for handler execution.

---

## Step 7 — Operational runbook verification

**Document:** `docs/EVENT_BUS_PHASE8_9_SUMMARY.md`.

| Topic | Covered | Notes |
|-------|---------|--------|
| Deployment | Yes | Backend (deploy API, apply AddJobRunEventId migration); frontend (build, deploy, `/admin/event-bus`, API base URL and auth); checklist. |
| Monitoring workflow | Yes | Sections "Observe event volume and failure pattern" and "Where to look" (Overview, Recent, Failed, Dead-letter); operational habits (review Overview, use top failing types/companies, use Correlation ID and Related links). |
| Retry procedure | Yes | "Operational workflow: Retry and Replay" — when to use Retry, effect (re-dispatch), permission; "Failed" / "Dead-letter" — open event, use Retry after fixing. |
| Replay procedure | Yes | When to use Replay, policy (allowed types only), permission; UI shows Replay only when replay allowed. |
| Root cause investigation | Yes | "Operational habits" and "Failed"/"Dead-letter": use Last error, Payload, Related links (Job runs, Workflow jobs), top failing types/companies; trace via Correlation ID to JobRun and WorkflowJob. |

**Step 7 verdict:** Runbook correctly describes deployment, monitoring, retry, replay, and root cause investigation.

---

## Step 8 — Readiness summary

| Area | Status | Notes |
|------|--------|--------|
| **Migration state** | Ready | AddJobRunEventId runs after AddJobRunsTable; adds EventId column and IX_JobRuns_EventId; idempotent. |
| **Monitor functionality** | Ready | Route, permission, tabs, dashboard, filters, event detail, related links, Retry/Replay actions. |
| **Event throughput** | N/A | Not measured; events are produced by workflow transitions (API, scheduler, background jobs). |
| **Failure rate** | N/A | No live run; dashboard and tabs are in place to observe. |
| **Retry / Replay verification** | Ready | APIs and policy verified; correlation and JobRun creation confirmed. |
| **Operational readiness** | Ready | Runbook covers deploy, monitor, retry, replay, and RCA. |

---

## Conclusion

- **Production readiness:** Confirmed for the Event Bus, Event Store, replay engine, observability links, and Event Bus Monitor. Migration order, schema, API, frontend, and runbook are aligned.
- **Operational traceability:** Event → EventStore → CorrelationId → JobRun and → WorkflowJob is implemented and exposed via event detail and `GET /api/event-store/events/{id}/related-links`.
- **Recommendation:** Deploy per runbook, run under real traffic, and use the Event Bus Monitor (Overview, Failed, Dead-letter, event detail, Related links) to observe event volume and failure patterns and to perform retries and replays as needed.

No critical issues were found; no architecture changes were made.

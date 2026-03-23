# Event Bus — Production Rollout & Stabilization Summary

**Date:** 2026-03-09  
**Reference:** Production validation report: `docs/EVENT_BUS_PRODUCTION_VALIDATION_REPORT.md`  
**Runbook:** `docs/EVENT_BUS_PHASE8_9_SUMMARY.md`

This document summarizes rollout readiness and provides production verification steps for the Event Bus, Event Store, Event Bus Monitor, and related services.

---

## Step 1 — Deployment verification

**Code/schema verified**

- Migration **`20260309120000_AddJobRunEventId`** exists; runs after `AddJobRunsTable`. Adds `JobRuns.EventId` (uuid, nullable) and index **`IX_JobRuns_EventId`** (idempotent when table/column/index already exist).
- **EventStore** schema (entity + configuration): table `EventStore`; fields **CorrelationId**, **CompanyId**, **Status**, **RetryCount**, **LastHandler**, **LastError**, **ProcessedAtUtc**, **LastErrorAtUtc** (processing timestamps). Indexes on Status, CorrelationId, OccurredAtUtc, (CompanyId, EventType, OccurredAtUtc).
- **API build:** Backend API builds successfully (0 errors, 0 warnings).
- **Event-store endpoints:** All present on `EventStoreController` at `api/event-store`: `GET events`, `GET events/failed`, `GET events/dead-letter`, `GET events/{eventId}`, `GET events/{eventId}/related-links`, `GET dashboard`, `POST events/{eventId}/retry`, `POST events/{eventId}/replay`, `GET replay-policy/{eventType}`.

**Production checks (run in target environment)**

1. **Database**
   - Confirm migration applied: `SELECT * FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309120000_AddJobRunEventId'`.
   - Confirm table: `SELECT 1 FROM information_schema.tables WHERE table_name = 'JobRuns'`.
   - Confirm column: `SELECT 1 FROM information_schema.columns WHERE table_name = 'JobRuns' AND column_name = 'EventId'`.
   - Confirm index: `SELECT 1 FROM pg_indexes WHERE tablename = 'JobRuns' AND indexname = 'IX_JobRuns_EventId'`.
   - EventStore columns: `SELECT column_name FROM information_schema.columns WHERE table_name = 'EventStore'` — must include CorrelationId, CompanyId, Status, RetryCount, LastHandler, LastError, ProcessedAtUtc, LastErrorAtUtc (and OccurredAtUtc, CreatedAtUtc).
2. **API**
   - With valid auth (user with `jobs.view`): `GET /api/event-store/dashboard`, `GET /api/event-store/events?page=1&pageSize=5`. Expect 200 and JSON (dashboard or list).

---

## Step 2 — Frontend deployment

**Verified**

- **Route:** `/admin/event-bus` in `App.tsx`; `EventBusMonitorPage` behind `SettingsProtectedRoute`.
- **Access:** Requires `jobs.view` (or SuperAdmin/Admin); page checks `canViewJobs` and shows permission message when denied. Sidebar shows "Event Bus Monitor" with `permission: 'jobs.view'`.
- **Tabs:** Overview, Recent events, Failed, Dead-letter — all render; each tab loads its data (dashboard, list events, failed list, dead-letter list).
- **Frontend build:** `npm run build` completes successfully; `dist/` is produced.

**Production checks**

- Deploy `dist/` to your frontend host. Open `/admin/event-bus` as a user with `jobs.view`. Confirm all four tabs render; Overview shows dashboard (may be zeros); Recent/Failed/Dead-letter load tables.

---

## Step 3 — Background processing health

**Verified in code**

- **BackgroundJobProcessorService:** Registered as hosted service in `Program.cs` (`AddHostedService<BackgroundJobProcessorService>`). Runs continuously and processes queued jobs.
- **EventHandlingAsync:** Handled in `BackgroundJobProcessorService` (branch `"eventhandlingasync"` → `ProcessEventHandlingAsyncJobAsync`). Job type registered in `JobDefinitionProvider` (DisplayName, RetryAllowed, MaxRetries, DefaultStuckThresholdSeconds). `AsyncEventEnqueuer` enqueues jobs with `JobType = "EventHandlingAsync"`.
- **Scheduler services:** Multiple hosted scheduler services registered (e.g. `EmailIngestionSchedulerService`, `StockSnapshotSchedulerService`, `LedgerReconciliationSchedulerService`, `PnlRebuildSchedulerService`, `MissingPayoutSnapshotSchedulerService`, `PayoutAnomalyAlertSchedulerService`, etc.). Scheduler tasks that trigger workflow transitions will cause events to be published.

**Production checks**

- Ensure the API process is running (BackgroundJobProcessorService runs in the same process). Optionally use Background Jobs UI (`/admin/background-jobs`) to confirm jobs are processed and no persistent backlog. After workflow transitions, EventHandlingAsync jobs (if any async handlers are registered) should appear and complete.

---

## Step 4 — Event flow validation

**Flow (code-verified)**

1. **Workflow transition** → `WorkflowEngineService.ExecuteTransitionAsync` completes successfully.
2. **WorkflowTransitionCompletedEvent** emitted → `_domainEventDispatcher.PublishAsync(evt)`.
3. **EventStore entry created** → `IEventStore.AppendAsync` then `MarkAsProcessingAsync`; handlers run or event enqueued for async; eventually `MarkProcessedAsync` (or Failed/DeadLetter).
4. **Event visible in Event Bus Monitor** → List/dashboard read from same EventStore.

**Production checks**

- Trigger a real workflow transition (e.g. change order status via UI or API). Open Event Bus Monitor → Recent events (or Overview). Confirm a new event (e.g. `WorkflowTransitionCompleted`) appears with correct Event type, Status, Occurred time, and optional CorrelationId/Entity.

---

## Step 5 — Traceability check

**Verified**

- **EventStore entry** has `CorrelationId` (and EventId, CompanyId, etc.).
- **JobRun** created for handler execution (in-process or async) with **EventId** and **CorrelationId** via `JobRunRecorderForEvents.StartHandlerRunAsync`.
- **WorkflowJob** has **CorrelationId**; workflow engine sets it when creating the job so it matches the event’s correlation.
- **Related-links API:** `GET /api/event-store/events/{id}/related-links` returns `EventStoreRelatedLinksDto`: JobRuns (EventId or CorrelationId match), WorkflowJobs (CorrelationId match), company-scoped.

**Production checks**

- Pick an event from the monitor (e.g. from Recent events). Note its EventId and CorrelationId. Open event detail; confirm **Related (same correlation)** shows Job runs and/or Workflow jobs where applicable. Call `GET /api/event-store/events/{eventId}/related-links` and confirm response includes `jobRuns` and `workflowJobs` arrays.

---

## Step 6 — Failure handling

**Verified**

- **Retry:** `POST /api/event-store/events/{eventId}/retry` — `EventReplayService.RetryAsync`; no policy check; re-dispatches stored event; correlation preserved (from stored payload); handler execution creates JobRuns (in-process or via async job).
- **Replay:** `POST /api/event-store/events/{eventId}/replay` — `EventReplayService.ReplayAsync`; **EventReplayPolicy** enforced (`IsReplayAllowed(entry.EventType)`); only **WorkflowTransitionCompleted** allowed; same dispatch and JobRun behavior as Retry.
- **Replay policy:** `EventReplayPolicy` allows only `WorkflowTransitionCompleted`; other types get `BlockedReason` and 400 from API.

**Production checks**

- For a failed or dead-letter event, call `POST /api/event-store/events/{eventId}/retry` (with `jobs.admin`). Confirm 200 and event re-dispatched (status may move to Processing then Processed/Failed). For a non-allowed event type, call `POST .../replay` and confirm 400 with blocked reason. For `WorkflowTransitionCompleted`, Replay should succeed when policy allows.

---

## Step 7 — Dashboard observability

**Verified**

- **EventStoreQueryService.GetDashboardAsync** returns: **EventsToday**, **ProcessedCount**, **FailedCount**, **DeadLetterCount**, **ProcessedPercent**, **FailedPercent**, **TotalRetryCount**, **TopFailingEventTypes** (top 10), **TopFailingCompanies** (top 10). Overview tab calls `getEventStoreDashboard(fromUtc, toUtc)` and displays all of these.

**Production checks**

- Open Event Bus Monitor → Overview. Confirm metrics populate (Events today, Processed %, Failed %, Dead-letter count, Retry count, Top failing event types, Top failing companies). If traffic exists, investigate any abnormal pattern (e.g. high Failed %, one type or company dominating failures) using Failed/Dead-letter tabs and event detail.

---

## Step 8 — Operational readiness (runbook)

**Document:** `docs/EVENT_BUS_PHASE8_9_SUMMARY.md`

| Topic | Included |
|-------|----------|
| Deployment steps | Yes — backend (deploy API, apply migration), frontend (build, deploy, route, checklist). |
| Monitoring workflow | Yes — Overview, Recent, Failed, Dead-letter; what to observe; operational habits. |
| Retry procedure | Yes — when to use, effect, permission; use from Failed/Dead-letter. |
| Replay procedure | Yes — when to use, policy (allowed types), permission. |
| Root cause investigation | Yes — Failed/Dead-letter detail (Last error, Payload, Related links); top failing types/companies; trace to JobRun/workflow. |
| Correlation tracing | Yes — Correlation ID and Related links; EventStore → JobRun and WorkflowJob; explicit "Correlation tracing" subsection. |

Runbook is accurate and complete for rollout operations.

---

## Step 9 — Final status report

| Item | Status | Notes |
|------|--------|--------|
| **Migration status** | Ready | `20260309120000_AddJobRunEventId` present and ordered after AddJobRunsTable; adds EventId and IX_JobRuns_EventId. Apply in production with `dotnet ef database update` or idempotent script. |
| **Monitor UI status** | Ready | Route `/admin/event-bus`; tabs Overview, Recent events, Failed, Dead-letter; dashboard and tables load from event-store API; access requires `jobs.view`. Frontend builds successfully. |
| **Event generation verification** | Ready | Events created only when workflow transitions run (`WorkflowEngineService.PublishAsync(WorkflowTransitionCompletedEvent)`). Transitions triggered by API/UI, scheduler, and other jobs. No code gaps; verify in production by triggering a transition and confirming event in monitor. |
| **Traceability** | Ready | EventStore → CorrelationId → JobRun (EventId + CorrelationId) and WorkflowJob (CorrelationId). Related-links API and event detail drawer expose this. |
| **Retry / Replay** | Ready | APIs and policy verified; replay allowed only for WorkflowTransitionCompleted; retry preserves correlation; handler runs create JobRuns. |
| **Dashboard observability** | Ready | All metrics implemented and displayed; runbook describes how to interpret and investigate. |
| **Background processing** | Ready | BackgroundJobProcessorService and EventHandlingAsync path registered; scheduler services registered. Async event handlers will execute when enqueued. |
| **Runbook** | Ready | Deployment, monitoring, retry, replay, root cause investigation, and correlation tracing documented. |

---

## Rollout checklist (production)

- [ ] Apply migration `20260309120000_AddJobRunEventId` in production DB (or confirm already applied).
- [ ] Confirm `JobRuns` table exists and has `EventId` column and `IX_JobRuns_EventId` index.
- [ ] Confirm EventStore table has required columns (CorrelationId, CompanyId, Status, RetryCount, LastHandler, LastError, processing timestamps).
- [ ] Deploy backend API; smoke-test `GET /api/event-store/dashboard` and `GET /api/event-store/events`.
- [ ] Deploy frontend; open `/admin/event-bus` with a user that has `jobs.view`; confirm tabs and dashboard load.
- [ ] Trigger at least one workflow transition; confirm event appears in Event Bus Monitor.
- [ ] Open an event; confirm Related links (Job runs, Workflow jobs) when applicable; optionally call `GET .../related-links` for same eventId.
- [ ] Optionally test Retry on a failed event and Replay on a WorkflowTransitionCompleted event; confirm policy blocks Replay for other types.
- [ ] Review dashboard metrics; investigate any abnormal failure pattern using runbook procedures.

---

**Conclusion:** Event Bus production rollout is ready. Codebase and runbook are aligned; production verification steps above should be run in the target environment to confirm deployment and event flow end-to-end.

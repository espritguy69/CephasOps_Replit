# Repository-Wide Tenant-Scope Hardening Audit

**Date:** 2026-03-12  
**Scope:** All write paths for tenant-scoped entities (background jobs, ingestion, webhooks, workflows).  
**Rules:** No weakening of TenantSafetyGuard; no blanket platform bypasses; minimal production-safe fixes.

---

## 1. Audit Coverage

### A. Hosted services / background services

| Service | Role | Tenant-scoped writes? | Scope / bypass |
|--------|------|------------------------|----------------|
| **JobExecutionWorkerHostedService** | Claims JobExecution, runs executor, marks result | Yes (JobExecutionStore.Mark*; executors may write) | Sets `TenantScope.CurrentTenantId = job.CompanyId` per job; restores in `finally`. **No change.** |
| **BackgroundJobProcessorService** | Processes legacy BackgroundJob rows | Yes (BackgroundJob state; recorder; deprecated processors) | Set scope per job; **batch SaveChangesAsync** at end wrote multiple jobs under one scope → **fixed**. Reap sees all tenants → **fixed** with bypass + IgnoreQueryFilters. |
| **EmailIngestionSchedulerService** | Enqueues emailingest JobExecutions | Writes JobExecution via enqueuer | Uses **EnterPlatformBypass** for scheduling loop (platform reads EmailAccounts, enqueues per-tenant jobs). **No change.** |
| **SlaEvaluationSchedulerService** | Enqueues SlaEvaluation per company | Enqueue only | Uses bypass when iterating companies; enqueuer runs under bypass. **No change.** |
| **PnlRebuildSchedulerService**, **LedgerReconciliationSchedulerService**, **StockSnapshotSchedulerService**, **MissingPayoutSnapshotSchedulerService**, **PayoutAnomalyAlertSchedulerService** | Enqueue JobExecution or platform work | Enqueue / platform | Bypass used for scheduler loop. **No change.** |
| **EventStoreDispatcherHostedService** | Dispatches events; marks EventStore | EventStoreRepository (raw SQL / ExecuteUpdate) | No ApplicationDbContext SaveChanges for tenant-scoped entities in audited path. **No change.** |
| **NotificationDispatchWorkerHostedService** | Claims NotificationDispatch, sends | NotificationDispatchStore (MarkProcessedAsync) | NotificationDispatch is **not** in TenantSafetyGuard list. **No change.** |
| **OutboundIntegrationRetryWorkerHostedService**, **EventPlatformRetentionWorkerHostedService**, **WorkerHeartbeatHostedService**, **NotificationRetentionHostedService**, **EventBusMetricsCollectorHostedService**, **JobPollingCoordinatorService**, **EmailCleanupService** | Various platform/retention/heartbeat | Platform or no tenant-scoped entity writes in guard list | **No change.** |

### B. Email ingestion / mailbox processing

- **EmailIngestionService**: Invoked from **EmailIngestJobExecutor** (JobExecution pipeline). Worker sets `TenantScope = job.CompanyId` before executing. **No change.**
- **ParserController / EmailAccountsController**: API; tenant from middleware. **No change.**

### C. Job execution / queue processing

- **JobExecutionStore.AddAsync**: Called by **JobExecutionEnqueuer**. Callers: EmailIngestionSchedulerService (under bypass), SlaEvaluationSchedulerService (under bypass), PnlRebuildSchedulerService (bypass), LedgerReconciliationSchedulerService (bypass), StockSnapshotSchedulerService (bypass), InvoiceSubmissionService (API tenant), DocumentGenerationJobEnqueuer (API tenant), RebuildJobEnqueuer / ReplayJobEnqueuer (replay/rebuild context). **No change.**
- **JobExecutionStore.MarkSucceededAsync / MarkFailedAsync**: Called from **JobExecutionWorkerHostedService** with `TenantScope = job.CompanyId` already set. **No change.**
- **JobRunRecorder**: JobRun is **not** in TenantSafetyGuard; called from BackgroundJobProcessorService with scope set per job. **No change.**

### D. Webhooks / external ingestion

- **InboundWebhookRuntime**: Creates/updates **InboundWebhookReceipt** (tenant-scoped in guard). Previously no tenant scope set before store Create/Update. **Fixed:** set `TenantScope = request.CompanyId` (or platform bypass when no company) and restore in `finally`.

### E. Admin / platform flows

- **CompanyProvisioningService**: Already uses **EnterPlatformBypass** / **ExitPlatformBypass** around provisioning. **No change.**
- **DatabaseSeeder**: Already uses platform bypass. **No change.**
- **EventPlatformRetentionService**: Uses bypass for retention deletes. **No change.**

### F. Risky patterns (grep)

- **IgnoreQueryFilters**: AuthService (forgot-password, change-password-required, refresh-token, reset-token lookups); EventPlatformRetentionService (InboundWebhookReceipt delete); DatabaseSeeder; StockLedgerService (testing); OrderService (soft-deleted order lookup); AssetService (deleted). No new blanket bypasses; existing uses are read-only or justified.
- **ExecuteSqlRaw / FromSqlRaw**: Used in TaskService, VipGroupService, EmailRuleService, ParserTemplateService, VipEmailService, SchedulerService, InvoiceSubmissionService, AdminService, WorkerCoordinatorService. These run in API or job context with tenant; no change in this audit.
- **SaveChangesAsync**: All touched paths either set scope, use bypass in a narrow block, or are invoked under existing scope (API/job).

---

## 2. Gaps Found

| # | Location | Issue | Risk |
|---|----------|--------|------|
| 1 | **BackgroundJobProcessorService** | (a) Queries for `runningMine` and `queuedUnclaimed` use global query filter without scope → only jobs with `CompanyId == null` were visible when scope was null. (b) Single **SaveChangesAsync** at end of loop persisted all modified jobs (multiple tenants) under one scope → guard could fail or wrong scope. (c) **ReapStaleRunningJobsAsync** queried/saved BackgroundJobs without scope or bypass → could not see or update all tenants’ stuck jobs. | Cross-tenant visibility and save violations. |
| 2 | **InboundWebhookRuntime** | Create/Update of **InboundWebhookReceipt** (tenant-scoped) without setting **TenantScope** or platform bypass → SaveChanges would throw or be inconsistent. | Guard failure or incorrect tenant context on webhook receipt persistence. |

---

## 3. Design Decisions

- **BackgroundJobProcessorService**
  - Use **IgnoreQueryFilters** on BackgroundJob queries so the processor can see jobs from all tenants (no scope set at poll time). Keep per-job **TenantScope = job.CompanyId** in **ProcessJobAsync** and **save inside ProcessJobAsync** (success, catch, cancel) so each job is persisted under its own scope. Remove reliance on a single batch SaveChanges at the end.
  - **ReapStaleRunningJobsAsync**: Treat as platform maintenance. Use **IgnoreQueryFilters** to load all Running jobs, then **EnterPlatformBypass** / **ExitPlatformBypass** around **SaveChangesAsync** so updates to any tenant’s stuck jobs are allowed.
- **InboundWebhookRuntime**
  - Set tenant scope (or platform bypass) for the whole **ProcessAsync** so all receipt Create/Update calls run with consistent context. Use **request.CompanyId** when present; otherwise **platform bypass** (e.g. global endpoint). Restore in **finally**.

---

## 4. Files Changed

| File | Change summary |
|------|----------------|
| `backend/src/CephasOps.Application/Workflow/Services/BackgroundJobProcessorService.cs` | IgnoreQueryFilters on job queries; platform bypass + IgnoreQueryFilters in ReapStaleRunningJobsAsync; per-job SaveChangesAsync in ProcessJobAsync (success, catch, cancel). |
| `backend/src/CephasOps.Application/Integration/InboundWebhookRuntime.cs` | Set TenantScope or platform bypass before receipt persistence; restore in finally; ProcessCoreAsync for core logic; receipt.CompanyId = request.CompanyId ?? endpoint.CompanyId. |

---

## 5. Exact Fixes Applied

### BackgroundJobProcessorService.cs

1. **ProcessJobsAsync – job queries**
   - Added **.IgnoreQueryFilters()** to both `context.BackgroundJobs.Where(...)` queries (runningMine and queuedUnclaimed) so jobs from all tenants are visible.

2. **ReapStaleRunningJobsAsync**
   - Added **.IgnoreQueryFilters()** to the Running jobs query.
   - Wrapped the `if (reaped > 0) { await context.SaveChangesAsync(...) }` block in **TenantSafetyGuard.EnterPlatformBypass()** / **try** / **finally** / **TenantSafetyGuard.ExitPlatformBypass()**.

3. **ProcessJobAsync – per-job save**
   - **Success path:** After `job.State = Succeeded` and `job.CompletedAt = ...`, added **await context.SaveChangesAsync(cancellationToken)**.
   - **Cancel path (OperationCanceledException):** After resetting job state to Queued, added **await context.SaveChangesAsync(cancellationToken)**.
   - **Catch path (Exception):** After setting job state and (optionally) retry, set **job.UpdatedAt = DateTime.UtcNow** and added **await context.SaveChangesAsync(cancellationToken)**.

### InboundWebhookRuntime.cs

1. **ProcessAsync**
   - After resolving `endpoint`, compute **useBypass = !request.CompanyId.HasValue || request.CompanyId.Value == Guid.Empty**.
   - **previousTenantId = TenantScope.CurrentTenantId**. If **useBypass** then **TenantSafetyGuard.EnterPlatformBypass()**, else **TenantScope.CurrentTenantId = request.CompanyId**.
   - **try { return await ProcessCoreAsync(request, endpoint, cancellationToken); }**
   - **finally:** if useBypass then **TenantSafetyGuard.ExitPlatformBypass()**, else **TenantScope.CurrentTenantId = previousTenantId**.

2. **ProcessCoreAsync** (new private method)
   - Contains previous body of ProcessAsync (receipt create/update, verification, idempotency, handler, etc.).
   - **receipt.CompanyId = request.CompanyId ?? endpoint.CompanyId** so receipt has a company when endpoint is company-scoped.

3. **Namespace**
   - Added **using CephasOps.Infrastructure.Persistence** for TenantScope and TenantSafetyGuard.
   - **ProcessCoreAsync** parameter type: **ConnectorEndpoint** (registry returns ConnectorEndpoint?).

---

## 6. Validation Performed

- **Build:** `dotnet build` for **CephasOps.Api** — **succeeded.**
- **Tests:** **AuthServiceTests** (33 tests) — **all passed.**
- **TenantSafetyGuard:** Not weakened; no removal of scope/bypass restoration; all new bypass uses are narrow (ReapStaleRunningJobsAsync only).
- **Restore patterns:** BackgroundJobProcessorService already restored scope in finally per job; InboundWebhookRuntime now restores (or exits bypass) in finally.

---

## 7. Remaining Assumptions / Risks

- **JobRun** is not in **TenantSafetyGuard.IsTenantScopedEntityType**; JobRunRecorder is only used from contexts that set tenant scope (e.g. BackgroundJobProcessorService per job). If JobRun is later treated as tenant-scoped, callers must set scope before Start/Complete/Fail.
- **NotificationDispatch** is not in the guard list; NotificationDispatchWorkerHostedService does not set TenantScope. If it is later considered tenant-scoped, scope should be set from **dispatch.CompanyId** before MarkProcessedAsync.
- **Email ingestion** and other **parser** paths that call **SaveChangesAsync** (e.g. EmailIngestionService) are only invoked from **EmailIngestJobExecutor** with TenantScope set by JobExecutionWorkerHostedService; no additional changes in this audit.
- **Event store / ledger / replay** write paths were not fully audited in this pass; they may use raw SQL or ExecuteUpdate. Follow-up: confirm whether any of those writes touch tenant-scoped entities and require scope or bypass.
- **InboundWebhookRuntime** when **request.CompanyId** is null uses platform bypass for the whole ProcessAsync; handler code may still write other tenant-scoped entities. Handlers should be invoked with tenant scope if they perform tenant-scoped writes; current fix only ensures receipt persistence is valid.

---

## 8. Intentionally Left on Platform Bypass (no change)

- **DatabaseSeeder** – design-time/seed data.
- **ApplicationDbContextFactory** – design-time.
- **CompanyProvisioningService** – new tenant/company/user creation.
- **EventPlatformRetentionService** – platform retention deletes.
- **EmailIngestionSchedulerService**, **SlaEvaluationSchedulerService**, **PnlRebuildSchedulerService**, **LedgerReconciliationSchedulerService**, **StockSnapshotSchedulerService**, **MissingPayoutSnapshotSchedulerService**, **PayoutAnomalyAlertSchedulerService** – scheduler loops that only enqueue work or read configuration; JobExecution add is under bypass.
- **ReapStaleRunningJobsAsync** (after fix) – platform maintenance; bypass only around the SaveChangesAsync for reaped jobs.

---

## 9. Tests Added/Updated

- **None** in this pass. Existing **AuthServiceTests** (33) still pass. BackgroundJobProcessorService and InboundWebhookRuntime are not covered by new tests in this audit; manual or integration testing recommended for webhook and legacy job processing.

---

## 10. Summary

- **Audit:** Hosted services, job execution, email ingestion entry points, webhook ingestion, and existing bypass usage were reviewed.
- **Gaps fixed:** (1) **BackgroundJobProcessorService** – tenant visibility for job queries, per-job save under correct scope, and reaping under platform bypass; (2) **InboundWebhookRuntime** – tenant scope (or bypass) set and restored around receipt Create/Update.
- **Guard and restore:** TenantSafetyGuard unchanged; all new bypass uses are scoped and paired with Exit in finally; scope is restored in finally where set.

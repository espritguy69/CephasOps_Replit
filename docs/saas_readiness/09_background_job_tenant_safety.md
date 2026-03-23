# Background Job and Automation Tenant-Safety Plan

**Date:** 2026-03-13

Explicit coverage for tenant safety of background jobs and automation: job ownership, tenant context resolution, retry behaviour, platform-wide vs tenant-scoped jobs, failure containment, duplicate processing, and log observability. Aligns with `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md` and [06_module_test_matrix.md](06_module_test_matrix.md).

---

## 1. Job ownership

| Requirement | CephasOps implementation | Verification |
|-------------|---------------------------|--------------|
| **Every tenant-scoped job has CompanyId** | BackgroundJob entity has CompanyId; set when enqueuing from API or scheduler. | Audit enqueue call sites: OrderAssignedOperationsHandler, EmailIngestionSchedulerService, PnlRebuildSchedulerService, document generation, MyInvois poll, etc. Ensure job.CompanyId = current tenant or payload company. |
| **Platform-wide jobs** | Jobs that intentionally run across tenants (e.g. reap, retention enumeration) do not carry a single CompanyId; execution uses RunWithPlatformBypassAsync for the platform part, and per-tenant work uses RunWithTenantScopeAsync(companyId) or a child job with CompanyId. | ReapStaleRunningJobsAsync: platform bypass only for state update. Retention: RunWithTenantScopeOrBypassAsync(companyId) per tenant. Schedulers: enumerate companies under bypass; enqueue per-tenant jobs with CompanyId. |
| **Nullable CompanyId** | Only where explicitly justified (e.g. legacy job, event with no company). Execution uses RunWithTenantScopeOrBypassAsync(null) so no tenant-scoped entity is written without bypass. | BackgroundJobProcessorService: effectiveCompanyId = job.CompanyId ?? TryGetCompanyIdFromPayload(payload); RunWithTenantScopeOrBypassAsync(effectiveCompanyId, …). |

**Action:** Code review all enqueue paths; add integration test that enqueued job has CompanyId when enqueued from tenant context.

---

## 2. Tenant context resolution

| Requirement | CephasOps implementation | Verification |
|-------------|---------------------------|--------------|
| **Execution sets scope before work** | Before running job delegate, set TenantScope from job.CompanyId (or enter bypass if null). | BackgroundJobProcessorService.ProcessJobAsync: await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(effectiveCompanyId, async (ct) => { … }, cancellationToken). |
| **No manual scope in job handler** | Job handlers (IJobExecutor, legacy handlers) must not set TenantScope or EnterPlatformBypass themselves; the processor sets scope once per job. | Review all job handler implementations; ensure they rely on ambient scope set by processor. |
| **Scope restored on exception** | TenantScopeExecutor restores previous scope or exits bypass in finally. | TenantScopeExecutorTests already verify; no manual set without restore in job path. |

**Action:** Run integration test: enqueue job with CompanyId = A; in test, assert that during execution TenantScope.CurrentTenantId (or SaveChanges guard) was A.

---

## 3. Retry behaviour

| Requirement | CephasOps implementation | Verification |
|-------------|---------------------------|--------------|
| **Retry same job same scope** | When a job is retried (e.g. failed then re-run), it runs again with the same job.CompanyId. | Job row is updated (state, StartedAt, etc.); CompanyId unchanged. Processor loads same job and runs with same effectiveCompanyId. |
| **No scope bleed** | After job (success or failure), next job in same worker run must get its own scope (next job’s CompanyId). | Processor runs jobs in loop; each iteration calls RunWithTenantScopeOrBypassAsync for that job; executor restores in finally. |
| **Idempotency** | Duplicate run of same job (e.g. retry, scheduler overlap) should not double-apply side effects (e.g. double notification, double P&L row). | Per job type: design idempotent payload (e.g. “rebuild P&L for period X”); use upsert or idempotency key where applicable. Document in architecture guardrails. |

**Action:** Retry a failed job; verify it runs with same CompanyId and does not write duplicate side effects when designed idempotent.

---

## 4. Platform-wide vs tenant-scoped jobs

| Job type | Ownership | Execution pattern | Example |
|----------|-----------|--------------------|--------|
| **Email ingest** | Tenant (per mailbox/account) | Scheduler enumerates accounts under bypass; each account has CompanyId; enqueue job with that CompanyId. Worker runs with RunWithTenantScopeOrBypassAsync(job.CompanyId). | EmailIngestionSchedulerService; EmailIngestJobExecutor (if migrated to JobExecution). |
| **P&L rebuild** | Tenant | Scheduler enumerates companies under bypass; enqueues per-company job with CompanyId. Worker runs with that CompanyId. | PnlRebuildSchedulerService; PnlRebuild job. |
| **Ledger reconciliation** | Tenant | Same pattern: enumerate tenants, enqueue per-tenant job. | LedgerReconciliationSchedulerService. |
| **Stock snapshot** | Tenant | Same. | StockSnapshotSchedulerService. |
| **SLA evaluation** | Tenant | Run per company with RunWithTenantScopeAsync(companyId, …) in scheduler (no job enqueue required for SLA eval) or enqueue per-company job. | SlaEvaluationSchedulerService. |
| **Notification retention** | Tenant or platform | RunWithTenantScopeOrBypassAsync(companyId); when companyId null, retention runs across tenants under bypass. | NotificationRetentionService. |
| **Reap stale running** | Platform | RunWithPlatformBypassAsync; only job state (State, CompletedAt, LastError) updated; no business entity read/write mixed. | BackgroundJobProcessorService.ReapStaleRunningJobsAsync. |
| **Document generation** | Tenant | Enqueued from order/workflow with order’s CompanyId; worker runs with that CompanyId. | Document generation enqueuer; job payload has orderId/companyId. |
| **MyInvois status poll** | Tenant | Enqueued per tenant or per submission; job.CompanyId set. | MyInvoisStatusPollJobExecutor (if present). |

**Action:** For each job type, document ownership and execution pattern; verify scheduler or enqueuer sets CompanyId for tenant jobs.

---

## 5. Failure containment

| Requirement | CephasOps implementation | Verification |
|-------------|---------------------------|--------------|
| **One job’s failure does not break others** | Each job runs in its own scope and try/catch; failure marks that job Failed and logs; processor continues to next job. | ProcessJobAsync: try/catch around delegate; job state updated to Failed; exception logged. |
| **No cross-tenant write in same transaction** | Tenant-scoped job must not open platform bypass and write another tenant’s data. | Code review: no EnterPlatformBypass inside tenant job handler; no IgnoreQueryFilters without AssertTenantContext in job handler. |
| **Reap does not corrupt tenant data** | Reap only updates BackgroundJob state; does not modify Orders, Invoices, Notifications, etc. | ReapStaleRunningJobsAsync only updates job row; uses RunWithPlatformBypassAsync for SaveChanges of job state only. |

**Action:** Unit or integration test: run job that throws; assert only that job marked Failed; next job still runs with correct scope.

---

## 6. Duplicate processing

| Requirement | CephasOps implementation | Verification |
|-------------|---------------------------|--------------|
| **Same job not run by two workers** | Claim via IWorkerCoordinator.TryClaimBackgroundJobAsync; only one worker claims a given job. | Coordinator and job state (WorkerId, State = Running) prevent double claim. |
| **Idempotent side effects** | Job types that can be retried or run twice (e.g. P&L rebuild for period X) are designed to be idempotent. | Document idempotency per job type; tests where applicable. |
| **Notification send** | Notification send job (if legacy) or NotificationDispatch: ensure duplicate dispatch does not send twice (e.g. idempotency key or state). | NotificationDispatchRequestService and dispatch logic. |

**Action:** Enqueue same logical job twice (e.g. same payload); verify side effect applied once (or twice with same outcome for idempotent jobs).

---

## 7. Log observability

| Requirement | CephasOps implementation | Verification |
|-------------|---------------------------|--------------|
| **Logs include tenant context** | When logging job start/end/error, include CompanyId or tenant identifier where applicable. | BackgroundJobProcessorService logs job Id, JobType; add CompanyId to log scope or message for tenant jobs. |
| **Job run record** | JobRun or JobExecution (if used) records CompanyId so observability UI can show tenant. | IJobRunRecorder records run with tenant; dashboard or ObservabilityController can filter by tenant. |
| **No PII/cross-tenant in logs** | Logs must not dump another tenant’s data (e.g. order details, user email) in clear text. | Review log messages; use IDs, not full entities from other tenants. |

**Action:** Run job for tenant A; check logs and job run record for CompanyId; confirm no B data in logs.

---

## 8. Queue / job dashboard validation

| Requirement | CephasOps implementation | Verification |
|-------------|---------------------------|--------------|
| **List jobs (admin)** | GET /api/background-jobs or SystemWorkersController: if list shows jobs from all tenants, ensure only admins can access; or filter by tenant for non-super-admin. | Authorization and optional tenant filter. |
| **Execution history** | Job run history shows which tenant’s job was run (CompanyId or company name). | ObservabilityController or job run list includes tenant. |
| **Manual trigger** | Manual trigger (e.g. POST run) must run job with correct CompanyId (from request or context). | Trigger endpoint receives or resolves companyId; enqueued job has CompanyId set. |

**Action:** Manual UAT: view job list and execution history; trigger job for tenant A; verify run record shows tenant A.

---

## 9. Summary checklist

- [ ] All tenant-scoped job enqueue paths set job.CompanyId.
- [ ] BackgroundJobProcessorService runs each job with RunWithTenantScopeOrBypassAsync(effectiveCompanyId, …).
- [ ] Reap uses RunWithPlatformBypassAsync only for job state update.
- [ ] Schedulers that enumerate tenants enqueue per-tenant jobs with CompanyId or run per-tenant work under RunWithTenantScopeAsync.
- [ ] No job handler manually sets TenantScope or EnterPlatformBypass.
- [ ] Retry and duplicate run behaviour is safe (same scope; idempotent where needed).
- [ ] Logs and job run records include tenant where applicable; no cross-tenant data leak in logs.
- [ ] Job list and dashboard (if exposed) are access-controlled and optionally tenant-filtered.

Use this plan with [01_master_checklist.md](01_master_checklist.md) section 6 and [05_execution_order.md](05_execution_order.md) Phase 5.

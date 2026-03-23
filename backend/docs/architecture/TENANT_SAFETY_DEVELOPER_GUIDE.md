# Tenant Safety — Developer Guide

**Primary entry point for tenant-safety.** Use this guide first for when and how to set tenant scope, bypass rules, try/finally restoration, PR checklist, and test execution.

**Purpose:** Authoritative short reference for the tenant-safety model. Use this when adding or changing code that touches tenant-scoped entities, hosted services, event dispatch, webhooks, or job workers.

**See also:** [SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md](SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md) (security and tenant-safety architecture diagram), [EF_TENANT_SCOPE_SAFETY.md](EF_TENANT_SCOPE_SAFETY.md) (persistence details), [operations/TENANT_SAFETY_FINAL_VERIFICATION.md](../operations/TENANT_SAFETY_FINAL_VERIFICATION.md) (verification report), [operations/PLATFORM_SAFETY_HARDENING_INDEX.md](../operations/PLATFORM_SAFETY_HARDENING_INDEX.md) (index of safeguards), [TENANT_SCOPE_EXECUTOR_COMPLETION.md](TENANT_SCOPE_EXECUTOR_COMPLETION.md) (executor rollout completion and final standard).

---

## 1. When TenantScope.CurrentTenantId must be set

- **Before any path that can call `SaveChangesAsync`** on a context that tracks tenant-scoped entities (e.g. `CompanyScopedEntity`, `User`, `BackgroundJob`, `JobExecution`, `OrderPayoutSnapshot`, `InboundWebhookReceipt`).
- **Request pipeline:** Set by API middleware from `ITenantProvider.CurrentTenantId` after `TenantGuardMiddleware` (you don’t set it in controllers; middleware does).
- **Background jobs:** Set by the job runner from `job.CompanyId` (or equivalent) **before** executing the job delegate.
- **Event dispatch / replay:** Set from the event’s `CompanyId` (or bypass when event has no company) **before** invoking the handler.
- **Webhooks:** Set from `request.CompanyId` when present; otherwise use platform bypass for the request (see below).
- **Auth flows (login, refresh, forgot-password):** Set from the resolved user’s `CompanyId` for the block that performs tenant-scoped reads/writes; use bypass only where the operation is intentionally platform-wide (e.g. lookup by email).

**Rule:** If your code (or a service it calls) can add, update, or delete tenant-scoped entities, ensure `TenantScope.CurrentTenantId` is set for that async context **before** the work runs, or use a documented platform bypass.

---

## 2. When TenantSafetyGuard.EnterPlatformBypass() is allowed

Platform bypass turns off SaveChanges tenant validation and allows `AssertTenantContext()` to pass without a tenant. Use **only** in these cases:

| Use | Where | Pairing |
|-----|--------|--------|
| **Retention / cleanup across tenants** | **EventPlatformRetentionService**, **NotificationRetentionService** (when `companyId` is null) use **TenantScopeExecutor.RunWithPlatformBypassAsync**. | Executor (or Enter/Exit in **finally**) |
| **Seeding** | DatabaseSeeder | Enter before seeding, Exit in **finally** |
| **Design-time DbContext** | ApplicationDbContextFactory.CreateDbContext | Enter in CreateDbContext; no Exit (process exits) |
| **Scheduler loops that enumerate tenants** | **EmailIngestionSchedulerService**, **PnlRebuildSchedulerService**, **LedgerReconciliationSchedulerService**, **StockSnapshotSchedulerService**, **MissingPayoutSnapshotSchedulerService**, **PayoutAnomalyAlertSchedulerService** use **TenantScopeExecutor.RunWithPlatformBypassAsync**. **SlaEvaluationSchedulerService** uses **TenantScopeExecutor.RunWithTenantScopeAsync**(companyId, …) per company. | Executor or Enter/Exit in **finally** |
| **Provisioning** | **CompanyProvisioningService** uses **TenantScopeExecutor.RunWithPlatformBypassAsync** (generic overload returns result). | Executor |
| **Webhook with no company** | InboundWebhookRuntime (when `request.CompanyId` is null/empty) | Enter at start of request, Exit in **finally** |
| **Event dispatch/replay with no company** | EventStoreDispatcherHostedService, EventReplayService (when event has no CompanyId) | Enter for that event, Exit in **finally** |
| **Stale job reap** | BackgroundJobProcessorService (reap path) uses **TenantScopeExecutor.RunWithPlatformBypassAsync**. | Executor |

**Rule:** Do not introduce new bypasses for “convenience.” Any new bypass must be justified, documented here or in the architecture doc, and **must** pair `ExitPlatformBypass()` in a `finally` block (except design-time factory).

---

## 3. Required try/finally restoration pattern

Whenever you set `TenantScope.CurrentTenantId` or call `EnterPlatformBypass()` in a code path that can throw or return early:

1. Capture the previous value: `var previousTenantId = TenantScope.CurrentTenantId;`
2. Set scope or enter bypass at the start of the logical block.
3. Use **try/finally**: in **finally**, either restore `TenantScope.CurrentTenantId = previousTenantId` or call `ExitPlatformBypass()` (and restore scope if you had set it in addition to bypass).

### 3.1 Preferred: TenantScopeExecutor (central helper)

**Use the central helper** so scope/bypass and restoration are consistent and hard to get wrong:

- **Run as tenant:** `TenantScopeExecutor.RunWithTenantScopeAsync(companyId, async ct => { ... }, cancellationToken)` — sets scope, runs work, restores previous scope in finally (even on exception).
- **Run as platform:** `TenantScopeExecutor.RunWithPlatformBypassAsync(async ct => { ... }, cancellationToken)` — enters bypass, runs work, exits bypass in finally.
- **Scope or bypass from nullable companyId:** `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyId, async ct => { ... }, cancellationToken)` — if `companyId` has a value (and is not empty), runs under tenant scope; otherwise runs under platform bypass; always restores/exits in finally.

**Location:** `CephasOps.Infrastructure.Persistence.TenantScopeExecutor` (static class). Use this in hosted services, event dispatchers, replay, webhooks, retention, and any path that needs explicit tenant vs platform execution. It preserves AsyncLocal safety and makes tenant vs platform intent explicit and auditable.

If you cannot use the executor (e.g. complex control flow), implement the manual try/finally pattern below and ensure restoration in **finally**.

**Example (scope only):**

```csharp
var previousTenantId = TenantScope.CurrentTenantId;
TenantScope.CurrentTenantId = companyId;
try
{
    await DoWorkAsync();
}
finally
{
    TenantScope.CurrentTenantId = previousTenantId;
}
```

**Example (bypass):**

```csharp
TenantSafetyGuard.EnterPlatformBypass();
try
{
    await RunRetentionAcrossAllTenantsAsync();
}
finally
{
    TenantSafetyGuard.ExitPlatformBypass();
}
```

**Example (scope or bypass per request):**

```csharp
var previousTenantId = TenantScope.CurrentTenantId;
var useBypass = !request.CompanyId.HasValue || request.CompanyId.Value == Guid.Empty;
if (useBypass)
    TenantSafetyGuard.EnterPlatformBypass();
else
    TenantScope.CurrentTenantId = request.CompanyId;
try
{
    return await ProcessAsync(request);
}
finally
{
    if (useBypass)
        TenantSafetyGuard.ExitPlatformBypass();
    else
        TenantScope.CurrentTenantId = previousTenantId;
}
```

---

## 4. Hosted services, dispatchers, replays, webhooks, auth, job workers

| Component | Responsibility |
|-----------|----------------|
| **API middleware** | Resolve tenant via `ITenantProvider.GetEffectiveCompanyIdAsync()`; set `TenantScope.CurrentTenantId` for the request; clear in finally. |
| **JobExecutionWorkerHostedService** | Uses **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync**(job.CompanyId, …) per job; scope or bypass, restore/exit in finally. |
| **BackgroundJobProcessorService** | Per job: **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync**(job.CompanyId ?? TryGetCompanyIdFromPayload(payload), …). Reap path: **TenantScopeExecutor.RunWithPlatformBypassAsync**(…). |
| **EventStoreDispatcherHostedService** | Uses **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync**(entry.CompanyId, …); per event: tenant scope or platform bypass, restore/exit in finally. |
| **EventReplayService** | Uses **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync**(entry.CompanyId, …); per replay entry: tenant scope or bypass, restore/exit in finally. |
| **InboundWebhookRuntime** | Uses **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync**(request.CompanyId, …); tenant scope or bypass, restore/exit in finally. |
| **NotificationRetentionService** | Uses **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync**(companyId, …); tenant scope or bypass, restore/exit in finally. |
| **AuthService** | For login/refresh/password flows that load or update tenant-scoped data: set TenantScope from user’s CompanyId (or use bypass for platform lookups); restore in finally. |
| **Scheduler services** | Use EnterPlatformBypass for the enumeration loop; when enqueueing per-tenant work, set TenantScope per tenant before enqueue or use jobs that carry CompanyId so the worker sets scope. |

---

## 5. Fail-closed expectations for null-company tenant-owned work

- **Do not** create tenant-scoped entities (e.g. BackgroundJob, Notification, NotificationDispatch, InboundWebhookReceipt with a write that implies a tenant) when the effective company is null or empty, unless the operation is explicitly under platform bypass.
- **Do** skip or early-return with a log when company cannot be resolved (e.g. OrderAssignedOperationsHandler: do not enqueue SLA job when order and event have no CompanyId; NotificationDispatchRequestService: do not create dispatch when effective company is null; EmailIngestionSchedulerService: skip accounts with null CompanyId).
- **Do** throw or return 403 for request-time API when tenant is required and unresolved (handled by TenantGuardMiddleware and RequireCompanyId).

---

## 6. PR / review checklist for tenant-sensitive changes

Use this when changing code that touches persistence, workflow, event dispatch, webhooks, or job execution:

- [ ] **TenantScope** is set (or platform bypass used) **before** any path that can call `SaveChangesAsync` on tenant-scoped entities.
- [ ] **Restoration:** Any block that sets `TenantScope.CurrentTenantId` or calls `EnterPlatformBypass()` restores in a **finally** block (except design-time factory).
- [ ] **No new blanket bypass:** New use of `EnterPlatformBypass` is justified and documented; no “convenience” bypass.
- [ ] **Null-company:** Tenant-owned operations (enqueue, dispatch, create notification) when company is null either skip/early-return with log or run under an explicit, narrow bypass.
- [ ] **IgnoreQueryFilters:** Any new use of `IgnoreQueryFilters` on tenant-relevant data is preceded by `TenantSafetyGuard.AssertTenantContext()` or runs inside a documented platform bypass.
- [ ] **Tests:** New or modified tenant-scoped paths have regression tests (scope set, restore in finally, null-company behavior); tests that insert tenant-scoped entities set `TenantScope` (or bypass) in setup.

---

## 7. Test execution (tenant-safety regression and limitations)

### 7.1 Stable tenant-safety regression suite

Run before merging changes that touch tenant resolution, TenantScope, TenantSafetyGuard, hosted services, event dispatch, webhooks, or notification/job enqueue:

```bash
cd backend/tests/CephasOps.Application.Tests
dotnet test --filter "FullyQualifiedName~TenantScopeExecutorTests|FullyQualifiedName~NotificationRetentionServiceTests|FullyQualifiedName~EventReplayServiceTenantScopeTests|FullyQualifiedName~InboundWebhookRuntimeTenantScopeTests|FullyQualifiedName~OrderAssignedOperationsHandlerTests.HandleAsync_OrderNotFound|FullyQualifiedName~OrderAssignedOperationsHandlerTests.HandleAsync_WhenOrderAndEventHaveNoCompanyId|FullyQualifiedName~NotificationServiceTests.CreateNotificationAsync_WhenCompanyId|FullyQualifiedName~NotificationServiceTests.CreateNotificationAsync_WhenCompanyIdNull|FullyQualifiedName~NotificationDispatchRequestServiceTests"
```

These tests cover the TenantScopeExecutor (tenant scope, platform bypass, nested, exception), scope restoration, bypass exit, and null-company behavior. Expect **21+ tests** to pass (including TenantScopeExecutorTests).

### 7.2 Isolated tests (run separately if needed)

Three **OrderAssignedOperationsHandlerTests** that rely on the handler loading an Order via the in-memory global query filter can fail when run in the same batch as other tests (AsyncLocal/execution context). They **pass when run in isolation**:

```bash
dotnet test --filter "HandleAsync_OrderWithAssignedSi_CreatesTask_CallsMaterialPack_EnqueuesSlaJob"
dotnet test --filter "HandleAsync_RepeatedCall_DoesNotDuplicateTask_EnqueuesSlaOnlyOnce"
dotnet test --filter "HandleAsync_OrderWithoutAssignedSi_SkipsTask_StillCallsMaterialPack_EnqueuesSla"
```

Production behavior is correct: the event dispatcher sets TenantScope from the event’s CompanyId before dispatching to the handler.

### 7.3 In-memory provider and global query filter

Tests that use `ApplicationDbContext` with the in-memory provider and global query filters (TenantScope.CurrentTenantId) can be sensitive to test order and parallel execution: the filter is evaluated at query time, and AsyncLocal can be affected by other tests. Tenant-scope–dependent test classes use the **TenantScopeTests** collection (`DisableParallelization = true`) to reduce interference. When writing new tests that add tenant-scoped entities, set `TenantScope.CurrentTenantId` (or use bypass) before `SaveChangesAsync` and restore in finally; add the test class to `[Collection("TenantScopeTests")]` if it manipulates TenantScope.

---

## 8. Quick reference

| I want to… | Do this |
|------------|---------|
| Write tenant-scoped entities in a request | Ensure middleware has set TenantScope (automatic for normal API). |
| Write tenant-scoped entities in a job | Set TenantScope from job.CompanyId before work; restore in finally (or use TenantScopeExecutor.RunWithTenantScopeAsync). |
| Run a platform-wide cleanup/retention | Use TenantScopeExecutor.RunWithPlatformBypassAsync, or EnterPlatformBypass at start, ExitPlatformBypass in finally. |
| Handle webhook/event with no company | Use TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyId, …), or manual bypass; enter at start, exit in finally. |
| Use IgnoreQueryFilters on tenant data | Call AssertTenantContext() immediately before, or run under documented bypass. |
| Add a new hosted service that writes tenant data | Set TenantScope per item (e.g. from event/job CompanyId) and restore in finally. |

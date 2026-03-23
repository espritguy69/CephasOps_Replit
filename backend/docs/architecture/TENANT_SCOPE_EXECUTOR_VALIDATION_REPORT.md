# TenantScopeExecutor — Validation Report

**Date:** 2026-03-13  
**Scope:** Single reusable tenant-scope execution pattern; safety-hardening and maintainability. No business behavior change, no weakening of TenantSafetyGuard, no new blanket bypasses, no migrations.

---

## 1. Root problem with the current repeated pattern

- **Repeated boilerplate:** Multiple call sites (EventStoreDispatcherHostedService, EventReplayService, InboundWebhookRuntime, NotificationRetentionService) each implemented the same logic: capture `TenantScope.CurrentTenantId`, decide scope vs bypass from nullable `companyId`, set scope or call `EnterPlatformBypass()`, run work, then in **finally** restore scope or call `ExitPlatformBypass()`.
- **Risk of mistakes:** Manual try/finally is easy to get wrong (e.g. missing restore on one path, wrong variable in finally). Inconsistent patterns across the codebase make auditing and onboarding harder.
- **No single “right way”:** New code had no obvious, auditable default for “run as tenant” vs “run as platform,” increasing the chance of ad-hoc or incorrect handling.

---

## 2. Design decision

- **Single static helper** in Infrastructure.Persistence (next to `TenantScope` and `TenantSafetyGuard`), so all execution modes are explicit and restorable in one place.
- **Three explicit entry points:** (1) run under tenant scope with known `companyId`, (2) run under platform bypass, (3) run from nullable `companyId` with explicit rule: has value and not empty → tenant scope; otherwise → platform bypass. No silent “default to platform” for ambiguous callers.
- **AsyncLocal-safe:** Helper always restores previous scope or exits bypass in `finally`, so exceptions and early returns do not leak state.
- **Cancellation:** Work delegates receive `CancellationToken` so callers can pass through cancellation without losing it inside the lambda.
- **Refactor scope:** Only four high-value, already “scope or bypass” call sites were refactored; job workers (BackgroundJobProcessorService, JobExecutionWorkerHostedService) and auth flows were left unchanged to avoid any behavior change for null-company paths and to limit risk.

---

## 3. New abstraction introduced

- **Name:** `TenantScopeExecutor`
- **Location:** `backend/src/CephasOps.Infrastructure/Persistence/TenantScopeExecutor.cs`
- **Type:** Static class in `CephasOps.Infrastructure.Persistence`.
- **Methods:**
  - `RunWithTenantScopeAsync(Guid companyId, Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)` and generic `<T>` — set scope, run work, restore in finally.
  - `RunWithPlatformBypassAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)` and generic `<T>` — enter bypass, run work, exit bypass in finally.
  - `RunWithTenantScopeOrBypassAsync(Guid? companyId, Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)` and generic `<T>` — if `companyId` has value and is not empty, run under tenant scope; else run under platform bypass; always restore/exit in finally.

---

## 4. Files changed

| File | Change |
|------|--------|
| `backend/src/CephasOps.Infrastructure/Persistence/TenantScopeExecutor.cs` | **New** — static executor with the three execution modes and XML docs (incl. reference to developer guide). |
| `backend/src/CephasOps.Application/Integration/InboundWebhookRuntime.cs` | Replaced manual previousTenantId/useBypass/try/finally with `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(request.CompanyId, …)`. |
| `backend/src/CephasOps.Application/Notifications/Services/NotificationRetentionService.cs` | Replaced manual scope/bypass and try/finally with `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyId, …)`; inner logic uses `ct` for cancellation. |
| `backend/src/CephasOps.Application/Events/Replay/EventReplayService.cs` | In `DispatchStoredEventAsync`, replaced manual previousTenantId/usedBypass/try/finally with `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, …)`. |
| `backend/src/CephasOps.Application/Events/EventStoreDispatcherHostedService.cs` | In `ProcessOneEventAsync`, replaced manual scope/bypass and try/finally with `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, …)`; inner code uses `ct` for cancellation. |
| `backend/tests/CephasOps.Application.Tests/Integration/TenantScopeExecutorTests.cs` | **New** — tests for tenant scope restore, platform bypass exit, nullable companyId (scope vs bypass), nested execution, and exception restoration. |
| `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md` | Added section 3.1 (TenantScopeExecutor as preferred pattern), updated section 4 table and quick reference, updated test filter to include TenantScopeExecutorTests. |
| `backend/docs/architecture/TENANT_SCOPE_EXECUTOR_VALIDATION_REPORT.md` | **New** — this report. |

---

## 5. Initial call sites refactored

1. **InboundWebhookRuntime** — `ProcessAsync`: single call to `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(request.CompanyId, (ct) => ProcessCoreAsync(request, endpoint, ct), cancellationToken)`.
2. **NotificationRetentionService** — `RunRetentionAsync`: single call to `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyId, async (ct) => { … }, cancellationToken)` with `ToListAsync(ct)` and `SaveChangesAsync(ct)`.
3. **EventReplayService** — `DispatchStoredEventAsync`: single call to `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, async (ct) => { … }, cancellationToken)`; replay context cleanup remains in outer finally.
4. **EventStoreDispatcherHostedService** — `ProcessOneEventAsync`: single call to `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, async (ct) => { … }, cancellationToken)`; concurrency release remains in outer finally.

**Intentionally not refactored in this pass:** BackgroundJobProcessorService, JobExecutionWorkerHostedService, AuthService, scheduler services — to avoid changing null-company or job-dispatch behavior and to keep the first rollout small and auditable.

---

## 6. Tests added/updated

- **New:** `TenantScopeExecutorTests` in `backend/tests/CephasOps.Application.Tests/Integration/TenantScopeExecutorTests.cs`, collection `TenantScopeTests`.
- **Cases:**  
  - `RunWithTenantScopeAsync_RestoresPreviousScope_AfterSuccess`  
  - `RunWithTenantScopeAsync_RestoresPreviousScope_AfterException`  
  - `RunWithTenantScopeAsync_ReturnsResult`  
  - `RunWithPlatformBypassAsync_ExitsBypass_AfterSuccess`  
  - `RunWithPlatformBypassAsync_ExitsBypass_AfterException`  
  - `RunWithTenantScopeOrBypassAsync_WhenCompanyIdSet_RunsUnderScopeAndRestores`  
  - `RunWithTenantScopeOrBypassAsync_WhenCompanyIdNull_RunsUnderBypassAndExits`  
  - `RunWithTenantScopeOrBypassAsync_WhenCompanyIdEmptyGuid_RunsUnderBypass`  
  - `Nested_TenantThenPlatform_RestoresOuterScope`  
  - `Nested_PlatformThenTenant_RestoresBypassThenScope`
- **Existing tests:** InboundWebhookRuntimeTenantScopeTests, NotificationRetentionServiceTests (tenant-scope behavior), EventReplayServiceTenantScopeTests — all still pass; they validate the refactored services behave the same.

---

## 7. Validation performed

- **Build:** `dotnet build` for CephasOps.Application.Tests (and dependencies) — succeeded.
- **Test run:**  
  `dotnet test --no-build --filter "FullyQualifiedName~TenantScopeExecutorTests|FullyQualifiedName~InboundWebhookRuntimeTenantScope|FullyQualifiedName~NotificationRetentionService|FullyQualifiedName~EventReplayServiceTenantScope"`  
  **Result:** 21 tests passed (10 TenantScopeExecutorTests + 2 InboundWebhookRuntime + 6 NotificationRetentionService + 2 EventReplayService tenant-scope tests).
- **Behavior:** No change to business logic; only the mechanism for setting/restoring scope and bypass was centralized. Fail-closed semantics for nullable companyId (empty or null → platform bypass) unchanged.

---

## 8. Remaining rollout recommendations

- **Use TenantScopeExecutor** for any **new** code that must run under tenant scope or platform bypass (hosted services, dispatchers, replay, webhooks, retention, auth blocks that set scope).
- **Gradual migration:** When touching BackgroundJobProcessorService, JobExecutionWorkerHostedService, AuthService, or scheduler services, consider refactoring to the executor **only** where the existing behavior is clearly “per-tenant scope” or “platform bypass” and the nullable-company behavior is intentionally unchanged.
- **Do not** change semantics: for paths that today treat null/empty company as “skip” or “early return,” keep that behavior; do not replace with executor and then implicitly run as platform unless that was already the intent.
- **Document** any new call site in the developer guide table (section 4) if it adds a new category of usage.
- **PR checklist:** Keep the existing tenant-safety checklist; add “Prefer TenantScopeExecutor over manual try/finally for scope and bypass” where applicable (see developer guide section 6).

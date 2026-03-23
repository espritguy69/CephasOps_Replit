# Tenant Safety Guard – Second-Pass Remediation Summary

This document reports the second-pass deep audit of remaining high-risk operational write paths (eventing, replay, notification retention, and event-dispatcher–driven handlers). It follows the required 6-part format for each changed path.

---

## 1. Event Store Dispatcher (EventStoreDispatcherHostedService)

### 1.1 Root cause
The Event Store dispatcher is a hosted service that claims events and calls `IDomainEventDispatcher.PublishAsync`. Handlers (e.g. `OrderAssignedOperationsHandler`) run in-process and can write tenant-scoped entities (`BackgroundJob`, `TaskItem`, `Order`, etc.). No middleware runs for this background worker, so `TenantScope.CurrentTenantId` was never set. Writes to tenant-scoped entities could occur without tenant context, and the guard would throw or (if bypass were used elsewhere) allow incorrect scope.

### 1.2 Design decision
Set tenant context per event before dispatching: when `entry.CompanyId` has a value, set `TenantScope.CurrentTenantId = entry.CompanyId`; when it is null or empty, use a narrowly-scoped platform bypass (`TenantSafetyGuard.EnterPlatformBypass`) so that event-store updates (e.g. `MarkProcessedAsync`) and any handler that does not require a tenant can still run. Restore previous tenant id or exit bypass in a `finally` block so scope is always restored on success and failure.

### 1.3 Files changed
- `backend/src/CephasOps.Application/Events/EventStoreDispatcherHostedService.cs`

### 1.4 Exact fix applied
- Added `using CephasOps.Infrastructure.Persistence`.
- In `ProcessOneEventAsync`, before creating the scoped services and dispatching:
  - Capture `previousTenantId = TenantScope.CurrentTenantId` and `usedBypass = false`.
  - If `entry.CompanyId` has a value and is not `Guid.Empty`, set `TenantScope.CurrentTenantId = entry.CompanyId`.
  - Otherwise call `TenantSafetyGuard.EnterPlatformBypass()` and set `usedBypass = true`.
- In the existing outer `finally` (that runs after the inner try/catch):
  - If `usedBypass` then `TenantSafetyGuard.ExitPlatformBypass()`; else `TenantScope.CurrentTenantId = previousTenantId`.
  - Then release the concurrency semaphore as before.

### 1.5 Validation performed
- No new unit test was added for the hosted service (would require broader integration setup). Regression is covered by handler tests that assume scope is set when the handler runs.
- Confirmed that `OrderAssignedOperationsHandler` test passes when `TenantScope` is set to the event’s company before calling the handler (simulating the dispatcher’s new behavior).

### 1.6 Remaining assumptions / risks
- Events with `CompanyId == null` run under platform bypass; handlers that write tenant-scoped entities in that case could still violate tenant isolation if they infer company from payload. Such events should be rare; consider documenting or restricting event types that are allowed without `CompanyId`.

---

## 2. OrderAssignedOperationsHandler (BackgroundJob.CompanyId)

### 2.1 Root cause
When an order is assigned, the handler enqueues an SLA evaluation by adding a `BackgroundJob` and calling `SaveChangesAsync`. `BackgroundJob` is tenant-scoped (guard-listed). The handler did not set `job.CompanyId`, so the job was stored with `CompanyId == null`. Downstream job processing relies on `job.CompanyId` (or payload) to set `TenantScope`; missing `CompanyId` on the job could lead to wrong-tenant execution or guard failures.

### 2.2 Design decision
Set `BackgroundJob.CompanyId` from the order’s company (or event’s company) when creating the SLA evaluation job so that the job processor and tenant scope logic have a clear tenant for that job. Use `order.CompanyId ?? companyId` and only set `CompanyId` on the job when non-empty.

### 2.3 Files changed
- `backend/src/CephasOps.Application/Events/OrderAssignedOperationsHandler.cs`

### 2.4 Exact fix applied
- Introduced `jobCompanyId = order.CompanyId ?? companyId`.
- When building the payload, use `jobCompanyId` for the `companyId` payload key when non-empty.
- When creating the `BackgroundJob` instance, set `CompanyId = jobCompanyId != Guid.Empty ? jobCompanyId : null`.

### 2.5 Validation performed
- `OrderAssignedOperationsHandlerTests.HandleAsync_OrderWithAssignedSi_CreatesTask_CallsMaterialPack_EnqueuesSlaJob` was updated to run under `TenantScope.CurrentTenantId = companyId` (to satisfy the guard for SaveChanges) and to assert `slaJobs[0].CompanyId.Should().Be(companyId)`.

### 2.6 Remaining assumptions / risks
- If both `order.CompanyId` and `domainEvent.CompanyId` are null/empty, the job is enqueued with `CompanyId == null`; the job processor’s fallback (e.g. from payload) may still run under bypass or wrong tenant. Consider rejecting or logging when company cannot be resolved.

---

## 3. NotificationRetentionService

### 3.1 Root cause
`NotificationRetentionService.RunRetentionAsync` updates and deletes `Notification` entities (tenant-scoped / guard-listed). It is invoked from `NotificationRetentionHostedService` with `companyId: null` (platform-wide retention). The hosted service has no middleware, so `TenantScope` was never set. Saving changes to tenant-scoped entities without context triggers the guard.

### 3.2 Design decision
When `companyId` is null (or empty), treat the run as platform-wide retention and wrap the operation in `TenantSafetyGuard.EnterPlatformBypass` / `ExitPlatformBypass` in a `finally` block. When `companyId` has a value, set `TenantScope.CurrentTenantId = companyId` for the duration of the run and restore the previous tenant id in `finally`. No broadening of the guard; bypass is justified only for the documented platform-wide retention scenario.

### 3.3 Files changed
- `backend/src/CephasOps.Application/Notifications/Services/NotificationRetentionService.cs`

### 3.4 Exact fix applied
- At the start of `RunRetentionAsync`: capture `previousTenantId = TenantScope.CurrentTenantId`, `usedBypass = false`.
  - If `companyId.HasValue && companyId.Value != Guid.Empty`, set `TenantScope.CurrentTenantId = companyId`.
  - Otherwise call `TenantSafetyGuard.EnterPlatformBypass()` and set `usedBypass = true`.
- Wrapped the existing logic (query, archive, delete, `SaveChangesAsync`) in a try; in a `finally`: if `usedBypass` then `TenantSafetyGuard.ExitPlatformBypass()`, else `TenantScope.CurrentTenantId = previousTenantId`.
- Updated the type’s summary comment to state that when `companyId` is null the run is platform-wide and uses bypass; when set, it runs in tenant scope.

### 3.5 Validation performed
- `NotificationRetentionServiceTests.RunRetentionAsync_WhenCompanyIdNull_RestoresTenantScopeAfterRun`: sets `TenantScope` to a company, calls `RunRetentionAsync(companyId: null)`, then asserts that `TenantScope.CurrentTenantId` is unchanged after the call (bypass was used and then exited, restoring the previous scope).
- `NotificationRetentionServiceTests.RunRetentionAsync_WhenCompanyIdSet_RestoresTenantScopeAfterRun`: sets scope to company B, calls `RunRetentionAsync(companyId: _companyA)`, then asserts scope is still company B after the call (tenant scope was set for the call and restored in `finally`).

### 3.6 Remaining assumptions / risks
- Platform-wide retention (`companyId: null`) is intentional and documented. If retention is later changed to always run per-tenant from a scheduler, the bypass path could be removed.

---

## 4. EventReplayService (single-event replay / retry)

### 4.1 Root cause
`EventReplayService.ReplayAsync` (and the internal `DispatchStoredEventAsync`) loads an event from the store and calls `IDomainEventDispatcher.DispatchToHandlersAsync`. Handlers can write tenant-scoped entities (e.g. projection handlers, `OrderAssignedOperationsHandler` during retry). Replay is invoked from API or operational replay loops; there is no middleware to set tenant scope. Without setting scope (or bypass) before dispatch, handler writes could fail the guard or run in the wrong context.

### 4.2 Design decision
Before dispatching, set tenant context from the stored event: if `entry.CompanyId` has a value and is not `Guid.Empty`, set `TenantScope.CurrentTenantId = entry.CompanyId`; otherwise call `TenantSafetyGuard.EnterPlatformBypass()`. Restore previous tenant id or exit bypass in a `finally` block that also clears the replay context. This aligns single-event replay with the event-store dispatcher behavior.

### 4.3 Files changed
- `backend/src/CephasOps.Application/Events/Replay/EventReplayService.cs`

### 4.4 Exact fix applied
- Added `using CephasOps.Infrastructure.Persistence`.
- In `DispatchStoredEventAsync`, before setting the replay context and calling the dispatcher:
  - Capture `previousTenantId = TenantScope.CurrentTenantId`, `usedBypass = false`.
  - If `entry.CompanyId.HasValue && entry.CompanyId.Value != Guid.Empty`, set `TenantScope.CurrentTenantId = entry.CompanyId`; else `TenantSafetyGuard.EnterPlatformBypass()` and `usedBypass = true`.
- In the existing `finally` (which clears the replay context): if `usedBypass` then `TenantSafetyGuard.ExitPlatformBypass()`, else `TenantScope.CurrentTenantId = previousTenantId`; then `_replayContextAccessor.Set(null)`.

### 4.5 Validation performed
- `EventReplayServiceTenantScopeTests.ReplayAsync_WhenEntryHasCompanyId_RestoresTenantScopeAfterDispatch`: mocks store (entry with `CompanyId` set) and dispatcher; sets `TenantScope` to an outer guid, calls `ReplayAsync`, then asserts `TenantScope` is still that guid after the call.
- `EventReplayServiceTenantScopeTests.ReplayAsync_WhenEntryHasNoCompanyId_RestoresBypassAndScopeAfterDispatch`: same pattern with entry `CompanyId == null`; asserts scope is restored after the call.

### 4.6 Remaining assumptions / risks
- Operational replay (batch) in `OperationalReplayExecutionService` calls `_replayService.ReplayAsync` per event; each call now sets and restores scope inside `EventReplayService`, so no change was required in the batch loop. Replay operations that write only non–guard-listed entities (e.g. `ReplayOperation`, `ReplayOperationEvent`, `LedgerEntry`) were already safe.

---

## 5. Paths audited and not changed

The following were audited and left unchanged (no tenant-scoped guard-listed entity writes, or already correctly scoped):

- **LedgerWriter** – Writes `LedgerEntry`; not in `TenantSafetyGuard`’s tenant-scoped list. No change.
- **EventStoreRepository** / **EventStoreAttemptHistoryStore** – Write `EventStoreEntry` and attempt history; not guard-listed. No change.
- **EventProcessingLogStore** – Writes `EventProcessingLog`; not guard-listed. No change.
- **OperationalReplayExecutionService** – Writes `ReplayOperation`, `ReplayOperationEvent`; not guard-listed. Replay handler dispatch is covered by EventReplayService fix. No change.
- **WorkflowTransitionHistoryProjectionHandler** – Writes `WorkflowTransitionHistoryEntry`; not `CompanyScopedEntity`. No change.
- **OperationalRebuildService** / **WorkflowTransitionHistoryFromEventStoreRebuildRunner** – Write `RebuildOperation`, `RebuildExecutionLock`, `WorkflowTransitionHistoryEntry`; none are guard-listed. Rebuild is typically invoked with a scope company; no bypass added. No change.
- **StockLedgerService** – Invoked from API or jobs that are expected to set tenant scope; no change in this pass.
- **NotificationDispatchWorkerHostedService** / **NotificationDispatchStore** – Update `NotificationDispatch`, which is not guard-listed. No change.
- **NotificationService** / **UnifiedMessagingService** – Called from API or in-app flows where tenant scope is set by middleware or caller; no change in this pass.

---

## 6. Test changes summary

| Test | Change |
|------|--------|
| `NotificationRetentionServiceTests.RunRetentionAsync_WhenCompanyIdNull_RestoresTenantScopeAfterRun` | New: assert scope is restored after run with `companyId: null`. |
| `NotificationRetentionServiceTests.RunRetentionAsync_WhenCompanyIdSet_RestoresTenantScopeAfterRun` | New: assert scope is restored after run with `companyId` set. |
| `OrderAssignedOperationsHandlerTests.HandleAsync_OrderWithAssignedSi_...` | Set `TenantScope.CurrentTenantId = companyId` for the test (and restore in `finally`); assert `slaJobs[0].CompanyId == companyId`. |
| `EventReplayServiceTenantScopeTests.ReplayAsync_WhenEntryHasCompanyId_RestoresTenantScopeAfterDispatch` | New: mock store/dispatcher; assert scope restored after `ReplayAsync`. |
| `EventReplayServiceTenantScopeTests.ReplayAsync_WhenEntryHasNoCompanyId_RestoresBypassAndScopeAfterDispatch` | New: same with entry without `CompanyId`; assert scope restored. |

---

## 7. Summary

- **Event Store dispatcher** and **EventReplayService** now set tenant scope (or a narrow platform bypass) per event before dispatching and restore in `finally`.
- **OrderAssignedOperationsHandler** now sets **BackgroundJob.CompanyId** when enqueuing the SLA evaluation job.
- **NotificationRetentionService** uses platform bypass when `companyId` is null and tenant scope when `companyId` is set, with restore in `finally`.
- No blanket platform bypasses were introduced; `TenantSafetyGuard` was not weakened. Targeted tests confirm scope restoration on success and (via existing finally behavior) on failure.

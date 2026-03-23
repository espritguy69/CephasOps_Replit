# EventStore Consistency Guard

**Date:** 2026-03-13  
**Purpose:** Platform hardening for event-driven consistency: duplicate processing, replay drift, partial state, wrong-tenant event access, and retry/replay safety. Evidence-based audit and minimal safe guards without weakening tenant isolation or changing business meaning of events.

---

## 1. Audited event paths and current model

### 1.1 Event identification and append

| Item | Finding |
|------|--------|
| **Unique identification** | `EventId` (Guid) is the primary key of `EventStoreEntry`. Each row is uniquely identified by `EventId`. `IdempotencyKey` exists on the entity but is not used for append deduplication (no unique index; optional caller use). |
| **Duplicate append (same EventId)** | **Protected.** Append path now checks for existing `EventId` before add; `RequireDuplicateAppendRejected` throws with a clear message. DB PK would also reject on insert. |
| **Duplicate append (same logical event, new EventId)** | **Not enforced.** If a caller generates a new `EventId` for the same logical operation, two rows can be created. Mitigation: callers can set `IdempotencyKey` and implement their own dedup; no schema change for global IdempotencyKey uniqueness in this phase. |

### 1.2 Append / write services

- **EventStoreRepository.AppendAsync** — Guard: `RequireTenantOrBypassForAppend`, duplicate-EventId check and `RequireDuplicateAppendRejected`, then `AppendInCurrentTransaction`; parent/root load and `RequireParentRootCompanyMatch`; stream consistency; `SaveChangesAsync`.
- **EventStoreRepository.AppendInCurrentTransaction** — Guard: tenant-or-bypass, `RequireEventMetadata`, `RequireCompanyWhenEntityContext`, `RequireValidParentRootLinkage`, same-transaction stream consistency; then `Add(entry)`.
- **DomainEventDispatcher** — Calls `IEventStore.AppendAsync` when `!alreadyStored`; runs in caller’s tenant scope (API or job under TenantScopeExecutor).

### 1.3 Event replay and dispatch

- **EventStoreDispatcherHostedService** — Claims batches (platform-wide); each event processed under `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`. Dispatch uses `alreadyStored: true`; no second append.
- **EventReplayService (Retry/Replay/Requeue)** — Loads entry by `GetByEventIdAsync`; when `scopeCompanyId.HasValue` enforces `entry.CompanyId == scopeCompanyId` and returns "Event not in scope." on mismatch (with structured warning log). Dispatches under `RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`. Single-event retry/replay sets `ReplayExecutionContext.ForSingleEventRetry` with `SuppressSideEffects = true`.
- **OperationalReplayExecutionService** — Replay runs with `scopeCompanyId`; `GetEventsForReplayAsync` filters by company; each event replayed via `EventReplayService.ReplayAsync(e.EventId, scopeCompanyId, ...)`. Replay context sets `SuppressSideEffects = true` so async handlers are not enqueued.

### 1.4 Job execution and event handling

- **EventHandlingAsyncJobExecutor** — Loads event by `GetByEventIdAsync(eventId)`. **Guard added:** when `job.CompanyId` and `entry.CompanyId` are both set and differ, throws `InvalidOperationException` and logs with `GuardReason=TenantMismatch`. Ensures a job scoped to one tenant cannot process another tenant’s event.

### 1.5 Trace / timeline

- **TraceQueryService** — `GetByEventIdAsync`, `GetByJobRunIdAsync`, `GetByWorkflowJobIdAsync`, `GetByEntityAsync` all filter by `scopeCompanyId` when provided. No cross-tenant timeline exposure for tenant-scoped calls.
- **EventStoreQueryService.GetByEventIdAsync** — Uses repository `GetByEventIdAsync` then returns `null` when `scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId`. Tenant-safe at service layer.

### 1.6 Read path tenant safety

- **IEventStore.GetByEventIdAsync** — No tenant filter (store is platform-wide for dispatch/claim). All callers that need tenant safety either pass `scopeCompanyId` and check after load (EventReplayService, EventStoreQueryService, TraceQueryService) or run in a context where the event was already selected by company (e.g. operational replay). **EventHandlingAsyncJobExecutor** now enforces job company vs event company match before processing.

---

## 2. Consistency risks found and classification

| Risk | Classification | Mitigation |
|------|----------------|------------|
| Duplicate append (same EventId) | **Critical** → addressed | RequireDuplicateAppendRejected in AppendAsync; clear exception and log. |
| Wrong-tenant event replayed when scope provided | **High** → addressed | EventReplayService and Requeue path check `entry.CompanyId` vs `scopeCompanyId`; return "Event not in scope." and log with GuardReason=TenantMismatch. |
| Async job processes another tenant’s event | **High** → addressed | EventHandlingAsyncJobExecutor verifies `job.CompanyId` vs `entry.CompanyId`; throws and logs on mismatch. |
| Replay re-runs non-idempotent side effects | **High** → partially addressed | Replay uses SuppressSideEffects so async handlers are not enqueued. Sync handlers still run; invoice/payment flows use idempotency keys where set. Documented as residual risk for sync handlers without idempotency. |
| IdempotencyKey not enforced on append | **Medium** → documented only | No unique index on (CompanyId, IdempotencyKey); no append-time dedup by key. Caller responsibility. |
| GetByEventIdAsync returns any event by ID | **Medium** → acceptable | All production callers that need tenant safety enforce it after load or via executor company check. |

---

## 3. Protections added (minimal safe guards)

1. **Duplicate event append**  
   In `EventStoreRepository.AppendAsync`, before `AppendInCurrentTransaction`: if an event with the same `EventId` already exists, call `EventStoreConsistencyGuard.RequireDuplicateAppendRejected(eventId, companyId, _logger)` which logs (PlatformGuardLogger + optional repository logger) and throws `InvalidOperationException` with message "Duplicate event append. EventId=... already exists in the store."

2. **Replay / requeue tenant mismatch**  
   In `EventReplayService`, when `scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId`: log structured warning (`EventId`, `EventCompanyId`, `ScopeCompanyId`, `Operation=Replay` or `Requeue`, `GuardReason=TenantMismatch`) and return "Event not in scope." (no throw; same API contract).

3. **Async event-handling job tenant mismatch**  
   In `EventHandlingAsyncJobExecutor`, after loading `entry`: if `job.CompanyId.HasValue && entry.CompanyId.HasValue && job.CompanyId != entry.CompanyId`, log structured warning and throw `InvalidOperationException` so the job fails and does not process the event.

4. **Replay side-effect suppression observability**  
   In `DomainEventDispatcher`, when `SuppressSideEffects` is true and async handlers are skipped: log with `Operation=ReplaySuppressSideEffects`, `EventId`, `EventType`, `CompanyId`, `GuardReason=SuppressSideEffects`, `AsyncHandlerCount`.

---

## 4. Replay safeguards (existing + verified)

- **Single-event Retry/Replay** — `ReplayExecutionContext.ForSingleEventRetry` sets `SuppressSideEffects = true`. Async handlers are not enqueued; only sync handlers run. Reduces duplicate async jobs and duplicate outbound/integration work when replayed.
- **Operational replay** — Uses same context with `SuppressSideEffects = true`; event set is filtered by `scopeCompanyId` and request filters; replay runs under `entry.CompanyId` per event.
- **Handler-level idempotency** — When `IEventProcessingLogStore` is available, **sync** handler execution uses `TryClaimAsync` so each handler runs at most once per (EventId, HandlerName) (completed handlers are skipped on replay). Async handler execution also uses the processing log in the async job executor.
- **Financial idempotency** — Invoice and payment creation support `IdempotencyKey` (e.g. order-invoice, email-payment); replay that re-runs sync handlers will not create duplicate invoices/payments when keys are set.
- **Sync handler replay guards** — See §11 for inventory and replay-unsafe handler guarding.

---

## 5. Tenant safety verification for events

- **Append:** Requires tenant or platform bypass; parent/root and stream consistency enforce same-company linkage.
- **Replay (API):** Controllers pass `ScopeCompanyId()`; replay service rejects when `entry.CompanyId != scopeCompanyId` and logs.
- **Dispatcher:** Each event processed under `entry.CompanyId` via TenantScopeExecutor.
- **Async job:** Executor refuses to process when `job.CompanyId` and `entry.CompanyId` differ.
- **Trace / query:** All tenant-facing APIs pass `scopeCompanyId` and filter; wrong-tenant event not returned.

---

## 6. Tests added

- **EventStoreRepositoryConsistencyTests.AppendAsync_DuplicateEventId_ThrowsBeforeSave** — Second append with same EventId throws with message containing "Duplicate event append".
- **EventStoreConsistencyGuardTests.RequireDuplicateAppendRejected_Throws** — Guard throws with expected message.
- **EventReplayServiceTenantScopeTests.ReplayAsync_WhenScopeCompanyIdDoesNotMatchEntry_ReturnsNotInScope** — Replay returns Success=false, ErrorMessage="Event not in scope." when scope company ≠ event company.
- **EventReplayServiceTenantScopeTests.RetryAsync_WhenScopeCompanyIdDoesNotMatchEntry_ReturnsNotInScope** — Same for RetryAsync.
- **EventHandlingAsyncJobExecutorEventConsistencyTests.ExecuteAsync_WhenEventCompanyIdDoesNotMatchJobCompanyId_Throws** — Executor throws when job company ≠ event company.
- **OrderAssignedOperationsHandlerTests.HandleAsync_WhenReplayContextActive_DoesNotEnqueueSlaJob** — When replay context is set, handler does not enqueue SLA evaluation job (replay does not create duplicate side effect).

---

## 7. Observability

Structured logs use consistent fields where applicable:

- **tenantId / CompanyId / EventCompanyId / ScopeCompanyId / JobCompanyId**
- **eventId / EventId**
- **operation** — e.g. Append, Replay, Requeue, EventHandlingAsync, ReplaySuppressSideEffects
- **guardReason** — e.g. DuplicateEventId, TenantMismatch, SuppressSideEffects

PlatformGuardLogger is used for duplicate-append violation (EventStoreConsistencyGuard). Application logs are used for replay/requeue tenant mismatch and async job tenant mismatch and for replay side-effect suppression.

---

## 8. Residual limitations

- **IdempotencyKey** — Not enforced at append (no unique constraint or short-circuit). Duplicate logical events with different EventIds can still be appended if callers do not enforce idempotency.
- **Sync handlers on replay** — When `IEventProcessingLogStore` is in use, sync handlers that have already completed for an event are skipped on replay (TryClaimAsync returns false). When a handler does run (e.g. retry after failure or first replay), only handlers that are idempotent or explicitly guarded avoid duplicate effects. OrderAssignedOperationsHandler is guarded (SLA enqueue skipped during replay); other sync handlers are either pure, projection/ledger (idempotent), or use downstream idempotency (invoice, notification, integration). See §11.
- **GetByEventIdAsync** — Repository method has no tenant filter; safety is enforced by callers and by EventHandlingAsyncJobExecutor company check.

---

## 9. Verdict

EventStore consistency is **strengthened** for:

- Duplicate event append (same EventId): **detected and rejected** with clear exception and logging.
- Wrong-tenant replay/requeue: **blocked** when scope is provided; **logged** for observability.
- Wrong-tenant async event handling: **blocked** by executor company check; job fails with clear exception.
- Replay side-effect suppression: **unchanged** (already in place); **observability added** for when async handlers are skipped.
- **Sync handler replay safety:** All sync handlers inventoried and classified; the one replay-unsafe side effect (OrderAssignedOperationsHandler SLA job enqueue) is guarded (skipped when `IsReplay`); other handlers are pure, idempotent, or use downstream idempotency. See §11.

No tenant isolation weakened; no broad `IgnoreQueryFilters()`; no schema or migration; no change to business meaning of events. Remaining risks (IdempotencyKey at append, any future sync handler without idempotency/guard) are documented and accepted.

---

## 10. Related documents

- [EVENTSTORE_CONSISTENCY_GUARD_REPORT.md](EVENTSTORE_CONSISTENCY_GUARD_REPORT.md) — Original append-path guard (tenant, metadata, parent/root, stream).
- [SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md](../architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md) — Overall safety layers.
- [SAAS_REMEDIATION_CHANGELOG.md](../remediation/SAAS_REMEDIATION_CHANGELOG.md) — Changelog entry for EventStore consistency hardening and sync handler replay safety.

---

## 11. Sync handler replay safety (Phase 2)

### 11.1 Inventory and classification

All synchronous event handlers (in-process, non–IAsyncEventSubscriber) that run during replay were audited. Classification:

| Handler | Event(s) | Classification | Replay-safe? |
|---------|----------|----------------|--------------|
| WorkflowTransitionCompletedEventHandler | WorkflowTransitionCompleted | Pure/read-only (logs only) | Yes |
| WorkflowTransitionHistoryProjectionHandler | WorkflowTransitionCompleted | State-mutating, idempotent by EventId (upsert) | Yes |
| WorkflowTransitionLedgerHandler | WorkflowTransitionCompleted | Ledger append; idempotent by (SourceEventId, Family) | Yes |
| OrderLifecycleLedgerHandler | OrderStatusChanged | Ledger append; idempotent by (SourceEventId, Family) | Yes |
| OrderStatusNotificationDispatchHandler | OrderStatusChanged | Side-effecting; downstream uses sourceEventId in idempotency key | Yes |
| IntegrationEventForwardingHandler | Multiple | Side-effecting; outbound bus idempotent by (EventId, EndpointId) | Yes |
| OrderCompletedAutomationHandler | OrderCompleted | State-mutating; IdempotencyKey + order.InvoiceId check; CreateInvoice idempotent | Yes |
| OrderCompletedInsightHandler | OrderCompleted | State-mutating; exists check by (CompanyId, Type, EntityType, EntityId) before insert | Yes |
| OrderAssignedOperationsHandler | OrderAssigned | Task creation idempotent by OrderId; material pack read-only; **SLA job enqueue not idempotent** | **Guarded** |

### 11.2 Guard added

- **OrderAssignedOperationsHandler** — Injects `IReplayExecutionContextAccessor` (optional). When `Current?.IsReplay == true`, skips the SLA evaluation job enqueue block and returns after task creation and material pack refresh. Logs with `Operation=OrderAssignedOperationsHandler`, `GuardReason=ReplaySkipSlaEnqueue`, `EventId`, `OrderId`. Prevents duplicate SLA evaluation jobs when the same event is replayed.

### 11.3 Handlers not guarded (replay-safe by design)

All other sync handlers are either pure, idempotent (projection/ledger by EventId or SourceEventId), or rely on downstream idempotency (notification dispatch, integration outbound, invoice creation, insight exists-check). No broad disable of sync handlers.

### 11.4 Residual replay risks

- **Processing log** — When `IEventProcessingLogStore` is not available, sync handlers run on every replay; all such handlers are either idempotent or guarded.
- **New sync handlers** — Any new sync handler that performs non-idempotent side effects should either use idempotency (key or processing log) or check `IReplayExecutionContextAccessor.Current?.IsReplay` and skip or no-op when true.

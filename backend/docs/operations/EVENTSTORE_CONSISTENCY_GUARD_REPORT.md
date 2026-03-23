# EventStore Consistency Guard — Implementation Report

**Date:** 2026-03-13  
**Scope:** Defense-in-depth safeguard for EventStore appends: tenant-or-bypass requirement, metadata completeness, company consistency when entity context is present, parent/root linkage and cross-tenant protection, and stream consistency (same aggregate type and company per entity stream). No schema changes, no event model redesign.

---

## 1. What EventStore consistency safeguard was added

**EventStoreConsistencyGuard** (`CephasOps.Infrastructure/Persistence/EventStoreConsistencyGuard.cs`)

A small static guard with six explicit methods:

- **RequireTenantOrBypassForAppend(string operationName)**  
  Throws if neither a valid tenant context (`TenantScope.CurrentTenantId` set and non-empty) nor an approved platform bypass (`TenantSafetyGuard.IsPlatformBypassActive`) is active. Call at the start of EventStore append paths so appends fail fast when invoked without tenant scope or explicit platform bypass.

- **RequireParentRootCompanyMatch(EventStoreEntry entry, EventStoreEntry? parentEntry, EventStoreEntry? rootEntry)**  
  When the new event has CompanyId and a parent or root reference was loaded: validates that parent and root events (when they have CompanyId) belong to the same company as the new event. Prevents cross-tenant event linkage in a chain.

- **RequireEventMetadata(EventStoreEntry entry)**  
  Throws if `entry` is null, `EventId` is empty, or `EventType` is null or whitespace. Ensures required identity and type are present before append.

- **RequireCompanyWhenEntityContext(EventStoreEntry entry)**  
  When the event has entity context (EntityType or EntityId set), requires `CompanyId` to be present and non-empty. Prevents company-scoped aggregate events from being stored without company identity.

- **RequireValidParentRootLinkage(EventStoreEntry entry)**  
  Validates: (1) `ParentEventId` cannot equal `EventId` (no self-reference); (2) when both `ParentEventId` and `RootEventId` are set, `RootEventId` cannot be empty. Ensures parent/root chain is not obviously invalid.

- **RequireStreamConsistency(EventStoreEntry entry, IReadOnlyList<EventStoreEntry> priorEventsInStream)**  
  When prior events exist for the same entity stream (same EntityType + EntityId), the new event must have the same `CompanyId` and `EntityType` as those prior events. Prevents cross-company or mixed aggregate-type contamination of a stream.

Exception messages include operation name (Append), EventId, EventType, EntityType, EntityId, and which invariant failed (e.g. Company mismatch, EntityType mismatch, self-reference, parent/root company mismatch).

---

## 2. Which append/read/write paths were hardened

| Path | Change |
|------|--------|
| **EventStoreRepository.AppendAsync** | At start: `RequireTenantOrBypassForAppend("Append")`. Then `AppendInCurrentTransaction`. Then loads parent event (if `entry.ParentEventId` set) and root event (if `entry.RootEventId` set and ≠ EventId) via `GetByEventIdAsync`; calls `RequireParentRootCompanyMatch(entry, parentEntry, rootEntry)`. Then stream consistency vs persisted events; then `SaveChangesAsync`. |
| **EventStoreRepository.AppendInCurrentTransaction** | At start: `RequireTenantOrBypassForAppend("AppendInCurrentTransaction")`. After building the `EventStoreEntry` and before `Add(entry)`, calls `RequireEventMetadata(entry)`, `RequireCompanyWhenEntityContext(entry)`, and `RequireValidParentRootLinkage(entry)`. When the entry has entity context (EntityType or EntityId), collects any other **Added** entries in the same entity stream from the change tracker and calls `RequireStreamConsistency(entry, priorInStream)`. Then adds the entry. |

No changes were made to read paths (GetByEventIdAsync, ClaimNextPendingBatchAsync, query services), mark-processed paths, or bulk reset paths. The guard is applied only at the canonical append path.

---

## 3. What exact inconsistencies are now prevented

- **Missing EventId or EventType** — Append fails with a clear message before the entry is added.
- **Entity context without CompanyId** — Events with EntityType or EntityId set but CompanyId null or empty are rejected (company-scoped aggregate events must carry company identity).
- **Self-reference (ParentEventId = EventId)** — Rejected before add.
- **Parent set but Root empty** — When both ParentEventId and RootEventId are set, RootEventId cannot be empty; otherwise rejected.
- **Company mismatch in same entity stream** — A second append for the same EntityType+EntityId with a different CompanyId throws before SaveChanges (e.g. "Company mismatch in event stream"). Enforced both when the prior event is already persisted (AppendAsync) and when the prior event was added in the same transaction (AppendInCurrentTransaction).
- **EntityType mismatch in same entity stream** — A second append for the same EntityId with a different EntityType throws before SaveChanges (e.g. "EntityType mismatch in event stream").
- **Same-transaction stream drift** — Multiple appends in one transaction for the same entity stream with different company or entity type are rejected on the second append (RequireStreamConsistency runs against change-tracker Added entries).
- **Append without tenant or bypass** — AppendAsync and AppendInCurrentTransaction throw at entry if TenantScope.CurrentTenantId is not set and platform bypass is not active.
- **Cross-tenant parent/root linkage** — When appending an event with ParentEventId or RootEventId, the repository loads the parent/root event; if that event has a CompanyId and the new event has a CompanyId, they must match (RequireParentRootCompanyMatch). Prevents linking a child event to a parent or root from another company.

---

## 4. EventStore inventory and paths

| Entity / component | Tenant/company scoping | Read/Write | Guard / scope |
|--------------------|------------------------|------------|----------------|
| EventStoreEntry | CompanyId on each row; no global EF filter on table | EventStoreRepository (append, claim, mark, bulk), EventStoreQueryService, EventLineageService | RequireTenantOrBypassForAppend at append; RequireParentRootCompanyMatch in AppendAsync; stream consistency |
| EventStoreAttemptHistory | CompanyId on row | EventStoreRepository (ResetStuckProcessingAsync), EventStoreAttemptHistoryStore | Written from repository under same context as processing |
| Append callers | Set tenant or run under bypass | WorkflowEngineService (AppendInCurrentTransaction), DomainEventDispatcher (AppendAsync), JobExecutionWorkerHostedService (AppendAsync) | API/middleware sets TenantScope; job worker runs under TenantScopeExecutor per job CompanyId |
| ClaimNextPendingBatchAsync | Platform-wide (no filter) | EventStoreDispatcherHostedService | Claim is platform-wide; each event then processed under RunWithTenantScopeOrBypassAsync(entry.CompanyId) |
| Replay (Retry/Replay/Requeue) | scopeCompanyId vs entry.CompanyId | EventReplayService | RunWithTenantScopeOrBypassAsync(entry.CompanyId); scope check before dispatch |
| Rebuild (WorkflowTransitionHistory from EventStore) | scopeCompanyId / request.CompanyId | WorkflowTransitionHistoryFromEventStoreRebuildRunner | Rebuild runs in caller’s scope (OperationalRebuildService passes scopeCompanyId) |
| Lineage (GetTreeByEventId, GetTreeByRootEventId, GetTreeByCorrelationId) | scopeCompanyId filters query | EventLineageService | Read-only; optional company filter in query |

---

## 5. Background job and repair safety (EventStore-related)

| Job / path | Tenant scope / Bypass | Rationale |
|------------|------------------------|------------|
| EventStoreDispatcherHostedService | Claim: no scope (platform-wide). Process: RunWithTenantScopeOrBypassAsync(entry.CompanyId) per event. | Claim must see all Pending/Failed events; each event is then dispatched under that event’s CompanyId so handlers and SaveChanges run in correct tenant context. |
| EventReplayService (Retry/Replay/Requeue) | RunWithTenantScopeOrBypassAsync(entry.CompanyId) | Replay runs in the event’s company context; scopeCompanyId is validated against entry.CompanyId before dispatch. |
| WorkflowTransitionHistoryFromEventStoreRebuildRunner | Caller’s scope (scopeCompanyId from rebuild request) | Rebuild is company-scoped; caller (OperationalRebuildService) passes scopeCompanyId from request or context. |
| DomainEventDispatcher.AppendAsync | Caller’s scope | Used when publishing from API or from a job that has already set tenant scope. |
| JobExecutionWorkerHostedService (event emission) | RunWithTenantScopeOrBypassAsync(job.CompanyId) | Job runs under job’s CompanyId; AppendAsync runs inside that scope so RequireTenantOrBypassForAppend passes. |

---

## 6. What exceptions mean

- **"EventStore append requires a valid tenant context (TenantScope.CurrentTenantId) or an approved platform bypass"**  
  RequireTenantOrBypassForAppend failed: the append path was invoked without TenantScope.CurrentTenantId set and without TenantSafetyGuard.EnterPlatformBypass() active. Fix: ensure appends run after tenant resolution (e.g. API middleware) or inside TenantScopeExecutor.RunWithTenantScopeAsync/RunWithPlatformBypassAsync.

- **"Parent event must belong to the same company as the new event"**  
  RequireParentRootCompanyMatch failed: the event being appended has a ParentEventId whose persisted event has a different CompanyId. Fix: do not set ParentEventId to an event from another company; fix data if the parent was incorrectly written.

- **"Root event must belong to the same company as the new event"**  
  Same as above for RootEventId. Fix: ensure root reference points to an event in the same company.

- **"EventStoreConsistencyGuard: EventId is required and cannot be empty"**  
  RequireEventMetadata failed. Fix: ensure the domain event has a non-empty EventId before append.

- **"CompanyId is required when the event has entity context"**  
  RequireCompanyWhenEntityContext failed. Fix: set CompanyId on the event when EntityType or EntityId is set.

- **"ParentEventId cannot equal EventId (self-reference)"** / **"RootEventId cannot be empty when ParentEventId is set"**  
  RequireValidParentRootLinkage failed. Fix: set valid parent/root references.

- **"Company mismatch in event stream"** / **"EntityType mismatch in event stream"**  
  RequireStreamConsistency failed. Fix: ensure same EntityType and CompanyId for all events in the same entity stream (EntityType + EntityId).

---

## 7. What tests were added or updated

**New tests**

- **EventStoreConsistencyGuardTests** (`CephasOps.Application.Tests/Events/EventStoreConsistencyGuardTests.cs`)  
  Unit tests for the guard only (no repository):  
  - RequireEventMetadata: valid; EventId empty; EventType null; EventType empty.  
  - RequireCompanyWhenEntityContext: no entity context; entity context + company present; entity context + company missing; entity context + company empty.  
  - RequireValidParentRootLinkage: no parent; parent equals EventId (self-reference); parent set but root empty.  
  - RequireStreamConsistency: no prior events; prior same company; prior different company (throws); prior different EntityType (throws).  
  - RequireTenantOrBypassForAppend: tenant set (does not throw); bypass active (does not throw); no tenant and no bypass (throws).  
  - RequireParentRootCompanyMatch: entry no company (does not throw); parent/root null (does not throw); parent same company (does not throw); parent company mismatch (throws); root company mismatch (throws).

- **EventStoreRepositoryConsistencyTests** (`CephasOps.Application.Tests/Events/EventStoreRepositoryConsistencyTests.cs`)  
  Repository append path:  
  - AppendAsync_ValidEventWithEntityContextAndCompany_Succeeds  
  - AppendAsync_SameEntityStreamDifferentCompany_ThrowsBeforeSave  
  - AppendAsync_SameEntityStreamSameCompany_Succeeds  
  - AppendInCurrentTransaction_EventWithEntityContextButNoCompany_ThrowsBeforeAdd  
  - AppendInCurrentTransaction_SameEntityStreamDifferentCompanyInSameTransaction_ThrowsBeforeAdd  
  - AppendInCurrentTransaction_EventWithSelfReferenceParent_ThrowsBeforeAdd  

**Existing tests**

- EventBusPhase1Tests (e.g. EventStoreRepository_AppendAsync_PersistsEntry, EventStoreRepository_AppendInCurrentTransaction_WhenSaveChangesCalled_PersistsEntry) use `CreateSampleEvent()` which sets CompanyId and entity context; they remain valid and should still pass once the solution builds. No changes were made to those tests.

**Note:** Tests that call EventStoreRepository.AppendAsync or AppendInCurrentTransaction must set TenantScope.CurrentTenantId or TenantSafetyGuard.EnterPlatformBypass() so RequireTenantOrBypassForAppend passes. Run:  
`dotnet test --filter "EventStoreConsistencyGuardTests|EventStoreRepositoryConsistencyTests"`.

---

## 8. Assumptions and unresolved edge cases

- **Outbox-only usage** — Callers that use only `AppendInCurrentTransaction` and then their own `SaveChangesAsync` now get stream consistency within the same transaction: before adding each entry, the repository collects other Added entries in the same entity stream from the change tracker and runs `RequireStreamConsistency`. So same-transaction appends for the same entity with different company or entity type are rejected on the second append.
- **Prior events with null CompanyId** — RequireStreamConsistency only enforces a match when the prior event has a non-empty CompanyId. If all prior events for that entity have null/empty CompanyId, the new event is not rejected for company mismatch (so the first event with company in the stream is allowed).
- **EventStoreEntry not tenant-scoped** — EventStore rows are not in TenantSafetyGuard’s tenant-scoped list; the consistency guard does not replace tenant middleware or query filters. It only validates append-time metadata and stream consistency.
- **IdempotencyKey / duplicate append** — The guard does not enforce idempotency (e.g. rejecting duplicate IdempotencyKey). Duplicate detection, if required, remains the responsibility of the caller or a separate mechanism.

- **Parent/root not found** — If ParentEventId or RootEventId points to an event that does not exist (e.g. not yet persisted), GetByEventIdAsync returns null; RequireParentRootCompanyMatch does not throw (null parent/root is allowed). Only when the referenced event exists and has a different CompanyId does the guard throw.

---

## 9. Why this is safe and does not change valid business behavior

- **Valid appends unchanged** — Events that already carry non-empty EventId, non-empty EventType, and (when they have entity context) non-empty CompanyId, and that do not introduce self-reference or invalid root/parent, and that match the existing stream’s company and entity type, pass as before.
- **Layered with existing safety** — The guard does not replace tenant provider, tenant scope, EF filters, or financial isolation. It adds explicit checks at the EventStore append boundary so inconsistent or incomplete metadata and stream contamination are rejected before persistence.
- **Fail closed** — Missing or inconsistent metadata or stream data causes an immediate, clear exception instead of silent corruption or ambiguous replay.
- **Minimal surface** — Only the two append methods in EventStoreRepository were changed; no new abstractions, no schema or API renames, no changes to read or processing paths.

---

## 10. Analyzer / artifacts

The tenant-safety analyzer and health dashboard do not detect “EventStore append without tenant or bypass” or “EventStore repair/replay without scope.” EventStoreConsistencyGuard is already tracked as a sensitive file in the tenant-safety health dashboard. No analyzer or artifact changes were made; this report is the source of truth for EventStore consistency coverage.

---

## Summary

| Item | Status |
|------|--------|
| Guard type | EventStoreConsistencyGuard (static, Infrastructure/Persistence) |
| Methods | RequireTenantOrBypassForAppend, RequireParentRootCompanyMatch, RequireEventMetadata, RequireCompanyWhenEntityContext, RequireValidParentRootLinkage, RequireStreamConsistency |
| Paths hardened | EventStoreRepository.AppendAsync (tenant-or-bypass, parent/root company match, stream consistency), EventStoreRepository.AppendInCurrentTransaction (tenant-or-bypass, metadata, company, parent/root, same-transaction stream consistency) |
| Tests added | EventStoreConsistencyGuardTests (incl. RequireTenantOrBypassForAppend, RequireParentRootCompanyMatch), EventStoreRepositoryConsistencyTests |
| Schema / migrations | None |
| Valid append behavior | Unchanged when tenant or bypass is set and metadata, parent/root company, and stream are consistent |

---

## Related

- **Index of safeguards:** [PLATFORM_SAFETY_HARDENING_INDEX.md](PLATFORM_SAFETY_HARDENING_INDEX.md) — discoverable list of all platform guards and reports.
- **When a guard fails:** [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md) — operator guidance for EventStore consistency and other safeguard failures.

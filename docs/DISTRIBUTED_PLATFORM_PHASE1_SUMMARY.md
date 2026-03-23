# CephasOps Distributed Ops Platform — Phase 1 Execution Summary

**Date:** 2026-03-09  
**Objective:** Transform CephasOps toward distributed readiness with strict boundaries, tenant foundation, reliable eventing, outbox/inbox, projections, worker-driven side effects, and extraction seams — without a big-bang rewrite or new deployables.

---

## A. Changes implemented

1. **Architecture audit and boundary map**
   - Added `docs/DISTRIBUTED_PLATFORM_PHASE1_AUDIT.md`: solution structure, bounded contexts, coupling, synchronous side effects, background job entry points, event-like behavior, CompanyId/tenant scoping, cross-module access, reporting-to-projection candidates, insertion points.
   - Added `docs/DISTRIBUTED_PLATFORM_BOUNDARIES.md`: current and target bounded contexts, ownership rules, forbidden cross-module patterns, future extraction order.

2. **Platform event envelope**
   - **IDomainEvent:** Added `Version` (string?) and `CausationId` (Guid?). Documented envelope (EventId, EventType, Version, CompanyId, CorrelationId, CausationId, EntityType, EntityId, OccurredAtUtc, Source).
   - **DomainEvent:** Default `Version = "1"`; added `CausationId`.
   - **EventStoreEntry:** Added `CausationId`. EventStoreRepository persists `Version` → PayloadVersion and CausationId.
   - **Platform event types:** Added `PlatformEventTypes` with `ops.workflow.transition_completed.v1`, `ops.order.status_changed.v1`, `ops.order.assigned.v1` and legacy names. WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent now use platform types and Version; WorkflowEngineService sets CausationId and ParentEventId on child events.
   - **EventTypeRegistry:** Registers both new (ops.*.v1) and legacy type names for replay compatibility.

3. **Transactional outbox**
   - Already in place: WorkflowEngineService uses `IEventStore.AppendInCurrentTransaction(evt)` and commits in the same transaction. EventStoreDispatcherHostedService claims and dispatches. Documented in DISTRIBUTED_PLATFORM_EVENT_ENVELOPE_SPEC.md and EVENT_BUS_PHASE4_PRODUCTION.md.

4. **Inbox / idempotent consumption**
   - Already in place: EventProcessingLogStore (TryClaimAsync per EventId + HandlerName) ensures at-most-once completion per handler. Documented as inbox in event envelope spec.

5. **Contract in Domain**
   - Moved `IEventStoreAttemptHistoryStore` and `EventStoreAttemptRecord` from Application to Domain so Infrastructure can implement without circular reference. Deleted Application’s duplicate file.

6. **IEventStore signature**
   - EventStoreRepository already implemented extended interface (ClaimNextPendingBatchAsync with optional nodeId/leaseExpiresAtUtc, MarkProcessedAsync with errorType/isNonRetryable). Call sites updated to pass the new optional parameters (null, false) where needed.

7. **Documentation**
   - `DISTRIBUTED_PLATFORM_EVENT_ENVELOPE_SPEC.md`: envelope fields, event type naming, persistence, outbox/inbox, causation/correlation.
   - `DISTRIBUTED_PLATFORM_PROJECTION_STRATEGY.md`: current projections (WorkflowTransitionHistory, Ledger), strategy, what stays transactional.
   - `DISTRIBUTED_PLATFORM_TENANT_HARDENING.md`: CompanyId/tenant rules, Phase 1 hardening, future multi-tenant.

---

## B. Migrations added

- **20260309200000_AddEventStoreCausationId:** Adds nullable `CausationId` (uuid) to `EventStore`. ApplicationDbContextModelSnapshot updated for EventStoreEntry.CausationId.

---

## C. New architectural seams introduced

- **Event envelope:** IDomainEvent + DomainEvent with Version and CausationId; platform event type constants and naming (ops.*.v1).
- **Boundary documentation:** Explicit bounded contexts, ownership, forbidden patterns, extraction order (DISTRIBUTED_PLATFORM_BOUNDARIES.md). No new deployables; seams are contractual and documented for future extraction (Notifications, Job Orchestration, Payroll, Inventory, Reporting, Workflow).

---

## D. Side effects moved to async

- **No new moves in this pass.** Existing design: workflow transition emits events in same transaction; EventStoreDispatcherHostedService runs handlers asynchronously. Inline notification call in WorkflowEngineService (OrderStatusChangedNotificationHandler) remains; documented in audit as a candidate to move to event-driven-only in a later pass.

---

## E. Projections / read models added

- **No new projection tables in this pass.** Existing projections (WorkflowTransitionHistoryEntry, LedgerEntry) and strategy are documented in DISTRIBUTED_PLATFORM_PROJECTION_STRATEGY.md. Foundation (event envelope, outbox, idempotent handlers, replay) is in place for adding projection handlers later.

---

## F. Tenant / company hardening completed

- **Events:** Workflow-engine-emitted events already set CompanyId; new envelope and CausationId/Version do not change that. All new events must set CompanyId when tenant-scoped (documented in DISTRIBUTED_PLATFORM_TENANT_HARDENING.md).
- **No DbContext global filter** for CompanyId in Phase 1 (single-company preserved). Scoping rules and extraction-ready guidance documented.

---

## G. Remaining architectural debt

- Inline `OrderStatusChangedNotificationHandler` call during workflow transition (in addition to event-driven path); consider moving to event-only.
- Some reporting and P&L still transactional/rebuild-job; can be moved to projection handlers in a later phase.
- CompanyId nullable and not enforced as tenant key in queries; acceptable for current single-company; document and enforce when moving to multi-tenant.
- Direct cross-module reads/writes in places (see audit); document exceptions and gradually push to events/contracts.

---

## H. Recommended Phase 2 extraction candidate

- **Notifications.** Clear interface (INotificationService), event-driven triggers, minimal shared state. Next: Job Orchestration (BackgroundJob + processor), then Payroll/Payout, Inventory, Reporting, Workflow (see DISTRIBUTED_PLATFORM_BOUNDARIES.md).

---

## I. Files / docs created or updated

**Created**
- `docs/DISTRIBUTED_PLATFORM_PHASE1_AUDIT.md`
- `docs/DISTRIBUTED_PLATFORM_BOUNDARIES.md`
- `docs/DISTRIBUTED_PLATFORM_EVENT_ENVELOPE_SPEC.md`
- `docs/DISTRIBUTED_PLATFORM_PROJECTION_STRATEGY.md`
- `docs/DISTRIBUTED_PLATFORM_TENANT_HARDENING.md`
- `docs/DISTRIBUTED_PLATFORM_PHASE1_SUMMARY.md`
- `backend/src/CephasOps.Application/Events/PlatformEventTypes.cs`
- `backend/src/CephasOps.Domain/Events/IEventStoreAttemptHistoryStore.cs` (moved from Application)
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260309200000_AddEventStoreCausationId.cs`

**Updated**
- `backend/src/CephasOps.Domain/Events/IDomainEvent.cs` — Version, CausationId.
- `backend/src/CephasOps.Domain/Events/DomainEvent.cs` — Version, CausationId.
- `backend/src/CephasOps.Domain/Events/EventStoreEntry.cs` — CausationId.
- `backend/src/CephasOps.Application/Events/WorkflowTransitionCompletedEvent.cs` — Platform event type, Version.
- `backend/src/CephasOps.Application/Events/OrderStatusChangedEvent.cs` — Platform event type, Version.
- `backend/src/CephasOps.Application/Events/OrderAssignedEvent.cs` — Platform event type, Version.
- `backend/src/CephasOps.Application/Events/Replay/EventTypeRegistry.cs` — Register ops.*.v1 and legacy names.
- `backend/src/CephasOps.Application/Workflow/Services/WorkflowEngineService.cs` — Use platform event types; set CausationId and ParentEventId on child events.
- `backend/src/CephasOps.Infrastructure/Persistence/EventStoreRepository.cs` — Persist Version, CausationId; ClaimNextPendingBatchAsync optional params; RETURNING and MapReaderToEntry include CausationId.
- `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Events/EventStoreEntryConfiguration.cs` — CausationId property.
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs` — CausationId on EventStoreEntry.
- `backend/src/CephasOps.Application/Events/DomainEventDispatcher.cs` — MarkProcessedAsync call sites (errorType, isNonRetryable).
- `backend/src/CephasOps.Application/Events/EventStoreDispatcherHostedService.cs` — MarkProcessedAsync call sites.
- `backend/src/CephasOps.Application/Workflow/Services/BackgroundJobProcessorService.cs` — MarkProcessedAsync call site.
- `backend/src/CephasOps.Infrastructure/Persistence/EventStoreAttemptHistoryStore.cs` — Use Domain.Events for contract.

**Deleted**
- `backend/src/CephasOps.Application/Events/IEventStoreAttemptHistoryStore.cs` (moved to Domain).

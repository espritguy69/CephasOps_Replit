# Event Bus Child Events Lineage – Audit and Implementation Report

**Date:** 2026-03-09  
**Goal:** Strengthen child event lineage propagation (RootEventId, ParentEventId, CorrelationId, CausationId) across the CephasOps event bus.

---

## A. Audit results

### Event publishing paths inspected

| Path | Location | How events are produced | Lineage status (before fix) |
|------|----------|-------------------------|-----------------------------|
| **Workflow transitions** | `WorkflowEngineService.ExecuteTransitionAsync` | Creates `WorkflowTransitionCompletedEvent`, `OrderStatusChangedEvent`, `OrderAssignedEvent` and appends via `IEventStore.AppendInCurrentTransaction` | Lineage was set manually (duplicated logic); not using `EventLineageHelper` |
| **Order lifecycle** | Same as above | `OrderStatusChangedEvent` and `OrderAssignedEvent` are children of workflow/order events | Same |
| **Payout calculations** | Application services | No domain events published to the event bus; no changes needed | N/A |
| **Notifications** | `OrderStatusNotificationDispatchHandler`, `OrderStatusChangedNotificationHandler` | Handlers consume events and call services (e.g. `RequestOrderStatusNotificationAsync`); they do **not** publish new domain events to the store | N/A |
| **Job orchestration** | `BackgroundJobProcessorService` | Processes jobs and can load events from store for dispatch; does not create new domain events | N/A |
| **Replay dispatch** | `EventStoreDispatcherHostedService`, `EventReplayService` | Re-dispatches already-stored events via `IDomainEventDispatcher.PublishAsync(..., alreadyStored: true)`. No new events created; lineage is preserved in payload and store | Replay does not mutate lineage; safe |

### Summary

- **Only production code path that creates and appends domain events:** `WorkflowEngineService` (workflow transitions for Order: `WorkflowTransitionCompletedEvent` → `OrderStatusChangedEvent` → `OrderAssignedEvent`).
- **Handlers** (e.g. `OrderAssignedOperationsHandler`, `OrderStatusNotificationDispatchHandler`) do not publish new events to the event store; they perform DB/work or enqueue jobs.
- **Replay:** Events are deserialized from the store and re-dispatched; lineage fields are in the payload and are not overwritten.

---

## B. Locations where lineage propagation was missing or inconsistent

| Location | Issue |
|----------|--------|
| `WorkflowEngineService.cs` (lines ~219–255) | Child events `OrderStatusChangedEvent` and `OrderAssignedEvent` set `ParentEventId`, `CausationId`, `RootEventId`, `CorrelationId` manually instead of using `EventLineageHelper.SetLineageFrom`. Risk of inconsistency and duplication; `OrderAssignedEvent` had `CausationId = evt.EventId` instead of the direct cause `orderEvt.EventId`. |

No other locations were found that create and publish/append domain events without setting lineage (or that set it incorrectly).

---

## C. Fixes implemented

1. **WorkflowEngineService** (`backend/src/CephasOps.Application/Workflow/Services/WorkflowEngineService.cs`)
   - **Root event:** `WorkflowTransitionCompletedEvent` already sets `evt.RootEventId = evt.EventId`; left as-is.
   - **First child:** After creating `OrderStatusChangedEvent`, call `EventLineageHelper.SetLineageFrom(orderEvt, evt)` instead of manually setting `CorrelationId`, `CausationId`, `ParentEventId`, `RootEventId`. Removed manual assignments for those fields.
   - **Second child:** After creating `OrderAssignedEvent`, call `EventLineageHelper.SetLineageFrom(assignedEvt, orderEvt)` so that the direct parent is `OrderStatusChangedEvent` and causation is correct. Removed manual assignments.
   - Resulting chain: `WorkflowTransitionCompleted` (root) → `OrderStatusChanged` (parent of OrderAssigned) → `OrderAssigned`; all share the same `RootEventId`; `ParentEventId` and `CausationId` form the correct chain.

2. **EventLineageHelper** (existing)
   - No code change. It already sets: `ParentEventId = causingEvent.EventId`, `CausationId = causingEvent.EventId`, `CorrelationId = causingEvent.CorrelationId ?? causingEvent.EventId.ToString("N")`, and `RootEventId` from parent’s RootEventId or parent’s EventId when parent is root.

3. **Replay safety**
   - Confirmed: replay does not create new events; it re-dispatches stored events. Lineage is stored in the payload and in EventStore columns; deserialization does not overwrite it. No code change required.

---

## D. New tests added

All in `backend/tests/CephasOps.Application.Tests/Events/Phase8PlatformEnvelopeAndPartitionTests.cs`:

| Test | Purpose |
|------|---------|
| `EventLineageHelper_MultiLevelChain_PreservesRootAndParentChain` | Root → child1 → child2; verifies each child has correct ParentEventId, RootEventId, CausationId, CorrelationId. |
| `EventLineageHelper_ReplayLineageIntegrity_SerializedPayloadPreservesLineage` | Serialize a child event (with lineage set via SetLineageFrom) and deserialize; asserts ParentEventId, RootEventId, CausationId, CorrelationId are preserved (simulates store round-trip). |
| `EventLineageHelper_CorrelationTree_AllDescendantsShareSameRootEventId` | Root with two children and one grandchild; verifies all share RootEventId and parent chain is correct. |

Existing tests in the same file already cover:
- `EventLineageHelper_SetLineageFrom_SetsParentRootCausationCorrelation`
- `EventLineageHelper_SetLineageFrom_WhenParentHasNoRoot_SetsRootToParentEventId`

Total Phase 8 lineage tests: **10** (7 existing + 3 new). All passing.

---

## E. Files modified

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Workflow/Services/WorkflowEngineService.cs` | Use `EventLineageHelper.SetLineageFrom(orderEvt, evt)` and `EventLineageHelper.SetLineageFrom(assignedEvt, orderEvt)`; remove manual lineage field assignments for child events. |
| `backend/tests/CephasOps.Application.Tests/Events/Phase8PlatformEnvelopeAndPartitionTests.cs` | Add 3 tests: multi-level chain, replay lineage integrity (serialize/deserialize), correlation tree. Add `using System.Text.Json` for serialization test. |
| `docs/EVENT_METADATA_CONTRACT.md` | Add section **"Child Event Lineage Rules"**: RootEventId rules, ParentEventId propagation, CorrelationId expectations, CausationId, Replay behavior. |
| `docs/EVENT_BUS_CHILD_EVENTS_LINEAGE_REPORT.md` | This report. |

---

## Documentation

- **Child Event Lineage Rules** are documented in `docs/EVENT_METADATA_CONTRACT.md` (new section), including RootEventId rules, ParentEventId propagation, CorrelationId expectations, CausationId, and replay behavior.
- Phase 3 correlation behavior is aligned with this contract; no separate `EVENT_BUS_PHASE3_CORRELATION.md` file was added, as the contract and this report cover the behavior.

---

## Verification

- `dotnet test --filter "FullyQualifiedName~Phase8PlatformEnvelopeAndPartitionTests"` passes (10 tests).
- Workflow transition chain (Order): `WorkflowTransitionCompleted` → `OrderStatusChanged` → `OrderAssigned` now uses `EventLineageHelper` for both child events; RootEventId remains the first event, ParentEventId chain is correct, CausationId reflects direct cause.

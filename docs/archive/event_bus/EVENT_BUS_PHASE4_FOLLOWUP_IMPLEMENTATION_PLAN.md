# Event Bus Phase 4 Follow-up — Implementation Plan

**Source:** [EVENT_BUS_PHASE3_CORRELATION.md](EVENT_BUS_PHASE3_CORRELATION.md) — "Follow-up for Phase 4"  
**Purpose:** Actionable steps for the remaining Event Bus follow-up items (migration, optional API, child events).

---

## 1. Summary

| Item | Status | Action |
|------|--------|--------|
| Apply migration `ExtendEventStorePhase2` | Pending | Run against each environment (see §2). |
| EventStore query/API (CompanyId, Status, CorrelationId, date range) | **Done** | `EventStoreQueryService.GetEventsAsync` and `EventStoreController` already support these filters via `EventStoreFilterDto`. |
| Child events: set CorrelationId + ParentEventId | Optional | When a handler publishes a follow-up event, set child from parent (see §3). |

---

## 2. Apply migration ExtendEventStorePhase2

**Migration:** `20260308194528_ExtendEventStorePhase2`  
**Location:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/ExtendEventStorePhase2.cs`  
**Adds to EventStore:** `CreatedAtUtc`, `TriggeredByUserId`, `Source`, `EntityType`, `EntityId`, `LastError`, `LastErrorAtUtc`, `LastHandler`, `ParentEventId`; index on `OccurredAtUtc`.

**Steps:**

0. **Prerequisite:** Ensure the solution builds. If `dotnet build` fails (e.g. `ReplayMetrics.RecordRunResumed`), fix that first; then run the migration.

1. **Local / dev:** From repo root:
   ```bash
   cd backend/src/CephasOps.Api
   dotnet ef database update --project ../CephasOps.Infrastructure
   ```
   Or apply the idempotent script if you use SQL-based updates:
   - Script is included in `backend/scripts/apply-all-migrations.sql` and `backend/scripts/sla-migrations-idempotent.sql` (ExtendEventStorePhase2 section).

2. **Staging / production:** Run the same migration via your deployment process (e.g. `dotnet ef database update` in release pipeline, or apply the idempotent SQL for the EventStore changes).

3. **Verify:** Confirm `__EFMigrationsHistory` contains `20260308194528_ExtendEventStorePhase2` and that `EventStore` table has columns `ParentEventId`, `CreatedAtUtc`, etc.

---

## 3. Optional: Child events (CorrelationId + ParentEventId)

**When:** A handler publishes a **follow-up event** (e.g. "OrderCompleted" triggers "SendInvoiceRequested").

**Rule (from Phase 3):** Set the child event’s `CorrelationId` from the parent event, and set `ParentEventId` on the stored child event so the trace links parent → child.

**Implementation steps:**

1. **Publishing the child event:** When building the child domain event in the handler, set:
   - `CorrelationId = parentEvent.CorrelationId` (or keep existing if the event DTO already carries it from the pipeline).

2. **EventStore append:** When the event bus appends the child to EventStore, set:
   - `ParentEventId = parentEvent.EventId` (or equivalent id of the event that triggered the handler).
   - Ensure `EventStoreRepository.AppendAsync` (or the code path that writes new events) accepts an optional `parentEventId` and writes it to `EventStoreEntry.ParentEventId`.

3. **Where to wire:** In the infrastructure that invokes handlers and then publishes new events (e.g. a handler that calls `IEventBus.PublishAsync(childEvent)`), pass the parent event id and correlation id into the append path. Exact location depends on whether child events are published synchronously in the same process or via a queue.

**Note:** Current code does not set `ParentEventId` when appending; it is nullable and reserved for this use. No schema change required.

---

## 4. EventStore API (already implemented)

`EventStoreFilterDto` and `GetEventsAsync` already support:

- `CompanyId`
- `Status`
- `CorrelationId` (substring match)
- `FromUtc` / `ToUtc` (date range)
- `EventType`, `EntityType`, `EntityId`, pagination

So no additional API work is required for "query by CompanyId, Status, CorrelationId, date range" unless you need different semantics (e.g. exact CorrelationId match or a dedicated dashboard endpoint).

---

## 5. Cursor todos alignment

- **eventbus-migration:** Complete when §2 is done for all target databases.
- **eventbus-api:** Can be marked **done**; API exists.
- **eventbus-child-events:** Optional; complete when §3 is implemented for at least one child-event flow.

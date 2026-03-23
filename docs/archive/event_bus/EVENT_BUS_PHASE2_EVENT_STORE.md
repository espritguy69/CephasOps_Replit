# Event Bus — Phase 2: Event Store

**Date:** 2026-03-09  
**Context:** Phase 1 (domain events, dispatcher, basic event store table) already existed. This document describes the Phase 2 audit and extensions.

---

## 1. What Already Existed (Phase 1)

- **EventStoreEntry** (Domain): EventId (PK), EventType, Payload (jsonb), OccurredAtUtc, ProcessedAtUtc, RetryCount, Status, CorrelationId, CompanyId.
- **IEventStore** (Domain): `AppendAsync(domainEvent)`, `MarkProcessedAsync(eventId, success, errorMessage)`.
- **EventStoreRepository** (Infrastructure): Append-only persist; on failure, increment RetryCount and set Status to Failed or DeadLetter (after 5 retries).
- **EventStore** table and migration `AddEventBusCorrelationAndEventStore` with indexes: (CompanyId, EventType, OccurredAtUtc), CorrelationId, Status.
- **Persistence flow:** DomainEventDispatcher calls `AppendAsync` before dispatching, then `MarkProcessedAsync` after all handlers run.

---

## 2. What Was Added (Phase 2)

### 2.1 Domain

- **IHasEntityContext** (Domain.Events): Optional interface with `EntityType` and `EntityId` so the event store can index by entity without depending on concrete event types. **WorkflowTransitionCompletedEvent** implements it.
- **EventStoreEntry** new fields:
  - **CreatedAtUtc** — when the record was inserted.
  - **TriggeredByUserId** — from IDomainEvent.
  - **Source** — from IDomainEvent (e.g. WorkflowEngine).
  - **EntityType** / **EntityId** — optional entity context (set when event implements IHasEntityContext).
  - **LastError** — last handler error message (sanitized, max 2000 chars).
  - **LastErrorAtUtc** — when the last failure occurred.
  - **LastHandler** — name of the handler that last failed (or last ran).
  - **ParentEventId** — optional; set when this event is a child of another (for future child-flow scenarios).

- **IEventStore**:
  - **MarkAsProcessingAsync(eventId)** — sets Status = `Processing` before dispatch (optional; used by dispatcher).
  - **MarkProcessedAsync** extended with optional **lastHandler** and now updates LastError, LastErrorAtUtc, LastHandler on failure.

### 2.2 Status Values

- **Pending** — just appended, not yet dispatched.
- **Processing** — dispatch started (after MarkAsProcessingAsync).
- **Processed** — all handlers completed successfully.
- **Failed** — at least one handler failed; RetryCount &lt; 5.
- **DeadLetter** — RetryCount ≥ 5 (poison).

### 2.3 Infrastructure

- **EventStoreRepository**: Populates TriggeredByUserId, Source, CreatedAtUtc; sets EntityType/EntityId when event implements IHasEntityContext; implements MarkAsProcessingAsync; in MarkProcessedAsync sets LastError (truncated), LastErrorAtUtc, LastHandler.
- **EventStoreEntryConfiguration**: Property max lengths for Source, EntityType, LastError, LastHandler; index on **OccurredAtUtc** for time-range queries.

### 2.4 Dispatcher

- After **AppendAsync**, calls **MarkAsProcessingAsync** so status reflects “dispatch in progress.”
- Passes **lastHandlerName** into **MarkProcessedAsync** so the store records which handler last ran or failed.

### 2.5 Schema Changes (Migration: ExtendEventStorePhase2)

| Column           | Type           | Nullable | Description |
|------------------|----------------|----------|-------------|
| CreatedAtUtc     | timestamptz    | no       | Insert time |
| TriggeredByUserId| uuid           | yes      | From event  |
| Source           | varchar(200)   | yes      | From event  |
| EntityType       | varchar(200)   | yes      | From IHasEntityContext |
| EntityId         | uuid           | yes      | From IHasEntityContext |
| LastError        | varchar(2000)  | yes      | Last failure message |
| LastErrorAtUtc   | timestamptz    | yes      | Last failure time |
| LastHandler      | varchar(500)   | yes      | Last handler name |
| ParentEventId    | uuid           | yes      | Optional parent event |

- New index: **IX_EventStore_OccurredAtUtc**.

---

## 3. Rules and Conventions

- **Append-only:** Only processing metadata (Status, ProcessedAtUtc, RetryCount, LastError, LastErrorAtUtc, LastHandler) is updated after insert. Payload and event identity are never overwritten.
- **Payload:** Stored as JSON; must not contain secrets or raw tokens. Sanitization is the responsibility of event producers; LastError is truncated to 2000 chars in the repository.
- **CompanyId:** Supported and indexed (compound with EventType, OccurredAtUtc).
- **CorrelationId:** Indexed and queryable (unchanged from Phase 1).

---

## 4. Risks and Assumptions

- **Existing rows:** Migration adds CreatedAtUtc with a default (UTC min) for existing rows; new rows get CreatedAtUtc set in code.
- **Payload size:** No explicit limit; very large payloads may affect storage and query performance.
- **ParentEventId:** Not set by current code paths; reserved for future “child event” flows (e.g. handler publishing a follow-up event).

---

## 5. Files Touched (Phase 2)

| Area          | File | Change |
|---------------|------|--------|
| Domain        | Events/IHasEntityContext.cs | New. |
| Domain        | Events/EventStoreEntry.cs | New fields. |
| Domain        | Events/IEventStore.cs | MarkAsProcessingAsync; MarkProcessedAsync(lastHandler). |
| Application   | Events/WorkflowTransitionCompletedEvent.cs | Implement IHasEntityContext. |
| Application   | Events/DomainEventDispatcher.cs | Call MarkAsProcessingAsync; pass lastHandler to MarkProcessedAsync. |
| Infrastructure| Persistence/EventStoreRepository.cs | New fields in Append; MarkAsProcessingAsync; LastError/LastHandler in MarkProcessedAsync. |
| Infrastructure| Configurations/Events/EventStoreEntryConfiguration.cs | New properties and index. |
| Infrastructure| Migrations/20260308194528_ExtendEventStorePhase2.cs | New. |
| Tests         | Events/EventBusPhase1Tests.cs | Verify MarkAsProcessingAsync; MarkProcessedAsync 5-arg signature. |

# Event Ledger Foundation

This document describes the CephasOps **Event Ledger** layer: an append-only operational event ledger that supports deterministic reconstruction, audit, and ledger-derived projections. It is a foundational platform capability, not a full accounting GL.

---

## 1. What the Event Ledger Is

- **Append-only ledger** of canonical operational facts derived from domain events or from replay operations.
- **Source linkage**: Each entry links to a source domain event (e.g. `WorkflowTransitionCompleted`) or to a completed replay operation.
- **Replay-safe**: Ledger writing is side-effect free and idempotent; safe during live processing and during replay (EventStore, Workflow, Projection targets).
- **Bounded scope**: A small set of **ledger families** is supported; each family has a clear ordering strategy and guarantee level.

## 2. What the Event Ledger Is Not

- **Not a general ledger (GL)**. It is an operational event ledger. Financial correctness and full accounting are future evolution goals; this phase does not claim them.
- **Not a replacement** for the Event Store or for existing projections (e.g. WorkflowTransitionHistory). It is an additional canonical layer that can feed derived projections and future financial/audit use cases.
- **Not multi-tenant by design in this phase**. Company scoping is applied at query/API level; schema is single-tenant.

---

## 3. Ledger Model

### 3.1 LedgerEntry (append-only)

- **Id** (Guid): Unique id of the ledger record.
- **SourceEventId** (Guid?, optional): When the entry was created from a domain event.
- **ReplayOperationId** (Guid?, optional): When the entry was created from a completed replay operation.
- **LedgerFamily**: Family identifier (e.g. `WorkflowTransition`, `ReplayOperationCompleted`).
- **Category** (optional): Sub-category within the family.
- **CompanyId**, **EntityType**, **EntityId**: Scope and entity context.
- **EventType**: Domain event type or fact type.
- **OccurredAtUtc**: When the fact occurred (event time or operation completion time).
- **RecordedAtUtc**: When the ledger record was written (immutable).
- **PayloadSnapshot** (optional): JSON snapshot of canonical payload for the family.
- **CorrelationId**, **TriggeredByUserId**, **OrderingStrategyId**: Audit and ordering metadata.

### 3.2 Idempotency

- **Event-driven entries**: Unique on `(SourceEventId, LedgerFamily)`. One ledger entry per source event per family.
- **Operation-driven entries**: Unique on `(ReplayOperationId, LedgerFamily)`. One ledger entry per replay operation per family.
- No in-place updates. Corrections are done by new entries or by replay/rebuild.
- **Conflict handling**: If two writers append for the same key under concurrency, the second insert hits the unique constraint; `LedgerWriter` catches this and treats it as success (no-op). See **LEDGER_APPEND_CONFLICT_HANDLING.md**.

### 3.3 Ordering

- Entries are ordered by `OccurredAtUtc` then `Id` for deterministic replay and timeline queries.
- Each family declares an **OrderingStrategyId** and **OrderingGuaranteeLevel** (e.g. StrongDeterministic), aligned with the replay engine.

---

## 4. Supported Ledger Families

| Family | Source | Description | Ordering |
|--------|--------|-------------|----------|
| **WorkflowTransition** | WorkflowTransitionCompletedEvent | Workflow transition completed (entity typically Order or other workflow-driven entity). | OccurredAtUtc ASC, EventId ASC (StrongDeterministic) |
| **ReplayOperationCompleted** | Replay operation completion | Replay operation reached terminal state (Completed, Cancelled, or Failed). | OccurredAtUtc (StrongDeterministic) |
| **OrderLifecycle** | OrderStatusChangedEvent | Order status changes at lifecycle transition points. Category derived from NewStatus (see §5.3). | OccurredAtUtc ASC, EventId ASC (StrongDeterministic) |

Additional families (e.g. bounded financial) may be added when source events and semantics are clearly defined and replay-safe. See **FINANCIAL_LEDGER_READINESS_AUDIT.md** for a bounded readiness audit; no financial ledger family is implemented today.

---

## 5. Ledger Writing

### 5.1 WorkflowTransition

- **Path**: Domain event handler that implements `IProjectionEventHandler<WorkflowTransitionCompletedEvent>` and calls `ILedgerWriter.AppendFromEventAsync`.
- **When**: On every dispatch of `WorkflowTransitionCompleted` (live and on all replay targets: EventStore, Workflow, Projection).
- **Idempotency**: By `(SourceEventId, LedgerFamily)`. Duplicate events (e.g. replay, rerun-failed, resume) do not create duplicate ledger rows.

### 5.2 ReplayOperationCompleted

- **Path**: `OperationalReplayExecutionService` calls `ILedgerWriter.AppendFromReplayOperationAsync` after persisting a terminal state (Completed, Cancelled, or Failed).
- **When**: After `SaveChanges` in the completion path of a full run, rerun-failed run, or cancel.
- **Idempotency**: By `(ReplayOperationId, LedgerFamily)`.

### 5.3 OrderLifecycle

- **Path**: Domain event handler that implements `IProjectionEventHandler<OrderStatusChangedEvent>` and calls `ILedgerWriter.AppendFromEventAsync` (OrderLifecycleLedgerHandler).
- **When**: On every dispatch of `OrderStatusChanged` (live and on all replay targets). `OrderStatusChangedEvent` is published from the workflow engine when a workflow transition completes for an entity of type **Order** (same flow as WorkflowTransitionCompletedEvent).
- **Idempotency**: By `(SourceEventId, LedgerFamily)`. One entry per order status change event.
- **Category**: Derived from `NewStatus` via `OrderLifecycleCategoryHelper`. Fallback is `StatusChanged`. Mapping (aligned with Domain OrderStatus constants):

  | NewStatus | Category |
  |-----------|----------|
  | Assigned | Assignment |
  | OnTheWay, MetCustomer, OrderCompleted | FieldProgress |
  | DocketsReceived, DocketsVerified, DocketsRejected, DocketsUploaded | Docket |
  | ReadyForInvoice, Invoiced, SubmittedToPortal | InvoiceReadiness |
  | Completed, Cancelled, Rejected | Completion |
  | All other (e.g. Pending, Blocker, ReschedulePendingApproval, Reinvoice) | StatusChanged |

### 5.4 Payload Snapshot Validation

All payload snapshots are validated **before** being written to the ledger (single validation point in `LedgerWriter`). This protects the ledger from malformed or oversized payloads that could degrade DB performance or cause projection readers to fail.

- **JSON validation** (when `ValidateJsonPayload` is true, default): The payload must parse as valid JSON and the root must be an **Object** or **Array**. Primitives (string, number, boolean) are rejected. Invalid JSON is **rejected**: the ledger entry is still written but `PayloadSnapshot` is stored as null, and a warning is logged. The event pipeline does not crash.
- **Payload size limit** (default **64 KB**): If the UTF-8 byte length of the payload exceeds `MaxPayloadSizeBytes`, the payload is **replaced with a metadata placeholder** (valid JSON) that includes `_ledgerPayloadTruncated`, `_originalSizeBytes`, `_maxPayloadSizeBytes`, `_ledgerFamily`, and `_eventType`. The ledger entry is written with this placeholder so readers can parse it and know the payload was truncated.
- **Configuration** (`Ledger` section in appsettings): `MaxPayloadSizeBytes` (default 65536), `ValidateJsonPayload` (default true). Safe defaults are used if the section is missing.
- **Logging**: Structured warnings are emitted when a payload is rejected (invalid JSON) or replaced (oversized), including LedgerFamily, EventType, SourceId, and payload size.

Handlers (e.g. WorkflowTransitionLedgerHandler, OrderLifecycleLedgerHandler) serialize small DTOs to JSON and pass the string to `ILedgerWriter`; validation runs inside the writer so all call paths are protected without scattering validation logic.

### 5.5 Replay Integration

- Existing replay targets (EventStore, Workflow, Projection) are **unchanged**. WorkflowTransition and OrderLifecycle ledger handlers run for all of them (registered as domain/projection handlers).
- Ledger generation does not introduce new replay targets. Rebuilding the ledger for a family is achieved by replaying the relevant events (e.g. Projection replay for WorkflowTransition and OrderStatusChanged) or by design already recording ReplayOperationCompleted when operations complete.
- No side effects: no outbound integrations, no async enqueue, from ledger writing.

---

## 6. Ledger-Derived Projections

- **Workflow transition timeline from ledger**: `IWorkflowTransitionTimelineFromLedger.GetByEntityAsync` reads from `LedgerEntries` where `LedgerFamily = WorkflowTransition` and returns a timeline for a given entity (e.g. Order). This proves the path: **domain events → ledger entries → derived projection**.
- **Order timeline from ledger**: `IOrderTimelineFromLedger.GetByOrderIdAsync` reads from `LedgerEntries` where `LedgerFamily = OrderLifecycle` and `EntityType = Order`, and returns an ordered timeline (occurred time, event type, prior/new status, source event linkage). Proves **order lifecycle event → ledger entry → order timeline**.
- Replay-operation timeline can be built by querying ledger entries with family `ReplayOperationCompleted` (filter by company/date as needed).
- **Unified order operational history**: `IUnifiedOrderHistoryFromLedger.GetByOrderIdAsync` merges **WorkflowTransition** and **OrderLifecycle** ledger entries for a given order into one ordered timeline (by OccurredAtUtc then Id). Read-only and replay-safe. Used for operator visibility of a single coherent history per order.

---

## 6a. Order Lifecycle Integrity

- **Canonical path**: All order status changes that update `Order.Status` and create `OrderStatusLog` go through **WorkflowEngineService.ExecuteTransitionAsync**. There are no direct writes to `Order.Status` or `OrderStatusLog` outside that path.
- **Callers**: OrderService.ChangeOrderStatusAsync, WorkflowController, SchedulerService, EmailIngestionService, InvoiceSubmissionService (order → SubmittedToPortal) all call `ExecuteTransitionAsync`; automation and escalation rules call `ChangeOrderStatusAsync`, which in turn calls the workflow engine. So the ledger is not silently incomplete due to bypasses.
- **Risks**: None identified. Any future code that sets `Order.Status` or adds to `OrderStatusLogs` without going through the workflow engine would bypass OrderStatusChangedEvent and should be avoided; add guardrails or explicit documentation if a temporary exception is required.

---

## 7. Admin API

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | /api/event-store/ledger/families | List ledger family descriptors (id, displayName, ordering, etc.). |
| GET | /api/event-store/ledger/entries | List ledger entries with filters: companyId, entityType, entityId, ledgerFamily, fromOccurredUtc, toOccurredUtc, page, pageSize. |
| GET | /api/event-store/ledger/entries/{id} | Get a single ledger entry by id. |
| GET | /api/event-store/ledger/timeline/workflow-transition | Ledger-derived workflow transition timeline for an entity (query params: entityType, entityId, companyId, fromOccurredUtc, toOccurredUtc, limit). |
| GET | /api/event-store/ledger/timeline/order | Ledger-derived order timeline (query params: orderId, companyId, fromOccurredUtc, toOccurredUtc, limit). |
| GET | /api/event-store/ledger/timeline/unified-order | Unified operational history for an order (WorkflowTransition + OrderLifecycle merged; query params: orderId, companyId, fromOccurredUtc, toOccurredUtc, limit). |

All endpoints require Jobs admin (or equivalent). Company scoping is applied for non–global admins.

---

## 8. Admin UI

- **Event Ledger** page under Admin: list ledger entries with family filter (including OrderLifecycle), pagination, and link to source event or replay operation. Detail panel shows full entry and payload snapshot. OrderLifecycle entries display **category** (Assignment, FieldProgress, Docket, InvoiceReadiness, Completion, or StatusChanged).
- **Order timeline** section: enter an order ID to load the order lifecycle timeline from ledger (OrderLifecycle family); categories are shown.
- **Unified order history** section: enter an order ID to load the merged WorkflowTransition + OrderLifecycle timeline (single ordered view per order).
- **Ledger families** and **ordering metadata** are displayed. No write actions in the UI (append-only).

---

## 8a. Replay Preview Ledger-Awareness

- Replay preview (dry-run) includes **ledger impact** when available: which ledger families may receive new entries, whether ledger writes are expected, and which ledger-derived projections may be updated (WorkflowTransitionTimeline, OrderTimeline, **UnifiedOrderHistory**).
- Impact is **estimated** (inferred from event-type → family mapping); exact counts or per-entity diffs are not promised. When no matched event types map to a ledger family, a short unavailable reason is shown.

---

## 9. Migrations and Schema

- **Table**: `LedgerEntries` (see Domain entity `LedgerEntry` and `LedgerEntryConfiguration`).
- **Indexes**: CompanyId+LedgerFamily+OccurredAtUtc; EntityType+EntityId+LedgerFamily; RecordedAtUtc; partial unique indexes for idempotency (SourceEventId+LedgerFamily, ReplayOperationId+LedgerFamily).
- **Migration**: `AddEventLedgerEntries`. Snapshot aligned.

---

## 10. Order Lifecycle Domain Events

- **OrderStatusChangedEvent** is published by the workflow engine when a workflow transition completes for an entity of type **Order** (same transition that produces WorkflowTransitionCompletedEvent and triggers CreateOrderStatusLog side effect). Fields: OrderId, CompanyId, PreviousStatus, NewStatus, OccurredAtUtc, TriggeredByUserId, CorrelationId. It is stored in the event store and replayed like other domain events, so ledger entries for OrderLifecycle can be rebuilt by replay.
- Order status changes are **only** emitted from this single lifecycle transition point (workflow engine). Other code paths that read or use OrderStatusLog do not publish domain events.

---

## 11. Limitations and Future Work

- **Payload validation**: Rejected payloads (invalid JSON) are stored as null; truncated payloads are stored as a placeholder. Projection readers should handle null or placeholder payloads gracefully. Exact byte limit is configurable; default 64 KB is sufficient for current handlers.
- **Financial ledger**: Not implemented. See **FINANCIAL_LEDGER_READINESS_AUDIT.md** for a structured audit of candidate financial families (payout, invoice, payment, cost, payroll); all assessed as not ready due to absence of domain events. Do not implement financial ledger families until source events and replay safety are in place.
- **Multi-tenant replay**: Ledger is company-scoped at query level; true multi-tenant replay safety and isolation are future evolution.
- **Exact before/after diff**: Ledger stores a payload snapshot per entry; it does not provide a full before/after diff UI in this phase.
- **Ledger impact in replay preview**: Quality is estimated (event-type mapping only); not exact per-entity or per-order.

The Event Ledger foundation is intended to evolve toward stronger financial correctness, operational reconstruction, and multi-company replay safety as CephasOps requirements mature.

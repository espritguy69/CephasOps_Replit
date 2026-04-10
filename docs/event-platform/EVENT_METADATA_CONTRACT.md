# Event Metadata Contract (Phase 8)

## Required metadata fields (envelope / store)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| EventId | Guid | Yes | Unique event identifier. |
| EventType | string | Yes | Event type name (e.g. WorkflowTransitionCompleted). |
| OccurredAtUtc | DateTime | Yes | When the domain fact occurred. |
| CreatedAtUtc | DateTime | Yes | When the platform captured/stored the event. |
| CorrelationId | string | No | Groups related flow (e.g. workflow run). |
| CompanyId | Guid? | No | Tenant/company scope. |
| CausationId | Guid? | No | Event or command that caused this event. |
| ParentEventId | Guid? | No | Immediate parent (child events). |
| RootEventId | Guid? | No | Origin of the full causality chain. |
| PartitionKey | string? | No | For ordering and concurrency; derived from CompanyId, EntityId, or CorrelationId. |
| PayloadVersion | string? | No | Payload/contract version (e.g. "1"). |

## Correlation semantics

- **CorrelationId**: same value for all events in one logical flow (e.g. one workflow run, one import job). Used for grouping and lineage by correlation.
- **CausationId**: the EventId of the event or command that directly caused this event. Used for “this caused that” chains.
- **ParentEventId**: the EventId of the event that this event is a direct child of (e.g. WorkOrderImported → WorkOrderParsed). Enables parent/child trees.
- **RootEventId**: the EventId of the very first event in the causality chain. All descendants in the chain should set the same RootEventId so the full tree can be queried by root.

Rules:

- For the **first** event in a flow: set CorrelationId (e.g. new Guid or workflow id); leave ParentEventId, CausationId, RootEventId null (or RootEventId = EventId if you want to mark it as root).
- For a **child** event: set ParentEventId = causing event’s EventId, CausationId = causing event’s EventId, CorrelationId = same as parent, RootEventId = parent’s RootEventId ?? parent’s EventId. Use `EventLineageHelper.SetLineageFrom(child, parent)`.

## Parent / root / causation rules

- **RootEventId**: must be the same for all events in one causality tree. Set on children from the parent’s RootEventId or, if parent is the root, parent’s EventId.
- **ParentEventId**: exactly one parent per child; the parent’s EventId.
- **CausationId**: the event (or command) that caused this event; often same as ParentEventId for child events.

## Child Event Lineage Rules

When a handler or service emits a **child event** (an event caused by another event), lineage must be propagated so that correlation trees and causality chains remain correct.

### RootEventId rules

- **Root event** (first in a flow): set `RootEventId = EventId` so the event is the root of its own tree.
- **Child event**: set `RootEventId = parent.RootEventId ?? parent.EventId`. All descendants in the same causality tree must share the same RootEventId so the full tree can be queried by root (e.g. "all events from this order transition").
- **Do not** set a new RootEventId on a child; always inherit from the causing event.

### ParentEventId propagation

- **Child event**: set `ParentEventId = causingEvent.EventId` (the immediate parent).
- **Multi-level chains**: each event has exactly one parent. Example: `WorkflowTransitionCompleted` → `OrderStatusChanged` → `OrderAssigned`; each child's ParentEventId is the EventId of the event that directly caused it.
- ParentEventId is used to build parent/child trees and related-links APIs.

### CorrelationId expectations

- **Root event**: set CorrelationId to a flow identifier (e.g. workflow job id, import id) when you want to group all events in that flow.
- **Child event**: set `CorrelationId = parent.CorrelationId` so all events in the flow share the same correlation value. Use `EventLineageHelper.SetLineageFrom(child, parent)` which does this (and uses `parent.EventId.ToString("N")` when parent has no CorrelationId).

### CausationId

- **Child event**: set `CausationId = causingEvent.EventId` (the event that directly caused this one). For handler-emitted events, the causing event is the one being handled. `EventLineageHelper.SetLineageFrom(child, parent)` sets this.

### Replay behavior

- **Replay does not create new events.** Replay re-dispatches existing stored events. Lineage fields (RootEventId, ParentEventId, CorrelationId, CausationId) are stored in the event payload and in EventStore columns; they are **not** changed during replay.
- **ReplayId** (when set) identifies a replay run for audit; it does not replace or clear RootEventId or ParentEventId.
- When deserializing an event from the store for dispatch (normal or replay), lineage comes from the serialized payload; the dispatcher does not overwrite it. Lineage reconstruction and correlation trees remain valid after replay.

## Replay metadata rules

- **ReplayId**: when an event is re-dispatched as part of a replay run, ReplayId can be set to the replay operation id (or correlation) so replayed events are distinguishable in the store. (Optional; replay execution audit is also in ReplayOperation / ReplayOperationEvent.)
- Replay operations use **ReplayCorrelationId** and **ReplayOperationId** for audit and idempotency; handlers can check **IReplayExecutionContext** to suppress side effects or run only projections.

## Partition key guidance

- **By tenant**: use CompanyId so all events for a company are in one partition (ordering per company).
- **By aggregate**: use EntityType + EntityId (e.g. Order:xyz) so all events for that order are ordered.
- **By workflow**: use CorrelationId so all events in one workflow run are in one partition.
- Default resolver (DefaultPartitionKeyResolver): CompanyId → EntityId → CorrelationId → EventId. Prefer setting CompanyId or EntityId (IHasEntityContext) so partition key is stable and meaningful.

## Optional envelope fields

- SourceService, SourceModule: publishing service and bounded context.
- CapturedAtUtc: when the platform captured the event.
- IdempotencyKey: for deduplication at append (future).
- TraceId, SpanId: distributed tracing.
- Priority: for future priority-aware dispatch.

All of these are optional and backward compatible; existing events can have nulls for new columns.

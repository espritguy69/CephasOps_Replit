# Operational Replay Engine — Implementation Plan

## Audit summary

- **Event replay:** IEventReplayService replays by ID via DispatchToHandlersAsync (in-process). Only WorkflowTransitionCompleted is allowed; its handler only logs. No side-effect handlers in event pipeline today.
- **Operational replay:** ReplayOperation/ReplayOperationEvent exist. OperationalReplayExecutionService creates operation, loads events (GetEventsForReplayAsync), filters by policy, replays each via IEventReplayService.ReplayAsync in-process. No replay context; async handlers would be enqueued if any existed.
- **Background jobs:** BackgroundJobProcessorService processes job types including EventHandlingAsync (loads event, runs IAsyncEventSubscriber handlers). No OperationalReplay job type yet.
- **Ordering:** GetEventsForReplayAsync uses OrderByDescending(OccurredAtUtc) — should be Ascending for deterministic replay.
- **Financial:** PnlRebuild job + RebuildPnlAsync exist; separate from event store.
- **Parser:** ParserReplayService (attachment/session/profile) exists; separate from event store.
- **Trace:** ITraceQueryService links EventStore, JobRun, WorkflowJob by CorrelationId/EventId.

## Implementation order

1. **Replay execution context** — IReplayExecutionContext + AsyncLocal accessor; set in replay path; dispatcher skips enqueue when SuppressSideEffects.
2. **Schema** — ReplayOperation: ReplayTarget, ReplayMode, StartedAtUtc, DurationMs, SkippedCount, ErrorSummary. ReplayOperationEvent: EventType, EntityType, EntityId, SkippedReason, DurationMs.
3. **Request/API** — ReplayRequestDto: ReplayTarget, ReplayMode. Order events ascending (OccurredAtUtc, EventId).
4. **Orchestrator** — Single orchestrator: resolve candidates, order, execute target (EventStore path), record results, support dry-run and apply.
5. **Background job** — OperationalReplay job type; create operation Pending, enqueue job; processor runs orchestrator, updates operation.
6. **API** — Optional async execute (202 + operation id); list/detail/event-results.
7. **Metrics** — Replay counters + structured logging.
8. **Docs** — Safety model, suppression, targets, API, runbook, limitations.

## Replay targets (Phase 1)

| Target       | Scope              | Implementation |
|-------------|--------------------|----------------|
| EventStore  | Company, date, type, entity, correlation | Existing event load + policy + dispatch with replay context. |
| Workflow    | Same as EventStore, event type = WorkflowTransitionCompleted | EventStore target with event type filter. |
| Financial   | Company, period    | Enqueue PnlRebuild job or call RebuildPnlAsync; record in ReplayOperation (single “event” = one rebuild). Bounded support. |
| Parser      | Attachment/session/profile | Call ParserReplayService; record operation. Bounded; no raw event-store replay. |
| Projection  | Alias for EventStore with projection-only semantics when we have handler metadata. Phase 1: same as EventStore. |

## Idempotency and safety

- Replay context sets SuppressSideEffects so dispatcher does not enqueue EventHandlingAsync jobs during replay (no duplicate async work).
- Only policy-allowed event types are replayed (default deny).
- EventStore rows are never mutated by replay (append-only).
- Financial replay: RebuildPnlAsync is upsert/rebuild by design.
- Parser replay: ParserReplayService writes only to ParserReplayRuns; read-only for orders/drafts.
- Future handlers that send SMS/email must check IReplayExecutionContextAccessor.Current?.SuppressSideEffects and skip outbound actions when true.

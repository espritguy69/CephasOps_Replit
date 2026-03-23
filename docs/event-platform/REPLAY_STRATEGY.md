# Event Replay Strategy

## Single-event replay

- **Retry:** Re-dispatch the stored event to current handlers without policy check. Use for Failed/DeadLetter recovery.
- **Replay:** Re-dispatch only if `IEventReplayPolicy.IsReplayAllowed(eventType)` and not blocked. Use when you want to re-apply handler logic (e.g. after a fix).

APIs: POST api/event-store/events/{eventId}/retry, POST api/event-store/events/{eventId}/replay.

## Tenant isolation

- Replay and retry APIs accept the current user’s scope; for non–global-admins, `scopeCompanyId` is the user’s CompanyId. The implementation loads the event only if `entry.CompanyId == scopeCompanyId` (or scope is global). No cross-tenant replay.

## Batch (operational) replay

- `IOperationalReplayExecutionService` runs replay for a filter (CompanyId, EventType, date range, etc.). A company-level lock ensures only one active replay per company at a time. Events are re-dispatched in order (e.g. by OccurredAtUtc).
- Options: full dispatch (all handlers) or projection-only (only `IProjectionEventHandler<T>`). Use projection-only to rebuild read models without re-running side-effect handlers.

## Avoiding duplicate side effects

- Handlers are idempotent (EventProcessingLog + design). Replaying the same event should not create duplicate orders, duplicate notifications, or duplicate outbound deliveries if handlers use event id / idempotency keys.
- For async handlers, replay with SuppressSideEffects does not enqueue them, so they do not run during that replay pass.

## Outbound integration replay

- Outbound Failed/DeadLetter deliveries are replayed via `IOutboundIntegrationBus.ReplayAsync(ReplayOutboundRequest)`, not via EventStore replay. Tenant and endpoint filters apply there as well.

# Replay Strategy

**Purpose:** How event replay works, tenant isolation, and avoiding duplicate side effects.

---

## 1. Replay by Event Id

- **IEventReplayService.ReplayAsync(eventId, scopeCompanyId, initiatedByUserId)**:
  1. Load event from **IEventStore.GetByEventIdAsync(eventId)**.
  2. **Tenant check:** If `scopeCompanyId` is set, require `entry.CompanyId == scopeCompanyId`; otherwise return error (event not in scope).
  3. **Policy:** **IEventReplayPolicy.IsReplayAllowed(entry.EventType)** — e.g. WorkflowTransitionCompleted allowed; unknown or blocked types rejected.
  4. Deserialize payload via **IEventTypeRegistry** to `IDomainEvent`.
  5. Set **IReplayExecutionContext** (e.g. SuppressSideEffects for single-event retry/replay).
  6. **IEventStore.MarkAsProcessingAsync(eventId)** then **IDomainEventDispatcher.DispatchToHandlersAsync(domainEvent)**.
  7. Handlers run again; **EventProcessingLog** prevents duplicate run of the same handler for the same EventId (idempotency).
  8. Clear replay context.

- **RetryAsync** is the same flow with policy check disabled (force redispatch).

---

## 2. Tenant Isolation

- Replay is only allowed for events whose **CompanyId** matches the caller’s scope (`scopeCompanyId`). API layer passes scope from current user (e.g. non–SuperAdmin => CompanyId).
- Bulk operations use **EventStoreBulkFilter** with **CompanyId** so only events for that tenant are reset or replayed.
- No cross-tenant event leakage: filters in EventStoreQueryService, EventReplayService, and ObservabilityController enforce CompanyId.

---

## 3. Avoiding Duplicate Side Effects

- **EventProcessingLog:** At-most-once per (EventId, HandlerName). On replay, handlers that already completed for that EventId are skipped (no second run).
- **SuppressSideEffects:** When replay context has SuppressSideEffects (e.g. single-event replay), **IAsyncEventSubscriber** handlers are not enqueued — so no duplicate background jobs.
- Handlers should be **idempotent by business key** where possible (e.g. upsert by OrderId + event type) so that any rare duplicate execution does not create duplicate business side effects.
- **Projection-only replay:** When ReplayTarget = Projection, only **IProjectionEventHandler** run; side-effect handlers are skipped.

---

## 4. Bulk and Operational Replay

- **EventBulkReplayService:** ReplayDeadLetterByFilterAsync, BulkResetDeadLetterToPendingAsync, etc., with **EventStoreBulkFilter** (CompanyId, EventType, FromUtc, ToUtc, MaxCount).
- **IOperationalReplayPolicy:** Stricter for batch/operational replay: max replay window (days), max count per request, blocked companies, destructive event types.
- Replay operations are recorded in **ReplayOperation** / **ReplayOperationEvent** for audit.

---

## 5. References

- Event lifecycle: `docs/event-platform/event-lifecycle.md`
- Handler guidelines: `docs/event-platform/handler-guidelines.md`
- Tenant safety: `docs/event-platform/tenant-safety.md`
- Runbook: `backend/scripts/EVENT_PLATFORM_RUNBOOK.md`

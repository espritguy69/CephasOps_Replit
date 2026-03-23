# Event Handler Guidelines

**Purpose:** How to implement handlers for the internal event platform so they are idempotent, retry-safe, and tenant-aware.

---

## 1. Handler Contract

- Implement **IDomainEventHandler&lt;TEvent&gt;** (or **IEventHandler&lt;TEvent&gt;**), where `TEvent : IDomainEvent`.
- Single method: **HandleAsync(TEvent evt, CancellationToken cancellationToken)**.
- Handlers are resolved from **DI** per event type; multiple handlers per type are supported.

---

## 2. Idempotency

- **EventProcessingLog** ensures at-most-once execution per (EventId, HandlerName). Duplicate delivery (e.g. retry or replay) will not run the same handler again for that event id.
- Business logic should still be **idempotent by business key** where possible (e.g. “upsert by OrderId + status” rather than “insert always”), so that if the same logical action is triggered by different events or replays, side effects are not duplicated.
- Document the idempotency contract when adding a new handler (e.g. “idempotent by OrderId + EventId” or “by IdempotencyKey”).

---

## 3. Retry-Safety

- Handlers can throw; the dispatcher will mark the event as Failed and schedule retry (or DeadLetter after max retries).
- Prefer **non-retryable** classification for poison messages (e.g. validation errors) so they go to DeadLetter and do not block the queue.
- Avoid operations that cannot be safely retried (e.g. sending a one-time code) unless you coordinate with an idempotency key or external deduplication.

---

## 4. Tenant Awareness

- The event carries **CompanyId**; handlers must not use data from another tenant.
- When reading or writing data, always scope by **evt.CompanyId** (or current tenant from request context that was validated against the event’s CompanyId).
- Do not forward or log payloads that could leak tenant data to another tenant.

---

## 5. Background vs In-Process

- **In-process:** Handlers run in the dispatcher process; keep them short and non-blocking.
- **Background:** Implement **IAsyncEventSubscriber&lt;TEvent&gt;** to be enqueued (e.g. job queue). During **replay** with SuppressSideEffects, async handlers are not enqueued to avoid duplicate background work.
- Use async handlers for heavy or external work (e.g. sending to integration bus, rebuilding projections).

---

## 6. Replay and Projections

- For **projection-only replay** (target = Projection), only **IProjectionEventHandler&lt;TEvent&gt;** run; other handlers are skipped.
- Projection handlers must be **read-model only** (no external side effects) and idempotent so replay is safe.

---

## 7. Forwarding to Integration Bus

- To publish an internal event to external connectors, build a **PlatformEventEnvelope** from the domain event (e.g. via **IDomainEventToPlatformEnvelopeBuilder**) and call **IOutboundIntegrationBus.PublishAsync(envelope)**.
- Do not block the main handler on outbound HTTP; the bus creates deliveries and processes them asynchronously (with retry worker).

---

## 8. References

- Event architecture: `docs/event-platform/event-architecture.md`
- Replay: `docs/event-platform/replay-strategy.md`
- Tenant safety: `docs/event-platform/tenant-safety.md`

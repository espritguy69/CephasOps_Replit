# Async Event Bus / Domain Events — Implementation Summary

**Date:** 2026-03-13  
**Purpose:** Concise summary of the first production-safe async event bus upgrade for CephasOps.

---

## 1. Files created or modified

### Created

| File | Purpose |
|------|---------|
| `Application/Audit/TenantActivityTimelineFromEventsHandler.cs` | Records OrderCreated, OrderCompleted, OrderAssigned, OrderStatusChanged to tenant activity timeline when event.CompanyId is set; skips when null/empty. |
| `docs/operations/ASYNC_EVENT_BUS_ARCHITECTURE.md` | Architecture: current flows, abstractions, dispatch strategy, outbox, first-use cases, observability, future phases. |
| `docs/operations/DOMAIN_EVENTS_GUIDE.md` | How to define, publish, and handle domain events; tenant safety; example event types; adding new events. |
| `docs/operations/EVENT_HANDLING_GUARDRAILS.md` | Five guardrails: tenant context, no cross-tenant execution, idempotency/duplicate safety, no platform bypass creep, no wrong eventual consistency. |
| `docs/operations/ASYNC_EVENT_BUS_IMPLEMENTATION_SUMMARY.md` | This file. |
| `tests/CephasOps.Application.Tests/Audit/TenantActivityTimelineFromEventsHandlerTests.cs` | Tests: timeline recorded when CompanyId set; not recorded when null/empty; OrderCompleted and OrderStatusChanged recorded for correct tenant. |

### Modified

| File | Change |
|------|--------|
| `Api/Program.cs` | Registered TenantActivityTimelineFromEventsHandler for OrderCreatedEvent, OrderCompletedEvent, OrderAssignedEvent, OrderStatusChangedEvent. |
| `docs/operations/SAAS_ARCHITECTURE_MAP.md` | Added event bus to overview; new §8 Async Event Bus; Event bus row in Security Model Summary; Final State wording. |
| `docs/operations/SAAS_ENTERPRISE_UPGRADES.md` | New §5 Async Event Bus (domain events); renumbered §6 Guard Against Common SaaS Failures. |
| `docs/remediation/SAAS_REMEDIATION_CHANGELOG.md` | New entry: 2026-03-13 Async Event Bus / Domain Events Upgrade (goal, scope, files, behavior, future). |
| `tests/CephasOps.Application.Tests/Platform/FeatureFlagServiceTests.cs` | Fixed Company seed: use LegalName/ShortName instead of Name (pre-existing build fix). |

---

## 2. Abstractions (existing; documented)

No new interfaces were added. The following were documented as the event bus surface:

- **IDomainEvent** — Event contract (EventId, EventType, CompanyId, CorrelationId, CausationId, OccurredAtUtc, etc.).
- **IEventBus** — PublishAsync, DispatchAsync (application-facing).
- **IDomainEventDispatcher** — Persist then dispatch; sync and async handler split.
- **IEventStore** — AppendAsync, AppendInCurrentTransaction, ClaimNextPendingBatchAsync, MarkProcessedAsync.
- **IDomainEventHandler&lt;TEvent&gt;** — Sync handler; runs under tenant scope set by dispatcher.
- **EventStoreEntry** — Persisted envelope (tenant context via CompanyId).

---

## 3. First-use cases

| Use case | Status | Notes |
|----------|--------|-------|
| **Tenant activity timeline** | Implemented | TenantActivityTimelineFromEventsHandler records OrderCreated, OrderCompleted, OrderAssigned, OrderStatusChanged when CompanyId is set. |
| **Notification request/send** | Already present | OrderStatusNotificationDispatchHandler; idempotent; runs in event scope. |
| **Outbound integration delivery** | Already present | IntegrationEventForwardingHandler → IOutboundIntegrationBus; tenant-prefixed idempotency. |

---

## 4. Event reliability

- **Outbox-style:** Events appended to EventStoreEntry; EventStoreDispatcherHostedService claims Pending (and due-retry Failed), runs under TenantScopeExecutor with entry.CompanyId, then marks Processed/Failed/DeadLetter.
- **Idempotency:** IEventProcessingLogStore per (EventId, HandlerName); notification and integration use tenant-scoped keys.
- **Replay:** SuppressSideEffects prevents async enqueue on replay; sync handlers idempotent or guarded.

---

## 5. Tenant-safety guarantees

- **Event envelope:** IDomainEvent.CompanyId required for tenant-scoped events; EventStoreEntry stores it.
- **Dispatcher:** EventStoreDispatcherHostedService runs each event in `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`.
- **Async jobs:** EventHandlingAsyncJobExecutor enforces job.CompanyId == entry.CompanyId.
- **Timeline handler:** Records only when event.CompanyId is set; uses it as TenantId; no cross-tenant write.
- **Guardrails:** EVENT_HANDLING_GUARDRAILS.md documents the five SaaS event mistakes and mitigations.

---

## 6. Tests added

- **TenantActivityTimelineFromEventsHandlerTests** (5 tests): HandleAsync_OrderCreatedEvent_WithCompanyId_RecordsToTimeline; WithNullCompanyId_DoesNotRecord; WithEmptyCompanyId_DoesNotRecord; OrderCompletedEvent_WithCompanyId_RecordsToTimeline; OrderStatusChangedEvent_WithCompanyId_RecordsToTimeline.
- **Pre-existing fix:** FeatureFlagServiceTests Company seed uses LegalName/ShortName so test project builds.

---

## 7. Docs updated

- **New:** ASYNC_EVENT_BUS_ARCHITECTURE.md, DOMAIN_EVENTS_GUIDE.md, EVENT_HANDLING_GUARDRAILS.md, ASYNC_EVENT_BUS_IMPLEMENTATION_SUMMARY.md.
- **Updated:** SAAS_ARCHITECTURE_MAP.md (event bus section and security table), SAAS_ENTERPRISE_UPGRADES.md (Async Event Bus §5), SAAS_REMEDIATION_CHANGELOG.md (new entry).

---

## 8. What remains for future event-driven scaling

- Optional **EventEnvelope** DTO for API/observability (EventStoreEntry is the persisted form).
- **More domain events** for billing, inventory, or platform operations as needed.
- **Event health** in platform observability dashboard (lag, failure rate per tenant).
- **Request-error aggregation** to TenantMetricsDaily and correlation with event failures.
- **Additional timeline event types** (e.g. InvoiceGenerated, WorkflowTransitionCompleted) if product requires.

---

## 9. Verdict

CephasOps already had a production event store, dispatcher, and tenant-scoped processing. This upgrade:

- **Documented** the existing architecture and guardrails.
- **Added** timeline-from-events integration (TenantActivityTimelineFromEventsHandler) for four order events.
- **Fixed** one unrelated test build error (FeatureFlagServiceTests Company).
- **Preserved** all tenant isolation and financial safety; no weakening of guards or bypass.

The platform now has a clear, documented async event backbone and is ready for incremental adoption of more event-driven flows.

# Event Handling Guardrails

**Date:** 2026-03-13  
**Purpose:** Guard against the five most common SaaS event-driven mistakes. All event handling in CephasOps must comply with these guardrails.

---

## 1. Missing tenant context in async handlers

**Risk:** A handler runs in a background job or delayed context and loses tenant identity, then writes data to the wrong tenant or fails in an unclear way.

**Guardrails:**

- Every **tenant-scoped event** must carry **CompanyId** on the event (IDomainEvent.CompanyId). Do not publish tenant-scoped events with null or empty CompanyId.
- **EventStoreDispatcherHostedService** runs each event under `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`. So when the handler runs, TenantScope.CurrentTenantId is set from the event’s CompanyId (or platform bypass only when CompanyId is null).
- **EventHandlingAsyncJobExecutor** (async handlers) loads the event from the store and enforces **job.CompanyId == entry.CompanyId** before running the handler; mismatch throws.
- Handlers that write tenant-scoped data must use the event’s CompanyId (or TenantScope.CurrentTenantId, which is set from it). If CompanyId is null, do not write tenant-scoped rows unless you have another explicit tenant source.

**Documentation:** IDomainEvent contract and DOMAIN_EVENTS_GUIDE require CompanyId for tenant-scoped events. EVENTSTORE_CONSISTENCY_GUARD documents append and replay tenant checks.

---

## 2. Cross-tenant handler execution

**Risk:** A handler processes an event that belongs to tenant A while executing in a context that writes to tenant B, or a job for tenant A processes an event for tenant B.

**Guardrails:**

- **Dispatcher:** Events from the store are dispatched only inside `RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`. So sync handlers run with tenant scope set to the event’s company.
- **Async jobs:** BackgroundJob carries CompanyId; EventHandlingAsyncJobExecutor loads the event and throws if `job.CompanyId != entry.CompanyId`. No cross-tenant processing.
- **Replay:** EventReplayService and OperationalReplayExecutionService filter by scopeCompanyId; when scope is provided, events for other tenants are not replayed (return "Event not in scope.").
- **Timeline:** TenantActivityTimelineFromEventsHandler records only when event.CompanyId is set; uses that as TenantId for the timeline row. No cross-tenant timeline entry.

**Documentation:** ASYNC_EVENT_BUS_ARCHITECTURE and EVENTSTORE_CONSISTENCY_GUARD describe dispatcher and job executor tenant enforcement.

---

## 3. Duplicate unsafe side effects

**Risk:** Notification or integration handlers run more than once (e.g. retry, replay) and send duplicate emails, duplicate webhooks, or duplicate external API calls.

**Guardrails:**

- **Replay:** When replay context has SuppressSideEffects = true, **async** handlers are not enqueued. So replay does not create duplicate async jobs for notifications/integrations.
- **Sync handler idempotency:** IEventProcessingLogStore.TryClaimAsync(EventId, HandlerName, ...) ensures each sync handler runs at most once per (EventId, HandlerName). Re-running the same event skips already-completed handlers.
- **Notification dispatch:** OrderStatusNotificationDispatchHandler uses idempotency key that includes sourceEventId; duplicate handler run does not send duplicate notification.
- **Outbound integration:** IOutboundIntegrationBus uses tenant-prefixed idempotency key (e.g. out-{CompanyId}-{EventId}-{EndpointId}); duplicate delivery is avoided.
- **Financial:** Invoice and payment creation support IdempotencyKey (e.g. order-invoice-{orderId}); automation and email ingestion set keys so replay does not create duplicate invoices/payments.
- **Handler-level guards:** OrderAssignedOperationsHandler skips SLA job enqueue when IsReplay to avoid duplicate SLA jobs.

**Documentation:** EVENTSTORE_CONSISTENCY_GUARD §4 (replay safeguards), §11 (sync handler inventory). Idempotency keys documented in SAAS_REMEDIATION_CHANGELOG and TENANT_FINANCIAL_SAFETY.

---

## 4. Platform bypass creep

**Risk:** Normal tenant-scoped event handling is implemented using platform bypass “for convenience,” weakening tenant isolation or audit.

**Guardrails:**

- **Do not use platform bypass** for normal event handling. EventStoreDispatcherHostedService uses **RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)**: when CompanyId has a value, scope is set to that tenant; bypass is used only when CompanyId is null (platform events).
- **Handlers** must not call EnterPlatformBypass / ExitPlatformBypass. They run in the scope provided by the dispatcher.
- **Reads in handlers:** Use the event’s CompanyId or TenantScope.CurrentTenantId for tenant-scoped queries. Do not use platform bypass to “see all tenants” in a handler.
- **Writes in handlers:** Tenant-scoped entities must be written with the correct CompanyId and within the current tenant scope. TenantSafetyGuard and SaveChanges tenant-integrity validation enforce this.

**Documentation:** TENANT_SCOPE_EXECUTOR_COMPLETION and 00_no_manual_scope: runtime code must use TenantScopeExecutor, not manual scope. EVENT_HANDLING_GUARDRAILS (this doc) state no bypass for normal event handling.

---

## 5. Event bus replacing transactional consistency incorrectly

**Risk:** Critical financial or core writes are moved to async event handlers, so that under failure or retry the system ends up with inconsistent or duplicate financial state.

**Guardrails:**

- **Strong consistency where required:** Invoice creation, payment creation, and payout snapshots that are critical for financial consistency are either done synchronously in the request path or use idempotency keys and same-transaction or tightly coupled flows. We do not move core financial writes to “fire-and-forget” async handlers without idempotency and audit.
- **Same-transaction emit:** WorkflowEngineService uses AppendInCurrentTransaction for workflow and order events so that the event is committed in the same transaction as the workflow state. Background dispatcher then processes the event. This preserves “happened in same transaction” for the event store.
- **Eventual consistency is acceptable** for: notifications, outbound integration delivery, tenant activity timeline, analytics, and non-financial side effects. These are designed to be idempotent or at-least-once with safe semantics.
- **Do not** move payment/invoice/payout persistence to an async handler that can run multiple times without idempotency. Keep critical writes in sync path or behind idempotency keys.

**Documentation:** FINANCIAL_ISOLATION, TENANT_FINANCIAL_SAFETY, and SAAS_REMEDIATION_CHANGELOG (financial idempotency) describe where strong consistency and idempotency are required.

---

## Summary

| Mistake | Mitigation |
|---------|------------|
| Missing tenant context | CompanyId on event; dispatcher sets scope from entry.CompanyId; async job executor enforces job vs event company match |
| Cross-tenant execution | Dispatch only under event’s CompanyId; replay filtered by scope; timeline records only with CompanyId |
| Duplicate side effects | SuppressSideEffects on replay; processing log per handler; idempotency keys for notification, integration, invoice, payment |
| Platform bypass creep | Dispatcher uses tenant scope when CompanyId set; handlers must not use bypass for normal handling |
| Wrong use of eventual consistency | Critical financial writes stay sync or idempotent; event bus used for safe side effects and decoupling |

These guardrails are part of the CephasOps event architecture and must not be weakened when adding new events or handlers.

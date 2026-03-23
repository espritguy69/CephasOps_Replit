# SaaS Enterprise Upgrades

**Date:** 2026-03-13  
**Purpose:** Summary of enterprise-level SaaS operational controls added without weakening tenant isolation.

## 1. Tenant Rate Limiting

- **Middleware:** TenantRateLimitMiddleware (existing); response body aligned to "Tenant request limit exceeded. Please retry later."; structured log **TenantRateLimitExceeded**.
- **Metrics:** RateLimitExceeded recorded in TenantUsageRecords; **TenantMetricsDaily.RateLimitExceededCount** populated by aggregation.
- **Docs:** [TENANT_RATE_LIMITING.md](TENANT_RATE_LIMITING.md).

## 2. Tenant Feature Flags

- **Service:** IFeatureFlagService / FeatureFlagService (Application/Platform/FeatureFlags).
- **Storage:** TenantFeatureFlags (existing entity).
- **Rules:** Tenant admins cannot enable platform-only keys (prefixes `Platform.`, `Admin.`); platform admin may override.
- **Docs:** [TENANT_FEATURE_FLAGS.md](TENANT_FEATURE_FLAGS.md).

## 3. Tenant Health Scoring

- **Service:** ITenantHealthScoringService / TenantHealthScoringService.
- **Storage:** TenantMetricsDaily.HealthScore, TenantMetricsDaily.HealthStatus (and RateLimitExceededCount).
- **Weights:** Job failures 30%, notification failures 20%, integration failures 20%, API error rate 20%, activity drop 10%. Score 90–100 Healthy, 70–89 Warning, &lt;70 Critical.
- **Execution:** After daily aggregation in TenantMetricsAggregationHostedService.
- **Dashboard:** Tenant operations overview shows HealthScore column.
- **Docs:** [TENANT_HEALTH_SCORING.md](TENANT_HEALTH_SCORING.md).

## 4. Tenant Activity Audit Timeline

- **Entity:** TenantActivityEvents (new table).
- **Service:** ITenantActivityService / TenantActivityService (Application/Audit).
- **API:** GET /api/platform/tenants/{tenantId}/activity-timeline (last 100 events).
- **Frontend:** Activity Timeline panel in tenant detail drawer (wire to above endpoint).
- **Docs:** [TENANT_ACTIVITY_TIMELINE.md](TENANT_ACTIVITY_TIMELINE.md).

## 5. Async Event Bus (Domain Events)

- **Purpose:** Scalable, tenant-safe event-driven backbone for orders, notifications, integrations, and audit timeline.
- **Abstractions:** IEventBus, IEventStore, IDomainEventDispatcher, IDomainEventHandler; EventStoreEntry (outbox).
- **Dispatch:** EventStoreDispatcherHostedService claims pending events and runs handlers under TenantScopeExecutor with entry.CompanyId.
- **First-use cases:** Tenant activity timeline (TenantActivityTimelineFromEventsHandler for OrderCreated, OrderCompleted, OrderAssigned, OrderStatusChanged), notification dispatch (OrderStatusNotificationDispatchHandler), outbound integration (IntegrationEventForwardingHandler).
- **Docs:** [ASYNC_EVENT_BUS_ARCHITECTURE.md](ASYNC_EVENT_BUS_ARCHITECTURE.md), [DOMAIN_EVENTS_GUIDE.md](DOMAIN_EVENTS_GUIDE.md), [EVENT_HANDLING_GUARDRAILS.md](EVENT_HANDLING_GUARDRAILS.md).

## 6. Guard Against Common SaaS Failures

- **Cross-tenant data leakage:** All queries use CompanyId/TenantId or EF global filters; no Guid.Empty/null to broaden scope.
- **Platform bypass abuse:** Bypass only in analytics, maintenance, seeding, provisioning; read-only where possible.
- **Financial context drift:** effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId; fail closed if missing.
- **Cache leakage:** React Query cache invalidates on tenant switch; query keys include tenant where possible.
- **Noisy tenant exhaustion:** TenantRateLimitMiddleware protects API; jobs/notifications/integrations constrained by existing queues and guards.

## Verification

- Tenant isolation tests remain in place; no removal of TenantSafetyGuard or financial isolation logic.
- All new data is tenant-scoped (TenantActivityEvents, TenantFeatureFlags, TenantMetricsDaily columns) or platform-only read (activity timeline, health score, observability).

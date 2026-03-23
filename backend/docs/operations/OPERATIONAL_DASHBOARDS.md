# Operational Dashboards (Command Center)

Operational dashboards provide read-only observability for platform admins and tenant admins. No business logic, tenant isolation, or financial services are modified.

## Architecture

- **Backend:** `CephasOps.Application.Insights.OperationalInsightsService` aggregates data from existing tables (Orders, TenantMetricsDaily, OrderPayoutSnapshots, ServiceInstallers, TenantActivityEvents, Invoices, OrderBlockers, OrderMaterialReplacements, JobEarningRecords, EventStore, SlaBreaches, OrderStatusLogs). When thresholds are exceeded, it raises **TenantAnomalyEvents** (observability only). **Operational Intelligence** is provided by `OperationalIntelligenceService` (rule-based risk signals for orders, installers, buildings, tenant); see [AUTOMATED_OPERATIONAL_INTELLIGENCE.md](AUTOMATED_OPERATIONAL_INTELLIGENCE.md). **SLA Breach Engine** is provided by `SlaBreachService` (nearing/breached classification using Order.KpiDueAt); see [SLA_BREACH_ENGINE.md](SLA_BREACH_ENGINE.md).
- **API:** `OperationalInsightsController` under `GET /api/insights/*`; `OperationalIntelligenceController` under `GET /api/insights/operational-intelligence/*` (tenant); `SlaBreachController` under `GET /api/insights/sla/*` (tenant); `PlatformOperationalIntelligenceController` for platform summary and platform-sla-summary (admin). Platform endpoints use `TenantScopeExecutor.RunWithPlatformBypassAsync`; tenant endpoints use request-scoped company from `RequireCompanyId(_tenantProvider)` and EF global query filters.
- **Frontend:** Dashboard pages under `frontend/src/pages/insights/` call the corresponding API; **Operational Intelligence** at `/insights/intelligence`, **SLA Breach** at `/insights/sla`; reusable components in `frontend/src/components/insights/` (MetricCard, TrendChart, StatusDistribution).

## Endpoints

| Route | Scope | Permission | Description |
|-------|--------|------------|-------------|
| `GET /api/insights/platform-health` | Platform | AdminTenantsView (SuperAdmin/Admin) | Active tenants, orders today, completion rate, failed orders, tenant health distribution, **event health** (events processed, event failures, retry queue size, event lag) |
| `GET /api/insights/tenant-performance` | Tenant | RequireCompanyId | Orders this month, completion rate, **avg install time**, active installers, device replacements, **orders within SLA**, **installer response time** |
| `GET /api/insights/operations-control` | Tenant | RequireCompanyId | Orders assigned/completed today, installers active, stuck orders, exceptions, **avg install time (today)**, **orders within SLA today** |
| `GET /api/insights/financial-overview` | Tenant | RequireCompanyId | Revenue today/month, installer payouts, profit margin, pending payouts |
| `GET /api/insights/risk-quality` | Tenant | RequireCompanyId | Customer complaints, device failures, rescheduled orders, installer rating (if available), repeat customer issues |
| `GET /api/insights/operational-intelligence/summary` | Tenant | RequireCompanyId | **Operational Intelligence:** summary counts (orders/installers/buildings at risk, severity bands). See [AUTOMATED_OPERATIONAL_INTELLIGENCE.md](AUTOMATED_OPERATIONAL_INTELLIGENCE.md). |
| `GET /api/insights/operational-intelligence/orders-at-risk` | Tenant | RequireCompanyId | Orders with risk signals (stuck, reschedule-heavy, blocker accumulation, etc.); optional `?severity=`. |
| `GET /api/insights/operational-intelligence/installers-at-risk` | Tenant | RequireCompanyId | Installers with risk signals; optional `?severity=`. |
| `GET /api/insights/operational-intelligence/buildings-at-risk` | Tenant | RequireCompanyId | Buildings/sites with risk signals; optional `?severity=`. |
| `GET /api/insights/operational-intelligence/tenant-risk-signals` | Tenant | RequireCompanyId | Tenant-level risk signals (stuck spike, abnormal replacement ratio). |
| `GET /api/insights/platform-operational-intelligence` | Platform | AdminTenantsView (SuperAdmin/Admin) | Aggregated operational intelligence summary across tenants (counts only). |
| `GET /api/insights/sla/summary` | Tenant | RequireCompanyId | **SLA Breach Engine:** distribution (OnTrack, NearingBreach, Breached, NoSla). See [SLA_BREACH_ENGINE.md](SLA_BREACH_ENGINE.md). |
| `GET /api/insights/sla/orders-at-risk` | Tenant | RequireCompanyId | Orders nearing or in breach; optional `?breachState=` and `?severity=`. |
| `GET /api/insights/platform-sla-summary` | Platform | AdminTenantsView (SuperAdmin/Admin) | Aggregated SLA distribution across tenants. |

## Dashboard Metrics

### Platform Health (platform admin only)

- **activeTenants:** Count of active tenants.
- **ordersToday:** Orders created today across all tenants.
- **completionRate:** Percentage of today’s orders with status Completed.
- **avgCompletionTimeHours:** Reserved (null if not computed).
- **failedOrders:** Orders in Rejected or DocketsRejected.
- **tenantHealthDistribution:** Counts per HealthStatus (Healthy, Warning, Critical) from TenantMetricsDaily.
- **eventsProcessed:** EventStore entries with Status = Processed and ProcessedAtUtc in last 24h.
- **eventFailures:** EventStore entries with Status Failed or DeadLetter and LastErrorAtUtc in last 24h.
- **retryQueueSize:** EventStore entries with Status Pending or (Failed and NextRetryAtUtc set).
- **eventLagSeconds:** Age in seconds of the oldest Pending event (null if none).

### Tenant Performance

- **ordersThisMonth,** **completionRate,** **avgInstallTimeHours** (from OrderStatusLog: Assigned → Completed), **activeInstallers,** **deviceReplacements.**
- **ordersCompletedWithinSla:** Completed orders this month minus SlaBreach count in period.
- **ordersBreachedSla:** Count of SlaBreach for company in the month.
- **installerResponseTimeHours:** Average time from order CreatedAt to first OrderStatusLog ToStatus = Assigned in the month.

### Operations Control

- **ordersAssignedToday:** Orders with AssignedSiId and UpdatedAt today.
- **ordersCompletedToday:** Orders with Status Completed and UpdatedAt today.
- **installersActive:** Active service installers for the company.
- **stuckOrders:** Orders where status ≠ Completed, AssignedSiId is set, and UpdatedAt &lt; now − 4 hours.
- **stuckOrdersList:** Up to 20 stuck orders (OrderId, Status, UpdatedAtUtc).
- **exceptions:** Count of TenantActivityEvent with EventType "Exception" in the last 7 days.
- **avgInstallTimeHours:** Average install time (Assigned → Completed from OrderStatusLog) for orders completed today.
- **ordersCompletedWithinSlaToday,** **ordersBreachedSlaToday:** From SlaBreach count today and completed count.

### Financial Overview

- **revenueToday / revenueMonth:** Sum of Invoice.TotalAmount for the period.
- **installerPayouts:** Sum of OrderPayoutSnapshot.FinalPayout for the month.
- **profitMarginPercent:** (revenueMonth − installerPayouts) / revenueMonth × 100 when revenueMonth &gt; 0.
- **pendingPayouts:** Sum of snapshots for orders not yet present in JobEarningRecords.

### Risk & Quality

- **customerComplaints:** OrderBlockers created this month.
- **deviceFailures:** OrderMaterialReplacements recorded this month.
- **rescheduledOrders:** Orders with RescheduleCount &gt; 0 updated this month.
- **installerRatingAverage:** Reserved (null if no rating source).
- **repeatCustomerIssues:** Count of customers (by CustomerPhone) with more than one order this month.

## Permission Model

- **Platform Health:** Requires `[Authorize(Roles = "SuperAdmin,Admin")]` and `[RequirePermission(PermissionCatalog.AdminTenantsView)]`. Tenant users receive 403.
- **Tenant dashboards:** Require valid company context via `RequireCompanyId(_tenantProvider)`. Missing or empty company returns 403. All queries are scoped by CompanyId (and TenantId where applicable); tenant admins only see their own company data.

## Security and Tenant Safety

- No changes to Order, Billing, or payout logic.
- TenantScopeExecutor used only for platform health (RunWithPlatformBypassAsync).
- Tenant endpoints rely on middleware-set TenantScope and explicit CompanyId filtering; TenantSafetyGuard and EF global filters remain in effect.
- No new migrations required; uses existing tables only.

## Performance

- Queries are aggregated (counts, sums) and avoid loading large datasets.
- Stuck orders list is limited to 20 rows.
- Target: dashboard responses under 200 ms with indexed filters (CompanyId, TenantId, date ranges).

## Anomaly alerts

When dashboard data is loaded, the service evaluates thresholds and **raises TenantAnomalyEvents** (same table and format as Platform Guardian). At most one event per kind per tenant per hour (deduplicated).

| Condition | Kind | Severity |
|-----------|------|----------|
| Stuck orders ≥ 5 | StuckOrdersAnomaly | Warning; ≥ 10 → Critical |
| Completion rate today &lt; 80% (of assigned) | HighFailureRate | Warning; &lt; 60% → Critical |
| Device/material replacements this month ≥ 10 | AbnormalMaterialReplacements | Warning; ≥ 25 → Critical |

These appear in platform observability (anomalies) and can drive alerts.

## Testing

- **OperationalInsightsApiTests:** Platform admin receives 200 for platform-health; tenant user with company receives 200 for tenant endpoints; tenant user without company receives 403 for tenant endpoints; member without AdminTenantsView receives 403 for platform-health.

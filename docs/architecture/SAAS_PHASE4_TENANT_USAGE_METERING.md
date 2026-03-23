# SaaS Phase 4 — Tenant Usage Metering

## Overview

Phase 4 adds a **tenant usage metering engine** so CephasOps can answer, per tenant:

- How many active/total users?
- How many orders were created?
- How many invoices were generated?
- How many report exports were triggered?
- How many background jobs ran?

Usage is stored in **TenantUsageRecords**, attributable to one tenant (via TenantId), and usable for analytics, plan limits, and future Stripe billing.

## Usage Model

- **Entity:** `TenantUsageRecord` (Domain.Billing.Entities). Columns: Id, TenantId, MetricKey, Quantity, PeriodStartUtc, PeriodEndUtc, CreatedAtUtc, UpdatedAtUtc.
- **Time bucketing:** Monthly (UTC). One row per (TenantId, MetricKey, PeriodStartUtc); Quantity is incremented or set.
- **Write:** Upsert by (TenantId, MetricKey, PeriodStartUtc). New row inserted or existing row’s Quantity updated and UpdatedAtUtc set.
- **Tenant resolution:** Callers usually have CompanyId (request or job context). The write service resolves Company → TenantId via Company.TenantId; when TenantId is null (legacy company), recording is skipped.

## Metric Definitions

| Metric | Classification | Write point |
|--------|----------------|------------|
| **OrdersCreated** | Event-driven | OrderService.CreateOrderAsync, CreateFromParsedDraftAsync (after save) |
| **InvoicesGenerated** | Event-driven | BillingService.CreateInvoiceAsync (after save) |
| **ReportExports** | Event-driven | ReportsController (stock-summary, orders-list, ledger, materials-list, scheduler-utilization export actions) |
| **BackgroundJobsExecuted** | Event-driven | JobExecutionWorkerHostedService (on MarkSucceededAsync); InventoryReportExportJobExecutor; legacy BackgroundJobProcessorService on success |
| **TotalUsers** | Derived | RecalculateUserMetricsForTenantAsync (count Users by company for tenant) |
| **ActiveUsers** | Derived | RecalculateUserMetricsForTenantAsync (count active Users by company for tenant) |

Exact event-driven metrics are incremented once per committed action. TotalUsers/ActiveUsers are snapshot values for the current month and can be recalculated on demand or by a scheduled job (not wired in this phase).

## Services

- **ITenantUsageService** (Billing.Usage): `RecordUsageAsync(companyId, metricKey, quantity)` — resolves tenant, current month bucket, upsert increment. `RecalculateUserMetricsForTenantAsync(tenantId)` — recomputes TotalUsers/ActiveUsers for current month.
- **ITenantUsageQueryService** (Billing.Usage): `GetUsageAsync(tenantId, periodStart, periodEnd)`, `GetCurrentMonthUsageAsync(tenantId)`, `GetMetricUsageAsync(tenantId, metricKey, periodStart, periodEnd)`.

## API Surface

- **Tenant (own usage):**
  - **GET** `/api/usage/current` — current tenant’s usage for the current month (requires tenant context).
  - **GET** `/api/usage/current/by-month?year=&month=` — current tenant’s usage for the given month.
- **Platform admin:**
  - **GET** `/api/platform/usage/tenants/{tenantId}` — tenant’s usage for current month (AdminTenantsView).
  - **GET** `/api/platform/usage/tenants/{tenantId}/by-month?year=&month=` — tenant’s usage for the given month (AdminTenantsView).

Tenant admin sees only their tenant (via ITenantContext). Platform admin can query any tenant by ID.

## Tenant Isolation

- All writes use TenantId (resolved from CompanyId when present). No cross-tenant writes.
- Reads: tenant-scoped endpoints use ITenantContext.TenantId; platform endpoints accept tenantId and are protected by AdminTenantsView.
- Background jobs: JobExecution.CompanyId is used to resolve tenant before recording BackgroundJobsExecuted; jobs without CompanyId do not record usage.

## Performance and Safety

- Writes are a single upsert (find or create, then update Quantity); no heavy aggregation on the write path.
- Recording is best-effort: failures are logged and do not fail the main operation (where the service is optional/null).
- Retries: upsert by (TenantId, MetricKey, PeriodStartUtc) is idempotent for increments; duplicate events may over-count unless deduplication is added elsewhere.

## Limitations / Future Work

- **TotalUsers / ActiveUsers** are not auto-updated on user create/delete; call RecalculateUserMetricsForTenantAsync when needed (e.g. from a scheduled job or admin action).
- **FilesStored / StorageBytesUsed** were not implemented in this phase.
- **ParserSessionsProcessed** and **ApiRequests** were not added; can be wired later using the same pattern.
- No enforcement of plan limits or seat caps in this phase; usage is recorded for future Phase 5 (Stripe) and enforcement.
- Billing cycle alignment (e.g. subscription period) is not applied; “current month” is calendar month UTC.

## Audit

See `docs/architecture/SAAS_PHASE4_USAGE_METERING_AUDIT.md` for the initial audit of existing usage/billing structures.

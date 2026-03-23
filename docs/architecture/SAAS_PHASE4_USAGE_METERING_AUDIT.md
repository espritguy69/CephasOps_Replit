# SaaS Phase 4 — Tenant Usage Metering Audit

**Implementation:** See [SAAS_PHASE4_TENANT_USAGE_METERING.md](./SAAS_PHASE4_TENANT_USAGE_METERING.md) for the usage model, metrics, services, API, and isolation.

---

## 1. Existing Structures

### TenantUsageRecord (Implemented)
- **Location:** `CephasOps.Domain.Billing.Entities.TenantUsageRecord`
- **Schema:** Id, TenantId, MetricKey (max 64), Quantity (decimal 18,4), PeriodStartUtc, PeriodEndUtc, CreatedAtUtc
- **Table:** TenantUsageRecords, unique index (TenantId, MetricKey, PeriodStartUtc)
- **Migration:** 20260310041112_Phase12_SubscriptionBilling
- **Usage:** No application service currently writes to it. Table exists and is suitable for metering.

### TenantSubscription / BillingPlan (Implemented)
- TenantSubscription, BillingPlan, TenantInvoice exist. No PlanFeature entity found. Subscription enforcement (Phase 3) uses status/plan.

### Report export (Implemented)
- ReportsController: ExportStockSummary, ExportOrdersList, ExportLedger, ExportMaterialsList, ExportSchedulerUtilization (GET .../export). Each uses companyId from _currentUserService and returns File(csv/xlsx/pdf).
- IReportExportFormatService.ExportToExcelBytes / ExportToPdfBytes; ICsvService.ExportToCsvBytes. Export is synchronous in controller.

### Background jobs (Implemented)
- JobExecution entity has CompanyId (nullable). JobExecutionWorkerHostedService marks Succeeded via store.MarkSucceededAsync(job.Id). Job types run via IJobExecutor.ExecuteAsync. No usage recording today.

### Order creation (Implemented)
- OrderService.CreateOrderAsync(CreateOrderDto, companyId, userId, departmentId). Saves order then returns. No usage recording.

### Invoice generation (Implemented)
- BillingService.CreateInvoiceAsync(dto, companyId, userId). Saves invoice then returns. No usage recording.

### File / Document storage
- File upload and document generation exist. Storage bytes would require aggregating file sizes by company; deferred as optional.

### Health / metrics endpoints
- No existing tenant usage or metering endpoints found.

## 2. Classification

| Item | Status | Notes |
|------|--------|--------|
| TenantUsageRecord | Implemented | Schema exists; no writer yet |
| Usage write service | Missing | To be added |
| Usage query service | Missing | To be added |
| Order creation → usage | Missing | Wire in OrderService |
| Invoice creation → usage | Missing | Wire in BillingService |
| Report export → usage | Missing | Wire in ReportsController |
| Background job → usage | Missing | Wire in JobExecutionWorkerHostedService |
| User counts (Total/Active) | Missing | Derived; recalc or event |
| Usage read API | Missing | Tenant + platform admin |
| Time bucketing | Missing | Monthly in usage service |

## 3. Decisions for Implementation

- Reuse **TenantUsageRecord**; add **UpdatedAtUtc** for upsert semantics.
- Resolve **TenantId** from **CompanyId** via Company.TenantId (request and job context use CompanyId).
- **Monthly** buckets: PeriodStartUtc = start of month UTC, PeriodEndUtc = end of month UTC.
- **Metrics:** OrdersCreated, InvoicesGenerated, ReportExports, BackgroundJobsExecuted (event-driven); TotalUsers, ActiveUsers (derived/recalculated).
- One write service (upsert by tenant + metric + period), one query service (summary by tenant + period).

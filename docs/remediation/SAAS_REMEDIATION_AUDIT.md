# SaaS Remediation Audit

**Date:** 2026-03-13  
**Scope:** Post-migration audit for single-company → multi-tenant SaaS break risks.  
**Objective:** Identify and remediate legacy single-company assumptions that break or weaken tenant isolation.

---

## 1. Tenant resolution and middleware

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| Tenant resolution | `TenantGuardMiddleware` | — | — | Middleware calls `GetEffectiveCompanyIdAsync()` and blocks when no tenant. No change. | Platform |
| Tenant scope set | `Program.cs` (inline middleware) | — | — | Sets `TenantScope.CurrentTenantId = tenantProvider.CurrentTenantId` after TenantGuard. No change. | Platform |
| SuperAdmin + null companyId | `OrderService.GetOrderByIdAsync` | When `companyId` is null, query relies only on global filter (current tenant from middleware). Comment says "SuperAdmin can access all companies" but effectively one company per request via X-Company-Id/JWT. | Low | Document: when companyId is null, result is still scoped by current tenant (X-Company-Id or JWT). No code change. | Tenant (scoped per request) |

---

## 2. Query safety (EF, IgnoreQueryFilters)

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| Order delete | `OrderService.DeleteOrderAsync` | Uses `IgnoreQueryFilters` with explicit company (companyId or TenantScope). OK. | — | None | Tenant |
| Order profitability | `OrderProfitabilityService.CalculateOrderProfitabilityAsync` | Uses `IgnoreQueryFilters` with explicit `Where(o => o.Id == orderId && o.CompanyId == companyId)`. OK. | — | None | Tenant |
| Asset disposal | `AssetService` (disposal) | `IgnoreQueryFilters` with `Where(a => a.Id == disposal.AssetId && a.CompanyId == disposal.CompanyId)`. OK. | — | None | Tenant |
| Workflow engine | `WorkflowEngineService` | Order lookups use `IgnoreQueryFilters` with explicit `o.CompanyId == companyId`. OK. | — | None | Tenant |
| Rate engine | `RateEngineService` | `IgnoreQueryFilters` with `Where(r => r.CompanyId == companyId.Value)`. OK. | — | None | Tenant |
| Auth | `AuthService` | Refresh token, user by email, password reset: `IgnoreQueryFilters` for platform-wide auth lookups (no company). Documented. OK. | — | None | Platform |
| Platform support | `PlatformSupportController` | `IgnoreQueryFilters` with `Where(u => u.CompanyId == companyId && u.IsActive)`. OK. | — | None | Platform (admin) |
| Background jobs | `BackgroundJobProcessorService` | `IgnoreQueryFilters` to list queued/running jobs; scope set per job in `ProcessJobAsync`. OK. | — | None | Platform |
| Event retention | `EventPlatformRetentionService` | `IgnoreQueryFilters` inside `RunWithPlatformBypassAsync` for platform-wide retention. OK. | — | None | Platform |
| Department access | `DepartmentAccessService` | `IgnoreQueryFilters` only when `EnvironmentName == "Testing"`. Production uses normal filter. OK. | — | None | Tenant |
| PnL OrderProfitability | `OrderProfitabilityService` (SI lookup) | `IgnoreQueryFilters` with `Where(s => s.Id == ... && s.CompanyId == order.CompanyId)`. OK. | — | None | Tenant |

---

## 3. Writes and CompanyId stamping

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| Order create | `OrderService.CreateOrderAsync` | Order and related entities receive `CompanyId = companyId`. OK. | — | None | Tenant |
| File upload | `FileService.UploadFileAsync` | File entity has `CompanyId = companyId`. OK. | — | None | Tenant |
| Create from draft | `OrderService.CreateFromParsedDraftAsync` | Uses `dto.CompanyId` for order and job. OK. | — | None | Tenant |

---

## 4. File / document lookup

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| File content | `FileService.GetFileContentAsync(Guid fileId)` | No company parameter; relies on global filter. When called outside request (e.g. job with no scope), `CurrentTenantId` can be null and filter allows all rows → cross-tenant read risk. | **High** | Add optional `Guid? companyId`; resolve effective company from `companyId ?? TenantScope.CurrentTenantId`; if empty, return null; else query with explicit `f.CompanyId == effectiveCompanyId`. Update callers to pass company when available. | Tenant |
| GetFileInfoAsync | `FileService.GetFileInfoAsync` | Interface says "without company restriction" – used for internal lookups. Same null-scope risk as GetFileContentAsync. | Medium | Prefer explicit company where possible; document that callers must be in tenant scope or pass company. Optional: add companyId overload. | Tenant |
| Download/Delete/Metadata | `FileService` | All take `companyId` and filter by it. OK. | — | None | Tenant |

---

## 5. Background jobs and hosted services

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| Tenant metrics aggregation | `TenantMetricsAggregationHostedService` + `TenantMetricsAggregationJob` | Job reads `TenantUsageRecords` / writes `TenantMetricsDaily` and `TenantMetricsMonthly`. These entities are not in `IsTenantScopedEntityType`; no global filter on usage records. Job runs without executor; if any future entity were tenant-scoped, SaveChanges could throw. | Medium | Run job inside `TenantScopeExecutor.RunWithPlatformBypassAsync` to document platform-wide behavior and avoid future guard failures. | Platform |
| Job execution worker | `JobExecutionWorkerHostedService` | Uses `RunWithTenantScopeOrBypassAsync(job.CompanyId, ...)`. OK. | — | None | Tenant/Platform |
| Background job processor | `BackgroundJobProcessorService` | Uses executor for process and reap. OK. | — | None | Platform |
| Event dispatcher / replay | `EventStoreDispatcherHostedService`, `EventReplayService` | Use `RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`. OK. | — | None | Tenant/Platform |
| Other schedulers | EmailIngestion, PnlRebuild, LedgerReconciliation, StockSnapshot, MissingPayoutSnapshot, PayoutAnomaly, SlaEvaluation | Use executor (bypass or per-tenant). OK. | — | None | Platform/Tenant |

---

## 6. Dashboard / operations overview

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| Operations overview | `OperationsOverviewService.GetOverviewAsync` | Calls event store, payout health, job summary with `scopeCompanyId: null` → platform-wide dashboard. Used by admin. OK. | — | None | Platform |
| Event store dashboard | `EventStoreQueryService.GetDashboardAsync` | When `scopeCompanyId` is null, returns all tenants. OK for platform admin. | — | None | Platform |

---

## 7. Cache and idempotency

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| Idempotency | `IdempotencyBehavior` / command store | Idempotency key is not prefixed by tenant. Two tenants using same key could conflict. | Low | Document: idempotency keys should be tenant-specific when provided by client, or store could add tenant to key when in tenant context. Optional follow-up. | Tenant |
| TenantProvider | `TenantProvider` | Caches `_cachedEffectiveCompanyId` per request. OK. | — | None | — |
| LedgerBalanceCache | Entity + ReconcileLedgerBalanceCache job | Cache is per CompanyId/DepartmentId. Job runs via scheduler (per-tenant enqueue or bypass). OK. | — | None | Tenant |

---

## 8. Raw SQL

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| ParserTemplateService, EmailTemplateService, VipGroupService, etc. | ExecuteSqlRaw with CompanyId in WHERE | All include CompanyId in predicates. OK. | — | None | Tenant |
| WorkerCoordinatorService | UPDATE BackgroundJobs | Raw SQL for job claim; runs under job processor with executor. OK. | — | None | Platform |

---

## 9. Uniqueness and validation scope

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| Order duplicate check | `OrderService` (ServiceId/TicketId, etc.) | Queries scoped by companyId. OK. | — | None | Tenant |
| Document/code lookups | Various services | Where audited, company scope applied. No global uniqueness assumed. | — | None | Tenant |

---

## 10. Frontend / DTO and API contracts

| Area | File / method | Issue type | Severity | Remediation | Ownership |
|------|----------------|------------|----------|-------------|-----------|
| DTOs | Various | No audit of frontend in this pass. Backend DTOs include CompanyId where needed. | — | Manual QA for pages that show blank or wrong totals. | — |

---

## Summary

- **Critical:** None (no unconstrained cross-tenant read/write found beyond file content edge case).
- **High:** 1 – `FileService.GetFileContentAsync` without company scoping when tenant scope is null.
- **Medium:** 2 – Tenant metrics job not wrapped in platform bypass; `GetFileInfoAsync` same pattern as GetFileContentAsync.
- **Low:** 2 – GetOrderByIdAsync comment; idempotency key tenant isolation (optional).

Remediation order: (1) File content/GetFileContentAsync, (2) Tenant metrics hosted service, (3) Documentation/comment updates, (4) Optional idempotency key hardening.

# SaaS Multi-Tenant Closeout Pass

## Summary

This document records the outcome of the mandatory SaaS multi-tenant closeout pass: build closure, tenant isolation enforcement, tenant-aware auth and job context, and documentation of what is complete vs deferred.

---

## A. Build closure status

**Status: GREEN**

- **Fix applied**: Removed circular dependency (Application ↔ Infrastructure) by moving `TenantScope` from `CephasOps.Application.Common` to `CephasOps.Infrastructure.Persistence`. Infrastructure no longer references Application; API middleware sets `TenantScope.CurrentTenantId` from `ITenantProvider` (Application interface, Api implementation).
- **Result**: `dotnet build` from `backend/src/CephasOps.Api` succeeds with 0 errors. Warnings are pre-existing (XML param tags, etc.).
- **Tests**: 737 passed, 2 failed (pre-existing: `SlaEvaluationSchedulerServiceTests` reflection parameter mismatch), 7 skipped.

---

## B. Migration/schema closure status

**Status: VERIFIED (with known drift)**

- **Database**: Tables `Tenants`, `Companies` (with `TenantId`), `BillingPlans`, `TenantSubscriptions`, `TenantUsageRecords`, `TenantInvoices` exist (verified via `information_schema.tables`).
- **Applied via**: Phase 11 and Phase 12 were applied using idempotent SQL scripts (not `dotnet ef database update`) because the EF migration chain is drift-prone (e.g. `VerifyNoPending` fails when column already exists).
- **EF drift**: The in-db migration history may not match the full list of migrations in code if older migrations were skipped or applied manually. For continued work, prefer idempotent scripts generated from the last known good migration to the target migration, or fix history and re-run. Schema and model are aligned for Phase 11 and Phase 12 entities.

---

## C. Tenant isolation coverage verified

**Enforcement mechanism**

- **Global query filter** in `ApplicationDbContext.OnModelCreating`: All entities inheriting `CompanyScopedEntity` get a filter: `IsDeleted == false` AND (`TenantScope.CurrentTenantId == null` OR `CompanyId == TenantScope.CurrentTenantId`). So when `CurrentTenantId` is set (HTTP request or job context), only that company’s rows are visible.
- **User** and **BackgroundJob** (non–CompanyScopedEntity) have explicit tenant filters using the same `TenantScope.CurrentTenantId`.

**API layer**

- Controllers use `ScopeCompanyId()` (super-admin → null, else `ICurrentUserService.CompanyId`) for operator/event-store/command/integration/trace endpoints.
- Many controllers enforce company explicitly: `companyId = _currentUserService.CompanyId` and return 403/400 when null and not super-admin (e.g. Inventory, Billing, BuildingTypes, Payments).

**Verified**

- Read/list/update/delete for CompanyScopedEntity go through the same DbContext; the global filter applies. No code path was found that bypasses the filter (e.g. raw SQL without tenant).
- **Gap repaired**: Background job and JobExecution workers did not set `TenantScope.CurrentTenantId` before processing, so workers could see all companies. **Fix**: `BackgroundJobProcessorService.ProcessJobAsync` sets `TenantScope.CurrentTenantId = job.CompanyId ?? TryGetCompanyIdFromPayload(payload)` at start and restores it in `finally`. `JobExecutionWorkerHostedService` sets `TenantScope.CurrentTenantId = job.CompanyId` per job and restores in `finally`.

**Remaining risk**

- Any future code that creates a new `ApplicationDbContext` without going through the request or job path (e.g. a one-off console job that does not set `TenantScope`) will see all rows if `CurrentTenantId` is null. New background entry points must set and clear `TenantScope.CurrentTenantId` explicitly.

---

## D. Tenant auth/context verified

**JWT and current user**

- **AuthService**: On login/refresh, resolves company via `ResolveUserCompanyIdAsync` (User.CompanyId or first department’s company) and adds claims `companyId` and `company_id` to the JWT.
- **ICurrentUserService**: Reads `UserId`, `CompanyId` (from JWT or company_id claim), `IsSuperAdmin`, roles from HTTP context.
- **ITenantProvider**: Returns `CurrentTenantId` (company id): JWT company_id → X-Company-Id header → TenantOptions.DefaultCompanyId. Registered in Api and used by middleware to set `TenantScope.CurrentTenantId`.

**Tenant context**

- **ITenantContext** (Phase 11): Resolved in Api from current user’s company’s `TenantId` (and slug). Used by subscription/me endpoints.
- **Middleware**: Before authorization, `TenantScope.CurrentTenantId` is set from `ITenantProvider.CurrentTenantId` and cleared in `finally`.

**Platform vs tenant admin**

- SuperAdmin/Admin roles and `RequirePermission` (e.g. AdminTenantsView, AdminBillingPlansEdit) restrict tenant/admin APIs. Non–super-admin requests use `CompanyId` for scoping; super-admin can pass `null` for company to see all (e.g. `ScopeCompanyId()`).

**Verified**

- JWT includes company identity; tenant scope is set per request; services that use the same DbContext see the filter. No inconsistency found between auth and tenant context for normal API requests.

---

## E. Tenant-aware jobs/workers verified

**BackgroundJob (legacy)**

- **BackgroundJobProcessorService.ProcessJobAsync**: Sets `TenantScope.CurrentTenantId` to `job.CompanyId ?? TryGetCompanyIdFromPayload(payload)` at start and restores in `finally`. All legacy job types (email ingest, notification, MyInvois poll, etc.) now run under the job’s company scope.

**JobExecution (Phase 9 pipeline)**

- **JobExecutionWorkerHostedService**: For each claimed job, sets `TenantScope.CurrentTenantId = job.CompanyId` before execution and restores in `finally`. Executors (PnlRebuild, DocumentGeneration, EmailIngest, etc.) run under the job’s company.

**Verified**

- Both execution paths set and restore tenant scope. No other worker entry points were found that run company-scoped work without setting scope.

**Remaining risk**

- **NotificationDispatch** and any other fire-and-forget or queue-based paths that create their own scopes must set `TenantScope.CurrentTenantId` if they touch CompanyScopedEntity data. Not fully audited in this pass.

---

## F. Reporting/export isolation verified

**Mechanism**

- Reporting and exports use the same ApplicationDbContext and services. When run from API, the request middleware has set `TenantScope.CurrentTenantId`, so the global filter applies.
- When run from a background job, the job’s company context is now set (see E), so exports/jobs (e.g. inventory report export, P&L rebuild) are company-scoped.

**Audited**

- P&L, billing, inventory summaries, and scheduler/admin dashboards are served by controllers that use `ICurrentUserService.CompanyId` or `ScopeCompanyId()` and/or the same DbContext. No explicit cross-tenant aggregation path was found.
- **Assumption**: All reporting endpoints are either (1) called from HTTP with tenant scope set, or (2) triggered by jobs that now set tenant scope. Any future report triggered from a context that does not set `TenantScope` could leak; such entry points should be reviewed.

**Risk level**

- Medium: No proof of cross-tenant leak was found, but not every report/export path was traced end-to-end. Recommend a dedicated audit of all report/export entry points and their callers.

---

## G. Regression validation results

**Not run end-to-end**

- Legacy Cephas flows (email parser → order, order → scheduler, installer workflow, materials/inventory, invoice generation, MyInvois submission/polling, payroll, P&L reporting) were not executed in this pass. Validation was by code audit and build/tests only.

**Code-level checks**

- Tenant scope is set for HTTP requests and for both BackgroundJob and JobExecution workers. No change was made to parser, order, workflow, or invoice logic beyond ensuring job context is set.
- The 2 failing tests are pre-existing (SlaEvaluationSchedulerServiceTests, reflection); 737 tests pass including tenant/service tests.

**Recommendation**

- Run a focused regression suite (or manual smoke) for: login → company context, order list by company, email ingest job for one company, P&L run for one company, invoice generation for one company. This pass did not execute those flows.

---

## H. Remaining SaaS risks

1. **EF migration drift**: Full migration history may not match database. Prefer idempotent scripts for new migrations; document any manual history fixes.
2. **Entry points without tenant scope**: Any new background or console entry point that uses ApplicationDbContext must set and clear `TenantScope.CurrentTenantId`.
3. **NotificationDispatch / other workers**: Not fully audited for tenant scope; may need the same set/restore pattern if they touch CompanyScopedEntity.
4. **Reporting/export**: No full end-to-end proof of isolation for every report; recommend a dedicated audit of report entry points and callers.
5. **Legacy regression**: No automated or manual run of legacy Cephas flows; recommend smoke tests for parser, order, scheduler, invoice, payroll, P&L.
6. **Tenant entity vs company**: Current isolation is by CompanyId (tenant scope holds company id). Tenant entity exists (Phase 11) but filtering is still by company; multi-company-per-tenant scenarios may need explicit TenantId in filters later.

---

## I. Files/docs changed

**Code**

- `backend/src/CephasOps.Infrastructure/Persistence/TenantScope.cs` — added (moved from Application).
- `backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs` — removed `using CephasOps.Application.Common`; filter uses `Persistence.TenantScope`.
- `backend/src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj` — removed ProjectReference to Application.
- `backend/src/CephasOps.Api/Program.cs` — middleware now sets `CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId`.
- `backend/src/CephasOps.Application/Workflow/Services/BackgroundJobProcessorService.cs` — set/restore `TenantScope.CurrentTenantId` in `ProcessJobAsync`.
- `backend/src/CephasOps.Application/Workflow/JobOrchestration/JobExecutionWorkerHostedService.cs` — set/restore `TenantScope.CurrentTenantId` per job.
- `backend/src/CephasOps.Application/Common/TenantScope.cs` — deleted (moved to Infrastructure).

**Docs**

- `docs/SAAS_MULTI_TENANT_CLOSEOUT.md` — created (this file).

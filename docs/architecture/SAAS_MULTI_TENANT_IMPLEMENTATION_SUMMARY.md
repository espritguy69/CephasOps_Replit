# SaaS Multi-Tenant Phase — Implementation Summary

## 1. Tenant Architecture Decision

- **Tenant root:** **Company** is the operational tenant root. All tenant-owned data is keyed by **CompanyId**.
- **Tenant entity:** Retained for billing only (TenantSubscription, Slug). 1:1 with Company: one Company = one Tenant.
- **Isolation key:** **CompanyId** on every tenant-owned entity. Resolution: **SuperAdmin only** may override via `X-Company-Id` header; **normal users** use JWT `company_id` only; fallback **Tenant:DefaultCompanyId** (config). See § Tenant resolution security below.
- **Default tenant:** Company **Name "Cephas", Code "CEPHAS"**. Migration creates/links default Tenant and backfills all NULL `CompanyId` to this company.

## 2. Tenant-Owned Table Coverage

- **CompanyScopedEntity** (all inheriting entities): Global query filter applied: `!IsDeleted && (TenantScope.CurrentTenantId == null || CompanyId == TenantScope.CurrentTenantId)`.
- **User:** Added **CompanyId**; same tenant filter applied.
- **BackgroundJob:** Added **CompanyId**; same tenant filter applied.
- **Company:** Added **Code**, **SubscriptionId**. No tenant filter (root entity).
- **Tenant, BillingPlan:** Global/platform; no CompanyId.

## 3. Migration / Backfill Status

- **Migration:** `20260310042938_AddMultiTenantArchitecture`
  - Adds `Users.CompanyId`, `Companies.Code`, `Companies.SubscriptionId`, `BackgroundJobs.CompanyId` and indexes.
  - **Data steps:**
    - Creates default **Tenant** (Name: Cephas, Slug: cephas) if missing.
    - Sets first **Company** to `Code = 'CEPHAS'`, `TenantId = <default tenant>`.
    - Backfills all tables with column `company_id` where NULL to default company id.
    - Explicit backfill for **Users** and **BackgroundJobs** (column `CompanyId`).
- **Post-migration:** Set **Tenant:DefaultCompanyId** in appsettings (or env) to the Cephas company Guid so unauthenticated/legacy flows resolve to the default tenant. Optional if all logins include `company_id` in JWT.

## 4. Auth Changes

- **JWT:** Login and refresh now set **company_id** and **companyId** claims from:
  - **User.CompanyId** if set, else
  - First **DepartmentMembership** → **Department.CompanyId**.
- **ResolveUserCompanyIdAsync:** New private helper in AuthService for the above.
- **CurrentUserService:** Already reads `companyId` / `company_id`; when missing, **TenantProvider** falls back to **Tenant:DefaultCompanyId**.

## 5. Tenant Isolation Enforcement

- **TenantScope.CurrentTenantId:** AsyncLocal set in middleware from **ITenantProvider.CurrentTenantId** (after authentication).
- **ITenantProvider:** Implemented by **TenantProvider** (Api). **Canonical request-time resolution:** (1) **X-Company-Id** (SuperAdmin only), (2) **JWT company_id** (from CurrentUserService; set at login from User.CompanyId or first department’s company via AuthService.ResolveUserCompanyIdAsync), (3) **Department→Company fallback** (when JWT company is null/empty; single company only), (4) **Unresolved** (null). TenantOptions.DefaultCompanyId is not used in the current TenantProvider. Request-time department fallback is via IUserCompanyFromDepartmentResolver.

- **ApplicationDbContext:** Global query filters on all **CompanyScopedEntity**, **User**, and **BackgroundJob**: data is filtered by **TenantScope.CurrentTenantId** when non-null.
- **Middleware:** After `UseAuthentication()`, **TenantGuardMiddleware** enforces valid tenant (uses ITenantProvider); then **SubscriptionEnforcementMiddleware** and **TenantContextService** use **ITenantProvider.CurrentTenantId** so guard, subscription, and tenant context are consistent (including SuperAdmin X-Company-Id). A middleware sets **TenantScope.CurrentTenantId** from ITenantProvider and clears it in `finally`.

## 6. Subscription Model Introduced

- **Existing (Phase 12):** **BillingPlan**, **TenantSubscription**, **TenantUsageRecord**, **TenantInvoice**.
- **Company:** **SubscriptionId** (optional) added for future link to subscription.
- **No payment gateway** integration in this phase.

## 7. Background Job Updates

- **BackgroundJob:** **CompanyId** added; index on `(CompanyId, State, CreatedAt)`.
- **Enqueuing:** Callers must set **BackgroundJob.CompanyId** from current tenant (e.g. **ITenantProvider.CurrentTenantId** or **TenantScope.CurrentTenantId**) when creating jobs.
- **Execution:** Job processors should set **TenantScope.CurrentTenantId** from **job.CompanyId** for the duration of the run so DbContext and services see the correct tenant. (Concrete job executor changes left as follow-up.)

## 8. Documentation Updates

- **docs/architecture/SAAS_MULTI_TENANT_AUDIT.md** — Full audit (entities, DbContext, auth, jobs, table classification).
- **docs/architecture/SAAS_MULTI_TENANT_IMPLEMENTATION_SUMMARY.md** — This file.
- **Product/architecture/data model/RBAC/background jobs:** To be updated in a follow-up (Step 14) to reflect tenant architecture, isolation rules, and platform vs tenant admin.

## 9. Configuration

- **Tenant:DefaultCompanyId** (optional): Guid of the default company (e.g. Cephas). Used when JWT has no `company_id` and no `X-Company-Id` header. After running the migration, set this in `appsettings.Development.json` or environment to the Cephas company Id for legacy single-tenant behaviour.

Example:

```json
{
  "Tenant": {
    "DefaultCompanyId": "<guid-of-cephas-company>"
  }
}
```

## 10. Tenant-Aware Logging (Patched)

- **RequestLogContextMiddleware:** Pushes **CompanyId** into Serilog LogContext (when authenticated and non-empty) so all request logs include tenant context. CorrelationId is set by CorrelationIdMiddleware earlier.
- **Background jobs:** BackgroundJobProcessorService and JobExecutionWorkerHostedService log **CompanyId** in completion/failure messages so job logs are tenant-identifiable.

## 11. Raw SQL Tenant-Safety (Patched)

- All tenant-owned raw SQL (ExecuteSqlRawAsync) has been audited. DELETE/UPDATE on VipGroups, VipEmails, ParserRules, ParserTemplates, InvoiceSubmissionHistory now include explicit **CompanyId** (or CompanyId IS NULL) in WHERE. TaskService UpdateTaskAsync uses parameterized WHERE with Id and CompanyId. See **docs/architecture/SAAS_TOP_5_RISKS_PATCH_SUMMARY.md** for the full audit table.

## 12. Usage Metering (Patched)

- **ITenantUsageService** / **TenantUsageService** record usage to **TenantUsageRecord** (TenantId, MetricKey, Quantity, Period). Metric keys: OrdersCreated, InvoicesGenerated, BackgroundJobsExecuted, ReportExports, TotalUsers, ActiveUsers.
- Wired into: OrderService (order create x2), BillingService (invoice create), BackgroundJobProcessorService and JobExecutionWorkerHostedService (job completed), InventoryReportExportJobExecutor (report export). Resolves TenantId from CompanyId via Companies table; no-op when TenantId is null (legacy).

## 13. Reporting and Observability Hardening

- **ReportsController** and **PnlController** use **current user CompanyId** only; no query-string override. Department scope enforced via **IDepartmentAccessService**. Event store / Observability list endpoints reject **companyId** query when it differs from scope (non–SuperAdmin). Export endpoints documented to use current user company only.

## 14. Remaining Risks and Follow-ups (pre-patch list; some addressed)

- **DepartmentAccessService:** Department list is not explicitly filtered by tenant; it relies on **UserId** and **DepartmentMembership**. With global query filter on **Department** (CompanyScopedEntity), only departments in the current tenant are visible. So tenant isolation holds as long as **TenantScope** is set before any query. **Risk:** Low if middleware order is correct.
- **Reporting:** Not audited in this phase. All reporting queries must run with tenant context set; any raw SQL or bypass of DbContext must include tenant filter (Step 13).
- **Background job executors:** Must set **TenantScope.CurrentTenantId** (or equivalent) from **job.CompanyId** when starting execution (Step 12).
- **Platform vs Tenant Admin:** Documented conceptually; permission boundaries and any new roles/policies to be refined in Step 9.
- **Module entitlements (PlanFeature / feature flags):** Not implemented in this phase (Step 11).

## 11. Build and Validation

- **Build:** Solution builds successfully.
- **Regression:** Critical flows (parser → order, scheduler, billing, payroll, P&L) should be re-tested with the default tenant (Cephas) and **Tenant:DefaultCompanyId** set to confirm no operational regressions (Step 15).

---

**Strategic note:** This phase turns CephasOps from an internal ops system into a multi-tenant SaaS foundation. Existing GPON operations continue under the single default tenant (Cephas); adding new tenants and enforcing strict isolation is now supported by the tenant model, auth, and query filters.

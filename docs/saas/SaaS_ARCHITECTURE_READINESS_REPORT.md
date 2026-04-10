# CephasOps SaaS Architecture Readiness Report

**Classification:** Engineering Audit — No implementation changes  
**Scope:** Full repository (backend, frontend, infrastructure, database, security, tenant isolation)  
**Reference:** Multi-tenant SaaS comparable to Stripe, Shopify, Linear  
**Date:** March 2025  

---

## Executive Summary

CephasOps has a **solid multi-tenant foundation** (Company as tenant root, global query filters, JWT `company_id`, tenant-scoped background jobs, and automated tenant provisioning). Critical gaps remain: **tenant spoofing via `X-Company-Id` header**, **no restriction of header to platform admins**, **entities and DbSets without tenant filters**, **raw SQL bypass paths**, and **observability/logging missing tenant context**. The platform is **PARTIALLY** ready for production multi-tenant SaaS; with the critical fixes below it can move toward Stripe-level architecture.

**Verdict: PARTIALLY READY.** Top 10 improvements are listed in the Final Verdict section.

### Scores Summary

| Score | Value | Notes |
|-------|--------|--------|
| **Multi-Tenant Safety** | 6/10 | Strong filters and job isolation; X-Company-Id and unfiltered EventStore/raw SQL reduce score. |
| **Security** | 5/10 | JWT and RBAC in place; tenant spoofing via header is critical. |
| **Scalability** | 6/10 | Single DB and queue; good indexing and tenant-scoped design; scale-out path not yet proven. |
| **Billing Architecture** | 5/10 | Plans and TenantSubscription exist; no features, seats, trials, or provider integration. |
| **Observability** | 5/10 | Serilog and some metrics; tenant id not in log context; no per-tenant dashboards. |
| **DevOps Readiness** | 6/10 | K8s and Terraform present; no Dockerfile/docker-compose in repo. |

---

## 1. System Architecture Map

### 1.1 Backend

| Layer | Location | Description |
|-------|----------|-------------|
| **API** | `backend/src/CephasOps.Api/` | ASP.NET Core 10, Controllers, Health, Middleware, TenantProvider, CurrentUserService |
| **Application** | `backend/src/CephasOps.Application/` | Use cases, services, DTOs, events, workflow, integration, provisioning, billing subscription |
| **Domain** | `backend/src/CephasOps.Domain/` | Entities (CompanyScopedEntity / BaseEntity), interfaces, domain events |
| **Infrastructure** | `backend/src/CephasOps.Infrastructure/` | Persistence (ApplicationDbContext, migrations, configurations), external services (SMS, WhatsApp, e-Invoice) |

**Key backend components:**

- **Persistence:** `ApplicationDbContext` — 100+ DbSets; global query filters for `CompanyScopedEntity`, `User`, `BackgroundJob`; `TenantScope.CurrentTenantId` (AsyncLocal) set per request from `ITenantProvider`.
- **Auth:** JWT with `company_id` / `companyId` claims; `AuthService` resolves user company from `User.CompanyId` or first department’s company.
- **Tenant resolution:** `TenantProvider`: 1) JWT `company_id` → 2) **X-Company-Id header** → 3) `TenantOptions.DefaultCompanyId`. **Risk:** Header is not restricted to SuperAdmin; any client can override tenant.
- **Background workers:** EventStore dispatcher, event bus metrics, retention, outbound integration retry, worker heartbeat, job execution worker, job polling coordinator, **BackgroundJobProcessorService**, EmailIngestionScheduler, LedgerReconciliation, StockSnapshot, PnlRebuild, SlaEvaluation, MissingPayoutSnapshot, PayoutAnomalyAlert, Notification retention/dispatch, Email cleanup. Jobs store `CompanyId` and restore `TenantScope.CurrentTenantId` before execution.

### 1.2 Frontend

- **Stack:** React 18, TypeScript, Vite, Syncfusion, TanStack Query, Tailwind CSS v4.
- **Entry:** `frontend/src/main.tsx` → `App.tsx` with routes, `ProtectedRoute`, `AuthProvider`, `DepartmentProvider`, `CompanySettingsProvider`.
- **Structure:** `pages/` (160+), `api/` (100+ API modules), `components/`, `contexts/`, `hooks/`, `lib/`, `types/`. State via React Query + contexts; no global Redux.

### 1.3 Infrastructure

- **CI/CD:** `.github/workflows/` — e2e, architecture-guardrails, migration-hygiene, parser-governance, diagnostics-manual, versioning.
- **Deployment:** `infra/k8s/` (deployment, service, ingress), `infra/terraform/` (main, variables, outputs), `infra/monitoring/` (Prometheus, Grafana dashboard). No root-level Dockerfile; no docker-compose in repo.
- **Database:** PostgreSQL; migrations in `backend/src/CephasOps.Infrastructure/Persistence/Migrations/`; seeds in `backend/scripts/postgresql-seeds/`.

### 1.4 Database

- **Schema:** EF Core migrations; snapshot in `ApplicationDbContextModelSnapshot.cs`. Many tables with `company_id` (nullable where “company feature removed”).
- **Connection:** `ConnectionStrings:DefaultConnection` (e.g. `appsettings.Development.json`); production user `cephasops_app` (non-superuser).

### 1.5 Background Workers (summary)

- Event: EventStoreDispatcherHostedService, EventBusMetricsCollectorHostedService, EventPlatformRetentionWorkerHostedService.
- Integration: OutboundIntegrationRetryWorkerHostedService.
- Jobs: WorkerHeartbeatHostedService, JobExecutionWorkerHostedService, JobPollingCoordinatorService, **BackgroundJobProcessorService**.
- Schedulers: EmailIngestionSchedulerService, LedgerReconciliationSchedulerService, StockSnapshotSchedulerService, PnlRebuildSchedulerService, SlaEvaluationSchedulerService, MissingPayoutSnapshotSchedulerService, PayoutAnomalyAlertSchedulerService.
- Notifications: NotificationRetentionHostedService, NotificationDispatchWorkerHostedService.
- Parser/Email: EmailCleanupService.

### 1.6 External Integrations

- SMS/WhatsApp (Twilio, WhatsApp Cloud API, Null providers), e-Invoice (MyInvois, Null), Carbone document renderer, SLA alerts, outbound integration HTTP client. Inbound/outbound webhooks with receipt storage.

---

## 2. Multi-Tenant Isolation Audit

### 2.1 Tenant mechanism

- **Company** = tenant root; operational entities use **CompanyId** (nullable on `CompanyScopedEntity`).
- **TenantScope.CurrentTenantId** (static AsyncLocal) set in API middleware from `ITenantProvider.CurrentTenantId`.
- **Global query filters:** Applied in `OnModelCreating` to:
  - All types assignable to **CompanyScopedEntity**: `(IsDeleted == false) AND (CurrentTenantId IS NULL OR CompanyId == CurrentTenantId)`.
  - **User:** `CurrentTenantId IS NULL OR CompanyId == CurrentTenantId`.
  - **BackgroundJob:** same as User.
- **DbContext:** Does **not** apply filters to: Tenant, Company, Roles, Permissions, UserRoles, RolePermissions, BillingPlan, TenantSubscription, TenantUsageRecord, TenantInvoice, EventStoreEntry, EventStoreAttemptHistory, ReplayOperation, ReplayOperationEvent, LedgerEntry, EventProcessingLog, CommandProcessingLog, ReplayExecutionLock, RebuildOperation, RebuildExecutionLock, WorkerInstance, ConnectorDefinition, ConnectorEndpoint, OutboundIntegrationDelivery, OutboundIntegrationAttempt, InboundWebhookReceipt, ExternalIdempotencyRecord, RefreshTokens, PasswordResetTokens, and other non–company-scoped entities. Many of these are platform-level or explicitly scoped in application code (e.g. EventStore by `scopeCompanyId`).

### 2.2 Entity–tenant scoping (summary)

**CompanyScopedEntity (filter applied):** Building, BuildingType, Splitter, Order, OrderType, OrderCategory, OrderStatusLog, OrderReschedule, OrderBlocker, OrderDocket, OrderMaterialUsage, OrderMaterialReplacement, OrderNonSerialisedReplacement, OrderStatusChecklistItem/Answer, Department, MaterialAllocation, DepartmentMembership, Partner, PartnerGroup, Vertical, Material, MaterialCategory, StockLocation, StockBalance, StockMovement, StockLedgerEntry, StockAllocation, SerialisedItem, MovementType, LocationType, RmaRequest, RmaRequestItem, Invoice, InvoiceLineItem, InvoiceSubmissionHistory, BillingRatecard, SupplierInvoice, SupplierInvoiceLineItem, Payment, PayrollPeriod, PayrollRun, PayrollLine, JobEarningRecord, SiRatePlan, RateCard, RateCardLine, CustomRate, RateGroup, OrderTypeSubtypeRateGroup, BaseWorkRate, RateModifier, ServiceProfile, OrderCategoryServiceProfile, GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, OrderPayoutSnapshot, PayoutSnapshotRepairRun, PayoutAnomalyReview, PayoutAnomalyAlert, PayoutAnomalyAlertRun, PnlPeriod, PnlFact, PnlDetailPerOrder, OverheadEntry, PnlType, OrderFinancialAlert, AssetType, Asset, AssetMaintenance, AssetDepreciation, AssetDisposal, GlobalSetting, MaterialTemplate, DocumentTemplate, GeneratedDocument, KpiProfile, TimeSlot, SmsTemplate, WhatsAppTemplate, SmsGateway, CustomerPreference, TaxCode, PaymentTerm, Vendor, Bin, Brand, ServicePlan, ProductType, Team, CostCentre, SlaProfile, AutomationRule, ApprovalWorkflow, ApprovalStep, BusinessHours, PublicHoliday, EscalationRule, GuardConditionDefinition, SideEffectDefinition, WorkflowDefinition, WorkflowTransition, WorkflowJob, WorkflowInstance, WorkflowStepRecord, WorkflowTransitionHistoryEntry, JobDefinition, JobRun, SystemLog, LedgerEntry, ReplayOperation, ReplayOperationEvent, AuditOverride, AuditLog, Supplier, PurchaseOrder, PurchaseOrderItem, Quotation, QuotationItem, Project, BoqItem, DeliveryOrder, DeliveryOrderItem, File, ServiceInstaller, ServiceInstallerContact, Skill, ServiceInstallerSkill, TaskItem, ScheduledSlot, SiAvailability, SiLeaveRequest, ParseSession, ParsedOrderDraft, ParsedMaterialAlias, EmailMessage, EmailAttachment, ParserRule, EmailAccount, VipEmail, VipGroup, ParserTemplate, EmailTemplate, ParserReplayRun, Notification, NotificationDispatch, NotificationSetting, MaterialVertical, MaterialTag, MaterialAttribute, LedgerBalanceCache, StockByLocationSnapshot, InstallationMethod, BuildingContact, BuildingRules, BuildingBlock, BuildingSplitter, Street, HubBox, Pole, SplitterPort, BuildingDefaultMaterial, CompanyDocument, CostCentre. (Some entities may be BaseEntity in code but have company_id in DB; see below.)

**BaseEntity only (no global filter; may have company_id in DB):** ProductType, Warehouse, Team, Vendor, TaxCode, PaymentTerm, Bin, Brand, ServicePlan, NotificationTemplate, ReportDefinition, GuardConditionDefinition, SideEffectDefinition, MaterialPartner. **Risk:** If these hold tenant-specific operational data and are queried without explicit CompanyId filter, they can leak across tenants. **Note:** GuardConditionDefinition and SideEffectDefinition have `company_id` in migrations; confirm whether they are CompanyScopedEntity or BaseEntity and whether all queries filter by company.

**Explicitly filtered in code (no CompanyScopedEntity):** User (filter in DbContext), BackgroundJob (filter in DbContext). EventStoreEntry has **no** global filter; EventStoreQueryService and ObservabilityController pass `scopeCompanyId` and filter by `CompanyId` in queries. Any new code that queries EventStore without `scopeCompanyId` could leak events.

**Platform / reference (no tenant filter by design):** Tenant, Company, BillingPlan, TenantSubscription, TenantUsageRecord, TenantInvoice, Roles, Permissions, UserRoles, RolePermissions, RefreshTokens, PasswordResetTokens. Access to these must be controlled by authorization (e.g. platform admin only for Tenants/Companies/BillingPlans).

### 2.3 Risk table (high level)

| Entity / Area | Tenant-scoped | Risk |
|---------------|--------------|------|
| Most operational entities | Yes (CompanyScopedEntity) | Low |
| User | Yes (explicit filter) | Low |
| BackgroundJob | Yes (explicit filter) | Low |
| EventStoreEntry | Manual filter only | Medium – must always pass scopeCompanyId |
| BaseEntity-only (e.g. MaterialPartner, Warehouse, Bin, etc.) | No filter | Medium – ensure all queries filter by company where data is tenant-specific |
| X-Company-Id header | N/A | **Critical** – any user can override tenant |

---

## 3. Global Query Filter Verification

### 3.1 Filters present

- **CompanyScopedEntity:** All entity types assignable to `CompanyScopedEntity` get: `IsDeleted == false AND (CurrentTenantId == null OR CompanyId == CurrentTenantId)`.
- **User:** `CurrentTenantId == null OR User.CompanyId == CurrentTenantId`.
- **BackgroundJob:** `CurrentTenantId == null OR BackgroundJob.CompanyId == CurrentTenantId`.

### 3.2 Bypass paths

1. **Raw SQL:** Multiple `ExecuteSqlRawAsync` / raw SQL usages (TaskService, ParserTemplateService, SchedulerService, InvoiceSubmissionService, EmailRuleService, EmailTemplateService, VipGroupService, VipEmailService, AdminService, WorkerCoordinatorService). These **do not** apply EF filters. Where they write `CompanyId` explicitly (e.g. TaskService INSERT/DELETE with CompanyId), tenant is enforced by the value passed from the caller; any raw SQL that does **not** restrict by CompanyId can bypass tenant isolation.
2. **IgnoreQueryFilters:** Used in `OrderService.DeleteOrderAsync` with an explicit `Where(o => o.CompanyId == companyId.Value)` when companyId is provided – safe. Used in `DatabaseSeeder` and seeders – acceptable for setup only.
3. **EventStore / EventStoreAttemptHistory:** No global filter. Relies on application code (EventStoreQueryService, ObservabilityController) to pass `scopeCompanyId`. Any new direct query on `EventStore` without company filter is a leak risk.
4. **Projection / AsNoTracking:** Filters still apply to the underlying DbSet; AsNoTracking does not remove the filter. No additional bypass from AsNoTracking alone.
5. **Joins:** When joining filtered entities with unfiltered entities (e.g. Tenant, Company), the filtered side remains tenant-scoped; ensure no join exposes rows from another tenant (e.g. AdminUserService uses User which is filtered).

### 3.3 Recommendations

- Restrict **X-Company-Id** to SuperAdmin (or platform admin) only; otherwise remove header-based override for tenant.
- Audit all **ExecuteSqlRaw** / raw SQL: ensure every statement that touches tenant-specific data includes CompanyId (or equivalent) in WHERE/INSERT.
- Ensure **EventStore** and **EventStoreAttemptHistory** are only ever queried with an explicit company/tenant scope in application code; consider a code rule or wrapper that requires scope.
- Document and review any **IgnoreQueryFilters** usage outside of seeding/migrations.

---

## 4. Authentication Architecture

- **JWT:** Issued by `AuthService`; includes `company_id` and `companyId` from `ResolveUserCompanyIdAsync` (User.CompanyId or first department’s company). Roles in claims.
- **CurrentUserService:** Reads `companyId` / `company_id` from claims; returns `Guid.Empty` when missing (single-company mode).
- **TenantProvider:** 1) JWT company_id → 2) **X-Company-Id** header → 3) DefaultCompanyId. **Critical:** There is no check that the user belongs to the company in the header; any authenticated user can send a different X-Company-Id and operate in another tenant.
- **Tenant scope:** Set in middleware after authentication; used by ApplicationDbContext filters and services that use `ICurrentUserService.CompanyId`.

**Security rating: 6/10.** JWT and tenant resolution are in place, but **X-Company-Id must be restricted or removed** for production multi-tenant SaaS.

---

## 5. Authorization Model

- **Roles:** SuperAdmin, Admin, Director, HeadOfDepartment, Supervisor, Member (and others in PermissionCatalog). Permission-based (RequirePermission) and role-based (Authorize(Roles = "…")).
- **Admin permissions:** AdminView, AdminUsersView/Edit, AdminTenantsView/Edit, AdminBillingPlansView/Edit, AdminRolesView/Edit, AdminSecurityView, etc. Admin and SuperAdmin get broad admin permissions.
- **Platform vs tenant admin:** Tenant provisioning and platform-level resources (e.g. TenantProvisioningController) use `Authorize(Roles = "SuperAdmin,Admin")` and `RequirePermission(AdminTenantsEdit)`. There is no strict “Platform Admin” vs “Tenant Admin” role split; “Admin” can be tenant-scoped via User filter, but AdminUserService and similar list users within the current tenant due to User filter. Cross-tenant admin risk is mainly via **X-Company-Id** allowing a tenant user to impersonate another tenant.
- **Recommendation:** Introduce explicit Platform Admin vs Tenant Admin (e.g. SuperAdmin only for platform, Admin for tenant), and restrict X-Company-Id to Platform Admin only.

---

## 6. Background Job Isolation

- **BackgroundJob** has **CompanyId** and a global query filter; job processor sets `TenantScope.CurrentTenantId = job.CompanyId ?? TryGetCompanyIdFromPayload(payload)` before execution and restores it in `finally`. JobExecution worker does the same. So jobs run in the correct tenant context and cannot intentionally operate across tenants without a bug (e.g. wrong CompanyId in payload).
- **Schedulers** (e.g. EmailIngestionSchedulerService) query jobs or data; when they enqueue jobs they should pass CompanyId. Email ingestion uses `AsNoTracking()` and typically works per company via the query context when TenantScope is set.
- **Risks:** Legacy job types that do not set or validate CompanyId in payload; any job that runs without setting TenantScope (none found in main processor). Recommendation: Ensure every enqueue path sets CompanyId and that all job executors assume TenantScope is set.

---

## 7. Reporting Safety Audit

- **ReportsController:** Uses `_currentUserService.CompanyId`; requires company context (401 if null and not SuperAdmin). Department scope resolved via `IDepartmentAccessService.ResolveDepartmentScopeAsync`; 403 if user has no access. Report run is tenant- and department-scoped.
- **PnlController:** Uses `_currentUserService.CompanyId ?? Guid.Empty` and passes companyId to `IPnlService`; department access validated. **Tenant safe.**
- **InventoryController, ParserController, OrdersController, AdminController, ReportDefinitionsController:** Use `_currentUserService.CompanyId` or equivalent. No evidence of cross-tenant report queries.
- **EventStore / ObservabilityController:** Event list filtered by `scopeCompanyId`; if user provides a different CompanyId in filter, controller rejects when non-SuperAdmin (`scopeCompanyId != companyId`). **Tenant safe** when used as intended.
- **Dashboard / exports:** Should be audited per endpoint; pattern of passing companyId from current user is consistent. Recommendation: Add a checklist for every new report/export to ensure companyId comes from current user and never from unchecked input.

| Report / area | Tenant safe | Risk |
|---------------|------------|------|
| Reports hub (run) | Yes | Low |
| P&L summary/details | Yes | Low |
| Event store list | Yes (scope enforced) | Low |
| Inventory/parser/orders (from controllers audited) | Yes | Low |
| Exports (e.g. inventory report export job) | Yes (job CompanyId) | Low |

---

## 8. Tenant Provisioning Capability

- **TenantProvisioningController:** `POST /api/platform/tenants/provision`; `Authorize(Roles = "SuperAdmin,Admin")`, `RequirePermission(AdminTenantsEdit)`. Calls `ICompanyProvisioningService.ProvisionAsync`.
- **CompanyProvisioningService:** Creates **Tenant**, **Company** (linked to Tenant), **default departments** (Operations GPON, Finance, Inventory, Scheduler, Admin), **tenant admin user** (Admin or SuperAdmin role), and optionally links **TenantSubscription** (if subscription id provided). Transactional. Does not create BillingPlan (platform-level); links tenant to subscription if requested.
- **Answers:**  
  - Can a new tenant be created automatically? **Yes.**  
  - Default departments seeded? **Yes (5).**  
  - Tenant admin created automatically? **Yes.**  
  - Subscription linked? **Only if requested in payload** (Company.SubscriptionId / TenantSubscription).  
- **Readiness:** **8/10.** Provisioning is automated and consistent; subscription linkage and post-provision hooks (welcome email, default settings) could be expanded.

---

## 9. Subscription Architecture

- **Entities:** BillingPlan (plan metadata), TenantSubscription (TenantId, BillingPlanId, Status, ExternalSubscriptionId, period), Company.SubscriptionId, TenantUsageRecord (TenantId, MetricKey, Quantity, PeriodStart/End), TenantInvoice.
- **BillingPlan:** No CompanyId; platform-level. No PlanFeature or seat limits in domain model yet.
- **TenantSubscription:** Links tenant to plan and external provider; status and period. No explicit seat limits or feature flags in entities.
- **TenantUsageRecord:** Supports metered usage by MetricKey and period. Good base for usage-based billing.
- **Gap vs Stripe-level:** No plan features/entitlements (e.g. module flags, seat caps), no trial/phase logic in entity model, no webhook or sync with Stripe/billing provider. **Score: 5/10** – foundation present; productized billing (plans, features, trials, provider integration) still to be built.

---

## 10. Usage Metering

- **TenantUsageRecord** exists (TenantId, MetricKey, Quantity, PeriodStartUtc, PeriodEndUtc). No application code found that **writes** TenantUsageRecord for active users, orders per tenant, invoice counts, parser usage, storage, or report exports.
- **Recommendation:** Implement a usage metering service that records metrics (e.g. active_users, orders_count, invoices_count, parser_sessions, storage_mb, report_exports) per tenant per period, and optionally feed TenantInvoice or external billing.

---

## 11. Observability

- **Serilog:** File and console; Enrich.FromLogContext; RequestLogContextMiddleware pushes **UserId, DepartmentId, Roles, OrderId, ParseSessionId**. **CompanyId / TenantId are not pushed** to LogContext – tenant cannot be inferred from logs without custom code.
- **Metrics:** EventBus metrics, job run recorder; some event metrics include company_id. Health checks (e.g. `/health`) present; no tenant-specific health noted.
- **Recommendation:** Add CompanyId/TenantId to RequestLogContextMiddleware (and optionally to all log calls in sensitive paths). Ensure all background job and event logs include tenant/company where relevant.

**Observability score: 5/10** – structured logging and some metrics exist; tenant context in logs and consistent metrics per tenant are missing.

---

## 12. DevOps & Infrastructure

- **Kubernetes:** deployment, service, ingress in `infra/k8s/`. Probes and scaling can be added (HPA, CronJobs mentioned in README).
- **Terraform:** main, variables, outputs present; no full stack reviewed.
- **Monitoring:** Prometheus, Grafana dashboard in `infra/monitoring/`.
- **CI/CD:** GitHub Actions for e2e, migrations, guardrails, versioning. No Dockerfile in repo; no docker-compose. **Score: 6/10** – direction is correct; container and local orchestration are incomplete.

---

## 13. Security Audit

- **Tenant spoofing:** **Critical.** X-Company-Id allows any authenticated user to switch tenant. Fix: allow only for SuperAdmin (or platform role) or remove.
- **SQL injection:** Parameterized queries and EF used in most places; raw SQL uses parameters in places (e.g. TaskService). Audit all raw SQL for concatenation.
- **Secrets:** Connection strings and JWT in config/env; production uses cephasops_app. Ensure no secrets in repo.
- **File storage:** Not fully audited; ensure paths or buckets are tenant-isolated (e.g. by CompanyId).
- **Token misuse:** JWT expiry and refresh flow in AuthService; session revocation via AdminSecuritySessionsRevoke. Adequate for baseline.
- **API exposure:** Controllers use Authorize and RequirePermission; no open sensitive endpoints found. **Security score: 5/10** – critical issue is X-Company-Id; rest is reasonable baseline.

---

## 14. Scalability Assessment

- **Database:** Single PostgreSQL; global filters and indexes on CompanyId (e.g. StockLedgerEntries, StockAllocations). At 100–1,000 tenants, single DB is typically fine with good indexing and connection pooling; at 10,000 tenants consider read replicas, partitioning by CompanyId, or tenant-aware routing.
- **Background jobs:** Single queue (BackgroundJobs table); processor and job execution worker restore tenant per job. Scale by adding worker instances; ensure job claim lease and TenantScope are correct under concurrency.
- **Reporting:** Heavy reports (P&L, inventory, exports) are company-scoped; watch for “noisy tenant” and consider per-tenant timeouts or rate limits.
- **Recommendation:** Define target scale (e.g. 1,000 tenants, 10,000 orders/day) and load-test; add connection pooling, caching (e.g. Redis) for reference data, and consider read replicas for reporting.

---

## 15. Comparison with Top SaaS (Stripe, Shopify, Linear, GitHub, Notion)

| Dimension | CephasOps | Typical top SaaS |
|-----------|-----------|-------------------|
| Tenant model | Company-scoped + global filters | Similar (org/workspace) + strict isolation |
| Security | JWT + filters; **X-Company-Id risk** | No client-overridable tenant; strict RBAC |
| Billing | Plans + TenantSubscription + usage table | Plans, features, trials, metering, provider integration |
| Provisioning | Automated (tenant, company, depts, admin) | Automated + onboarding flows |
| Observability | Logs/metrics; **no tenant in log context** | Tenant/user in every log; per-tenant metrics |
| Usage metrics | Entity present; **no recording** | Full metering and billing integration |
| Modular architecture | Clean layers; some god services | Bounded contexts, feature flags |

**Overall maturity (0–10): 5.5** – strong tenant and provisioning base; security fix, billing productization, metering, and observability needed to reach “world-class” multi-tenant SaaS.

---

## Gap Analysis

### Critical

1. **X-Company-Id header** allows any user to switch tenant; restrict to SuperAdmin (or remove).
2. **Tenant in logs:** Add CompanyId/TenantId to Serilog LogContext (e.g. in RequestLogContextMiddleware).
3. **Raw SQL audit:** Ensure every raw SQL that touches tenant data includes CompanyId (or equivalent) and does not allow cross-tenant access.
4. **EventStore** has no global filter; enforce and document “always pass scopeCompanyId” for all EventStore queries.

### Important

5. **Usage metering:** Implement writing to TenantUsageRecord (active users, orders, invoices, parser usage, etc.) and optionally feed billing.
6. **Billing productization:** Plan features, seat limits, trials, and optional Stripe (or other) integration.
7. **BaseEntity entities** (MaterialPartner, Warehouse, Bin, etc.): Confirm which are tenant-specific and add explicit CompanyId filter to all their queries (or migrate to CompanyScopedEntity where appropriate).
8. **Platform vs Tenant Admin:** Clarify role model (e.g. SuperAdmin = platform only) and restrict cross-tenant actions to platform admin only.

### Future Enhancements

9. **Database scalability:** Partitioning or read replicas by tenant/company when scaling beyond ~1,000 tenants.
10. **Docker / docker-compose** for local and CI; Dockerfile for API and workers.
11. **Feature flags / entitlements** driven by TenantSubscription and BillingPlan.
12. **Stripe (or similar) integration** for subscriptions and usage-based billing.

---

## Recommended Engineering Roadmap

### Phase A — Critical SaaS safety (4–6 weeks)

- Restrict **X-Company-Id** to SuperAdmin only (or remove); validate that user belongs to the requested company when header is allowed.
- Add **CompanyId/TenantId** to Serilog LogContext in RequestLogContextMiddleware (and ensure no PII in tenant id).
- **Audit all ExecuteSqlRaw / raw SQL** for tenant-specific tables; add CompanyId to WHERE/INSERT and document.
- **Document and enforce** EventStore/EventStoreAttemptHistory: all reads must use an explicit tenant/company scope; add code review rule or shared wrapper.

### Phase B — SaaS maturity (8–12 weeks)

- **Usage metering service:** Record TenantUsageRecord for key metrics (active users, orders, invoices, parser sessions, etc.) per period.
- **Billing:** Extend BillingPlan/TenantSubscription with plan features, seat limits, trial fields; optional Stripe (or other) webhook integration.
- **BaseEntity tenant review:** For each BaseEntity that is tenant-specific, add CompanyId to queries or migrate to CompanyScopedEntity and add to global filter.
- **Observability:** Dashboards and alerts per tenant (or per tenant segment); ensure all background job and event logs include tenant id where applicable.

### Phase C — Enterprise SaaS (ongoing)

- **Scale:** Read replicas, partitioning, or tenant-aware routing when approaching 1,000+ tenants.
- **Platform vs Tenant Admin:** Formalize roles and permissions; restrict platform-only APIs to SuperAdmin (or dedicated platform role).
- **Docker/CI:** Dockerfile(s) and docker-compose for API and workers; CI to build and push images; optional K8s deployment from pipeline.
- **Feature flags / entitlements:** Driven by subscription and plan features; enforce in API and frontend.

---

## Final Verdict

**Is CephasOps ready for production multi-tenant SaaS?**

### **PARTIALLY**

The platform has a solid multi-tenant foundation (tenant model, global filters, JWT company_id, job isolation, provisioning). One **critical** issue (X-Company-Id tenant override) and several important gaps (tenant in logs, raw SQL, usage metering, billing productization) must be addressed before claiming production-ready, “Stripe-level” multi-tenant SaaS.

---

## Top 10 Architectural Improvements to Reach Stripe-Level SaaS

1. **Remove or strictly restrict X-Company-Id** so that only platform admins can set tenant context via header; otherwise derive tenant only from JWT.
2. **Add CompanyId/TenantId to all request and job logs** (Serilog LogContext and background job logging).
3. **Audit and fix all raw SQL** so tenant-scoped data is always filtered or keyed by CompanyId.
4. **Enforce tenant scope on EventStore** (and related stores): require scope in all call paths; no unfiltered cross-tenant reads.
5. **Implement usage metering** (TenantUsageRecord) for active users, orders, invoices, parser usage, and optionally report exports.
6. **Productize billing:** plan features, seat limits, trial logic, and optional Stripe (or other) integration.
7. **Clarify Platform vs Tenant Admin** and restrict cross-tenant and platform-level APIs to platform admin only.
8. **Ensure every BaseEntity that is tenant-specific** is either migrated to CompanyScopedEntity or has explicit CompanyId filtering in every query.
9. **Add tenant-aware observability:** dashboards and alerts per tenant (or segment); health and metrics tagged by tenant where appropriate.
10. **Document and automate** tenant provisioning post-steps (e.g. default settings, welcome email, subscription assignment) and add Docker/CI for consistent builds and deployments.

---

---

## Post-Audit Patches (March 2025)

The following patches were applied to address the top 5 SaaS risks identified in this audit:

1. **X-Company-Id spoofing:** TenantProvider now honours the header only for SuperAdmin; normal users use JWT `company_id` only.
2. **Tenant context in logs:** RequestLogContextMiddleware pushes CompanyId; background job logs include CompanyId.
3. **Raw SQL tenant-safety:** All tenant-owned DELETE/UPDATE raw SQL now include explicit CompanyId in WHERE; TaskService UPDATE parameterized.
4. **Usage metering:** ITenantUsageService/TenantUsageService record OrdersCreated, InvoicesGenerated, BackgroundJobsExecuted, ReportExports to TenantUsageRecord; wired into order create, invoice create, job completion, report export.
5. **Reporting/observability hardening:** Verified and documented; ReportsController export comment added.

See **docs/architecture/SAAS_TOP_5_RISKS_PATCH_SUMMARY.md** for the full patch report and raw SQL audit table.

---

*End of report. Patches applied as per SAAS_TOP_5_RISKS_PATCH_SUMMARY.md.*

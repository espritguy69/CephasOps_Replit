# Post–Company Migration Regression Audit

**Date:** 2025-03-12  
**Scope:** Whole-system regression audit after company migration / tenant-company provisioning changes.  
**Objective:** Identify what broke, why, where, how to fix, and in what order.

---

## 1. Executive Summary

After the company migration (Tenant → Company → Departments → Memberships, with global query filters and `TenantScope.CurrentTenantId`), several failure modes are **confirmed or highly likely**:

- **Schema / migration drift:** If the database was restored from an older backup or migrations were applied out of order, the **EventStore** table may lack the **RootEventId** (and other Phase 8) columns. The EventStore dispatcher uses raw SQL that **RETURNING**s these columns; missing columns cause runtime failure. Similarly, **OrderPayoutSnapshots** and **InboundWebhookReceipts** tables are required by services and retention jobs; if their migrations were not applied, those operations fail.
- **Query / EF translation:** **JobExecutions.PayloadJson** is mapped as **jsonb**. Using `j.PayloadJson.Contains(accountIdString)` in EF (e.g. in `EmailIngestionSchedulerService`) can translate to SQL that is invalid or inefficient on jsonb (e.g. LIKE on jsonb without cast), causing the email ingestion scheduler to fail or misbehave.
- **Company/tenant context:** API requests rely on **JWT `company_id`**, which is resolved from **User.CompanyId** or the first department’s company. Legacy users with **null User.CompanyId** and no **TenantOptions.DefaultCompanyId** get **null** tenant → global filter allows all rows; if **DefaultCompanyId** is set, they are scoped to that company. Controllers that use `_currentUserService.CompanyId ?? Guid.Empty` can over-filter or behave incorrectly when `CompanyId` is null.
- **Single-company legacy:** **CompanyService.CreateCompanyAsync** still enforces a single company (“Only a single company is allowed”). New tenant provisioning uses **ICompanyProvisioningService**; the old Companies API create path is not SaaS-safe.
- **Hosted services:** Background workers (EventStore dispatcher, retention, missing payout snapshot repair, email ingestion scheduler, etc.) run **without HTTP context**, so **TenantScope.CurrentTenantId** is never set. For **CompanyScopedEntity** this means “tenantIdIsNull” → all rows visible, which is intended for system-wide jobs. **EventStore** and **JobExecution** are not CompanyScopedEntity; they are not filtered by tenant. Failure modes are therefore mainly **schema** (missing tables/columns) and **query translation** (jsonb Contains).

**Summary of breakage:**  
Core regressions are (1) **missing DB migrations** (EventStore Phase 8, OrderPayoutSnapshots, InboundWebhookReceipts, JobExecutions), (2) **jsonb Contains** in JobExecutions queries, (3) **unset DefaultCompanyId** and **null User.CompanyId** for legacy users, and (4) **over-reliance on CompanyId ?? Guid.Empty** in some controllers leading to wrong or empty results when company is null.

---

## 2. Confirmed Failures

### 2.1 EventStore.RootEventId missing (schema)

| Item | Detail |
|------|--------|
| **Title** | EventStore table missing RootEventId (and Phase 8 columns) |
| **Layer** | DB / Infrastructure |
| **Exact files** | `backend/src/CephasOps.Infrastructure/Persistence/EventStoreRepository.cs` (raw SQL RETURNING clause lines 179–185); migrations `20260309210000_AddEventStorePhase8PlatformEnvelope.cs`, `20260310031127_AddExternalIntegrationBus.cs` |
| **Root cause** | Raw SQL in `ClaimNextPendingBatchAsync` includes `e."RootEventId"` and other Phase 8 columns in RETURNING. If the EventStore table was never updated (e.g. DB restored from pre–Phase 8 backup, or migrations not fully applied), the query fails at runtime. |
| **Impact** | EventStoreDispatcherHostedService fails when claiming events; event processing stops. |
| **Recommended fix** | Ensure all migrations are applied to the database in order, especially those that add EventStore columns: `AddEventStorePhase8PlatformEnvelope` and any later migration that touches EventStore. Run idempotent migration script or `dotnet ef database update` (or apply SQL manually) so that EventStore has RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority. |

### 2.2 OrderPayoutSnapshots / InboundWebhookReceipts missing (schema)

| Item | Detail |
|------|--------|
| **Title** | Tables OrderPayoutSnapshots or InboundWebhookReceipts missing |
| **Layer** | DB / Infrastructure |
| **Exact files** | `ApplicationDbContext.cs` (DbSet OrderPayoutSnapshots, InboundWebhookReceipts); `MissingPayoutSnapshotRepairService.cs` (queries OrderPayoutSnapshots); `EventPlatformRetentionService.cs` (queries InboundWebhookReceipts); migrations that create these tables (e.g. `AddOrderPayoutSnapshot`, `AddExternalIntegrationBus` for InboundWebhookReceipts). |
| **Root cause** | Migrations that create these tables were not applied. |
| **Impact** | MissingPayoutSnapshotSchedulerService and OrderPayoutSnapshotService fail; EventPlatformRetentionWorkerHostedService fails when cleaning InboundWebhookReceipts; PayoutHealthDashboardService and related reporting fail. |
| **Recommended fix** | Apply all pending migrations so that OrderPayoutSnapshots and InboundWebhookReceipts exist with the expected columns (including OrderPayoutSnapshots.Provenance if required by code). |

### 2.3 JobExecutions.PayloadJson jsonb Contains / LIKE (query translation)

| Item | Detail |
|------|--------|
| **Title** | EF query using PayloadJson.Contains() on jsonb column |
| **Layer** | Backend / Application + Persistence |
| **Exact files** | `backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs` (lines 127–131): `context.JobExecutions.AnyAsync(j => ... && j.PayloadJson.Contains(accountIdString))`; `JobExecutionConfiguration.cs` (PayloadJson as jsonb). |
| **Root cause** | PayloadJson is mapped as **jsonb**. In PostgreSQL, string.Contains often translates to SQL LIKE. jsonb does not support LIKE directly; Npgsql/EF may generate invalid or inefficient SQL (e.g. missing cast to text). |
| **Impact** | Email ingestion scheduler can throw at runtime or return incorrect results when checking for existing emailingest jobs. |
| **Recommended fix** | Avoid querying inside jsonb by substring in EF. Options: (1) Load pending/running emailingest jobs for the account in memory and filter by deserialized payload (small set); (2) Use raw SQL with `PayloadJson::text LIKE '%' || @accountId || '%'`; (3) Add a dedicated column or index (e.g. EmailAccountId) for this check. Prefer (1) or (2) for a minimal, safe fix. |

---

## 3. Likely Regressions

### 3.1 Null or unset DefaultCompanyId for legacy users

| Item | Detail |
|------|--------|
| **Title** | TenantScope null when User.CompanyId and DefaultCompanyId are both null |
| **Why likely** | AuthService.ResolveUserCompanyIdAsync returns User.CompanyId or first department’s company; if both are null/empty, JWT gets no company_id. TenantProvider then returns TenantOptions.DefaultCompanyId. If DefaultCompanyId is not configured (e.g. in appsettings), CurrentTenantId is null. |
| **Exact files** | `AuthService.cs` (ResolveUserCompanyIdAsync, JWT claim "company_id"); `TenantProvider.cs` (CurrentTenantId); `Program.cs` (TenantOptions section); `TenantScope` + ApplicationDbContext global query filter. |
| **How to verify** | Log in as a user with User.CompanyId = null and no department memberships (or first department’s company null). Check JWT for company_id. Check TenantScope.CurrentTenantId in a request. If null, global filter shows all companies’ data (tenantIdIsNull branch). |
| **Recommended fix** | For legacy single-tenant: set `Tenant:DefaultCompanyId` in configuration to the single company’s Id. For multi-tenant: ensure every user has User.CompanyId or at least one department membership with a valid company; provision tenant admin with CompanyId set in CompanyProvisioningService (already done). |

### 3.2 Controllers using CompanyId ?? Guid.Empty

| Item | Detail |
|------|--------|
| **Title** | Using Guid.Empty when CompanyId is null can over-filter or match nothing |
| **Why likely** | Many controllers do `var companyId = _currentUserService.CompanyId ?? Guid.Empty` and then filter with `c.CompanyId == companyId`. When CompanyId is null, Guid.Empty does not match any real CompanyId (GUIDs are not null), so queries return empty. |
| **Exact files** | e.g. `InventoryController.cs`, `PnlController.cs`, `ApprovalWorkflowsController.cs`, `AssetTypesController.cs`, `EmailAccountsController.cs`, `SupplierInvoicesController.cs`, and others (see grep for `CompanyId ?? Guid.Empty`). |
| **How to verify** | As a user with no company_id in JWT and no DefaultCompanyId, open inventory, P&L, or settings; lists may be empty or 403. |
| **Recommended fix** | When the operation is company-scoped, require CompanyId: if `!_currentUserService.CompanyId.HasValue || _currentUserService.CompanyId == Guid.Empty` return 403 or 400 with a clear message. Do not use Guid.Empty as “all companies” for normal API actions. For true “platform admin sees all” use SuperAdmin + X-Company-Id or a dedicated admin API path. |

### 3.3 Frontend not sending company context

| Item | Detail |
|------|--------|
| **Title** | Frontend sends X-Department-Id but not X-Company-Id |
| **Why likely** | client.ts builds headers with Authorization and X-Department-Id only. TenantProvider uses JWT company_id (and X-Company-Id only for SuperAdmin). So company context is server-side only from JWT. |
| **Exact files** | `frontend/src/api/client.ts` (buildHeaders); `TenantProvider.cs`. |
| **How to verify** | Confirm JWT contains company_id after login. If frontend ever needs to switch company (e.g. SuperAdmin), it would need to send X-Company-Id; currently only backend SuperAdmin path uses it. |
| **Recommended fix** | No change if all users have a single company in JWT. If you add company switcher for SuperAdmin, send X-Company-Id from frontend for that flow. |

### 3.4 CompanyService.CreateCompanyAsync blocks second company

| Item | Detail |
|------|--------|
| **Title** | Legacy create company API blocks multi-tenant creation |
| **Why likely** | CompanyService.CreateCompanyAsync throws if existingCount > 0. New tenant provisioning is via TenantProvisioningController + ICompanyProvisioningService, not CompaniesController create. |
| **Exact files** | `CompanyService.cs` (CreateCompanyAsync); `CompaniesController.cs` (if it exposes create); `TenantProvisioningController.cs`; `CompanyProvisioningService.cs`. |
| **How to verify** | Call POST to create company when one company already exists; expect InvalidOperationException. |
| **Recommended fix** | Document that tenant creation is only via platform admin provisioning API. Optionally restrict or remove legacy company create endpoint, or make it explicitly “legacy single-tenant only”. |

---

## 4. Page/Action Audit Matrix

Abbreviations: **OK** = no identified risk; **Broken** = confirmed failure; **Likely** = likely broken by code inspection; **Verify** = needs runtime verification.

| Feature Area | Route/Page | User Action | API / Backend | Company-Scope Risk | Schema Risk | Status |
|--------------|------------|-------------|----------------|--------------------|-------------|--------|
| Login / startup | /login | Login | AuthService, JWT company_id | Null company_id if User.CompanyId null | — | Verify |
| App shell | /* | Load layout, sidebar | TenantScope from JWT | Null tenant → all data visible | — | Verify |
| Dashboard | /dashboard | Load dashboard | Various read APIs | Same as tenant | — | OK/Likely |
| Company/tenant | /settings/company | View/edit company | CompaniesController, CompanyService | Single-company create block | — | OK (provisioning separate) |
| Departments | /settings/company/departments | List/edit departments | DepartmentsController | Requires CompanyId | — | Likely if CompanyId null |
| Users/roles | /admin/users | List/edit users | AdminUserService, User list | User filter by tenant | — | OK |
| Orders | /orders, /orders/:id | List, detail, create | OrdersController, OrderService | Company-scoped | — | OK |
| Workflow | /workflow/definitions | List definitions | WorkflowDefinitionsController | CompanyScopedEntity | — | OK |
| Stock/inventory | /inventory/* | Ledger, receive, transfer, etc. | InventoryController, services | CompanyId ?? Guid.Empty | — | Likely |
| Rate/payout | /reports/payout-health | Load dashboard | PayoutHealthDashboardService | Uses OrderPayoutSnapshots | OrderPayoutSnapshots missing | Broken if schema missing |
| P&L | /pnl/* | Summary, drilldown | PnlController | CompanyId ?? Guid.Empty | — | Likely |
| Email ingestion | (background) | Scheduler runs | EmailIngestionSchedulerService | JobExecution by company | PayloadJson Contains/jsonb | Broken/Likely |
| Event store | (background) | Dispatcher claims events | EventStoreRepository | — | RootEventId missing | Broken if migration missing |
| Event retention | (background) | Retention job | EventPlatformRetentionService | — | InboundWebhookReceipts missing | Broken if migration missing |
| Missing payout snapshot | (background) | Repair job | MissingPayoutSnapshotRepairService | TenantScope null → all orders | OrderPayoutSnapshots missing | Broken if schema missing |
| Background jobs | /admin/background-jobs | List/filter jobs | BackgroundJobsController | Filter by CompanyId | — | OK |
| Job executions | (worker) | Claim and run | JobExecutionWorkerHostedService | Sets TenantScope from job.CompanyId | JobExecutions table | OK if migrations applied |

---

## 5. Schema Drift Audit

| Check | Finding |
|-------|--------|
| **Missing tables** | If migrations not applied: EventStore (Phase 8 columns), OrderPayoutSnapshots, InboundWebhookReceipts, JobExecutions, PayoutSnapshotRepairRuns, ExternalIdempotencyRecords, etc. |
| **Missing columns** | EventStore: RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority. OrderPayoutSnapshots: Provenance (if migration 20260310120000 applied). |
| **Mismatched names** | EventStoreEntry maps to table "EventStore". JobExecution to "JobExecutions". InboundWebhookReceipt to "InboundWebhookReceipts". OrderPayoutSnapshot to "OrderPayoutSnapshots". |
| **Pending migrations** | Run `dotnet ef migrations list` (or inspect __EFMigrationsHistory) and compare to migrations folder. Apply idempotent script or update database. |
| **Seed/provisioning gaps** | New companies need default departments and tenant admin with User.CompanyId set (CompanyProvisioningService does this). Legacy users need User.CompanyId or DefaultCompanyId so JWT has company_id. |

---

## 6. Scoping Audit

| Category | Location | Finding |
|----------|----------|--------|
| **Single-company assumption** | CompanyService.CreateCompanyAsync | Throws if any company exists; blocks second company. |
| **Missing company filter** | N/A (filter is global) | Global query filter applies to all CompanyScopedEntity when TenantScope.CurrentTenantId is set; when null, no company filter (all rows). |
| **Over-filtering** | Controllers using CompanyId ?? Guid.Empty | When CompanyId is null, filtering by Guid.Empty returns no rows. |
| **Wrong company source** | TenantProvider | Correct: JWT company_id for normal users; X-Company-Id for SuperAdmin only; DefaultCompanyId fallback. |

---

## 7. Startup / Hosted Service Audit

| Hosted Service | Depends On | Safe at Startup? | Failure Mode | Hardening |
|----------------|------------|------------------|--------------|-----------|
| EventStoreDispatcherHostedService | EventStore table with Phase 8 columns | No if RootEventId missing | SQL exception on ClaimNextPendingBatch | Apply migrations; optionally guard with schema check. |
| JobExecutionWorkerHostedService | JobExecutions table | No if table missing | DbUpdateException / missing table | Apply migrations. Sets TenantScope from job.CompanyId. |
| EventPlatformRetentionWorkerHostedService | EventStore, Outbound*, InboundWebhookReceipts, ExternalIdempotencyRecords | No if any table missing | Query/delete fails | Apply migrations; retention service already has try/catch per table. |
| MissingPayoutSnapshotSchedulerService | OrderPayoutSnapshots, Orders, PayoutSnapshotRepairRuns | No if tables missing | Repair or save fails | Apply migrations. |
| EmailIngestionSchedulerService | JobExecutions (PayloadJson Contains) | No if jsonb Contains broken | Exception or wrong duplicate check | Fix PayloadJson query (see 2.3). |
| BackgroundJobProcessorService | BackgroundJob (legacy), TenantScope set per job | Yes for legacy jobs | N/A | Already sets TenantScope from job.CompanyId. |
| SlaEvaluationSchedulerService | JobExecutionEnqueuer, SLA data | Yes | — | — |
| PnlRebuildSchedulerService | Job executions, P&L entities | Yes if schema OK | — | — |
| NotificationDispatchWorkerHostedService | Notification tables | Yes | — | — |
| WorkerHeartbeatHostedService | Worker instances / DB | Yes | — | — |

---

## 8. Prioritized Fix Plan

1. **Schema (blocking)**  
   - Apply all pending EF migrations so that EventStore (with Phase 8 columns), OrderPayoutSnapshots, InboundWebhookReceipts, JobExecutions, and related tables exist.  
   - Verify __EFMigrationsHistory and table existence (e.g. EventStore.RootEventId, OrderPayoutSnapshots, InboundWebhookReceipts).

2. **Query translation (blocking for email ingest)**  
   - Fix EmailIngestionSchedulerService: replace `j.PayloadJson.Contains(accountIdString)` with either in-memory filter after loading candidates, or raw SQL with PayloadJson::text LIKE, or a dedicated column.

3. **Tenant/company context (high)**  
   - Set Tenant:DefaultCompanyId in configuration for the legacy default company (if single-tenant).  
   - Ensure provisioned users have User.CompanyId set (already done in CompanyProvisioningService).  
   - Backfill User.CompanyId for existing users from their first department’s company if currently null.

4. **Controller CompanyId handling (high)**  
   - Replace “CompanyId ?? Guid.Empty” with explicit check: if company-scoped action and no CompanyId, return 403 or 400.  
   - Do not use Guid.Empty to mean “all companies” in normal API.

5. **Documentation and guards (medium)**  
   - Document that company create via UI/legacy API is single-company only; multi-tenant creation is via platform provisioning.  
   - Optionally add startup health check that verifies EventStore and JobExecutions schema (e.g. column list or single query).

6. **Hardening (lower)**  
   - Event platform retention: already handles errors per table.  
   - Missing payout snapshot: ensure TenantScope is not required (runs as system job; null tenant is correct).

---

## 9. Exact Code Change Recommendations

### 9.1 EmailIngestionSchedulerService – PayloadJson check

- **File:** `backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs`  
- **Method:** Logic that uses `context.JobExecutions.AnyAsync(j => ... && j.PayloadJson.Contains(accountIdString))`.  
- **Change:** Load pending/running emailingest jobs for the company (or globally for this scheduler), then in C# filter by deserialized payload containing emailAccountId; or use raw SQL: `PayloadJson::text LIKE '%' || @p || '%'` with parameter.  
- **Why:** Avoids EF translating Contains to invalid or inefficient sql on jsonb.

### 9.2 Controllers – avoid CompanyId ?? Guid.Empty for scoped actions

- **Files:** e.g. `InventoryController.cs`, `PnlController.cs`, `ApprovalWorkflowsController.cs`, `AssetTypesController.cs`, `EmailAccountsController.cs`, `SupplierInvoicesController.cs`.  
- **Method:** Any action that does `var companyId = _currentUserService.CompanyId ?? Guid.Empty` and then filters by companyId.  
- **Change:** If the operation is company-scoped, require CompanyId:  
  `var companyId = _currentUserService.CompanyId;`  
  `if (!companyId.HasValue || companyId.Value == Guid.Empty) return Forbid();`  
  Then use companyId.Value in queries.  
- **Why:** Prevents empty results and makes missing company context explicit (403 instead of empty list).

### 9.3 DefaultCompanyId configuration

- **File:** `appsettings.Development.json` (and production config).  
- **Change:** Add section:  
  `"Tenant": { "DefaultCompanyId": "<guid-of-default-company>" }`  
  for the legacy/default company.  
- **Why:** So requests with no company_id in JWT (legacy users) still get a valid tenant scope.

### 9.4 (Optional) EventStore schema check at startup

- **File:** e.g. a new health check or `Program.cs` startup.  
- **Change:** Run a simple query that selects RootEventId from EventStore (e.g. LIMIT 0) or check column existence. If it fails, log critical and optionally delay dispatcher start.  
- **Why:** Fails fast with a clear message instead of raw SQL exception in dispatcher.

---

## 10. Runtime Verification Checklist

After applying fixes, run through this checklist:

- [ ] **Migrations:** __EFMigrationsHistory contains entries for AddEventStorePhase8PlatformEnvelope (or equivalent), AddExternalIntegrationBus, AddOrderPayoutSnapshot, AddJobExecutions. Tables EventStore, OrderPayoutSnapshots, InboundWebhookReceipts, JobExecutions exist.
- [ ] **EventStore:** Start API; EventStoreDispatcherHostedService starts without exception. Trigger a domain event; event is claimed and processed (check EventStore status).
- [ ] **Login:** Log in as user with User.CompanyId set; JWT contains company_id. Log in as user with User.CompanyId null but with department membership; JWT contains company_id from department’s company.
- [ ] **DefaultCompanyId:** Log in as user with no company_id; with Tenant:DefaultCompanyId set, requests are scoped to that company (e.g. orders list shows only that company).
- [ ] **Company-scoped pages:** As normal user, open Orders, Inventory, P&L, Settings (departments, partners). Lists are non-empty when data exists for that company; no 500.
- [ ] **Email ingestion:** With EmailAccounts active, wait for scheduler or trigger; no exception from JobExecutions PayloadJson query; duplicate emailingest jobs are not enqueued for same account.
- [ ] **Missing payout snapshot:** Run MissingPayoutSnapshotSchedulerService (or wait for interval); repair runs without exception; OrderPayoutSnapshots and PayoutSnapshotRepairRuns exist.
- [ ] **Event retention:** EventPlatformRetentionWorkerHostedService runs without exception (tables exist); optional: run retention and check deletes in EventStore/InboundWebhookReceipts.
- [ ] **SuperAdmin:** As SuperAdmin, send X-Company-Id header; verify tenant scope switches (e.g. different company’s orders).
- [ ] **Provisioning:** Create new tenant via platform provisioning API; new company, departments, and admin user created; admin can log in and see their company’s data.

---

*End of audit.*

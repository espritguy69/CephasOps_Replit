# Post–Company Migration Remediation Plan

**Date:** 2025-03-12  
**Source:** [POST_COMPANY_MIGRATION_REGRESSION_AUDIT.md](./POST_COMPANY_MIGRATION_REGRESSION_AUDIT.md)

This plan groups fixes into four sections with priority, root cause, exact files, migration need, verification steps, and business impact.

---

## 1. Immediate Blockers (Fix Today)

### 1.1 Apply pending database migrations

| Field | Detail |
|-------|--------|
| **Priority** | P0 – app/workers cannot run correctly without these |
| **Why it broke** | DB restored from older backup or migrations not applied. EventStore lacks Phase 8 columns (RootEventId, etc.); OrderPayoutSnapshots, InboundWebhookReceipts, or JobExecutions tables may be missing. Raw SQL and EF queries then fail at runtime. |
| **Exact files to change** | None (operational). Migrations already exist in `backend/src/CephasOps.Infrastructure/Persistence/Migrations/`. Apply via: idempotent script from `dotnet ef migrations script --idempotent`, or `dotnet ef database update --project backend/src/CephasOps.Api`. Key migrations: `20260309210000_AddEventStorePhase8PlatformEnvelope`, `20260309120000_AddOrderPayoutSnapshot`, `20260310031127_AddExternalIntegrationBus`, `20260309230000_AddJobExecutions`. |
| **Migration required?** | **Yes** – run all pending migrations against the target database. |
| **Runtime verification** | (1) Query `SELECT "RootEventId" FROM "EventStore" LIMIT 0;` – no error. (2) Query `SELECT 1 FROM "OrderPayoutSnapshots" LIMIT 0;` and `FROM "InboundWebhookReceipts" LIMIT 0` – no error. (3) Start API; EventStoreDispatcherHostedService and MissingPayoutSnapshotSchedulerService start without exception. (4) Check `__EFMigrationsHistory` for the above migration names. |
| **Business impact** | Without this: event processing stops, payout repair fails, retention job fails, reporting (payout health) can fail. Core platform and background jobs are blocked. |

### 1.2 Fix JobExecutions.PayloadJson query (jsonb)

| Field | Detail |
|-------|--------|
| **Priority** | P0 – email ingestion scheduler can throw or behave incorrectly |
| **Why it broke** | PayloadJson is mapped as **jsonb**. EF translates `PayloadJson.Contains(accountIdString)` to SQL that may use LIKE on jsonb; PostgreSQL requires cast to text or causes invalid/inefficient query. |
| **Exact files to change** | `backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs` – replace the `AnyAsync` that uses `j.PayloadJson.Contains(accountIdString)` with: load pending/running emailingest jobs for the account’s company, then in C# filter by deserializing PayloadJson and checking for `emailAccountId` equal to account Id. |
| **Migration required?** | **No** |
| **Runtime verification** | (1) Start API with at least one active EmailAccount (PollIntervalSec > 0). (2) Wait for scheduler cycle (~30s) or trigger; no exception in logs. (3) Optionally add a second account and confirm only one emailingest job per account when due (no duplicate enqueue). (4) Check JobExecutions table for new emailingest rows. |
| **Business impact** | Email ingestion stops or throws; orders/parser flows that depend on ingested emails are blocked. Fix restores reliable email polling. |

---

## 2. Core App Flows (Fix Next)

### 2.1 Configure DefaultCompanyId for legacy users

| Field | Detail |
|-------|--------|
| **Priority** | P1 |
| **Why it broke** | When User.CompanyId and first-department company are both null, JWT has no company_id. TenantProvider falls back to TenantOptions.DefaultCompanyId. If unset, CurrentTenantId is null; global filter then shows all companies’ data (or downstream logic fails). |
| **Exact files to change** | Configuration only: `backend/src/CephasOps.Api/appsettings.Development.json` (and production appsettings or env). Add: `"Tenant": { "DefaultCompanyId": "<guid-of-default-company>" }`. Optional: document in README or ops runbook. |
| **Migration required?** | **No** |
| **Runtime verification** | (1) Set Tenant:DefaultCompanyId to the single company’s Id. (2) Log in as user with User.CompanyId = null but with department in that company; verify JWT has company_id. (3) Log in as user with no company_id (no CompanyId, no departments); verify requests are scoped to DefaultCompanyId (e.g. orders list shows only that company). |
| **Business impact** | Prevents cross-tenant data visibility for legacy users and ensures scoped APIs return correct data instead of empty or wrong company. |

### 2.2 Require CompanyId in company-scoped controllers (replace Guid.Empty) ✅ Done

| Field | Detail |
|-------|--------|
| **Priority** | P1 |
| **Why it broke** | Controllers use `_currentUserService.CompanyId ?? Guid.Empty` and filter by that. When CompanyId is null, Guid.Empty matches no real row, so lists are empty or behaviour is wrong. |
| **Exact files to change** | **Done:** `ControllerExtensions.cs` – `RequireCompanyId(ITenantProvider)`; all company-scoped controllers use it and return 403 when context missing. See `REMEDIATION_2_1_2_2_2_3.md`. |
| **Migration required?** | **No** |
| **Runtime verification** | (1) As user with company_id in JWT: inventory, P&L, settings pages load with data. (2) As user with no company_id and no DefaultCompanyId: expect 403 (or 400) on company-scoped endpoints instead of empty lists. (3) As SuperAdmin with X-Company-Id: can switch company and see that company’s data. |
| **Business impact** | Stops silent empty results; makes “no company context” explicit (403). Restores correct scoped data for valid users. |

### 2.3 Backfill User.CompanyId for existing users (optional data fix)

| Field | Detail |
|-------|--------|
| **Priority** | P2 |
| **Why it broke** | Legacy users may have User.CompanyId = null. JWT then uses first department’s company; if no departments, company_id is missing unless DefaultCompanyId is set. |
| **Exact files to change** | One-time script or SQL: for each User where CompanyId IS NULL, set CompanyId = (SELECT d.CompanyId FROM DepartmentMemberships dm JOIN Departments d ON dm.DepartmentId = d.Id WHERE dm.UserId = User.Id LIMIT 1). Optionally add a small migration or admin tool that runs this backfill. No change to AuthService if backfill is done; otherwise DefaultCompanyId is required. |
| **Migration required?** | **Optional** – data backfill only; can be a one-off SQL script or a small console job. No new EF migration required if using raw SQL. |
| **Runtime verification** | (1) Run backfill. (2) Query Users where CompanyId was null; verify they now have CompanyId set. (3) Those users log in and JWT contains company_id. |
| **Business impact** | Ensures every user has a deterministic company for JWT and reduces reliance on DefaultCompanyId for legacy users. |

---

## 3. Lower-Risk Regressions

### 3.1 Document legacy company create vs provisioning

| Field | Detail |
|-------|--------|
| **Priority** | P2 |
| **Why it broke** | CompanyService.CreateCompanyAsync throws “Only a single company is allowed” when a company already exists. New tenants must use platform provisioning; this is by design but undocumented. |
| **Exact files to change** | Docs: e.g. `docs/architecture/SAAS_PHASE2_TENANT_PROVISIONING_IMPLEMENTATION.md` or a short “Company and tenant creation” section. State: creating a second company via legacy Companies API is blocked; use platform admin provisioning API for new tenants. Optionally: add XML summary on CompaniesController create action or CompanyService.CreateCompanyAsync. |
| **Migration required?** | **No** |
| **Runtime verification** | (1) Call POST create company when one exists; expect 400/409 with clear message. (2) Use provisioning API to create tenant; success. |
| **Business impact** | Avoids confusion and failed attempts to create companies via the wrong path; clarifies supported multi-tenant onboarding. |

### 3.2 Frontend company context (SuperAdmin company switcher)

| Field | Detail |
|-------|--------|
| **Priority** | P3 |
| **Why it broke** | Frontend sends X-Department-Id but not X-Company-Id. Backend uses JWT company_id; X-Company-Id is only for SuperAdmin. If you add a company switcher UI, frontend must send X-Company-Id. |
| **Exact files to change** | Only if adding company switcher: `frontend/src/api/client.ts` – when user is SuperAdmin and has selected a company, add header `X-Company-Id: <selectedCompanyId>`. Backend already honours it in TenantProvider. |
| **Migration required?** | **No** |
| **Runtime verification** | (1) As SuperAdmin, select different company in UI; verify API requests include X-Company-Id. (2) Verify tenant-scoped data changes to selected company. |
| **Business impact** | Enables SuperAdmin to switch company in UI without manual header tools; low risk until switcher is implemented. |

---

## 4. Hardening / Technical Debt (After Stabilization)

### 4.1 Startup health check for critical schema

| Field | Detail |
|-------|--------|
| **Priority** | P3 |
| **Why it broke** | If EventStore or JobExecutions schema is missing, failure appears later (dispatcher or scheduler) with a raw SQL exception instead of a clear “migrations not applied” message. |
| **Exact files to change** | Add a health check or startup check (e.g. in `Program.cs` or IHealthCheck) that runs a minimal query that would fail if schema is wrong: e.g. `SELECT 1 FROM "EventStore" LIMIT 0` with a column that only exists after Phase 8 (e.g. `"RootEventId"`), or check `__EFMigrationsHistory` for expected migration. Optionally delay or disable EventStoreDispatcher until check passes. |
| **Migration required?** | **No** |
| **Runtime verification** | (1) With migrations applied: app starts and health check passes. (2) With DB missing Phase 8: health check fails with clear message (or logs critical). |
| **Business impact** | Faster, clearer failure when migrations are missing; easier ops and support. |

### 4.2 Event platform retention error handling

| Field | Detail |
|-------|--------|
| **Priority** | P3 |
| **Why it broke** | Retention job touches multiple tables; if one table is missing, the job could fail entirely instead of skipping that step. |
| **Exact files to change** | `backend/src/CephasOps.Application/Integration/EventPlatformRetentionService.cs` – already has per-step try/catch and Errors list. Consider: catch per table (e.g. InboundWebhookReceipts delete in its own try/catch) so one missing table does not stop the rest; log and add to result.Errors. |
| **Migration required?** | **No** |
| **Runtime verification** | (1) Run retention with all tables present; no errors. (2) Simulate missing table (e.g. rename); verify other steps still run and error is reported. |
| **Business impact** | Retention continues for other tables when one is missing; fewer total outages. |

### 4.3 Centralize company-scoped guard (optional)

| Field | Detail |
|-------|--------|
| **Priority** | P4 |
| **Why it broke** | Many controllers repeat the same “require CompanyId or 403” logic; duplication and risk of inconsistency. |
| **Exact files to change** | Add a shared helper or filter, e.g. `RequireCompanyContext()` extension or action filter that sets companyId and returns 403 if missing. Use it in company-scoped controllers instead of inline checks. Files: new helper/filter; then replace inline checks in InventoryController, PnlController, etc. |
| **Migration required?** | **No** |
| **Runtime verification** | Same as 2.2; ensure 403 behaviour is unchanged and no regressions. |
| **Business impact** | Consistent behaviour and less duplication; easier to add new company-scoped endpoints. |

---

## Implementation: Highest-Priority Blocker (1.2 PayloadJson fix) — DONE

The highest-priority **code** blocker is **1.2 Fix JobExecutions.PayloadJson query**. Blocker **1.1** is operational (apply migrations) and has no code change.

**Implemented:** `EmailIngestionSchedulerService` no longer uses `PayloadJson.Contains(accountIdString)` in EF. It now:
1. Queries `JobExecutions` for `JobType == "emailingest"` and `Status` Pending/Running, optionally scoped by `CompanyId == account.CompanyId`.
2. Loads only `PayloadJson` for those rows.
3. Uses a private static helper `PayloadContainsEmailAccountId(payloadJson, accountIdString)` that parses JSON and checks the `emailAccountId` property in C#.

**Files changed:** `backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs`

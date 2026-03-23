# Remediation 2.1, 2.2, 2.3 – Deliverables and 2.3 Assessment

**Date:** 2026-03-12

---

## 2.1 Configure and verify DefaultCompanyId

### Exact files changed
- **`backend/src/CephasOps.Api/appsettings.Development.json`** – Added section `"Tenant": { "DefaultCompanyId": "00000000-0000-0000-0000-000000000001" }`.
- **`backend/src/CephasOps.Api/appsettings.json`** – Added section `"Tenant": { "DefaultCompanyId": null }` so production can override via config/env.

### Why it broke
When `User.CompanyId` and the first-department company are both null, the JWT has no `company_id`. `TenantProvider` falls back to `TenantOptions.DefaultCompanyId`. If that was unset, `CurrentTenantId` was null; the global filter and company-scoped logic then behaved incorrectly (e.g. empty or wrong company scope).

### Migration required?
**No.**

### Runtime verification steps
1. **Set real default company Id**  
   Replace the placeholder in `appsettings.Development.json` with your default company Id, e.g. from:
   ```sql
   SELECT "Id" FROM "Companies" LIMIT 1;
   ```
   (Or set via production config / env `Tenant__DefaultCompanyId`.)
2. Log in as a user with `User.CompanyId = null` but with at least one department in that company → JWT should include `company_id`.
3. Log in as a user with no company (no `User.CompanyId`, no department memberships) → requests should still be scoped to `DefaultCompanyId` (e.g. orders/list shows only that company).
4. With `DefaultCompanyId` unset and no JWT `company_id` → company-scoped endpoints now return **403** (see 2.2) instead of empty results.

### Business impact
Newly provisioned or admin users without a company on the user record now get a valid default company context when `DefaultCompanyId` is set. This prevents cross-tenant data exposure and ensures scoped APIs return the correct company’s data.

---

## 2.2 Company-scoped enforcement (replace Guid.Empty)

### Exact files changed
- **`backend/src/CephasOps.Api/Common/ControllerExtensions.cs`** – Added `RequireCompanyId(this ControllerBase controller, ITenantProvider tenantProvider)` returning `(Guid companyId, ActionResult? error)`. Uses effective request company (JWT, SuperAdmin `X-Company-Id`, or `DefaultCompanyId`); returns 403 when missing.
- **Controllers** (injected `ITenantProvider`, use `RequireCompanyId` instead of `CompanyId ?? Guid.Empty`):
  - `ApprovalWorkflowsController`, `AssetTypesController`, `AutomationRulesController`, `BillingRatecardController`, `BinsController`, `BuildingsController`, `BusinessHoursController`, `EmailAccountsController`, `EmailsController`, `EscalationRulesController`, `GuardConditionDefinitionsController`, `InfrastructureController`, `InstallationMethodsController`, `InventoryController`, `KpiProfilesController`, `OrderStatusesController`, `PartnerGroupsController`, `PaymentsController`, `PayrollController`, `PnlController`, `PnlTypesController`, `ParserController`, `RatesController`, `SideEffectDefinitionsController`, `SlaProfilesController`, `SupplierInvoicesController`, `TasksController`, `WorkflowController`, `WorkflowDefinitionsController`, `WarehousesController`.

### Why it broke
Controllers used `_currentUserService.CompanyId ?? Guid.Empty` and passed that to queries. When `CompanyId` was null, `Guid.Empty` matched no rows, so lists were empty or behaviour was wrong with no clear error.

### Migration required?
**No.**

### Runtime verification steps
1. As a user **with** `company_id` in the JWT (or with `DefaultCompanyId` set): inventory, P&L, settings and other company-scoped pages load with correct data.
2. As a user **without** company context (no JWT `company_id`, no `DefaultCompanyId`): company-scoped endpoints return **403** with message “Company context is required for this operation.” (no silent empty lists).
3. As **SuperAdmin** with header `X-Company-Id: <companyGuid>`: can switch company and see that company’s data.

### Business impact
“No company context” is now explicit (403) instead of silent empty results. Scoped data is correct for users with valid context; SuperAdmin company switching continues to work.

---

## 2.3 User.CompanyId / DefaultCompanyId backfill – Assessment

### Is a one-time backfill required?
**Not strictly required**, but **recommended** for consistency and to reduce reliance on `DefaultCompanyId` and on resolving company from departments at login.

- **Current behaviour:**  
  `AuthService.ResolveUserCompanyIdAsync` already sets JWT `company_id` from `User.CompanyId` or, if null, from the first department’s company. If both are null, `TenantProvider` uses `DefaultCompanyId` (2.1). So with 2.1 and 2.2 in place, users can operate without a backfill.
- **Why backfill helps:**  
  (1) JWT can be built from `User.CompanyId` only, without querying departments on every login.  
  (2) Any code that reads `User.CompanyId` directly (e.g. admin/reporting) sees a value for users who have department membership.  
  (3) Fewer edge cases where “no company” is only fixed by `DefaultCompanyId`.

### Safest implementation path (if you do the backfill)

1. **Pre-check (read-only)**  
   Count users that would be updated and users that would stay null:
   ```sql
   -- Users with NULL CompanyId who have at least one department (candidates for backfill)
   SELECT COUNT(*) FROM "Users" u
   WHERE u."CompanyId" IS NULL
     AND EXISTS (SELECT 1 FROM "DepartmentMemberships" dm
                 INNER JOIN "Departments" d ON dm."DepartmentId" = d."Id"
                 WHERE dm."UserId" = u."Id");
   ```
   Optionally list them (e.g. `SELECT u."Id", u."Email", (SELECT d."CompanyId" FROM "DepartmentMemberships" dm INNER JOIN "Departments" d ON dm."DepartmentId" = d."Id" WHERE dm."UserId" = u."Id" LIMIT 1) AS "TargetCompanyId" FROM "Users" u WHERE ...`).

2. **One-time backfill script (idempotent)**  
   Run in a transaction; only set `CompanyId` where it is currently NULL and the user has at least one department (use first department’s company):
   ```sql
   BEGIN;
   UPDATE "Users" u
   SET "CompanyId" = sub."CompanyId"
   FROM (
     SELECT DISTINCT ON (dm."UserId") dm."UserId", d."CompanyId"
     FROM "DepartmentMemberships" dm
     INNER JOIN "Departments" d ON dm."DepartmentId" = d."Id"
     INNER JOIN "Users" u ON u."Id" = dm."UserId"
     WHERE u."CompanyId" IS NULL
     ORDER BY dm."UserId", d."Id"
   ) sub
   WHERE u."Id" = sub."UserId" AND u."CompanyId" IS NULL;
   -- Review row count, then COMMIT; or ROLLBACK;
   COMMIT;
   ```
   (Exact column names may differ; match your schema, e.g. `"CompanyId"` vs `CompanyId`.)

3. **Verification after backfill**  
   - Re-run the pre-check count: users with memberships and previously null `CompanyId` should now have `CompanyId` set.  
   - Users with **no** department memberships should still have `CompanyId` NULL (do not set).  
   - Spot-check: have a backfilled user log in and confirm JWT contains `company_id`.  
   - Optionally: run the backfill script again; it should update 0 rows (idempotent).

### Migration required?
**No** (data-only script; no EF migration).

### Business impact
Users with department membership get a stable `User.CompanyId` for JWT and reporting; login and company resolution are more predictable and less dependent on `DefaultCompanyId` and department lookups.

---

## Summary

| Step | Migration | Main outcome |
|------|-----------|--------------|
| 2.1  | No        | Default company context for users without company in JWT when `Tenant:DefaultCompanyId` is set. |
| 2.2  | No        | Company-scoped endpoints return 403 when context is missing instead of empty results. |
| 2.3  | No (optional data script) | Optional backfill of `User.CompanyId` from first department for existing users; recommended for consistency. |

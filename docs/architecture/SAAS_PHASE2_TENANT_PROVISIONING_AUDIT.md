# SaaS Phase 2 — Tenant Provisioning Audit

**Implementation:** See [SAAS_PHASE2_TENANT_PROVISIONING_IMPLEMENTATION.md](./SAAS_PHASE2_TENANT_PROVISIONING_IMPLEMENTATION.md) for the provisioning flow, APIs, defaults, and lifecycle.

---

## 1. Existing Company Creation Paths

### 1.1 CompaniesController

- **Location:** `CephasOps.Api/Controllers/CompaniesController.cs`
- **Endpoints:** GET list, GET by id, **POST create**, PUT update, DELETE. Deployment (template, validate, import, export).
- **Authorization:** `[Authorize]` only — no platform-admin-only restriction.
- **Create:** Calls `ICompanyService.CreateCompanyAsync(CreateCompanyDto)`.

### 1.2 CompanyService.CreateCompanyAsync

- **Location:** `CephasOps.Application/Companies/Services/CompanyService.cs`
- **Behaviour:**
  - **Single-company enforcement:** Throws if `existingCount > 0` — "Only a single company is allowed." So **current creation is not SaaS onboarding-safe**; it blocks a second company.
  - Validates ShortName uniqueness.
  - Creates Company with LegalName, ShortName, Vertical, locale settings. **Does not set:** Code, TenantId, SubscriptionId.
- **Conclusion:** Current company creation is **legacy single-company only**. New tenants cannot be created via this path.

### 1.3 TenantsController & TenantService

- **TenantsController:** `api/tenants` — List, GetById, GetBySlug, Create, Update. **Authorize:** SuperAdmin, Admin + `AdminTenantsView` / `AdminTenantsEdit`.
- **TenantService.CreateAsync:** Creates **Tenant** only (Id, Name, Slug, IsActive, CreatedAtUtc, UpdatedAtUtc). **No slug uniqueness check** in code (relies on DB/constraint). No link to Company, no subscription, no departments/users.

### 1.4 User Creation (AdminUserService)

- **IAdminUserService.CreateAsync:** Creates User (no CompanyId set), UserRoles, DepartmentMemberships (CompanyId from department). Validates duplicate email, at least one role, department memberships. **User.CompanyId is never set** in CreateAsync — so tenant resolution at login falls back to first department's company.
- **Conclusion:** For provisioning we must set **User.CompanyId** when creating the tenant admin.

### 1.5 Department Creation

- **IDepartmentService.CreateDepartmentAsync(CreateDepartmentDto, companyId):** Creates Department with CompanyId (nullable). Used by deployment/import. Suitable for provisioning if called with new company id.

### 1.6 Role Assignment

- **UserRoles:** UserId, RoleId, CompanyId (nullable). Roles are global (SuperAdmin, Admin, Director, etc.). Provisioning will assign Admin (or similar) to the tenant admin.

### 1.7 Seeders / Setup Scripts

- **Seed migration (20260106014834):** Inserts default Company (Cephas), Roles, GPON Department, Admin and Finance users, DepartmentMemberships, Skills, etc. All keyed by `v_company_id`. **One-time seed** for legacy tenant; not a reusable provisioning flow.
- **No** existing “create new tenant + company + departments + admin” flow.

### 1.8 Subscription Link

- **TenantSubscription:** TenantId, BillingPlanId, Status, StartedAtUtc, CurrentPeriodEndUtc, ExternalSubscriptionId.
- **ITenantSubscriptionService.SubscribeAsync(tenantId, planSlug):** Finds BillingPlan by slug, optionally calls IPaymentProvider (NoOp returns success), creates TenantSubscription. Can be used during provisioning if a default plan (e.g. trial) exists.
- **Company.SubscriptionId:** Optional; not currently linked to TenantSubscription (TenantSubscription is tenant-scoped; Company links to Tenant).

### 1.9 Auth / Onboarding

- Login uses User.CompanyId or first Department → CompanyId for JWT `company_id`. No invite or forced first-login flow beyond **MustChangePassword** (admin reset). Provisioning can set **MustChangePassword = true** for tenant admin so first login forces password change.

---

## 2. Gaps for SaaS Tenant Provisioning

| Capability | Exists? | Gap |
|------------|---------|-----|
| Create company | Yes (API) | Blocked after first company; no Code/TenantId/Subscription |
| Create tenant | Yes (API) | Tenant only; no Company link, no subscription/departments/users |
| Create user | Yes (Admin) | No CompanyId set; requires existing departments |
| Create departments | Yes | Needs company id; no “default set” for new tenant |
| Role assignment | Yes | Via UserRoles; works once user/departments exist |
| Subscription link | Partial | TenantSubscription exists; no automatic link on tenant create |
| Uniqueness (code, slug, email) | Partial | ShortName checked; slug/email not checked in tenant create |
| Lifecycle state (Active/Suspended/etc.) | No | Company has IsActive only; no PendingProvisioning/Active/Suspended |
| Single transactional flow | No | No one-shot “provision tenant” flow |
| Platform-only access | Partial | Tenants are Admin-only; Companies are any authenticated |

---

## 3. What a New Tenant Would Be Missing Today

If a platform admin created only a **Tenant** and a **Company** (bypassing the single-company check):

- No **default departments** (Operations, Finance, Inventory, etc.).
- No **tenant admin user** with CompanyId and memberships.
- No **TenantSubscription** (no plan).
- No **Company.Code** or **Company.TenantId** set on company.
- **User.CompanyId** would be null for any new user; login would rely on department membership.
- No baseline **settings/workflows/templates** for the new tenant (beyond global seed).

---

## 4. Conclusion

- **Current company creation:** Single-company only; not suitable for SaaS.
- **Current tenant creation:** Creates only the Tenant record.
- **No** unified, production-safe, repeatable flow that creates Company + Tenant + subscription + default departments + tenant admin.
- **Phase 2** must add a dedicated **tenant provisioning** path (e.g. `ICompanyProvisioningService`) that runs in one transactional (or clearly documented) flow and does **not** rely on the existing single-company `CreateCompanyAsync` path.

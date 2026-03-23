# SaaS Phase 2 — Tenant Provisioning Implementation

## Overview

Phase 2 makes CephasOps **tenant-provisionable**: a platform admin can create a new company tenant and get a usable operational environment without manual database work.

## Provisioning Architecture

- **Canonical unit:** Company = operational tenant root; Tenant = identity/slug; TenantSubscription = billing link (optional).
- **Flow:** Single transactional provisioning creates Tenant → Company → default departments → tenant admin user (with roles and department memberships) → optional TenantSubscription.
- **Service:** `ICompanyProvisioningService` / `CompanyProvisioningService` in `CephasOps.Application/Provisioning/`.

## Services and Endpoints

### Application

- **ICompanyProvisioningService**
  - `ProvisionAsync(ProvisionTenantRequestDto)` → `ProvisionTenantResultDto`
  - `IsCompanyCodeInUseAsync(code)`, `IsSlugInUseAsync(slug)`
- **ICompanyService** (extended)
  - `SetCompanyStatusAsync(companyId, CompanyStatus)` — activate/suspend/disable/trial/archived

### Platform Admin API (`api/platform/tenants`)

- **POST** `/provision` — Provision new tenant (company + defaults + admin). Requires `AdminTenantsEdit`.
- **GET** `/check-code?code=` — Check company code availability. Requires `AdminTenantsView`.
- **GET** `/check-slug?slug=` — Check tenant slug availability. Requires `AdminTenantsView`.
- **PATCH** `/companies/{companyId}/status` — Set company lifecycle status (body: `{ "status": "Active" | "Suspended" | "Disabled" | "Trial" | "Archived" | "PendingProvisioning" }`). Requires `AdminTenantsEdit`.

All require **SuperAdmin** or **Admin** role and the stated permission.

### Existing APIs Used by Platform Admin

- **GET** `/api/companies` — List all companies (Status and Code in DTO).
- **GET** `/api/companies/{id}` — Company detail including Status.
- **GET** `/api/tenants` — List tenants; **GET** `/api/tenants/{id}` and `by-slug/{slug}`.

## Provisioning Request and Result

- **ProvisionTenantRequestDto:** CompanyName, CompanyCode, Slug (optional, default from code), AdminFullName, AdminEmail, AdminPassword (optional), PlanSlug (optional), DefaultTimezone, DefaultLocale, InitialStatus (optional, default Active).
- **ProvisionTenantResultDto:** TenantId, CompanyId, CompanyCode, CompanyName, Slug, Status, AdminUserId, AdminEmail, MustChangePassword, Departments (id/name/code), SubscriptionId, PlanSlug.

Uniqueness enforced: company code, tenant slug, admin email. Duplicate code/slug/email → 409 with clear message.

## Default Tenant Bootstrap

On provision, the following are created **for the new company** (all stamped with the new CompanyId):

- **Default departments:** Operations (GPON), Finance, Inventory, Scheduler, Admin.
- **Tenant admin user:** CompanyId set, hashed password (or generated temp), MustChangePassword when no password supplied.
- **UserRole:** Admin or SuperAdmin (first found in DB).
- **DepartmentMemberships:** All five departments; first is default; ADMIN department role HOD, others Member.
- **TenantSubscription (optional):** When PlanSlug is provided; status Trialing or Active from InitialStatus.

No workflow/template/reference data beyond what is required for login and basic usage (departments and admin).

## Tenant Admin Bootstrap

- If **AdminPassword** is provided: that password is hashed and used; **MustChangePassword** = false.
- If **AdminPassword** is empty: a temporary password is generated; **MustChangePassword** = true. (First login or password-reset flow should force change.)
- Tenant admin can authenticate and is scoped to their CompanyId (JWT/session company context).

## Lifecycle States

- **CompanyStatus** enum: `PendingProvisioning`, `Active`, `Suspended`, `Disabled`, `Trial`, `Archived`.
- Stored on **Company** as string (max 32). Migration: `20260310140000_AddCompanyStatus` adds column with default `Active`.
- Existing companies receive `Active` via default. Platform can set status via **PATCH** `api/platform/tenants/companies/{companyId}/status`.

## Authorization Boundaries

- **Platform admin:** SuperAdmin or Admin role + `AdminTenantsView` / `AdminTenantsEdit`. Can provision tenants, list companies/tenants, set company status, check code/slug.
- **Tenant admin:** Cannot create another tenant, cannot call platform provisioning or status endpoints (no permission), cannot inspect other tenants (tenant filtering in existing APIs).
- Enforced via `[Authorize(Roles = "SuperAdmin,Admin")]` and `[RequirePermission(AdminTenantsView | AdminTenantsEdit)]` on platform endpoints.

## Safety and Idempotency

- Duplicate **company code** → 409.
- Duplicate **slug** → 409 (and TenantService.CreateAsync now checks slug uniqueness).
- Duplicate **admin email** → 409.
- Provisioning runs in a **single database transaction**; on failure, transaction is rolled back (no half-created tenant).
- Retries with same code/slug/email fail at validation; no duplicate departments/users/subscriptions created.

## Tenant Context Integrity

- Company, Department, User, UserRole, DepartmentMembership, and optional TenantSubscription created in provisioning all use the **new** CompanyId or TenantId. No provisioned entity is assigned to the legacy Cephas company.

## Documentation and Validation

- **Audit:** `docs/architecture/SAAS_PHASE2_TENANT_PROVISIONING_AUDIT.md`.
- **This doc:** provisioning flow, defaults, lifecycle, platform vs tenant admin, failure/repair (rollback on exception; fix data and retry with new code/slug/email if needed).

**Recommended validation:**

1. Call **POST** `api/platform/tenants/provision` with a new company code, slug, and admin email.
2. Confirm 201 and result DTO (company id, admin user id, departments).
3. Log in as the new tenant admin; confirm tenant-scoped data only.
4. Confirm default departments exist for the new company.
5. Confirm legacy Cephas tenant and data are unchanged.
6. Call **PATCH** `api/platform/tenants/companies/{companyId}/status` with `Suspended`; confirm company status updated.

## Remaining Risks / Follow-ups

- **CompanyService.CreateCompanyAsync** still enforces single-company; new tenants must be created **only** via provisioning (POST `api/platform/tenants/provision`). Do not use POST `api/companies` for SaaS onboarding.
- First-login **password change** UX (when MustChangePassword is true) depends on existing auth/reset flows; no change in this phase.
- **Subscription enforcement** (blocking suspended/expired tenants) is Phase 3; provisioning only sets state.
- **Minimal admin UI** for tenant list/create/detail was not implemented; backend correctness was prioritized. UI can be added later using the same APIs.

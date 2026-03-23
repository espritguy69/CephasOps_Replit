# Provisioning Flow

**Date:** 2026-03-13  
**Purpose:** Describe tenant creation, company creation, department initialization, tenant admin creation, role assignment, subscription linkage, and feature flags. Aligns with CompanyProvisioningService and platform signup.

---

## 1. Tenant Creation

- **Trigger:** Platform signup (e.g. `POST /api/platform/signup`) or platform-admin–driven provisioning.
- **Actor:** Unauthenticated (signup) or PlatformAdmin (admin-driven).
- **Process:** **CompanyProvisioningService.ProvisionAsync** runs under **TenantScopeExecutor.RunWithPlatformBypassAsync** (platform bypass required because no tenant exists yet).

Steps (conceptual):

1. Validate input (company name, code, slug, admin email/password).
2. Check uniqueness: company code, slug, email (no duplicate tenant/company/user).
3. Create **Tenant** (platform entity).
4. Create **Company** (linked to Tenant).
5. Create default **Departments** (e.g. Operations, Finance) for the company.
6. Create **TenantSubscription** (e.g. trial) and link to tenant.
7. Initialise **TenantFeatureFlags** and **TenantOnboardingProgress** as needed.
8. Create admin **User** (CompanyId = new company), assign roles (e.g. Admin), and optionally DepartmentMembership.
9. Return tenant id, company id, admin email, and optional must-change-password flag.

---

## 2. Company Creation

- **Within provisioning:** Company is created as part of ProvisionAsync (step 4 above); not via **CompanyService.CreateAsync**, which enforces "only a single company" and is for legacy single-company flows.
- **Data:** Company record with legal name, code, timezone, locale, etc., and CompanyId used for all tenant-scoped entities.

---

## 3. Department Initialization

- Default departments are created during provisioning for the new company (e.g. Operations, Finance). Each department has **CompanyId** set to the new company.

---

## 4. Tenant Admin Creation

- Admin user is created with **CompanyId** = new company, and assigned roles that grant tenant-admin capabilities (e.g. Admin, or TenantAdmin if that role exists). Password is set from signup or generated; optional must-change-password on first login.

---

## 5. Role Assignment

- Roles are assigned to the new user via the same RBAC mechanism used elsewhere (e.g. UserRole, or role claims). Ensure the admin has permissions to manage users, departments, and settings **within that tenant only**.

---

## 6. Subscription Linkage

- **TenantSubscription** (or equivalent) is created and linked to the **Tenant** (e.g. trial plan, start/end date). Subscription state is used by **SubscriptionEnforcementMiddleware** (or equivalent) to block or limit access when subscription is cancelled, past-due, or expired.

---

## 7. Feature Flags Initialization

- **TenantFeatureFlags** (or equivalent) are initialised per tenant so that features can be enabled/disabled per tenant. Provisioning sets defaults (e.g. trial feature set).

---

## 8. How Onboarding Works

- After signup, the admin logs in and may be guided through an **onboarding wizard** (see docs/saas_operations/ONBOARDING_FLOW.md).
- Progress is stored in **TenantOnboardingProgress** (per tenant). Steps: company setup, department setup, user invitations, basic config. APIs: e.g. `GET /api/onboarding/status`, `PATCH /api/onboarding/status` to mark steps complete.

---

## 9. References

- **Backend:** CompanyProvisioningService (platform bypass, creates Tenant, Company, Departments, Admin).
- **Docs:** [ONBOARDING_FLOW.md](../saas_operations/ONBOARDING_FLOW.md), [TENANT_PROVISIONING_FLOW.md](../saas_scaling/TENANT_PROVISIONING_FLOW.md) (if present).

---

*See also: [TENANCY_MODEL.md](TENANCY_MODEL.md), [AUTHORIZATION_MATRIX.md](AUTHORIZATION_MATRIX.md), [BILLING_SUBSCRIPTIONS.md](BILLING_SUBSCRIPTIONS.md).*

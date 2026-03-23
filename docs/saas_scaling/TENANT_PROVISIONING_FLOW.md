# Tenant Provisioning Flow

**Date:** 2026-03-13

End-to-end flow for self-service (or admin-driven) tenant provisioning in CephasOps.

---

## 1. Request

**POST /api/platform/tenants/provision**

- **Authorization:** SuperAdmin (and permission AdminTenantsEdit).
- **Body (JSON):**
  - **CompanyName** (required)
  - **CompanyCode** (required, unique)
  - **Slug** (optional; derived from CompanyCode if not set)
  - **AdminFullName** (required)
  - **AdminEmail** (required, unique)
  - **AdminPassword** (optional; if empty, a temporary password is generated and MustChangePassword = true)
  - **PlanSlug** (optional; if not set, a default trial subscription is created)
  - **TrialDays** (optional; default 14 when creating a trial)
  - **DefaultTimezone** (optional; default Asia/Kuala_Lumpur)
  - **DefaultLocale** (optional; default en-MY)
  - **InitialStatus** (optional; Active, Trial, etc.)

---

## 2. Validation (before transaction)

- **Company code:** Normalized (trim, upper); must not exist (**IsCompanyCodeInUseAsync**). Conflict → 409.
- **Slug:** Normalized (trim, lower); must not exist (**IsSlugInUseAsync**). Conflict → 409.
- **Admin email:** Normalized (trim, lower); must not exist in Users. Conflict → 409.
- Required fields empty → 400.

---

## 3. Transactional creation

All steps run inside **RunWithPlatformBypassAsync** and a single **DbContext** transaction:

1. **Tenant:** Insert Tenant (Id, Name, Slug, IsActive, CreatedAtUtc, UpdatedAtUtc).
2. **Company:** Insert Company (Id, TenantId, LegalName, ShortName, Code, Status, DefaultTimezone, DefaultLocale, …). Set **SubscriptionId** after subscription is created.
3. **SaveChanges** to get Company.Id.
4. **Default departments:** Insert 5 departments (Operations GPON, Finance, Inventory, Scheduler, Admin) with CompanyId.
5. **SaveChanges**.
6. **Admin user:** Resolve Admin (or SuperAdmin) role; insert User (CompanyId, Name, Email, PasswordHash, MustChangePassword, …); insert UserRole; insert DepartmentMemberships for all departments (HOD for Admin dept, Member for others).
7. **SaveChanges**.
8. **TenantSubscription:** Resolve plan by PlanSlug or default "trial" or first active plan. Insert TenantSubscription (TenantId, BillingPlanId, Status Trialing/Active, StartedAtUtc, CurrentPeriodEndUtc, **TrialEndsAtUtc** when trial, **BillingCycle**, **NextBillingDateUtc**). Set **Company.SubscriptionId** and SaveChanges.
9. **Commit** transaction.

On any exception, transaction is rolled back and the exception is rethrown.

---

## 4. Response

**201 Created** with **ProvisionTenantResultDto:**

- TenantId, CompanyId, CompanyCode, CompanyName, Slug, Status
- AdminUserId, AdminEmail, MustChangePassword
- Departments (Id, Name, Code)
- SubscriptionId, PlanSlug

---

## 5. Post-provision

- Tenant admin logs in with AdminEmail and the provided (or temporary) password.
- If **MustChangePassword** is true, force password change on first login (e.g. via change-password-required flow).
- **SubscriptionEnforcementMiddleware** and **SubscriptionAccessService** will enforce trial end and subscription status on subsequent requests.

---

## 6. Uniqueness and rollback

- **Slug uniqueness:** Enforced by check before transaction and unique index (if present) on Tenants.Slug.
- **Company code uniqueness:** Enforced by check and unique constraint on Companies.Code.
- **Email uniqueness:** Enforced by check and unique constraint on Users.Email.
- **Rollback on failure:** Single transaction ensures no partial tenant (no orphan Tenant without Company or vice versa).

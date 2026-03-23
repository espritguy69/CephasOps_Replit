# SaaS Onboarding Flow

**Date:** 2026-03-13

This document describes the tenant onboarding flow: self-service signup, first login, and the onboarding wizard.

---

## 1. Self-service signup

- **Endpoint:** `POST /api/platform/signup` (public, no auth).
- **Body:** `SignupRequestDto`
  - `CompanyName` (required)
  - `CompanyCode` (required)
  - `Slug` (optional; defaults from CompanyCode)
  - `AdminFullName` (required)
  - `AdminEmail` (required, valid email)
  - `AdminPassword` (required, min 8 characters)

**Safeguards:**
- Company code uniqueness: rejected if another company has the same code (case-insensitive).
- Slug uniqueness: rejected if another tenant has the same slug.
- Email uniqueness: rejected if a user with that email already exists.

**Flow:**
1. Request validated (format, uniqueness).
2. Tenant provisioning runs (tenant, company, default departments, trial subscription, admin user).
3. Response: `SignupResultDto` with `TenantId`, `CompanyId`, `AdminEmail`, `Message`, `MustChangePassword`.

**Email verification:** Current implementation does not require email verification before first login. You can add a verification step (e.g. send email with link, require verification token before enabling login) in a later phase.

---

## 2. First login

- After signup, the admin user can log in with `POST /api/auth/login` using `AdminEmail` and `AdminPassword`.
- If a temporary password was used, `MustChangePassword` is true and the user may be required to change password on first login (per your auth flow).

---

## 3. Onboarding wizard

- **Purpose:** Guide new tenants through company setup, department setup, user invitations, and basic configuration.
- **Tracking:** Progress is stored in `TenantOnboardingProgress` (per tenant).

**Steps (conceptual):**
- **Company setup** – e.g. company profile, timezone, locale.
- **Department setup** – e.g. adjust default departments, add more.
- **User invitations** – invite additional users.
- **Basic config** – e.g. order types, settings.

**APIs (authenticated tenant user):**
- `GET /api/onboarding/status` – Returns current onboarding progress for the tenant (`CompanySetupDone`, `DepartmentSetupDone`, `UserInvitationsDone`, `BasicConfigDone`, `IsComplete`, `CompletedAtUtc`). If no row exists, one is created with all steps false.
- `PATCH /api/onboarding/status` – Mark a step complete. Body: `{ "step": "company" | "department" | "invitations" | "config" }`. Step names are case-insensitive; aliases (`companysetup`, `departmentsetup`, etc.) are accepted.

**Frontend:** After first login, call `GET /api/onboarding/status`. If `IsComplete` is false, show the wizard and call `PATCH` when each step is done.

---

## 4. References

- [TENANT_PROVISIONING_FLOW.md](../saas_scaling/TENANT_PROVISIONING_FLOW.md) – Provisioning details.
- [SAAS_OPERATIONS_GUIDE.md](../saas_scaling/SAAS_OPERATIONS_GUIDE.md) – Operations overview.

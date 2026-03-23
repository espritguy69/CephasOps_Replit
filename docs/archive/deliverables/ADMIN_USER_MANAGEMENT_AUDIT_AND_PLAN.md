# Admin User Management – Audit and Implementation Plan

**Date:** 2026-03-08  
**Status:** Implementation reference

---

## Phase 1 – Audit Summary

### 1. User entity

- **Location:** `backend/src/CephasOps.Domain/Users/Entities/User.cs`
- **Shape:** `Id`, `Name`, `Email`, `Phone?`, `PasswordHash?`, `IsActive`, `CreatedAt`
- **Navigation:** `DepartmentMemberships` (collection) → many-to-many with Department via `DepartmentMembership`
- **Note:** No `Username`; login is by **Email** only. UI will show "Email" as the login identifier.

### 2. UsersController (existing)

- **Route:** `api/users`
- **Endpoints:**
  - `GET /api/users` – list users (department-scoped when `departmentId` or X-Department-Id), optional `search`, `isActive`; returns `UserListDto[]` (no roles in list).
  - `GET /api/users/{id}` – single user with roles and department memberships (`UserDetailDto`).
  - `GET /api/users/by-department/{departmentId}` – users in a department.
- **Authorization:** `[Authorize]` only; department scope enforced via `IDepartmentAccessService`.
- **Gaps:** No create, update, activate/deactivate, change role, or password reset. No admin-only, unscoped “list all users” for SuperAdmin/Admin.

### 3. Auth / login

- **AuthController:** `POST /api/auth/login` (email + password), `POST /api/auth/refresh`, `GET /api/auth/me` (current user).
- **AuthService:** Validates email, active status, password via `DatabaseSeeder.VerifyPassword`. JWT includes `sub`, email, roles. Refresh tokens stored hashed; old tokens revoked on login.
- **Password hashing:** `DatabaseSeeder.HashPassword` / `VerifyPassword` (SHA256 + salt). Used in seeder and DiagnosticsController only; no public reset/change API.

### 4. Role / permission model

- **Entities:** `Role` (Id, Name, Scope), `UserRole` (UserId, RoleId, CompanyId?), `Permission`, `RolePermission`.
- **Department membership:** `DepartmentMembership` (UserId, DepartmentId, Role string e.g. "Member", "HOD", IsDefault); company-scoped via `CompanyScopedEntity`.
- **Roles (from seed/docs):** SuperAdmin, Admin, Director, HeadOfDepartment, Supervisor, Member. SuperAdmin is global; others can be department-restricted.
- **No existing API** that returns a list of role names for dropdowns; admin user UI will need roles list (e.g. from new admin endpoint or seed data).

### 5. User create/update and password flows

- **Create/update:** Not present in API. Users are created only in `DatabaseSeeder` (default admin, Finance HOD).
- **Password reset/change:** No API. DiagnosticsController has dev-only `fix-admin-user` (hardcoded email/password); not for production use.

### 6. Department membership

- **Model:** Many-to-many: User ↔ DepartmentMembership ↔ Department. Each membership has a `Role` string and `IsDefault`.
- **Existing behaviour:** Non–SuperAdmin users are department-restricted; frontend sends X-Department-Id; backend uses `IDepartmentAccessService` / `ResolveDepartmentScopeAsync`.

### 7. Admin / settings routes (frontend)

- **Settings:** Under `/settings/*`, protected by `SettingsProtectedRoute` (SuperAdmin, Director, HeadOfDepartment, Supervisor).
- **Admin:** `/admin/background-jobs` uses `SettingsProtectedRoute`.
- **No route** that restricts to SuperAdmin/Admin only; backend will enforce `[Authorize(Roles = "SuperAdmin,Admin")]` for admin user management.
- **RBAC v2 Phase 2 (2026-03-09):** Admin user management, security activity, session revoke, admin roles, payout health/anomalies, rates, and payroll are now also protected by permission attributes (e.g. admin.users.view, admin.users.edit, admin.security.view, admin.roles.view, payout.health.view, rates.edit, payroll.run). See [RBAC_V2_ROLLOUT_PHASE2](RBAC_V2_ROLLOUT_PHASE2.md) and [RBAC_V2_PERMISSION_MATRIX_DELIVERABLE](RBAC_V2_PERMISSION_MATRIX_DELIVERABLE.md) §12.
- **RBAC v2 Phase 3 (2026-03-09):** Department-aware authorization: permissions define what a user can do; department memberships define which departments they can access. Department-scoped endpoints (orders, reports, payroll si-rate-plans, inventory, etc.) already use IDepartmentAccessService; controller helpers ResolveDepartmentScopeOrFailAsync / EnsureDepartmentAccessOrFailAsync normalize 400/403. See [RBAC_V2_ROLLOUT_PHASE3_DEPARTMENT_SCOPE](RBAC_V2_ROLLOUT_PHASE3_DEPARTMENT_SCOPE.md).
- **RBAC v2 Phase 4 (2026-03-09):** Full module coverage: Orders, Reports, Inventory, Background Jobs, and Settings endpoints are protected by RequirePermission (orders.view/edit, reports.view/export, inventory.view/edit, jobs.view, settings.view/edit). Admin seed and TestUserPermissionProvider grant these prefixes to Admin. See [RBAC_V2_ROLLOUT_PHASE4_FULL_COVERAGE](RBAC_V2_ROLLOUT_PHASE4_FULL_COVERAGE.md).

### 8. Audit logging

- **IAuditLogService** exists (`LogAuditAsync`, `GetAuditLogsAsync`). Can be used for user Created/Updated/Deactivated/PasswordReset.

### 9. Last login

- Not stored on User or RefreshToken. **Last Login** column can be omitted in v1 or later derived from `RefreshToken.CreatedAt` (max per user).

---

## Phase 2 – First release scope

- **User list page:** Paged, search (name/email), filter by role, filter by active/inactive.
- **View user:** Reuse/align with existing `UserDetailDto` (name, email, phone, roles, departments, status, createdAt).
- **Create user:** Name, email, phone, initial password (or temp), role(s), optional department memberships; duplicate email/username validation (email only – no username in DB).
- **Edit user:** Name, email, phone, roles, department memberships; no password in edit (use “Reset password”).
- **Activate / deactivate:** Toggle with confirmation; guard: do not deactivate current user if it would lock access; do not allow removing last active SuperAdmin/Admin.
- **Change role:** Update UserRole(s); ensure at least one admin remains.
- **Department memberships:** Include in create/edit if supported cleanly (existing DepartmentMembership model supports it).
- **Reset password:** Admin-only action: set temporary password; optionally “must change on first login” if we add a flag later (v1 can be simple set password).
- **No** permissions matrix UI in v1.

---

## Phase 3–7 – Implementation approach

- **Backend:** New controller `AdminUsersController` at `api/admin/users`, `[Authorize(Roles = "SuperAdmin,Admin")]`. Service layer for validation (duplicate email, last admin, self-deactivate). DTOs and validators; audit log for sensitive actions. Use `DatabaseSeeder.HashPassword` for new/reset passwords; never return hashes.
- **Frontend:** New User Management page under settings (`/settings/users`) or admin (`/admin/users`), using existing settings layout and table/modal patterns. Columns: Full Name, Email, Role(s), Departments, Status, Created At, Actions (View, Edit, Change Role, Activate/Deactivate, Reset Password). Reuse api client, toasts, confirmations.
- **Security:** Backend authorization mandatory; department rules unchanged; no public registration; no plain-text password storage except temporary password in reset flow (immediate hash).
- **Tests:** Admin list, non-admin 403, duplicate email, role update, activate/deactivate, reset password, last-admin guard, department assignments.
- **Docs:** Update department_rbac, onboarding, api_surface_summary, data_model_overview as needed.

---

## Assumptions and risks

- **Username:** Domain has no Username; UI uses Email as login identifier.
- **Password hashing:** Continue using `DatabaseSeeder.HashPassword` for consistency; consider moving to a dedicated password hasher (e.g. Identity) in a later iteration.
- **Must-change-password:** Not implemented in auth flow today; v1 reset can set password without forcing change on first login; can add later.
- **Last login:** Omitted in v1; can add later from RefreshToken or new column.

---

## Files to add/change (summary)

- **Backend:** AdminUsersController, DTOs, optional IAdminUserService + implementation, validators; register in DI; audit calls where applicable.
- **Frontend:** User management page, API module for admin users, route, sidebar link (Admin or Settings).
- **Docs:** department_rbac.md, onboarding.md, api_surface_summary.md, data_model_overview.md (targeted updates).

---

## v1.1 enhancements (2026-03-08)

- **Department membership UI:** Create/edit user form includes a department multi-select (checkboxes) and optional per-department role (Member, HOD). Table and view show departments as badges with role. Duplicate department IDs and invalid department IDs are rejected by the backend.
- **Audit logging:** Sensitive actions (create user, update user, activate, deactivate, change roles, reset password) are logged via `IAuditLogService` with entity type `User`, action, and change summary. Passwords are never logged. Audit service is optional in the service (null in tests).
- **Validation and safety:** At least one role required on create and update; duplicate department IDs in request rejected; invalid department IDs return 400; last active admin and self-deactivate guards unchanged.
- **UX and diagnostics:** Inline form error message in create/edit modal; “Creating…” / “Saving…” on submit buttons; troubleshooting note on page; API error messages surfaced in toasts.

---

## v1.2 account lifecycle hardening (2026-03-08)

- **Last login:** `User.LastLoginAtUtc` (nullable UTC) updated on successful login only. Shown in admin list (Last Login column), detail view, and API DTOs.
- **Must-change-password:** `User.MustChangePassword` (bool). When true:
  - Login and refresh return **403** with `{ data: { requiresPasswordChange: true } }`; no tokens issued; `LastLoginAtUtc` not updated.
  - User must complete change via **POST /api/auth/change-password-required** (email + current + new password, no auth).
- **Admin reset:** Reset-password request supports `ForceMustChangePassword` (default true). When set, user’s `MustChangePassword` is set to true after reset.
- **Self-service change:** Authenticated **POST /api/auth/change-password** (current + new password); unauthenticated **POST /api/auth/change-password-required** for forced-change flow. On success: password updated, `MustChangePassword = false`, refresh tokens revoked (change-password endpoint).
- **Audit:** Password-change-required flag changes, self-service password change, and admin reset (including force flag) are audited; passwords/hashes never logged.
- **Frontend:** Admin UI shows Last Login and Must Change Password; reset modal has “Require password change on next login” (default on). Forced-change users are routed to Change Password page until they complete the flow.

---

## v1.4 Phase 1 – Authentication Security Monitoring (2026-03-08)

- **Goal:** Give admins visibility into login and security events so they can monitor and investigate incidents.
- **Approach:** Reuse existing `IAuditLogService` and `AuditLog` table; auth events use `EntityType = "Auth"` and `Action` = event type. No new table; IP and UserAgent stored in `MetadataJson` where available.
- **Event types:** `LoginSuccess`, `LoginFailed`, `AccountLocked`, `PasswordChanged`, `PasswordResetRequested`, `PasswordResetCompleted`, `AdminPasswordReset`, `TokenRefresh`. Passwords, reset tokens, and hashes are never logged.
- **Emitted in:** AuthService (login, refresh, change-password, change-password-required, forgot-password, reset-with-token), AdminUserService (admin reset).
- **Admin Security Activity page:** Route `/admin/security/activity`. Paginated table with filters: user, event type, date range. Columns: timestamp, user email, event, IP, user agent. API: `GET /api/logs/security-activity` (SuperAdmin/Admin only).
- **User detail:** “Security Activity (last 10)” block on Admin User Management view modal for quick diagnosis.
- **Docs:** See “Authentication Security Monitoring” below and [dev/onboarding](dev/onboarding.md).

### Authentication Security Monitoring

**Event types**

| Event | When logged |
|-------|-------------|
| LoginSuccess | After successful login (user id known) |
| LoginFailed | Invalid password, unknown user, or inactive (user id if known) |
| AccountLocked | When login is rejected due to lockout |
| PasswordChanged | Self-service change or forced-change flow |
| PasswordResetRequested | Forgot-password when a reset token is created |
| PasswordResetCompleted | User completed reset-with-token |
| AdminPasswordReset | Admin used “Reset password” in User Management |
| TokenRefresh | Refresh token exchanged for new access token |

**Admin Security Activity page**

- **Path:** `/admin/security/activity` (sidebar: Admin → Security Activity).
- **Filters:** User (dropdown), event type, date from, date to.
- **Table:** Timestamp (UTC), user email, event, IP address, user agent. Metadata JSON is available in the API for extra context.
- **Use:** Monitor suspicious logins, confirm lockouts, verify password resets and admin resets, investigate “I can’t log in” reports.

**Investigating incidents**

1. **User locked out:** Filter by user and event type “AccountLocked”; check timestamp and IP. Confirm with “LoginFailed” events just before.
2. **Password reset abuse:** Filter by “PasswordResetRequested” or “PasswordResetCompleted”; check IP and user agent for unknown devices.
3. **Admin reset audit:** Filter by “AdminPasswordReset” and optionally user to see who reset whom and when.
4. **Failed logins:** Filter by “LoginFailed” (and optionally user) to see brute-force or wrong-password patterns; correlate with “AccountLocked” if lockout is enabled.

---

## v1.4 Phase 2 – Suspicious Activity Detection (2026-03-08)

- **Goal:** Automatically detect suspicious patterns in auth events and surface them to admins. Additive only; no auth flow changes.
- **Approach:** `SecurityAnomalyDetectionService` scans recent Auth audit events (via `GetAuthEventsForDetectionAsync`), runs configurable rules, and returns alerts. No new DB table; alerts are computed on demand.
- **Rules (configurable in code – see `SecurityDetectionRules` and `SecurityAlertTypes`):**
  - **Excessive login failures:** &gt;10 `LoginFailed` events for the same user within 5 minutes.
  - **Password reset abuse:** &gt;3 `PasswordResetRequested` events for the same user within 15 minutes.
  - **Multiple IP login:** `LoginSuccess` from 3 or more different IP addresses for the same user within 10 minutes.
- **API:** `GET /api/logs/security-alerts` (SuperAdmin/Admin only) with optional `dateFrom`, `dateTo`, `userId`, `alertType`.
- **Security Activity page:** New “Security Alerts” panel with table (timestamp, user, alert type, description, IP summary) and filter by alert type.
- **User detail:** “Security Alerts” section showing alerts for that user in the last 7 days (e.g. “⚠ Excessive login failures detected on …”).

### Suspicious Activity Detection

**Alert types**

| Alert type | Rule | Threshold (code config) |
|------------|------|-------------------------|
| Excessive login failures | Same user, LoginFailed events in short window | &gt;10 in 5 minutes |
| Password reset abuse | Same user, PasswordResetRequested in window | &gt;3 in 15 minutes |
| Multiple IP login | Same user, LoginSuccess from different IPs | ≥3 distinct IPs in 10 minutes |

**Where to see alerts**

- **Security Activity page** (`/admin/security/activity`): “Security Alerts” panel above the activity table. Use the same user/date filters as the activity table; optionally filter by alert type and click “Refresh alerts”.
- **User detail (Admin User Management):** When viewing a user, “Security Alerts” shows alerts for that user in the last 7 days.

**How to investigate**

1. **Excessive login failures:** Indicates possible brute-force or repeated wrong password. Check the Security Activity table for that user and time range; look at IP and user agent. Consider lockout (v1.3 Phase C) and whether the user needs a password reset.
2. **Password reset abuse:** User may be requesting many reset emails (forgot-password). Check IP summary for multiple locations; confirm with the user if legitimate. Consider rate-limiting or alerting if this recurs.
3. **Multiple IP login:** Successful logins from several IPs in a short window may indicate credential sharing or compromise. Cross-check with the user; consider forcing password change or revoking sessions.

Detection is read-only analysis over existing audit data; it does not change login, lockout, or password reset behaviour.

---

## v1.4 Phase 3 – Session Management (2026-03-08)

- **Goal:** Admins can see and revoke active user sessions (refresh tokens). No JWT or token architecture changes.
- **Session model:** One active session = one non-revoked, non-expired refresh token. `UserSessionDto`: SessionId (RefreshToken.Id), UserId, UserEmail, CreatedAtUtc, ExpiresAtUtc, IpAddress, UserAgent, IsRevoked.
- **RefreshToken:** Existing entity; added nullable `UserAgent`. `CreatedFromIp` and `UserAgent` are now set on login and on token refresh (from HTTP context).
- **API:** `GET /api/admin/security/sessions` (filters: userId, dateFrom, dateTo, activeOnly), `GET /api/admin/security/sessions/user/{userId}`, `POST /api/admin/security/sessions/{sessionId}/revoke`, `POST /api/admin/security/sessions/revoke-all/{userId}`. Admin/SuperAdmin only.
- **UI:** Security Activity page has an “Active Sessions” section (table: User, Created, Expires, IP, Device, Status, Revoke). User detail modal has “Active Sessions” with per-session Revoke and “Revoke all sessions”. Confirmation when revoking own session.
- **Safety:** Revoking a session sets `IsRevoked` and `RevokedAt` on the refresh token; the access token expires naturally. No JWT changes.

### Session Management

**What is a session**

- Each **refresh token** in the database represents one “session” (one device/browser that has logged in and can obtain new access tokens). When the user logs in or refreshes, a new refresh token is stored; old ones for that user are revoked on login.
- **Active session** = refresh token that is not revoked and not expired.

**Where to see and revoke sessions**

- **Security Activity page** (`/admin/security/activity`): “Active Sessions” section. Filter by user and date range; “Active only” checkbox. Table shows User, Created, Expires, IP, Device (UserAgent), Status, and a “Revoke” button per row.
- **User detail (Admin User Management):** When viewing a user, “Active Sessions” lists that user’s sessions with “Revoke” per session and “Revoke all sessions”.

**When to revoke sessions**

- User reports a lost or stolen device: revoke that session (match by IP/device if visible) or revoke all for that user.
- After investigating a security alert (e.g. multiple IP login): revoke suspicious or all sessions and ask the user to change password.
- Offboarding: revoke all sessions so the user can no longer refresh tokens.

**Revoking your own session**

- If an admin revokes a session that belongs to themselves, the UI shows a confirmation (“This will log you out…”). The revoked refresh token will no longer work; the current access token remains valid until it expires (short-lived), then the user must log in again.

---

## TODO / future enhancements

- **v1.3:** Phase A done (admin reset revokes refresh tokens). Phase B done (IPasswordHasher + CompatibilityPasswordHasher; legacy verify, modern BCrypt; rehash on login). Phase C done (per-user lockout). Phase D done (email-based password reset: forgot-password, reset-with-token, PasswordResetTokens table, optional email via Auth:PasswordReset). See [Admin User Management v1.3 Planning](ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md), [Phase C Deliverable](ADMIN_USER_MANAGEMENT_V1.3_PHASE_C_DELIVERABLE.md), [Phase D Deliverable](ADMIN_USER_MANAGEMENT_V1.3_PHASE_D_DELIVERABLE.md).
- **Password hasher modernization:** Consider dedicated hasher (e.g. Identity-compatible) with compatibility-safe abstraction; keep current behaviour until migration path is clear.
- **RBAC v2 (2026-03-09):** Permission catalog (module.action), role–permission matrix API, Admin UI at `/admin/security/roles`, permission-based enforcement on selected admin/security endpoints. SuperAdmin bypass; Admin role seeded with all admin.* permissions. See [RBAC v2 Permission Matrix Deliverable](RBAC_V2_PERMISSION_MATRIX_DELIVERABLE.md) and [Phase 1 Audit](RBAC_V2_PHASE1_AUDIT.md).
- **User activity history:** Expose audit log entries for a user (who changed what, when) in user detail or a dedicated tab (auth events are now in Security Activity and user detail “last 10”).

# CephasOps v1.4 Phase 1 – Authentication Event Visibility (Deliverable)

**Date:** 2026-03-08  
**Status:** Complete

---

## 1. Audit findings

- **IAuditLogService:** Exists with `LogAuditAsync(actorUserId?, entityType, entityId, action, ipAddress?, metadataJson?, ct)` and `GetAuditLogsAsync(...)`. Audit records stored in `AuditLog` table (Id, Timestamp, UserId, EntityType, EntityId, Action, IpAddress, MetadataJson, etc.).
- **Current usage:** AdminUserService logs User create/update/activate/deactivate/roles/reset-password; WorkflowEngineService logs workflow actions. Auth flows did **not** previously write to the audit log.
- **Gaps for auth:** No auth-specific event types; no IP/UserAgent in login or password flows; no admin visibility of login failures, lockouts, or password resets.
- **Decision:** Reuse existing audit table. Use `EntityType = "Auth"` and `Action` = event type string. Store UserAgent and any extra context in `MetadataJson`. No new table or migration.

---

## 2. Event model chosen

- **Storage:** Same `AuditLog` entity. `EntityType = "Auth"`, `EntityId` = user id (or empty if unknown). `Action` = one of the constants in `AuthEventTypes`.
- **AuthEventTypes (Application/Auth/AuthEventTypes.cs):**  
  `LoginSuccess`, `LoginFailed`, `AccountLocked`, `PasswordChanged`, `PasswordResetRequested`, `PasswordResetCompleted`, `AdminPasswordReset`, `TokenRefresh`.
- **Payload:** UserId (when known), EventType (= Action), TimestampUtc (= row), IP from `IHttpContextAccessor`, UserAgent in MetadataJson. No passwords, reset tokens, or hashes ever logged.

---

## 3. Files changed

| Area | File | Change |
|------|------|--------|
| Application | `Auth/AuthEventTypes.cs` | New: event type constants |
| Application | `Auth/Services/AuthService.cs` | Optional IAuditLogService + IHttpContextAccessor; LogAuthEventAsync; emit events in login, refresh, change-password, forgot-password, reset-with-token |
| Application | `Admin/Services/AdminUserService.cs` | Log AdminPasswordReset auth event after reset |
| Application | `Audit/DTOs/AuditLogDto.cs` | New: SecurityActivityEntryDto |
| Application | `Audit/Services/IAuditLogService.cs` | New: GetSecurityActivityAsync |
| Infrastructure | `Audit/Services/AuditLogService.cs` | Implement GetSecurityActivityAsync (EntityType == "Auth", join User for email, UserAgent from MetadataJson) |
| Api | `Controllers/LogsController.cs` | New: GET security-activity with filters, [Authorize(Roles = "SuperAdmin,Admin")] |
| Api | `Program.cs` | (if needed) ensure IHttpContextAccessor and AuditLogService registered for AuthService |
| Frontend | `api/logs.ts` | New: getSecurityActivity, types for SecurityActivityEntry and params |
| Frontend | `pages/admin/SecurityActivityPage.tsx` | New: page with filters (user, event type, date range), paginated table |
| Frontend | `pages/admin/UserManagementPage.tsx` | “Security Activity (last 10)” in view user modal |
| Frontend | `components/layout/Sidebar.tsx` | New nav item: Security Activity → /admin/security/activity |
| Frontend | `App.tsx` | Route /admin/security/activity → SecurityActivityPage |
| Tests | `Auth/AuthServiceTests.cs` | Tests: LoginSuccess, LoginFailed, AccountLocked, ForgotPassword→PasswordResetRequested, ResetWithToken→PasswordResetCompleted |
| Tests | `Admin/AdminUserServiceTests.cs` | Test: ResetPasswordAsync logs AdminPasswordReset |
| Docs | `ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md` | New § v1.4 Phase 1 and § Authentication Security Monitoring |
| Docs | `dev/onboarding.md` | v1.4 Security Activity and link to audit doc |

---

## 4. Admin UI added

- **Security Activity page:** Route `/admin/security/activity`. Sidebar: Admin → Security Activity (ShieldCheck icon). Paginated table; filters: user (dropdown), event type, date from, date to. Columns: timestamp (UTC), user email, event, IP, user agent.
- **User detail (Admin User Management):** In the view-user modal, “Security Activity (last 10)” section showing latest auth events for that user (timestamp + event label).

---

## 5. Tests summary

- **AuthServiceTests:**  
  - `LoginAsync_Success_LogsLoginSuccessAuditEvent`  
  - `LoginAsync_InvalidPassword_LogsLoginFailedAuditEvent`  
  - `LoginAsync_WhenLocked_LogsAccountLockedAuditEvent`  
  - `ForgotPasswordAsync_ExistingUser_LogsPasswordResetRequestedAuditEvent`  
  - `ResetPasswordWithTokenAsync_ValidToken_LogsPasswordResetCompletedAuditEvent`  
  All use mocked `IAuditLogService` and verify `LogAuditAsync` with `EntityType = "Auth"` and the corresponding `AuthEventTypes.*` action. No email delivery tested.
- **AdminUserServiceTests:**  
  - `ResetPasswordAsync_LogsAdminPasswordResetAuthEvent`  
  Verifies audit call for AdminPasswordReset after reset.
- **Regression:** Full application test run: 521 passed, 5 skipped (pre-existing). Auth behaviour unchanged.

---

## 6. Docs updated

- **ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md:** New section “v1.4 Phase 1 – Authentication Security Monitoring” (approach, event types, where events are emitted, Security Activity page and user-detail section). New subsection “Authentication Security Monitoring” with event-type table, Admin Security Activity page description, and “Investigating incidents” (lockout, password reset abuse, admin reset audit, failed logins).
- **dev/onboarding.md:** Admin user management bullet updated to include v1.4 Phase 1 (auth events in audit log, Security Activity page, user detail last 10 events) and link to the audit doc.

---

## 7. Regression review (Phase 8)

- **Login / refresh / password reset / lockout / admin reset:** No behaviour changes; only additive audit logging. Existing tests (including new auth-audit tests) pass.
- **Snapshot scheduler jobs / dashboard:** No code paths modified; no regressions expected. Full test suite (521 passed) confirms no breakage.

---

## Success criteria (met)

- Auth events are logged (login success/failure, lockout, password change, reset requested/completed, admin reset, token refresh).
- Admins can view user security activity via `/admin/security/activity` and in the user detail modal (last 10).
- Security issues can be investigated using filters and the incident guidance in the docs.
- Auth flows (JWT, refresh, lockout, reset) remain unchanged; no auth redesign, no new Identity migration, minimal schema (reuse of AuditLog).

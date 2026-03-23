# CephasOps v1.4 Phase 3 – Session Management (Deliverable)

**Date:** 2026-03-08  
**Status:** Complete

---

## 1. Audit findings

- **RefreshToken entity:** Id, UserId, TokenHash, ExpiresAt, IsRevoked, RevokedAt, **CreatedFromIp**, CreatedAt, User (navigation). **UserAgent** was not stored.
- **CreatedFromIp** was present but not populated by AuthService when creating tokens. **UserAgent** was added (nullable) for session visibility.
- **Revocation:** AdminUserService (admin reset) and AuthService (change-password, login replaces old tokens, refresh rotates token) already set IsRevoked + RevokedAt. Session revoke reuses the same pattern.

---

## 2. Session model

- **UserSessionDto:** SessionId (= RefreshToken.Id), UserId, UserEmail, CreatedAtUtc, ExpiresAtUtc, IpAddress (= CreatedFromIp), UserAgent, IsRevoked. Sessions map 1:1 to refresh tokens.
- **Active session** = refresh token with !IsRevoked && ExpiresAt > UtcNow.

---

## 3. API endpoints

| Method | Route | Description |
|--------|--------|-------------|
| GET | /api/admin/security/sessions | List sessions; query: userId, dateFrom, dateTo, activeOnly |
| GET | /api/admin/security/sessions/user/{userId} | Sessions for one user |
| POST | /api/admin/security/sessions/{sessionId}/revoke | Revoke one session |
| POST | /api/admin/security/sessions/revoke-all/{userId} | Revoke all sessions for user |

All require Admin or SuperAdmin. Implemented in `AdminSecuritySessionsController`.

---

## 4. Service design

- **UserSessionService:** Depends on ApplicationDbContext. GetSessionsAsync (filters, activeOnly, join User for email), GetSessionsForUserAsync(userId), RevokeSessionAsync(sessionId), RevokeAllSessionsForUserAsync(userId). Revoke logic: set IsRevoked = true, RevokedAt = UtcNow, SaveChanges — same as existing revocation in AuthService and AdminUserService.

---

## 5. UI changes

- **Security Activity page:** New “Active Sessions” section (above Security Alerts). Table: User, Created, Expires, IP, Device (UserAgent), Status, Actions (Revoke). Filters: user/date (shared with page), “Active only” checkbox; “Refresh sessions” button. Confirm dialog when revoking; if session belongs to current user, message: “This will log you out on that device. Continue?”
- **User detail (Admin User Management):** “Active Sessions” section with list (created, IP, device snippet, status) and “Revoke” per active session, plus “Revoke all sessions”. Confirm dialogs for revoke one and revoke all; own-session warning when revoking self.

---

## 6. Safety

- Revoking a session invalidates the refresh token (IsRevoked = true); the access token is not modified and expires naturally. RefreshTokenAsync already rejects revoked tokens.
- Admin revoking their own session: UI shows confirmation (“This will log you out…”). No backend guard; permission is Admin/SuperAdmin.

---

## 7. Tests summary

- **UserSessionServiceTests:** GetSessionsAsync_ReturnsSessionsWithUserEmail; GetSessionsAsync_ActiveOnly_ExcludesRevokedTokens; RevokeSessionAsync_MarksTokenRevoked; RevokeSessionAsync_NotFound_ReturnsFalse; RevokeAllSessionsForUserAsync_RevokesAllForUser; GetSessionsForUserAsync_ReturnsOnlyThatUserSessions. All use in-memory DB and pass.
- **Regression:** Login, refresh, password reset, lockout, admin reset, and suspicious detection are unchanged; AuthService and AdminUserService revocation behaviour unchanged. New migration adds only nullable UserAgent column.

---

## 8. Docs updated

- **ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md:** “v1.4 Phase 3 – Session Management” and “Session Management” (what is a session, where to see/revoke, when to revoke, revoking own session).
- **dev/onboarding.md:** v1.4 Phase 3 Session Management in admin user management bullet.

---

## 9. Regression

- **Login / refresh / token issuance:** Unchanged; AuthService only sets CreatedFromIp and UserAgent when creating new refresh tokens.
- **Password reset / lockout / admin reset:** Unchanged.
- **Suspicious activity detection:** Unchanged.
- **Schema:** One new nullable column, RefreshToken.UserAgent; migration: AddUserAgentToRefreshToken.

---

## Files changed (summary)

- **Domain:** RefreshToken.cs (UserAgent property).
- **Infrastructure:** Migration AddUserAgentToRefreshToken.
- **Application:** UserSessionDto.cs, IUserSessionService.cs, UserSessionService.cs; AuthService (set CreatedFromIp/UserAgent on new tokens).
- **Api:** AdminSecuritySessionsController.cs, Program.cs (register IUserSessionService).
- **Frontend:** api/adminSessions.ts; SecurityActivityPage (Active Sessions + confirm); UserManagementPage (Active Sessions + revoke/revoke all + confirms).
- **Tests:** UserSessionServiceTests.cs.
- **Docs:** ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md, dev/onboarding.md.

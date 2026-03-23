# Admin User Management v1.3 – Implementation Proposal (Planning Only)

**Date:** 2026-03-08  
**Status:** Phase A, Phase B, Phase C, and Phase D implemented (2026-03-08).  
**Scope:** Password hasher modernization, refresh-token revocation on admin reset (Phase A), account lockout (Phase C), email-based password reset (Phase D).

---

## 1. Current state

### 1.1 Auth and password flow

- **Login:** `POST /api/auth/login` → AuthService validates email + password via `DatabaseSeeder.VerifyPassword`, checks IsActive and MustChangePassword; on success issues JWT + refresh token, updates LastLoginAtUtc, revokes previous refresh tokens for the user.
- **Refresh:** `POST /api/auth/refresh` → Validates refresh token (hashed lookup), revokes old token, issues new JWT + refresh token. If user has MustChangePassword, throws RequiresPasswordChangeException → 403.
- **Me:** `GET /api/auth/me` [Authorize] → Returns current user DTO (includes MustChangePassword).
- **Change password (authenticated):** `POST /api/auth/change-password` → Validates current password, updates hash, sets MustChangePassword = false, **revokes all refresh tokens for the user**, saves.
- **Change password (forced, no auth):** `POST /api/auth/change-password-required` → Validates email + current password + MustChangePassword; updates hash and clears flag; **does not revoke refresh tokens** (user has no valid session in this flow).
- **Admin reset password:** AdminUsersController → AdminUserService.ResetPasswordAsync: sets new hash, ForceMustChangePassword; **revokes all refresh tokens for the target user** (v1.3 Phase A).

### 1.2 Password hasher

- **Location:** `CephasOps.Infrastructure.Persistence.DatabaseSeeder` static methods `HashPassword(string)` and `VerifyPassword(string, string)`.
- **Algorithm:** SHA256 with a **fixed salt** (`"CephasOps_Salt_2024"`). Hash = Base64(SHA256(password + salt)).
- **Usage:** AuthService (login, change-password, change-password-required), AdminUserService (create user, reset password), DatabaseSeeder (seed admin), DiagnosticsController (diagnostic check).
- **Comments in code:** Explicitly note “For production, consider using BCrypt or ASP.NET Core Identity PasswordHasher” and “use a random salt per user”.
- **Storage:** Single `User.PasswordHash` string column; no format/version prefix.

### 1.3 Refresh tokens

- **Storage:** `RefreshTokens` table (UserId, TokenHash, ExpiresAt, IsRevoked, RevokedAt, etc.). Token stored as SHA256 hash.
- **Revocation today:**
  - **On login:** All existing non-revoked, non-expired refresh tokens for the user are revoked before adding the new one.
  - **On refresh:** The used refresh token is revoked; a new one is issued.
  - **On authenticated change-password:** All refresh tokens for the user are revoked.
  - **On admin reset:** **Revoked** (v1.3 Phase A).
  - **On change-password-required:** N/A (user has no tokens in that flow).

### 1.4 Lockout / brute-force (Phase C implemented)

- **Current:** Per-user lockout. User entity has `FailedLoginAttempts` (int, default 0) and `LockoutEndUtc` (DateTime?, UTC). Config: `Auth:Lockout` with `MaxFailedAttempts` (default 5) and `LockoutMinutes` (default 15). On failed password: increment attempts; when ≥ threshold set LockoutEndUtc = UtcNow + duration. On login, if LockoutEndUtc > UtcNow return 423 with account-locked payload. On success: reset FailedLoginAttempts to 0 and clear LockoutEndUtc. Unknown user still returns same 401 message.

### 1.5 Email-based password reset

- **Current:** None. No “forgot password” or token-based reset. Only admin reset (admin sets new password) and change-password-required (user must know current password).
- **Existing app capability:** Email sending exists (e.g. IEmailSendingService, EmailTemplateService) for other features; could be reused for reset links later.

---

## 2. Risks

| Area | Risk | Impact |
|------|------|--------|
| **Hasher** | SHA256 + fixed salt is weak and not per-user; if DB leaks, hashes are easier to attack. | High (credential exposure). |
| **Hasher** | Changing algorithm without compatibility layer invalidates all existing passwords. | High (everyone locked out). |
| **Refresh on admin reset** | ~~After admin resets password, old refresh tokens still work until expiry.~~ **Mitigated in v1.3 Phase A:** admin reset now revokes all refresh tokens for the target user. | — |
| **No lockout** | Brute-force on login (e.g. password spraying) has no rate limit or lockout. | Medium (account takeover / DoS). |
| **No email reset** | Users who forget password depend entirely on admin; no self-service. | Low–medium (ops burden, UX). |

---

## 3. Recommended phased rollout

### Phase A – Refresh token revocation on admin reset (small, low risk) ✅ Implemented

- **Goal:** When an admin resets a user’s password, revoke all refresh tokens for that user so existing sessions are invalidated.
- **Change:** In AdminUserService.ResetPasswordAsync, after updating password and MustChangePassword, load all refresh tokens for the user, set IsRevoked = true and RevokedAt = UtcNow, then SaveChanges (same pattern as AuthService.ChangePasswordAsync).
- **Scope:** One service method; no new columns, no hasher change, no new endpoints. Audit message extended to include “refresh tokens revoked”.
- **Status:** Done. Tests: admin reset revokes target user’s tokens; unrelated users’ tokens unchanged; ForceMustChangePassword and password hash update unchanged.

### Phase B – Password hasher modernization (backward-compatible) ✅ Implemented

- **Goal:** Move to a strong, per-user hasher while keeping existing logins working.
- **Implemented:**
  1. **IPasswordHasher** (Application/Common/Interfaces): HashPassword, VerifyPassword, NeedsRehash.
  2. **CompatibilityPasswordHasher** (Application/Common/Services): Verifies legacy (SHA256 + fixed salt, same as DatabaseSeeder); writes modern (BCrypt via BCrypt.Net-Next). Legacy detected by hash not starting with `$2`.
  3. Runtime auth (AuthService, AdminUserService, DiagnosticsController) use IPasswordHasher via DI; no direct DatabaseSeeder for runtime password verify/hash.
  4. **Rehash on login:** On successful login, if NeedsRehash(user.PasswordHash) then user.PasswordHash is updated to modern format and saved in the same transaction.
- **Seed/bootstrap:** DatabaseSeeder unchanged; still uses its own static HashPassword for seeding. Seeded users get legacy hashes and can log in; they are upgraded to modern hash on first successful login.
- **Status:** Done. Tests: CompatibilityPasswordHasherTests (legacy verify, modern format, NeedsRehash); AuthServiceTests (login rehash to modern); AdminUserServiceTests (reset writes modern).

### Phase C – Account lockout / brute-force protection ✅ Implemented

- **Goal:** Limit repeated failed logins per account to reduce brute-force and credential stuffing.
- **Implemented:**
  1. **User entity:** `FailedLoginAttempts` (int, default 0), `LockoutEndUtc` (DateTime?, UTC). Migration: `20260308200000_AddLockoutFieldsToUser`.
  2. **Config:** `LockoutOptions` bound from `Auth:Lockout`: `MaxFailedAttempts` (default 5), `LockoutMinutes` (default 15).
  3. **AuthService.LoginAsync:** Before password check, if user is locked (LockoutEndUtc > UtcNow) throw `AccountLockedException` → API returns **423** with `accountLocked: true` and optional `lockoutEndUtc`. On invalid password: increment FailedLoginAttempts; if ≥ threshold set LockoutEndUtc = UtcNow + LockoutMinutes; save; throw UnauthorizedAccessException. On success: set FailedLoginAttempts = 0, LockoutEndUtc = null; then existing flow (rehash, LastLoginAtUtc, tokens). Unknown user: same 401 “Invalid email or password”.
  4. **Frontend:** Login catches 423 / accountLocked and shows: “Your account is temporarily locked due to repeated failed sign-in attempts. Please try again later.”
- **Status:** Done. JWT, refresh, must-change-password, and password hasher behaviour unchanged. Tests: failed login increments; threshold sets lockout; locked user cannot login; success resets counters; unknown user safe response.

### Phase D – Email-based password reset ✅ Implemented

- **Goal:** Allow user to request a time-limited reset link via email; set new password with token (no current password).
- **Implemented:**
  1. **Table:** `PasswordResetTokens` (Id, UserId, TokenHash, ExpiresAtUtc, UsedAtUtc, CreatedAtUtc). Migration: `20260308190000_AddPasswordResetTokens`. Token stored as SHA256 hash; raw token only in email link.
  2. **Config:** `Auth:PasswordReset`: `EmailAccountId` (Guid?, optional – if null no email sent), `TokenExpiryMinutes` (default 60), `FrontendResetUrlBase` (e.g. http://localhost:5173).
  3. **Forgot-password:** POST /api/auth/forgot-password with email. Always returns 200 with generic message. If user exists and active: create token, invalidate previous unused tokens for that user, optionally send email via IEmailSendingService when EmailAccountId configured. No enumeration.
  4. **Reset-with-token:** POST /api/auth/reset-password-with-token (Token, NewPassword, ConfirmPassword). Validates token (exists, not expired, not used); updates password (IPasswordHasher), sets MustChangePassword = false, FailedLoginAttempts = 0, LockoutEndUtc = null; marks token used; revokes all refresh tokens for user.
  5. **Frontend:** Forgot password page, Reset password page (token from URL ?token=), “Forgot your password?” link on login. Generic success message; invalid/expired token message on reset page.
- **Status:** Done. JWT, lockout, must-change-password, hasher unchanged. Tests: forgot non-existing/existing, reset valid/expired/used/invalid, second request invalidates first, reset revokes refresh tokens and clears lockout.

---

## 4. Backward-compatibility strategy

- **Phase A:** No breaking change. Existing clients and tokens unchanged; only after an admin reset do existing refresh tokens for that user become invalid (intended).
- **Phase B:** Critical. All existing PasswordHash values remain valid. New hashes written in new format. Verify must support both formats indefinitely (or until a one-time migration rehashes every user on next login and legacy path is removed). DatabaseSeeder and seed migrations that set PasswordHash must continue to work: either seed with legacy hasher or run a one-time seed update to modern hashes after deployment.
- **Phase C:** New columns nullable or default 0 / null; existing users get FailedLoginAttempts = 0, LockoutEndUtc = null. No change to JWT or refresh format. Login response stays the same; only new 403/423 for lockout.
- **Phase D:** Additive only (new endpoints, new table). No change to existing login/change-password/admin-reset contracts.

---

## 5. Migration and testing implications

### Migrations

- **Phase A:** None.
- **Phase B:** None if hash remains a single string (only format of value changes). If you ever add a “hash algorithm” column, that would be a separate migration.
- **Phase C:** One migration adding FailedLoginAttempts (int, default 0) and LockoutEndUtc (DateTime?, nullable) to Users.
- **Phase D:** One migration adding PasswordResetTokens (or equivalent) table.

### Testing

- **Phase A:** Unit test: AdminUserService.ResetPasswordAsync → after reset, all refresh tokens for user are revoked. Integration: admin reset then refresh with old token → 401.
- **Phase B:** Unit tests: legacy hash still verifies; new hash verifies; new hash written has new format; rehash-on-login updates hash. Integration: login with seeded (legacy) user succeeds; login with modern-hash user succeeds; after login, legacy user’s hash is upgraded in DB. Seed data: ensure DatabaseSeeder (or seed migration) still produces valid hashes; if you switch seed to modern hasher, existing seed scripts that compare hashes may need updating.
- **Phase C:** Unit tests: lockout after N failures; lockout prevents login until window passes; successful login clears attempts and lockout. Integration: login returns 403 when locked; after window, login works again.
- **Phase D:** Tests for token creation, expiry, single use, and revocation on use; email not sent when email not found (to avoid enumeration); rate limiting if implemented.

### JWT and auth design

- No change to JWT structure or refresh token format. All phases are compatible with current JWT auth; no move to ASP.NET Identity required. Optional: in Phase C, include “locked” in a custom claim only if needed for UI (e.g. show “Account locked until X”); otherwise keep lockout as a login-time check only.

---

## 6. Summary

| Phase | What | Backward compatibility | Migration | Status |
|-------|------|------------------------|-----------|--------|
| **A** | Revoke refresh tokens on admin reset | Yes | None | ✅ Done |
| **B** | Password hasher abstraction + modern algorithm + rehash on login | Yes (dual verify, new hash format) | None (hash string only) | ✅ Done |
| **C** | Per-user lockout (failed attempts + lockout window) | Yes | Add columns to User | ✅ Done |
| **D** | Email-based password reset (forgot + reset-with-token) | Additive | New table | ✅ Done |

This keeps the current JWT design, minimizes risk via small phases, and preserves existing logins and seeds through a compatibility-aware hasher rollout.

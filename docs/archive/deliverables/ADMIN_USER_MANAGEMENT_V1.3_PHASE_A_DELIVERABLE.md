# Admin User Management v1.3 Phase A – Deliverable

**Date:** 2026-03-08

---

## 1. Audit findings

- **AdminUserService.ResetPasswordAsync:** Updated user PasswordHash and MustChangePassword, called SaveChangesAsync, logged and audited. Did not touch RefreshTokens.
- **Self-service revocation (AuthService.ChangePasswordAsync):** After updating password and MustChangePassword, loaded all refresh tokens for the user with `_context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync()`, set each `IsRevoked = true` and `RevokedAt = DateTime.UtcNow`, then SaveChangesAsync in the same transaction.
- **RefreshToken entity:** UserId, TokenHash, ExpiresAt, IsRevoked, RevokedAt, etc.; accessed via `_context.RefreshTokens`. AdminUserService uses the same ApplicationDbContext.
- **Tests:** No existing test covered refresh token revocation for admin reset; AuthServiceTests cover change-password flow but not token revocation explicitly in AdminUserServiceTests.
- **Reusable logic:** Same pattern: query RefreshTokens by UserId and !IsRevoked, mark revoked, single SaveChanges.
- **Files to change:** AdminUserService.cs (ResetPasswordAsync); AdminUserServiceTests.cs (new tests); docs (v1.3 planning, audit/plan).

---

## 2. Exact fix applied

In **AdminUserService.ResetPasswordAsync**, after setting `user.PasswordHash` and `user.MustChangePassword` and before `SaveChangesAsync`:

1. Load all non-revoked refresh tokens for the target user:  
   `var oldTokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync(cancellationToken);`
2. For each token set `IsRevoked = true` and `RevokedAt = DateTime.UtcNow`.
3. Call `await _context.SaveChangesAsync(cancellationToken)` once (user + tokens).
4. Log message extended to include `refresh tokens revoked={Count}` (oldTokens.Count).
5. Audit metadata extended to include `refresh tokens revoked` (no passwords/hashes logged).

No new types, no JWT changes, no API changes. Same revocation pattern as AuthService.ChangePasswordAsync.

---

## 3. Files changed

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Admin/Services/AdminUserService.cs` | In ResetPasswordAsync: load and revoke all refresh tokens for userId; extend log and audit text. |
| `backend/tests/CephasOps.Application.Tests/Admin/AdminUserServiceTests.cs` | Added 3 tests: ResetPasswordAsync_RevokesExistingRefreshTokensForTargetUser, ResetPasswordAsync_DoesNotRevokeUnrelatedUsersRefreshTokens, ResetPasswordAsync_UpdatesPasswordHash. |
| `docs/ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md` | Status set to Phase A implemented; current state and Phase A section updated; risks table updated; summary table status. |
| `docs/ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md` | v1.3 TODO updated: Phase A done, B–D planned. |
| `docs/ADMIN_USER_MANAGEMENT_V1.3_PHASE_A_DELIVERABLE.md` | This file. |

---

## 4. Tests added/updated

- **ResetPasswordAsync_RevokesExistingRefreshTokensForTargetUser:** Two refresh tokens for admin user; after ResetPasswordAsync both are revoked (IsRevoked true, RevokedAt set).
- **ResetPasswordAsync_DoesNotRevokeUnrelatedUsersRefreshTokens:** Second user with a refresh token; admin resets first user’s password; second user’s token remains not revoked.
- **ResetPasswordAsync_UpdatesPasswordHash:** After reset, user’s PasswordHash is updated and verifies with new password (DatabaseSeeder.VerifyPassword).

Existing tests **ResetPasswordAsync_WithForceMustChangePassword_SetsFlag** and **ResetPasswordAsync_WithForceMustChangePasswordFalse_ClearsFlag** unchanged; all 15 AdminUserServiceTests pass.

---

## 5. Regression notes

- **Self-service password change:** Not modified; AuthService.ChangePasswordAsync still revokes tokens. No regression.
- **Admin reset:** Same contract (request DTO, controller); only behavior change is revocation. ForceMustChangePassword and password update unchanged.
- **Forced password change flow:** Unchanged (change-password-required; no tokens in that flow).
- **Login/refresh after reset:** User must log in again with new password; refresh with old token returns 401 (token revoked). Expected.

---

## 6. Docs updated

- **ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md:** Phase A marked implemented; current state and “Revocation today” updated; Phase A section and summary table updated; risk “Refresh on admin reset” marked mitigated.
- **ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md:** v1.3 bullet updated to “Phase A done; Phases B–D planned”.

---

## 7. Remaining v1.3 phases still pending

- **Phase B:** Password hasher modernization (IPasswordHasher, legacy + modern, rehash on login).
- **Phase C:** Account lockout / brute-force protection (e.g. FailedLoginAttempts, LockoutEndUtc).
- **Phase D:** Email-based password reset (optional, later).

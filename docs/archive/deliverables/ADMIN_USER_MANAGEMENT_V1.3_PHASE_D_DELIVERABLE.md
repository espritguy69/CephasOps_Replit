# Admin User Management v1.3 Phase D – Deliverable (Email-Based Password Reset)

**Date:** 2026-03-08

---

## 1. Audit findings (pre-implementation)

- **Auth endpoints:** Login, refresh, me, change-password, change-password-required. No forgot-password or reset-with-token.
- **Email:** IEmailSendingService (SendEmailAsync) exists; requires emailAccountId, to, subject, body. Used by background jobs. No dedicated “system” account; config-driven EmailAccountId chosen for password reset.
- **Token pattern:** RefreshToken stores TokenHash (SHA256), ExpiresAt, one-time use via revocation. Same pattern reused for password reset tokens.
- **User model:** Has lockout fields; reset flow clears them on success.
- **Frontend:** Login page had “Contact your administrator”; ChangePasswordPage for must-change flow. No forgot/reset pages.
- **Decisions:** New table PasswordResetTokens; token stored hashed; generic response for forgot-password (no enumeration); allow forgot-password even when account is locked (user can reset to regain access); when creating a new reset token, invalidate previous unused tokens for that user.

---

## 2. Reset-token design chosen

- **Entity:** PasswordResetToken (Id, UserId, TokenHash, ExpiresAtUtc, UsedAtUtc, CreatedAtUtc). Raw token never stored; same SHA256 hash as refresh tokens.
- **Expiry:** Configurable TokenExpiryMinutes (default 60). UTC throughout.
- **One-time use:** UsedAtUtc set when token is consumed; validation requires UsedAtUtc == null.
- **Invalidation:** When issuing a new reset token for a user, all existing unused tokens for that user are marked used (UsedAtUtc = UtcNow) so only the latest link works.
- **Forgot-password:** Always return 200 with generic message. If email exists and user is active: create token, invalidate previous, optionally send email when Auth:PasswordReset:EmailAccountId is set and IEmailSendingService is available.
- **Reset-with-token:** Validate token; update password (IPasswordHasher), clear MustChangePassword, FailedLoginAttempts, LockoutEndUtc; revoke all refresh tokens; mark token used.

---

## 3. Files changed

| File | Change |
|------|--------|
| **Domain/Users/Entities/PasswordResetToken.cs** | New entity. |
| **Infrastructure/Persistence/ApplicationDbContext.cs** | DbSet PasswordResetTokens. |
| **Infrastructure/Persistence/Migrations/20260308190000_AddPasswordResetTokens.cs** | New migration. |
| **Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs** | PasswordResetToken entity. |
| **Application/Auth/PasswordResetOptions.cs** | New: Auth:PasswordReset, EmailAccountId, TokenExpiryMinutes, FrontendResetUrlBase. |
| **Application/Auth/DTOs/LoginRequestDto.cs** | ForgotPasswordRequestDto, ResetPasswordWithTokenRequestDto. |
| **Application/Auth/Services/IAuthService.cs** | ForgotPasswordAsync, ResetPasswordWithTokenAsync. |
| **Application/Auth/Services/AuthService.cs** | Forgot/reset implementation; optional IEmailSendingService, PasswordResetOptions. |
| **Api/Program.cs** | Configure PasswordResetOptions. |
| **Api/Controllers/AuthController.cs** | POST forgot-password, POST reset-password-with-token. |
| **Frontend api/auth.ts** | forgotPassword, resetPasswordWithToken. |
| **Frontend pages/auth/ForgotPasswordPage.tsx** | New page. |
| **Frontend pages/auth/ResetPasswordPage.tsx** | New page (token from URL). |
| **Frontend App.tsx** | Routes /forgot-password, /reset-password. |
| **Frontend pages/auth/LoginPage.tsx** | “Forgot your password?” link to /forgot-password. |
| **Tests/Auth/AuthServiceTests.cs** | Phase D tests (forgot/reset, valid/expired/used/invalid, invalidation, revoke/clear lockout). |
| **docs/ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md** | Phase D implemented. |
| **docs/ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md** | Phase D done. |
| **docs/dev/onboarding.md** | Auth:PasswordReset config note. |
| **docs/ADMIN_USER_MANAGEMENT_V1.3_PHASE_D_DELIVERABLE.md** | This file. |

---

## 4. Migration

- **Name:** 20260308190000_AddPasswordResetTokens
- **Contents:** Create table PasswordResetTokens (Id, UserId, TokenHash, ExpiresAtUtc, UsedAtUtc, CreatedAtUtc), FK to Users (Cascade delete), indexes on UserId and TokenHash.

---

## 5. Backend flow behaviour

- **POST /api/auth/forgot-password** (body: Email): Always 200. Message: “If an account exists for that email, you will receive a password reset link.” If user exists and IsActive: create token (hash + expiry), invalidate previous unused tokens for user; if Auth:PasswordReset:EmailAccountId set and IEmailSendingService available, send email with reset link (FrontendResetUrlBase + /reset-password?token=…). Raw token never logged.
- **POST /api/auth/reset-password-with-token** (body: Token, NewPassword, ConfirmPassword): 400 if validation fails (password length, mismatch). 401 if token invalid/expired/used. 200 on success: password updated (IPasswordHasher), MustChangePassword = false, FailedLoginAttempts = 0, LockoutEndUtc = null, token marked used, all refresh tokens for user revoked.

---

## 6. Frontend flow behaviour

- **Login:** “Forgot your password?” links to /forgot-password.
- **Forgot password:** Email field, submit → generic success message regardless of outcome; link back to sign in.
- **Reset password:** Route /reset-password; token from query ?token=…. New password + confirm; submit → success message and link to sign in; on invalid/expired token show error and link to request new link / sign in.

---

## 7. Test summary

- ForgotPasswordAsync_NonExistingEmail_DoesNotThrow; ForgotPasswordAsync_ExistingUser_CreatesTokenRecord.
- ResetPasswordWithTokenAsync_ValidToken_UpdatesPasswordAndClearsLockout; ResetPasswordWithTokenAsync_ValidToken_RevokesRefreshTokens.
- ResetPasswordWithTokenAsync_ExpiredToken_Throws; ResetPasswordWithTokenAsync_UsedToken_Throws; ResetPasswordWithTokenAsync_InvalidToken_Throws.
- ForgotPasswordAsync_SecondRequest_InvalidatesPreviousToken.
- All existing AuthServiceTests (login, refresh, lockout, must-change-password, legacy rehash) still pass (19 total).

---

## 8. Docs updated

- ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md: Phase D section and summary table marked implemented.
- ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md: v1.3 Phase D done, link to Phase D deliverable.
- docs/dev/onboarding.md: Auth:PasswordReset config (EmailAccountId, TokenExpiryMinutes, FrontendResetUrlBase).

---

## 9. Security / edge cases

- **Enumeration:** Forgot-password always returns same success response; no distinction between existing/non-existing email.
- **Token:** One-time use (UsedAtUtc); expiry enforced; stored as hash; raw token only in email and request body, never logged.
- **Multiple requests:** New token invalidates previous unused tokens for that user (only latest link works).
- **Locked account:** Forgot-password is allowed when account is locked; successful reset clears lockout so user can sign in.
- **Refresh tokens:** Successfully resetting password revokes all refresh tokens for that user.
- **Config:** If EmailAccountId is not set, tokens are still created but no email is sent (log message); deployer configures an EmailAccount for reset emails.

---

## 10. Regression review

- Login, refresh, me, must-change-password, change-password, admin reset, lockout, legacy-hash rehash, Phase A token revocation, Phase B hasher: unchanged. New behaviour is additive (forgot-password, reset-with-token only).

---

## 11. Remaining auth hardening TODOs

- None for v1.3. Optional future: rate limit forgot-password by IP/email; CAPTCHA; MFA.

---

## Success criteria (met)

- Users can request a password reset by email (when EmailAccountId configured).
- Users can reset password with a one-time token; token expiry and one-time use enforced.
- Responses do not reveal whether an email exists.
- Successful reset revokes refresh tokens and clears lockout.
- JWT and existing auth flows unchanged.
- Docs show v1.3 Phase D complete.

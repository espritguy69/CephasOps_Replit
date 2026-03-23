# Admin User Management v1.3 Phase C – Deliverable (Account Lockout)

**Date:** 2026-03-08

---

## 1. Audit findings (pre-implementation)

- **AuthService.LoginAsync:** Validates email, active, password; on success issues JWT + refresh, updates LastLoginAtUtc, optional rehash; on failure throws UnauthorizedAccessException. No lockout or failed-attempt tracking.
- **User entity:** Had no FailedLoginAttempts or LockoutEndUtc; safe to add with defaults.
- **AuthController:** Returns 401 for UnauthorizedAccessException, 403 for RequiresPasswordChangeException. No lockout-specific handling.
- **Config pattern:** CephasOps uses `Configure<T>(configuration.GetSection(...))` and `IOptions<T>`; added LockoutOptions following that pattern.
- **Frontend:** Login handles 401 and 403/requiresPasswordChange; needed to handle 423/accountLocked for lockout message.

---

## 2. Lockout design chosen

- **Per-user lockout only** (no IP rate limit in this phase).
- **User fields:** `FailedLoginAttempts` (int, default 0), `LockoutEndUtc` (DateTime?, UTC). All times UTC.
- **Config:** `Auth:Lockout` → `MaxFailedAttempts` (default 5), `LockoutMinutes` (default 15).
- **Flow:**
  1. After resolving user (exists, active, has password hash), if `LockoutEndUtc.HasValue && LockoutEndUtc > UtcNow` → throw `AccountLockedException(LockoutEndUtc)` → API returns **423** with `accountLocked: true`, `lockoutEndUtc` (optional).
  2. On invalid password: increment `FailedLoginAttempts`; if `>= MaxFailedAttempts` set `LockoutEndUtc = UtcNow.AddMinutes(LockoutMinutes)`; save; throw UnauthorizedAccessException ("Invalid email or password").
  3. On successful login: set `FailedLoginAttempts = 0`, `LockoutEndUtc = null`; then existing logic (rehash if needed, LastLoginAtUtc, tokens, single SaveChangesAsync).
- **Unknown/invalid user:** Same 401 "Invalid email or password" (no extra info, no timing/enumeration change).
- **JWT, refresh, must-change-password, password hasher:** Unchanged. Lockout check runs before password verification; must-change-password and rehash run after success as before.

---

## 3. Files changed

| File | Change |
|------|--------|
| **Domain/Users/Entities/User.cs** | Added FailedLoginAttempts (int), LockoutEndUtc (DateTime?). |
| **Infrastructure/Persistence/Migrations/20260308200000_AddLockoutFieldsToUser.cs** | New migration: add columns with default 0 and null. |
| **Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs** | User entity: FailedLoginAttempts, LockoutEndUtc. |
| **Application/Auth/LockoutOptions.cs** | New: section Auth:Lockout, MaxFailedAttempts, LockoutMinutes, defaults. |
| **Application/Auth/AccountLockedException.cs** | New: exception with optional LockoutEndUtc for API payload. |
| **Application/Auth/Services/AuthService.cs** | Lockout check before password; increment/set lockout on failure; reset on success; IOptions<LockoutOptions>. |
| **Api/Program.cs** | Configure<LockoutOptions>(GetSection(LockoutOptions.SectionName)). |
| **Api/Controllers/AuthController.cs** | Catch AccountLockedException → 423 with accountLocked, lockoutEndUtc. |
| **Frontend api/auth.ts** | Login catch: 423 or data?.data?.accountLocked → throw Error with accountLocked. |
| **Frontend contexts/AuthContext.tsx** | Login result type and handling for accountLocked. |
| **Frontend pages/auth/LoginPage.tsx** | Show lockout message when result.accountLocked. |
| **Tests/Auth/AuthServiceTests.cs** | IOptions<LockoutOptions> in ctor; new tests: invalid password increments, repeated failures lock, locked blocks login, success resets, unknown user same message. |
| **docs/ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md** | Phase C implemented; 1.4 and Phase C section and table updated. |
| **docs/ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md** | v1.3 Phase C done. |
| **docs/dev/onboarding.md** | Phase C lockout and Auth:Lockout config note. |
| **docs/ADMIN_USER_MANAGEMENT_V1.3_PHASE_C_DELIVERABLE.md** | This file. |

---

## 4. Migration

- **Name:** `20260308200000_AddLockoutFieldsToUser`
- **Changes:** Add to `Users`: `FailedLoginAttempts` (int, default 0), `LockoutEndUtc` (timestamp with time zone, nullable). Existing users get 0 and null; no seed or auth flow changes required.

---

## 5. Backend / frontend behaviour

- **Login – valid:** Unchanged (tokens, LastLoginAtUtc, rehash if legacy). Counters reset before issuing tokens.
- **Login – invalid password:** 401 "Invalid email or password"; FailedLoginAttempts incremented; if threshold reached, LockoutEndUtc set and persisted.
- **Login – locked:** 423 Locked; body `{ data: { accountLocked: true, lockoutEndUtc: "<UTC ISO>" } }`. No tokens; no counter change.
- **Login – unknown email:** 401 "Invalid email or password"; no user record updated.
- **Frontend:** On 423 or accountLocked, show: "Your account is temporarily locked due to repeated failed sign-in attempts. Please try again later." No redesign of login page; must-change-password flow unchanged.

---

## 6. Test summary

- **AuthServiceTests (11 total):** LoginAsync_InvalidPassword_IncrementsFailedLoginAttempts; LoginAsync_RepeatedInvalidPasswords_EventuallyLocksAccount (MaxFailedAttempts=2); LoginAsync_WhenLocked_ThrowsAccountLockedEvenWithCorrectPassword; LoginAsync_Success_ResetsFailedLoginAttemptsAndLockoutEndUtc; LoginAsync_UnknownEmail_ThrowsUnauthorizedWithSameMessage. Existing tests (login success, refresh, me, must-change-password, legacy rehash) unchanged and passing. Admin flows are in AdminUserServiceTests and are unaffected.

---

## 7. Docs updated

- **ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md:** Status and 1.4 updated; Phase C section marked implemented with implementation details; summary table Phase C = Done.
- **ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md:** v1.3 TODO updated to Phase C done; link to Phase C deliverable.
- **docs/dev/onboarding.md:** v1.3 Phase C lockout and Auth:Lockout config mentioned in architecture map.

---

## 8. Remaining v1.3 phases pending

- **Phase D:** Email-based password reset (forgot-password + reset-with-token). Optional, later.

---

## 9. Regression review (Phase 9)

- **Login:** Lockout logic added; success and failure paths and error responses otherwise unchanged.
- **Refresh:** Not modified; still validates refresh token, revokes old, issues new; RequiresPasswordChangeException → 403 unchanged.
- **Me / current-user:** Not modified.
- **Must-change-password:** Still enforced after password verification; lockout check runs before password check; no change to 403/requiresPasswordChange behaviour.
- **Password reset (change-password, change-password-required):** Not modified.
- **Admin reset:** Not modified; still revokes target user’s refresh tokens (Phase A) and uses IPasswordHasher (Phase B).
- **Legacy hash rehash on login:** Still runs on successful login after lockout reset; unchanged.
- **v1.3 Phase A (token revocation on admin reset):** Unchanged.
- **v1.3 Phase B (hasher abstraction, rehash on login):** Unchanged.

No redesign or changes outside the login lockout scope.

---

## Success criteria (met)

- Repeated failed logins temporarily lock the account.
- Successful login resets FailedLoginAttempts and LockoutEndUtc.
- Lockout does not break JWT, must-change-password, or legacy-hash compatibility.
- Tests cover: increment on failure, lockout at threshold, locked user blocked, success reset, unknown user safe.
- Docs record Phase C as complete and describe new fields, config, and behaviour.

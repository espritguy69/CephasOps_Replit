# Admin User Management v1.3 Phase B – Deliverable (Password Hasher Modernization)

**Date:** 2026-03-08

---

## 1. Audit findings

- **Runtime usages of DatabaseSeeder.HashPassword / VerifyPassword:**
  - **AuthService:** Login (verify); ChangePasswordAsync (verify + hash); ChangePasswordRequiredAsync (verify + hash).
  - **AdminUserService:** CreateAsync (hash); ResetPasswordAsync (hash).
  - **DiagnosticsController:** CheckAdminUser (verify); FixAdminUser (hash for comparison/update).
- **Seed/bootstrap:** DatabaseSeeder.SeedAsync uses HashPassword internally for default admin and finance HOD user. No other seed migrations write PasswordHash directly except seed data scripts that may reference the same format.
- **Decision:** Runtime auth migrated to IPasswordHasher. Seed left unchanged so existing seeded users keep legacy hashes and can log in; they are rehashed to modern on first successful login.

---

## 2. Abstraction design chosen

- **Interface:** `IPasswordHasher` (Application/Common/Interfaces) with:
  - `HashPassword(string password)` – always uses modern algorithm (BCrypt).
  - `VerifyPassword(string password, string storedHash)` – supports legacy (SHA256 + fixed salt) and modern (BCrypt); legacy detected when hash does not start with `$2`.
  - `NeedsRehash(string storedHash)` – true when hash is legacy format.
- **Implementation:** `CompatibilityPasswordHasher` (Application/Common/Services):
  - Legacy: same algorithm as DatabaseSeeder (SHA256 + salt "CephasOps_Salt_2024"), inlined so runtime does not depend on DatabaseSeeder for verify.
  - Modern: BCrypt via package BCrypt.Net-Next (work factor 10).
- **DI:** Registered in Program.cs as scoped; injected into AuthService, AdminUserService, DiagnosticsController.

---

## 3. Exact files changed

| File | Change |
|------|--------|
| **Application/Common/Interfaces/IPasswordHasher.cs** | New interface. |
| **Application/Common/Services/CompatibilityPasswordHasher.cs** | New implementation (legacy verify inlined, modern BCrypt). |
| **Application/CephasOps.Application.csproj** | Added package BCrypt.Net-Next. |
| **Application/Auth/Services/AuthService.cs** | Injected IPasswordHasher; login/change-password/change-password-required use it; rehash on login when NeedsRehash. |
| **Application/Admin/Services/AdminUserService.cs** | Injected IPasswordHasher; CreateAsync and ResetPasswordAsync use it. |
| **Api/Controllers/DiagnosticsController.cs** | Injected IPasswordHasher; CheckAdminUser and FixAdminUser use it (FixAdminUser writes modern hash). |
| **Api/Program.cs** | Registered IPasswordHasher → CompatibilityPasswordHasher. |
| **Tests/Common/CompatibilityPasswordHasherTests.cs** | New: legacy verify, modern format, NeedsRehash. |
| **Tests/Auth/AuthServiceTests.cs** | Injected CompatibilityPasswordHasher; assertion uses hasher; new test LoginAsync_WithLegacyHash_RehashesToModernFormat. |
| **Tests/Admin/AdminUserServiceTests.cs** | Injected CompatibilityPasswordHasher; ResetPasswordAsync_UpdatesPasswordHash uses hasher.VerifyPassword; CreateAsync test with audit passes hasher. |
| **docs/ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md** | Phase B marked implemented; description and summary table updated. |
| **docs/ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md** | v1.3 note: Phase B done. |
| **docs/dev/onboarding.md** | Short v1.3 Phase B hasher note. |
| **docs/ADMIN_USER_MANAGEMENT_V1.3_PHASE_B_DELIVERABLE.md** | This file. |

---

## 4. How legacy compatibility works

- **Verification:** If `storedHash` does not start with `$2`, it is treated as legacy. Legacy verification uses the same formula as DatabaseSeeder: Base64(SHA256(password + "CephasOps_Salt_2024")). No dependency on DatabaseSeeder at runtime.
- **New writes:** HashPassword always returns BCrypt hash (starts with `$2`). All password changes (login rehash, change-password, change-password-required, admin reset, admin create) go through IPasswordHasher.HashPassword.
- **Seeded users:** DatabaseSeeder still writes legacy hashes. Those users can log in; CompatibilityPasswordHasher.VerifyPassword accepts legacy hashes. On first successful login, NeedsRehash is true so the password is rehashed to BCrypt and saved.

---

## 5. Login rehash implemented

Yes. In AuthService.LoginAsync, after successful password verification and before updating LastLoginAtUtc and issuing tokens:

```csharp
if (_passwordHasher.NeedsRehash(user.PasswordHash))
{
    user.PasswordHash = _passwordHasher.HashPassword(request.Password);
    _logger.LogInformation("Rehashed legacy password to modern format for user {Email}", user.Email);
}
```

The same SaveChangesAsync that persists tokens also persists the updated user (including PasswordHash).

---

## 6. Test summary

- **CompatibilityPasswordHasherTests (6):** VerifyPassword_LegacyHash_ReturnsTrue; VerifyPassword_LegacyHash_WrongPassword_ReturnsFalse; HashPassword_ProducesModernFormat; VerifyPassword_ModernHash_ReturnsTrue; NeedsRehash_LegacyHash_ReturnsTrue; NeedsRehash_ModernHash_ReturnsFalse.
- **AuthServiceTests:** Existing tests updated to inject hasher; new **LoginAsync_WithLegacyHash_RehashesToModernFormat** (login with legacy hash then assert stored hash starts with `$2`).
- **AdminUserServiceTests:** All existing tests updated to pass IPasswordHasher; ResetPasswordAsync_UpdatesPasswordHash asserts with _passwordHasher.VerifyPassword (modern hash).
- **Total:** 27 tests passing (CompatibilityPasswordHasherTests 6, AuthServiceTests 6, AdminUserServiceTests 15).

---

## 7. Docs updated

- **ADMIN_USER_MANAGEMENT_V1.3_PLANNING.md:** Status “Phase B implemented”; Phase B section rewritten with implementation details; summary table Phase B = Done.
- **ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md:** v1.3 bullet updated to Phase A + Phase B done.
- **docs/dev/onboarding.md:** One sentence on v1.3 Phase B (IPasswordHasher, legacy verify, BCrypt for new, rehash on login, seed unchanged).

---

## 8. Remaining v1.3 phases still pending

- **Phase C:** Account lockout / brute-force protection (FailedLoginAttempts, LockoutEndUtc, etc.).
- **Phase D:** Email-based password reset (optional, later).

---

## Seed / bootstrap caveat

DatabaseSeeder is unchanged. It still uses its own static `HashPassword` for seeding. Seeded users therefore have legacy hashes. They can log in because CompatibilityPasswordHasher verifies legacy hashes; on first successful login their hash is upgraded to modern. No schema or seed script changes were required.

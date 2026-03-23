# Admin User Management v1.2 – Audit (Account Lifecycle Hardening)

**Date:** 2026-03-08

---

## 1. Current login flow

- **AuthController** `POST /api/auth/login`: calls `IAuthService.LoginAsync(LoginRequestDto)`. On success returns `ApiResponse<LoginResponseDto>` with AccessToken, RefreshToken, ExpiresAt, User. On failure throws `UnauthorizedAccessException` → 401 "Invalid email or password".
- **AuthService.LoginAsync**: finds user by email; checks IsActive and PasswordHash; verifies password via `DatabaseSeeder.VerifyPassword`; loads user DTO via `GetCurrentUserAsync`; generates JWT and refresh token; revokes old refresh tokens; saves new refresh token; returns `LoginResponseDto` (no LastLoginAtUtc update today).
- **Refresh**: `POST /api/auth/refresh` with RefreshToken; validates stored token; revokes old; issues new JWT + refresh token; no user DB update.
- **Me**: `GET /api/auth/me` [Authorize]; uses `ICurrentUserService.UserId`; returns `UserDto` from `GetCurrentUserAsync`.

## 2. User entity and auth columns

- **User** (`Domain/Users/Entities/User.cs`): Id, Name, Email, Phone, PasswordHash, IsActive, CreatedAt. **No** LastLoginAtUtc or MustChangePassword.
- **RefreshToken**: Id, UserId, TokenHash, ExpiresAt, IsRevoked, RevokedAt, CreatedFromIp, CreatedAt. No “last login” stored on token.

## 3. Refresh token storage

- Stored in `RefreshTokens` table; hashed (SHA256); one active per user after login (old ones revoked). Refresh does not update any User column.

## 4. Auth DTOs

- **LoginRequestDto**: Email, Password.
- **LoginResponseDto**: AccessToken, RefreshToken, ExpiresAt, User (UserDto).
- **UserDto**: Id, Name, Email, Phone, Roles. No MustChangePassword or LastLoginAtUtc.
- **Admin** list/detail DTOs: no lastLoginAtUtc or mustChangePassword.

## 5. Migrations pattern

- EF Core migrations in `Infrastructure/Persistence/Migrations`; snapshot in `ApplicationDbContextModelSnapshot.cs`. User table has Id, CreatedAt, Email, IsActive, Name, PasswordHash, Phone.

## 6. Frontend auth

- **AuthContext**: login calls `api/auth` login; stores token + user in state and localStorage; no check for “must change password”. Init: if token, calls getCurrentUser (GET /api/auth/me).
- **LoginPage**: on login success calls `navigate('/dashboard')`. No forced-change flow.
- **api/auth.ts**: login maps backend PascalCase to AuthResponse; no handling of password-change-required. There is a `changePassword` that POSTs to `/auth/change-password` but **backend has no such endpoint**.
- **types/auth.ts**: User has id, name, email, phone, roles; no mustChangePassword.

## 7. Forced password change / onboarding

- No existing forced-password-change or onboarding flow in backend or frontend.

---

## 8. Decisions for v1.2

| Topic | Decision |
|-------|----------|
| **Where to store last login** | Add nullable `LastLoginAtUtc` on **User** (UTC). Update only on successful login when not in must-change path. |
| **Where to store must-change-password** | Add **MustChangePassword** (bool, default false) on **User**. |
| **Login when MustChangePassword is true** | Do **not** issue tokens or update LastLoginAtUtc. Return **403** with a structured error (e.g. `code: "PasswordChangeRequired"` or message that client can detect) so frontend can redirect to change-password page. User must change password via authenticated flow; we will support that by issuing a **limited** token that only allows POST /auth/change-password, or by having a dedicated “force change” endpoint that accepts email + current password + new password without a token. Simpler: **require current password on change-password page** and **do not log in** until password is changed; so we **do not** issue any token when MustChangePassword is true. User sees “You must change your password” and is directed to a form that submits **email + current password + new password** to a **public** endpoint (e.g. POST /api/auth/change-password-required) that validates email+current password, updates hash and sets MustChangePassword=false, then returns success so frontend can redirect to login. Alternatively: issue a short-lived token that only allows POST /auth/change-password (more complex). **Chosen:** When MustChangePassword is true, return 403 with `requiresPasswordChange: true` and **no tokens**. Frontend shows “Change password required” and a form: current password, new password, confirm. Frontend calls a **new unauthenticated** endpoint **POST /api/auth/change-password-required** with **email + currentPassword + newPassword**; backend validates, updates hash, sets MustChangePassword=false, returns 200; frontend then redirects to login. So we need one new endpoint that does not require JWT. |
| **Authenticated password change** | Add **POST /api/auth/change-password** [Authorize]: body CurrentPassword, NewPassword. Validates current password, updates hash, sets MustChangePassword=false, optionally revokes refresh tokens. Used for normal “change my password” from profile/settings. |
| **Admin reset** | Request DTO add **ForceMustChangePassword** (bool). When admin resets password, set user.MustChangePassword = ForceMustChangePassword (default true). |
| **New endpoint** | **POST /api/auth/change-password-required** (no Auth): Email, CurrentPassword, NewPassword. For users who hit “must change password” and cannot use JWT. Validates email+current password and MustChangePassword=true, then updates password and clears flag. |

---

## 9. Files to change (plan)

| Area | File | Change |
|------|------|--------|
| Domain | User.cs | Add LastLoginAtUtc (DateTime?), MustChangePassword (bool). |
| Infra | Migration | Add columns to Users; nullable LastLoginAtUtc, MustChangePassword default false. |
| Application | Auth DTOs | LoginResponseDto: optional RequiresPasswordChange. UserDto: MustChangePassword. Add ChangePasswordRequestDto, ChangePasswordRequiredRequestDto. |
| Application | IAuthService / AuthService | LoginAsync: if MustChangePassword return/throw so controller returns 403 with no tokens; else update LastLoginAtUtc and return tokens. Add ChangePasswordAsync(userId, currentPassword, newPassword). Add ChangePasswordRequiredAsync(email, currentPassword, newPassword). |
| Api | AuthController | Login: handle “must change” from service (e.g. custom exception or result) → 403 + message/code. Add POST change-password [Authorize], POST change-password-required. |
| Application | Admin DTOs | List/detail: LastLoginAtUtc, MustChangePassword. AdminResetPasswordRequestDto: ForceMustChangePassword. |
| Application | AdminUserService | ResetPasswordAsync: accept forceMustChangePassword; set user.MustChangePassword; audit. List/GetById: map new fields. |
| Api | AdminUsersController | Reset password: pass ForceMustChangePassword from body. |
| Frontend | types/auth | User: mustChangePassword. Auth response: requiresPasswordChange. |
| Frontend | api/auth | login: handle 403 + requiresPasswordChange. Add changePasswordRequired(email, currentPassword, newPassword). changePassword for authenticated. |
| Frontend | AuthContext | After login success, if user.mustChangePassword redirect to change-password. On init (getCurrentUser), if mustChangePassword redirect to change-password. |
| Frontend | LoginPage | If login returns requiresPasswordChange, show change-password form or redirect to /change-password with state. |
| Frontend | New page | ChangePasswordPage: for “required” flow (email + current + new + confirm) and/or for authenticated flow (current + new + confirm). |
| Frontend | App routing | Route /change-password; when user has mustChangePassword, redirect to it from ProtectedRoute or similar. |
| Frontend | Admin UserManagementPage | Last Login column; Must Change Password badge; reset modal: “Require password change on next login” (default ON). |
| Tests | Auth + Admin | Last login update; must-change 403; change-password clears flag; admin reset with force flag; audit. |
| Docs | Listed docs | v1.2 section and updates. |

---

## 10. Security notes

- Last login only updated on real successful login (not on refresh; not when MustChangePassword).
- Change-password-required endpoint: rate-limit or cap attempts; validate current password; never log passwords.
- Authenticated change-password: require current password; revoke refresh tokens on success to force re-login if desired (optional).
- Admin reset: no current password; ForceMustChangePassword stored and audited.

---

## 11. Implementation status (v1.2 complete)

- **Phase 1:** Audit done (this document).
- **Phase 2:** User.LastLoginAtUtc, User.MustChangePassword; migration `20260308180000_AddLastLoginAndMustChangePasswordToUser`.
- **Phase 3–4:** AuthService login/refresh/GetCurrentUser/ChangePasswordAsync/ChangePasswordRequiredAsync; AuthController 403 for RequiresPasswordChangeException; change-password and change-password-required endpoints; Admin DTOs and ResetPassword with ForceMustChangePassword; audit for password/force-change.
- **Phase 5:** Admin UI (Last Login, Must Change Password, reset modal checkbox); ChangePasswordPage; AuthContext pendingPasswordChangeEmail; login/refresh 403 handling; route /change-password.
- **Phase 6:** Security review (no passwords in logs; last login only on full login; forced change not bypassable).
- **Phase 7:** AuthServiceTests (login updates LastLoginAtUtc; MustChangePassword throws; ChangePasswordRequired clears flag; wrong current password fails; GetCurrentUser returns MustChangePassword). AdminUserServiceTests (ResetPassword with ForceMustChangePassword true/false).
- **Phase 8:** ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md v1.2 section; api_surface_summary.md; onboarding.md; department_rbac.md.
- **Deliverable:** See docs/ADMIN_USER_MANAGEMENT_V1.2_DELIVERABLE.md.

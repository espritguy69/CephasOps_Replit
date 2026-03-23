# RBAC v2 – Phase 1 Audit: Current Authorization Model

**Date:** 2026-03-09  
**Status:** Complete

---

## 1. Domain / entities

| Entity | Location | Shape | Notes |
|--------|----------|--------|--------|
| **Role** | `backend/src/CephasOps.Domain/Users/Entities/Role.cs` | `Id`, `Name`, `Scope` (e.g. Company, Global) | Used; seeded (SuperAdmin, Director, HeadOfDepartment, Supervisor, FinanceManager). |
| **UserRole** | `backend/src/CephasOps.Domain/Users/Entities/UserRole.cs` | `Id`, `UserId`, `CompanyId?`, `RoleId`, `CreatedAt` | Used; links User to Role. |
| **Permission** | `backend/src/CephasOps.Domain/Users/Entities/Permission.cs` | `Id`, `Name`, `Description?` | **Exists but not used.** No seed data. |
| **RolePermission** | `backend/src/CephasOps.Domain/Users/Entities/RolePermission.cs` | `RoleId`, `PermissionId` (composite PK) | **Exists but not used.** No seed data. |
| **DepartmentMembership** | `backend/src/CephasOps.Domain/Departments/Entities/DepartmentMembership.cs` | `UserId`, `DepartmentId`, `Role` (string e.g. Member, HOD), `IsDefault`; company-scoped | Used for department-scoped access; separate from RBAC roles. |
| **User** | `backend/src/CephasOps.Domain/Users/Entities/User.cs` | Id, Name, Email, Phone, PasswordHash, IsActive, LastLoginAtUtc, MustChangePassword, FailedLoginAttempts, LockoutEndUtc, DepartmentMemberships | No direct Permission collection. |

**Summary:** Role, UserRole, Permission, RolePermission are all present and configured in EF. Only Role and UserRole are seeded and used. Permission and RolePermission tables exist but have no seed and no API.

---

## 2. Backend authorization usage

### 2.1 `[Authorize]` and `[Authorize(Roles = ...)]`

- **Controller-level `[Authorize]`:** Most controllers use `[Authorize]` only (authenticated user).
- **Role-restricted:** `[Authorize(Roles = "SuperAdmin,Admin")]` used on:
  - `AdminUsersController` (entire controller)
  - `AdminSecuritySessionsController` (entire controller)
  - `LogsController` (e.g. download/endpoints)
  - `BusinessHoursController`, `SlaProfilesController`, `EscalationRulesController`, `ApprovalWorkflowsController`, `AutomationRulesController` (specific write/delete endpoints)
- **Policy-based:** `[Authorize(Policy = "Orders")]`, `"Inventory"`, `"Reports"`, `"Jobs"`, `"Settings"` in `Program.cs`:
  - **Orders / Inventory / Reports:** `RequireAuthenticatedUser()` only.
  - **Jobs:** `RequireRole("SuperAdmin", "Admin")`.
  - **Settings:** `RequireRole("SuperAdmin", "Admin", "Director", "HeadOfDepartment", "Supervisor")`.

### 2.2 Permission checks

- **None.** No permission-based authorization in backend. No `ICurrentUserService` permissions; only `UserId`, `Email`, `Roles`, `IsSuperAdmin`, `CompanyId`, `ServiceInstallerId`.

### 2.3 CurrentUser / claims

- **AuthService:** JWT includes `sub`, email, `ClaimTypes.Role` per role. **Permissions are not in JWT.**
- **CurrentUserService:** Reads `UserId`, `Email`, `Roles` (from role claims), `IsSuperAdmin`. No permissions API.

**Summary:** Authorization is role-only. No permission catalog or permission checks in backend.

---

## 3. Frontend authorization usage

### 3.1 Route guards

- **ProtectedRoute:** Requires auth; optional `requiredPermission` – currently **not enforced by permission**; elevated access (SuperAdmin, Director, HeadOfDepartment, Supervisor) or “any role” used.
- **SettingsProtectedRoute:** Allows only SuperAdmin, Director, HeadOfDepartment, Supervisor (hardcoded roles).

### 3.2 Sidebar / menu visibility

- **Sidebar.tsx:** `NAV_SECTIONS` items can have `permission` (e.g. `orders.view`, `admin.view`, `settings.view`).
- **hasPermission(permission):** Implemented as:
  - SuperAdmin → always true.
  - `admin.view` → Admin or SuperAdmin.
  - Admin → true for all.
  - Otherwise “any role” → true.
- **No API call for permissions;** purely role-based with hardcoded permission strings.

### 3.3 Page-level checks

- No page-level permission checks beyond route guards and sidebar visibility.

### 3.4 RBAC API (frontend)

- **frontend/src/api/rbac.ts** and **types/rbac.ts**: Define `getRoles()`, `getRole()`, `getPermissions()`, `assignPermissionsToRole()`, etc., calling `/admin/roles`, `/admin/permissions`, `/admin/roles/{id}/permissions`.
- **Backend does not implement these routes.** Only `GET /api/admin/users/roles` exists (returns role **names** for dropdowns from `AdminUserService.GetRoleNamesAsync()`).

**Summary:** Frontend has permission strings in config and a prepared RBAC API surface; backend has no role/permission CRUD or permission claims.

---

## 4. Existing seed / bootstrap

- **Roles:** Seeded in `DatabaseSeeder`: SuperAdmin, Director, HeadOfDepartment, Supervisor, FinanceManager (via `EnsureRoleAsync`). **Admin** is referenced in code (`Authorize(Roles = "SuperAdmin,Admin")`, `AdminRoleNames`) but **not** explicitly seeded in the audited seeder section; may need to be added or is created elsewhere.
- **Permissions:** **Not seeded.**
- **RolePermission:** **Not seeded.**
- **UserRole:** Default admin user gets SuperAdmin; Finance HOD gets FinanceManager + department membership.

---

## 5. Summary: what exists vs gaps

| Area | Exists | Partially | Missing / notes |
|------|--------|-----------|------------------|
| Role entity & UserRole | ✅ | | |
| Permission & RolePermission entities | ✅ | | Not seeded; not used in auth. |
| JWT with roles | ✅ | | |
| JWT with permissions | | | ❌ Not implemented. |
| Role-based `[Authorize]` | ✅ | | Heavy use. |
| Policy-based (Orders, Settings, Jobs, etc.) | ✅ | | Role-based policies only. |
| Permission-based checks (backend) | | | ❌ None. |
| ICurrentUserService permissions | | | ❌ Only Roles. |
| Seed: roles | ✅ | | Admin role should be ensured. |
| Seed: permissions | | | ❌ None. |
| Seed: RolePermission | | | ❌ None. |
| API: GET/PUT role permissions | | | ❌ Frontend expects it; backend missing. |
| API: GET permissions catalog | | | ❌ Missing. |
| Frontend permission strings | ✅ | | In Sidebar; not from backend. |
| Frontend permission check (API) | | | ❌ Uses roles only. |

---

## 6. Where role checks are hardcoded

- **Backend:** `[Authorize(Roles = "SuperAdmin,Admin")]` on AdminUsersController, AdminSecuritySessionsController, LogsController; policy "Jobs" and "Settings" in `Program.cs`; `AdminRoleNames` in AdminUserService.
- **Frontend:** `SettingsProtectedRoute` (SuperAdmin, Director, HeadOfDepartment, Supervisor); Sidebar `hasPermission` (SuperAdmin, Admin, admin.view).

---

## 7. What can be reused directly

- **Domain:** Role, UserRole, Permission, RolePermission – reuse as-is.
- **DbContext:** Already has `Roles`, `UserRoles`, `Permissions`, `RolePermissions`.
- **Configurations:** PermissionConfiguration, RolePermissionConfiguration – reuse.
- **AuthService:** Keep JWT generation; **add** optional permission claims or keep permissions server-side only (recommended: load permissions server-side for enforcement; optionally add to JWT for frontend visibility).
- **AdminUsersController:** Keep; add or reference new Admin Roles/Permissions controller for matrix.
- **Frontend:** Sidebar permission strings and `hasPermission` – align with backend permission names and, when ready, use permissions from user context (e.g. from `/auth/me` or dedicated endpoint).

---

## 8. Files likely to change (RBAC v2)

| Layer | Files |
|-------|--------|
| **Backend – catalog** | New: Permission constants/catalog (e.g. `Permissions.cs` or `PermissionCatalog.cs`). |
| **Backend – auth** | AuthService (optional: add permissions to UserDto/me); ICurrentUserService + CurrentUserService (optional: Permissions); permission authorization handler/policy. |
| **Backend – API** | New: AdminRolesController or extend admin (GET /api/admin/roles, GET /api/admin/roles/{id}/permissions, PUT /api/admin/roles/{id}/permissions, optional GET /api/admin/permissions). |
| **Backend – seed** | DatabaseSeeder: seed Permissions, RolePermissions (e.g. SuperAdmin gets all, Admin gets admin.*). Ensure Admin role exists. |
| **Backend – enforcement** | Controllers: add permission checks or policy (e.g. admin.users.view, admin.security.sessions.revoke) alongside or instead of role checks where chosen. |
| **Frontend** | types/auth.ts (add permissions to User if from me); api/auth.ts (parse permissions); Sidebar hasPermission (use user.permissions when present); new page admin/security/roles or admin/settings/roles; rbac.ts (align with real API paths). |
| **Docs** | ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md, RBAC_V2_PERMISSION_MATRIX_DELIVERABLE.md, dev/onboarding.md. |

---

This audit is the basis for RBAC v2 design and implementation (permission catalog, role–permission matrix API, backend enforcement, admin UI, and safe migration).

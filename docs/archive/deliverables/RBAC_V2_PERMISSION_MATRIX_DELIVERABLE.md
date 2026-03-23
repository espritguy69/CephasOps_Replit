# RBAC v2 – Permission Matrix Deliverable

**Date:** 2026-03-09  
**Status:** Design and implementation reference

---

## 1. Scope (RBAC v2)

- **Permission catalog** in code/backend (single source of truth).
- **Role → Permission** assignment stored in DB (RolePermission); management via API.
- **Admin UI** to view/edit role permissions (matrix or grouped checklist).
- **Backend** permission enforcement (policy/helper) for selected endpoints.
- **Frontend** visibility aligned to permissions where practical (sidebar, actions); role fallback during transition.

**Out of scope for v2:** Migrating every endpoint to permission-only; ASP.NET Identity; changing JWT auth flow.

---

## 2. Permission naming convention

**Style:** `module.action` or `module.submodule.action`

**Examples:**

| Permission | Description |
|------------|-------------|
| admin.users.view | View user list and detail |
| admin.users.edit | Create/update users, set roles |
| admin.users.reset-password | Admin reset user password |
| admin.security.view | View security activity and sessions |
| admin.security.sessions.revoke | Revoke user sessions |
| admin.roles.view | View roles and role-permission matrix |
| admin.roles.edit | Edit role permissions |
| payout.health.view | View payout health dashboard |
| payout.repair.run | Run payout repair/anomaly actions |
| rates.view | View rate engine / rate groups |
| rates.edit | Edit rates and mappings |
| payroll.view | View payroll |
| payroll.run | Run payroll |
| orders.view | View orders and related |
| orders.edit | Create/edit orders |
| reports.view | View report definitions and run reports |
| reports.export | Export report data (CSV/Excel/PDF) |
| inventory.view | View materials, stock, movements, ledger, reports |
| inventory.edit | Create/edit materials, movements, receive, transfer, allocate, issue, return, import |
| jobs.view | View background jobs health and summary |
| jobs.run | Trigger or run background jobs (reserved) |
| settings.view | Access settings area (broad) |
| settings.edit | Create/update/delete global and integration settings |

Catalog is maintained in backend (e.g. `PermissionCatalog.cs`); DB Permission records seeded from or validated against it.

**RBAC v2 Phase 4 (full module coverage):** Orders, Reports, Inventory, Background Jobs, and Settings are permission-enforced as above. See [RBAC_V2_ROLLOUT_PHASE4_FULL_COVERAGE](RBAC_V2_ROLLOUT_PHASE4_FULL_COVERAGE.md).

---

## 3. Role vs permission model

- **Roles:** Broad grouping (SuperAdmin, Admin, Director, HeadOfDepartment, Supervisor, etc.). Used for backward compatibility and coarse access.
- **Permissions:** Feature-level (module.action). Used for fine-grained control and future scaling.
- **Enforcement:** SuperAdmin can bypass permission checks (configurable). Admin can have all `admin.*` by default via seed; other roles get explicit RolePermission rows.

---

## 4. Backend enforcement approach

- **Keep** existing `[Authorize(Roles = "SuperAdmin,Admin")]` where it is; add **permission** requirements incrementally.
- **Add** a permission policy (e.g. `RequirePermission("admin.users.view")`) and/or an extension method for programmatic check (e.g. `ICurrentUserService.HasPermission("admin.users.view")`).
- **Load user permissions** from DB (via roles → RolePermission → Permission) in a scoped service (e.g. current user's permissions resolved once per request).
- **Critical admin endpoints** (e.g. user management, security activity, session revoke, payout repair) get permission checks; role checks remain as fallback so existing users are not locked out.

---

## 5. API endpoints (Role Permission Matrix)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | /api/admin/roles | List roles (Id, Name, Scope) | Admin or SuperAdmin |
| GET | /api/admin/roles/{roleId}/permissions | Get permission names (or IDs) for role | Admin or SuperAdmin |
| PUT | /api/admin/roles/{roleId}/permissions | Set permission names for role (replace); validate names against catalog | Admin or SuperAdmin |
| GET | /api/admin/permissions | List permission catalog (name, description, optional group) | Admin or SuperAdmin |

Safety: prevent removing last SuperAdmin; prevent removing all permissions from Admin if that would lock admin capability (optional guard).

---

## 6. Frontend role/permission matrix UI

- **Page:** e.g. `/admin/security/roles` or `/admin/settings/roles`.
- **Layout:** Left: roles list. Right: grouped permissions by module (checkboxes). Save button.
- **Behaviour:** Load roles and catalog; for selected role load current permissions; save via PUT; show loading/success/error.

---

## 7. Default role mappings (seed)

- **SuperAdmin:** All permissions (or bypass); never lock out.
- **Admin:** All `admin.*` permissions at minimum; optionally others as needed.
- **Director / HeadOfDepartment / Supervisor:** Selected permissions (e.g. settings.view, orders.view, reports.view); refine as needed.

---

## 8. Transition strategy

- **Hybrid:** Role-based + permission-based. If user has role that previously granted access, keep granting until permissions are fully seeded and UI is updated.
- **SuperAdmin:** Always has access (bypass permission check).
- **Admin:** Seed with full admin.* so existing Admin users keep access.
- **Adding a new permission:** Add to catalog; add to seed for appropriate roles; protect endpoint/page; update frontend visibility.

---

## 9. How to add a new permission

1. Add constant and description to backend permission catalog.
2. Seed Permission entity if not already (idempotent).
3. Assign to roles in seed or via Admin UI.
4. Secure endpoint: `[Authorize(Policy = "Permission")]` with requirement, or use `ICurrentUserService.HasPermission(...)` in controller/service.
5. Frontend: add permission string to Sidebar or guard; use `user.permissions` when available.

---

## 10. How to secure a new endpoint/page

- **Backend:** Add permission check (policy or inline) for the action; keep role check if needed during transition.
- **Frontend:** Guard route or button with same permission; sidebar item already has permission field.

---

## 11. Implementation summary (2026-03-09)

- **Permission catalog:** `backend/src/CephasOps.Domain/Authorization/PermissionCatalog.cs` – constants, All, ByModule, IsValid, FilterValid.
- **Seeding:** DatabaseSeeder seeds Admin role, all Permission rows from catalog, and RolePermissions: SuperAdmin gets all permissions, Admin gets all `admin.*` permissions.
- **User permissions in auth:** AuthService.GetCurrentUserAsync loads permissions via IUserPermissionProvider and returns them in UserDto.Permissions; frontend login/refresh/me map Permissions to user.
- **Enforcement:** PermissionAuthorizationHandler (SuperAdmin bypass; else DB permissions). RequirePermissionAttribute on controllers. Phase 2 (see §12) expanded to Admin users (view/edit/reset-password), Admin security (view, session revoke), Logs security-activity/alerts, Admin roles (view/edit), Payout health and anomaly review, Rates (view/edit), Payroll (view/run).
- **API:** AdminRolesController – GET /api/admin/roles, GET /api/admin/roles/{roleId}/permissions, PUT /api/admin/roles/{roleId}/permissions (body: { permissionNames }), GET /api/admin/permissions. All require admin.roles.view or admin.roles.edit as appropriate.
- **UI:** Role Permissions page at /admin/security/roles (roles list, grouped permission checkboxes, Save). Sidebar: Payout Health / Payout Anomalies use payout.health.view; Security Activity uses admin.security.view; Role Permissions uses admin.roles.view; anomaly action buttons require payout.anomalies.review.
- **Safety:** SuperAdmin always allowed by handler. Seed gives Admin all admin.*, payout.*, rates.*, payroll.* so existing Admin users keep access. PUT rejects removing all permissions from SuperAdmin role.
- **Hybrid:** Endpoints still use [Authorize(Roles = "SuperAdmin,Admin")] where present; RequirePermission adds an extra check. Orders, Reports, Inventory, Background jobs remain policy/role-only for now.

This deliverable is updated as implementation progresses.

---

## 12. RBAC v2 Phase 2 rollout (2026-03-09)

- **Permission-enforced modules:** Admin users (admin.users.view, admin.users.edit, admin.users.reset-password), Admin security (admin.security.view, admin.security.sessions.revoke), Logs security-activity/alerts (admin.security.view), Admin roles (admin.roles.view, admin.roles.edit), Payout health dashboard and anomaly APIs (payout.health.view, payout.anomalies.review), Rates – all read and mutation endpoints (rates.view, rates.edit), Payroll – periods, runs, earnings, si-rate-plans (payroll.view, payroll.run).
- **Still hybrid (role or policy only):** Orders (Policy "Orders"), Inventory (Policy "Inventory"), Reports (Policy "Reports"), Background jobs (Policy "Jobs"), Settings (Policy "Settings").
- **How to add a permission check:** (1) Add constant to PermissionCatalog if new. (2) Seed permission and assign to Admin (or target role) in DatabaseSeeder. (3) Add [RequirePermission(PermissionCatalog.X)] on the controller action. (4) Frontend: set permission on Sidebar item or guard action buttons with user.permissions / role fallback.
- **Tests:** RbacPermissionEnforcementTests (SuperAdmin bypass, Member gets 403 on admin/roles and admin/permissions; invalid permission name rejected by PUT role permissions). AdminUsersIntegrationTests and application PermissionCatalogTests remain.

---

## 13. RBAC v2 Phase 3 – Department scope (2026-03-09)

- **Model:** Permission = WHAT; department scope = WHERE. SuperAdmin: all permissions, all departments. Admin: broad access (global when no department memberships). Department member: only assigned departments (DepartmentMembership).
- **Backend:** IDepartmentAccessService (GetAccessAsync, EnsureAccessAsync, ResolveDepartmentScopeAsync) and IDepartmentRequestContext (X-Department-Id, query departmentId) already exist. Added controller helpers: ResolveDepartmentScopeOrFailAsync and EnsureDepartmentAccessOrFailAsync (CephasOps.Api.Common.DepartmentScopeExtensions) for consistent 400/403 (missing department → 400, no access → 403).
- **Department-scoped modules (already enforced):** Orders, Reports, Payroll (si-rate-plans only), Inventory, PnL, Scheduler, Users (by department), Departments, ServiceInstallers, Tasks, Assets, Skills, OrderTypes, and other settings controllers. Rates and Payroll periods/runs/earnings remain company-wide (no department filter).
- **Frontend:** X-Department-Id sent via api/client buildHeaders; departmentId in query where used. Role Permissions page includes a note: permissions define capability, department memberships define scope.
- **Docs:** [RBAC_V2_ROLLOUT_PHASE3_DEPARTMENT_SCOPE](RBAC_V2_ROLLOUT_PHASE3_DEPARTMENT_SCOPE.md) – audit, model, rollout, and how to secure a new department-scoped endpoint.

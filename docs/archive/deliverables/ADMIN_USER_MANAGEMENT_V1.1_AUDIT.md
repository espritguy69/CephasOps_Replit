# Admin User Management v1.1 – Audit Summary

**Date:** 2026-03-08

---

## Current implementation (v1)

- **DTOs:** AdminUserListItemDto, AdminUserDetailDto, Create/Update request DTOs with `List<AdminUserDepartmentMembershipDto>` (DepartmentId, Role, IsDefault). Backend accepts and persists department memberships; invalid department IDs are skipped silently (dept == null continue). No dedupe of department IDs in the list.
- **Service:** AdminUserService in Application; CreateAsync, UpdateAsync, SetActiveAsync, SetRolesAsync, ResetPasswordAsync. Uses DatabaseSeeder.HashPassword. Validates duplicate email, last active admin, self-deactivate. No IAuditLogService; no actor ID for audit.
- **Controller:** AdminUsersController at api/admin/users; [Authorize(Roles = "SuperAdmin,Admin")]; passes currentUserId only to SetActive and SetRoles. Create/Update/ResetPassword do not pass actor.
- **Frontend:** UserManagementPage loads departments via getDepartments() but does not render a department selector in create/edit; formData.departmentMemberships is populated on edit from detail.departments and sent on create/update. Table and view show departments as comma-joined names (and role in view). Roles are checkboxes; no multi-select component for departments.

## What can be reused directly

- **getDepartments()** – Already used; returns Department[] (id, name, code, isActive). No new endpoint needed.
- **IAuditLogService** – Injected in Application (e.g. WorkflowEngineService uses optional `IAuditLogService?`). LogAuditAsync(companyId, userId, entityType, entityId, action, fieldChangesJson, "Api", null, metadataJson). AuditLogService catches exceptions and logs; does not throw.
- **API error handling** – Client handleError() reads message/errors from envelope; showError(err.message) already used. Can surface err.message or err.data?.errors in UI for clarity.
- **Form pattern** – formData state, Modal, Button disabled when formLoading; add department section alongside Roles.
- **Department DTO** – AdminUserDepartmentMembershipDto already has DepartmentId, Role, IsDefault; backend persists. Expose department picker + optional Role per row (default "Member").

## Gaps

1. **Department UI** – No multi-select or checklist for departments in create/edit; no per-department role in form (only in view).
2. **Audit** – No calls to IAuditLogService from AdminUserService; need actor userId (e.g. ICurrentUserService in service or pass from controller).
3. **Validation** – Duplicate department IDs in request not rejected; invalid department IDs skipped silently; empty role list allowed (could leave user with no roles).
4. **UX** – No inline error under form; mutation errors only toast; no “Saving…” on primary button during request; no troubleshooting note.
5. **Table/detail** – Departments shown as comma-joined; could use badges or list for readability.

## Files to change (plan)

| Area | File | Changes |
|------|------|---------|
| Backend | Application/Admin/Services/IAdminUserService.cs | No signature change (optional: add actor to Create/Update/Reset for audit – or use ICurrentUserService in service). |
| Backend | Application/Admin/Services/AdminUserService.cs | Inject IAuditLogService? and ICurrentUserService; dedupe and validate department IDs; reject invalid department IDs; optional: validate at least one role on create/update; call LogAuditAsync after Create, Update, SetActive, SetRoles, ResetPassword (no password in audit). |
| Backend | Api/Controllers/AdminUsersController.cs | Pass current user to service for audit where needed (or rely on service ICurrentUserService). Return 400 with clear message for validation (e.g. invalid department). |
| Frontend | pages/admin/UserManagementPage.tsx | Department multi-select in create/edit (checkboxes or list with add/remove); optional per-department role dropdown; show departments as badges in table and view; inline error state for form; loading label on submit buttons; optional helper/troubleshooting text; refetch list after mutations. |
| Tests | Application.Tests/Admin/AdminUserServiceTests.cs | Duplicate department IDs rejected; invalid department ID rejected; audit called when service provided (mock). |
| Tests | Api.Tests/Integration/AdminUsersIntegrationTests.cs | Optional: assert audit entry when available. |
| Docs | ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md | v1.1 scope, TODO/follow-up section. |
| Docs | api_surface_summary.md, department_rbac.md, onboarding.md | Minor: user management v1.1, audit, department UI. |

## Risks / assumptions

- Single-company: getDepartments() returns all departments; no company filter in admin user flow.
- IAuditLogService optional in service (null in tests); ICurrentUserService required for audit actor (mock in tests).
- Per-department role: expose as dropdown (Member, HOD, etc.) if simple; else document “department only” for v1.1.
- No breaking API or DTO changes; additive only.

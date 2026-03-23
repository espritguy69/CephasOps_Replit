# RBAC v2 Rollout Phase 3 – Department-aware authorization

**Date:** 2026-03-09  
**Status:** Phase 3 – Department scope refinement

---

## 1. Phase 1 – Audit: current department model

### 1.1 Domain entities

| Entity | Location | Purpose |
|--------|----------|---------|
| **Department** | Domain/Departments/Entities/Department.cs | Company-scoped; Name, Code, IsActive. |
| **DepartmentMembership** | Domain/Departments/Entities/DepartmentMembership.cs | UserId, DepartmentId, Role (e.g. "Member"), IsDefault. Links users to departments. |
| **User** | Domain/Users/Entities/User.cs | Has collection DepartmentMemberships. |
| **Role / UserRole** | Domain/Users (Role, UserRole) | Global roles (SuperAdmin, Admin, etc.); separate from department membership. |

**DepartmentScope / CompanyId:** Department and DepartmentMembership extend CompanyScopedEntity (CompanyId). Current code often uses companyId = null (company feature removed in many places).

### 1.2 Backend – department context and access

| Component | Purpose |
|-----------|---------|
| **IDepartmentRequestContext** | Reads department from request: header `X-Department-Id` or query `departmentId`. |
| **DepartmentRequestContext** | Implementation; registered in DI; used by controllers. |
| **IDepartmentAccessService** | `GetAccessAsync()` → DepartmentAccessResult (Global / None / list of department IDs). `EnsureAccessAsync(departmentId)` throws if no access. `ResolveDepartmentScopeAsync(requestedId)` returns resolved scope or throws (missing → "Department selection is required", no access → "You do not have access to this department"). |
| **DepartmentAccessService** | SuperAdmin → Global. Admin with no memberships → Global. Others → department IDs from DepartmentMemberships. |
| **DepartmentService.GetDepartmentsAsync** | Already filters: when user does not have global access, returns only departments in user's DepartmentIds. |

### 1.3 Controllers already using department scope

Controllers that call `ResolveDepartmentScopeAsync` or `EnsureAccessAsync` (source: query param `departmentId`, or `_departmentRequestContext.DepartmentId`, or body):

| Module | Controller | Department source | Current behavior |
|--------|------------|-------------------|------------------|
| Orders | OrdersController | Query, header (via ResolveDepartmentScopeAsync(query ?? context)) | 403 on UnauthorizedAccessException |
| Reports | ReportsController | Query, header | 403 on UnauthorizedAccessException |
| Payroll | PayrollController | Query, header (si-rate-plans, GetSiRatePlans, ExportSiRatePlans only) | 403 on UnauthorizedAccessException |
| Inventory | InventoryController | Query, header, body | 403 on UnauthorizedAccessException |
| PnL | PnlController | Query, header | ResolveDepartmentScopeAsync |
| Scheduler | SchedulerController | Query, header (private helper) | ResolveDepartmentScopeAsync |
| Users | UsersController | Query, header; EnsureAccessAsync for by-department | 403 |
| Departments | DepartmentsController | Query, header | ResolveDepartmentScopeAsync |
| ServiceInstallers | ServiceInstallersController | Query, header | ResolveDepartmentScopeAsync |
| Tasks | TasksController | Query, header | ResolveDepartmentScopeAsync |
| Assets | AssetsController | Query, header | ResolveDepartmentScopeAsync |
| Skills | SkillsController | Query, header | ResolveDepartmentScopeAsync |
| OrderTypes | OrderTypesController | Query, header | ResolveDepartmentScopeAsync |
| InstallationMethods, BusinessHours, EscalationRules, ApprovalWorkflows, AutomationRules, SlaProfiles, SplitterTypes, BillingRatecard, Warehouses | Various | Query, header | ResolveDepartmentScopeAsync |

**RatesController:** Does **not** use department; uses CompanyId only. Rates are company-wide in current design.

**PayrollController:** GET periods, GET runs, GET earnings do **not** pass department; only si-rate-plans and export/import use department scope.

### 1.4 Frontend – department selection and sending

| Location | Behavior |
|----------|----------|
| **DepartmentContext** | Loads departments via GET /api/departments (already filtered by backend to user's departments when not global). User selects active department; stored in localStorage. `setDepartmentGetter` provides current department to API client. |
| **api/client.ts** | `buildHeaders()` adds `X-Department-Id` from `getActiveDepartmentId()` (from DepartmentContext) to every request when present. |
| **useDepartment()** | Returns departments, activeDepartment, departmentId, selectDepartment. Used by orders, inventory, reports, payroll (si-rate-plans), settings pages, tasks, scheduler, etc. |
| **Query params** | Many API calls also pass `departmentId` in query (e.g. orders list, reports run). |

### 1.5 Audit table – module / endpoint / department behavior / recommended scope

| Module | Endpoint / page | Current department behavior | Source of department | Recommended scope rule |
|--------|------------------|-----------------------------|----------------------|------------------------|
| Orders | GET/POST/PUT orders, list, search | Department-scoped via ResolveDepartmentScopeAsync | Query, header | Permission + department (already enforced) |
| Reports | Run report, export | Department-scoped via ResolveDepartmentScopeAsync | Query, header | Permission + department (already enforced) |
| Payroll | GET si-rate-plans, export, import | Department-scoped | Query, header | Permission + department (already enforced) |
| Payroll | GET periods, runs, earnings; POST periods, runs, finalize, mark-paid | No department filter | N/A | Global (company-wide) or optional department filter later |
| Rates | All rate endpoints | No department; company-scoped | N/A | Keep company-wide (no department in Phase 3) |
| Inventory | Ledger, receive, transfer, etc. | Department-scoped | Query, header, body | Permission + department (already enforced) |
| Admin users | User list by department, memberships | EnsureAccessAsync for department | Query, header | Already enforced |
| Departments | GET list | Filtered by GetAccessAsync in DepartmentService | N/A | Already enforced |

---

## 2. Phase 2 – Authorization model (permission + department scope)

### 2.1 Hybrid model

- **Permission** = WHAT the user may do (e.g. payroll.view, orders.view).
- **Department scope** = WHERE they may do it (which departments).

### 2.2 Policy rules

| Role / situation | Permission | Department scope |
|------------------|------------|------------------|
| **SuperAdmin** | All (bypass); not restricted by department | All departments (HasGlobalAccess) |
| **Admin** | Per role-permission matrix; typically broad | If no department memberships: global (all departments). If has memberships: still get global in current implementation (DepartmentAccessService gives Admin global when memberships.Count == 0). |
| **Department member** | Per role-permission matrix | Only assigned departments (DepartmentMembership). Must supply department for department-scoped endpoints; ResolveDepartmentScopeAsync enforces. |

### 2.3 Evaluation rule

1. **Global-only endpoint** (e.g. admin roles, payout health): require **permission** only; no department check.
2. **Department-scoped endpoint**: require **permission** and **department access**:
   - Resolve department from: header `X-Department-Id`, then query `departmentId`, then body where applicable.
   - Call `ResolveDepartmentScopeAsync(requestedDepartmentId)`.
   - If missing and required: return **400** "Department selection is required" (or 403 depending on product choice).
   - If supplied but user has no access: return **403** "You do not have access to this department".
3. **Optional department** (e.g. list all or filter by department): resolve when supplied; when not supplied and user is not global, use default department or return error per existing behavior.

No ABAC or row-level security in this phase; reuse existing ResolveDepartmentScopeAsync / EnsureAccessAsync.

---

## 3. Phase 3 – Backend refinement

### 3.1 Reusable helper for controllers

- **Existing:** `IDepartmentAccessService.ResolveDepartmentScopeAsync` and `EnsureAccessAsync`; controllers catch `UnauthorizedAccessException` and return 403.
- **Addition:** Controller extension method `ResolveDepartmentScopeOrFailAsync` that:
  - Takes requested department ID (nullable), plus optional fallback from `IDepartmentRequestContext`.
  - Calls `ResolveDepartmentScopeAsync`.
  - On success: returns `(Guid? scope, null)`.
  - On throw: maps "Department selection is required" → 400, otherwise → 403; returns `(null, ActionResult)`.

This reduces duplicated try/catch and normalizes 400 vs 403.

### 3.2 No change to JWT or permission model

- SuperAdmin bypass and Admin default permissions unchanged.
- Department logic remains in DepartmentAccessService; no new attributes required for this phase.

---

## 4. Phase 4 – Targeted endpoint rollout

- **Already department-scoped:** Orders, Reports, Payroll (si-rate-plans only), Inventory, PnL, Scheduler, Users (by department), Departments, ServiceInstallers, Tasks, Assets, Skills, OrderTypes, and other settings controllers listed above.
- **Explicitly global (no department):** Admin users/roles/security, Payout health, Rates, Payroll periods/runs/earnings (no department filter in current design).
- **Rollout:** No new endpoints converted in Phase 3; document which are department-scoped and normalize 400/403 via the new helper where we apply it.

---

## 5. Phase 5 – Frontend alignment

- Department context already sent via `X-Department-Id` (api/client.ts buildHeaders) and query params where used (useDepartment(), ensureDepartmentParam).
- Handle 403: API errors are surfaced by the client; when backend returns 403 with message "You do not have access to this department", show that message (e.g. via toast or error state). User can select a different department from the department switcher.
- 400 "Department selection is required": same; show message and prompt user to select a department if none is selected.
- Role Permissions page: added short note that permissions define capability and department memberships (User Management) define scope.
- No wholesale UI redesign.

---

## 6. Phase 6 – Admin UX

- User detail / admin user management: ensure department memberships are visible and editable.
- Optional short note in Role Permissions or User Management: "Permissions grant capability; department memberships grant scope (which departments you can work in)."

---

## 7. Phase 7 – Safety

- SuperAdmin and Admin remain unrestricted at department level (existing behavior).
- Modules not yet using department (e.g. Rates) unchanged.
- Document: permission-only vs permission + department-scope vs hybrid/pending.

---

## 8. Phase 8 – Tests

- User with permission but without department membership → 403 for department-scoped endpoint.
- User with permission and department membership → 200 when requesting that department.
- SuperAdmin → 200 regardless of department.
- Admin (with or without memberships) → 200 for department-scoped when allowed by existing logic.
- Missing department when required → 400 or 403 as designed.

---

## 9. Phase 9 – Docs

- Update RBAC_V2_PERMISSION_MATRIX_DELIVERABLE.md and RBAC_V2_ROLLOUT_PHASE2.md (or this doc) with Phase 3 summary.
- Update ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md and dev/onboarding.md with department-scope model and link to this doc.

---

## 10. How to secure a new department-scoped endpoint

1. **Backend:** Inject IDepartmentAccessService and IDepartmentRequestContext. Resolve scope at the start of the action:
   - `var (scope, err) = await this.ResolveDepartmentScopeOrFailAsync(_departmentAccessService, _departmentRequestContext, departmentIdFromQueryOrRoute, cancellationToken);`
   - `if (err != null) return err;`
   - Use `scope` (Guid?) when calling the service. When the department is already known (e.g. from body), use `EnsureDepartmentAccessOrFailAsync(controller, departmentAccessService, departmentId, cancellationToken)` instead.
2. **Department source:** Prefer query `departmentId` or header X-Department-Id; document in the endpoint summary.
3. **Permission:** Keep existing [RequirePermission(...)] so both permission and department scope are required.
4. **Frontend:** Ensure the page sends X-Department-Id (via DepartmentContext) and/or departmentId in query so the backend receives department context.

---

## 11. Remaining backlog (post–Phase 3)

- Consider filtering GET /api/departments by user's departments only (already done in DepartmentService when !HasGlobalAccess).
- Optional: require department for more payroll endpoints (e.g. filter periods by department) in a later phase.
- Optional: rates by department if product later requires it.
- Migrate more controllers from manual try/catch UnauthorizedAccessException to ResolveDepartmentScopeOrFailAsync for consistent 400/403.

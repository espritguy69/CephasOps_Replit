# RBAC Matrix Report

**Audit date:** 3 February 2026  
**Scope:** Single-company, department-based access.  
**Source:** Codebase audit only (no guessing; "Unknown" where unclear).

---

## 1) Where permissions come from

| Source | Description | Evidence |
|--------|-------------|----------|
| **Roles (JWT)** | Claims: `ClaimTypes.Role`, `role`, `roles`. Loaded from `UserRole` table per user. Used for authentication and some endpoint restrictions. | `AuthService.cs` (GenerateJwtToken, GetCurrentUserAsync); `CurrentUserService.cs` (Roles); `AuthController.cs` |
| **Department membership (DB)** | `DepartmentMembership` table: UserId, DepartmentId, Role, IsDefault. Resolved at runtime by `DepartmentAccessService.GetAccessAsync()` — not stored in JWT. | `Domain/Departments/Entities/DepartmentMembership.cs`; `DepartmentAccessService.cs` |
| **Request department scope** | Frontend sends active department via header `X-Department-Id` or query `departmentId`. Backend reads via `IDepartmentRequestContext`. Validated only where `IDepartmentAccessService.ResolveDepartmentScopeAsync` or `EnsureAccessAsync` is used. | `DepartmentRequestContext.cs`; `frontend/src/api/client.ts` (buildHeaders, ensureDepartmentParam) |
| **Policies** | No named authorization policies defined. Only `[Authorize]` and `[Authorize(Roles = "SuperAdmin,Admin")]` on some endpoints. | `Program.cs` (AddAuthorization, no policies); controllers (see Evidence below) |

**Important:** JWT does **not** include department IDs. Department access is enforced server-side wherever code calls `DepartmentAccessService.ResolveDepartmentScopeAsync` or `EnsureAccessAsync`. Department scope is enforced across **all** department-scoped endpoints (Orders, Inventory, Scheduler, Departments, Skills, Payroll, BillingRatecard, BusinessHours, ServiceInstallers, OrderTypes, BuildingTypes, SplitterTypes, ApprovalWorkflows, SlaProfiles, AutomationRules, AgentMode, Users, EscalationRules, Tasks, Pnl, and related list/export/detail). Requesting another department returns 403.

---

## 2) Departments (rows) × Modules (columns)

**Note:** Rows are expressed as **effective access level** (role + department membership), because the codebase does not define permissions per department name (e.g. GPON vs CWO). Permissions are “user has role X” and “user has membership in department Y” (when enforced).

| Access level | Orders | Inventory | Reports | Admin/Settings | Jobs/Automation | Users/Auth |
|--------------|--------|------------|---------|----------------|-----------------|------------|
| **SuperAdmin** | View, Create, Edit, Delete (all departments); Approve/Submit where applicable | View, Create, Edit, Delete (all departments; ResolveDepartmentScopeAsync returns requested) | View, Create, Edit, Delete (companyId from query; department resolved where used) | View, Create, Edit, Delete (full Settings access) | View, Trigger Job (no department check) | View, Create, Edit, Delete (e.g. current user, refresh) |
| **Admin** | Same as SuperAdmin for Orders; some actions explicitly check `userRoles.Contains("Admin")` | Same as SuperAdmin (department scope resolved; global access) | Same | View, Create, Edit, Delete on endpoints with `[Authorize(Roles = "SuperAdmin,Admin")]` (BusinessHours, EscalationRules, ApprovalWorkflows, AutomationRules, SlaProfiles); other Settings via [Authorize] | View, Trigger Job (no restriction) | Same |
| **Director / HeadOfDepartment / Supervisor** | View, Create, Edit, Delete **only for departments they belong to** (ResolveDepartmentScopeAsync validates membership); otherwise 403 | View, Create, Edit, Delete **only for departments they belong to** (ResolveDepartmentScopeAsync/ResolveLedgerContextAsync); requesting other department → 403 | View, Create, Edit, Delete (department resolved where used; other dept → 403) | View, Create, Edit, Delete (SettingsProtectedRoute allows these roles); department-scoped endpoints resolve scope | View, Trigger Job (no department check) | Same; GetUsers/GetUsersByDepartment enforce department access |
| **Member (with department membership)** | View, Create, Edit, Delete only for their departments (validated); department required (403 if not supplied and no default) | View, Create, Edit, Delete only for their departments (validated); other dept → 403 | Same (department resolved; other dept → 403) | **None** (SettingsProtectedRoute denies; only SuperAdmin, Director, HeadOfDepartment, Supervisor) | View, Trigger Job (no department check) | Same (e.g. own profile); user list/by-department enforce department |
| **Member (no department membership)** | **Unknown** — `DepartmentAccessService` returns None or Global per implementation; no membership may require department selection (403) | Same as Member with membership where enforcement applied | Same | None | View, Trigger Job | Same |

**Cell notes**

- **Orders:** Department scoping enforced via `ResolveDepartmentScopeAsync` (requested or X-Department-Id must be in user’s DepartmentMemberships; SuperAdmin bypasses). Evidence: `OrdersController.cs`, `DepartmentAccessService.ResolveDepartmentScopeAsync`.
- **Inventory:** Department scope enforced via `ResolveDepartmentScopeAsync` / `ResolveLedgerContextAsync` on materials, ledger, stock-summary, receive, export, schedule. Requesting another department → 403. Evidence: `InventoryController.cs`.
- **Reports:** ReportDefinitionsController uses `[Authorize]` and companyId from query. PnlController.GetPnlDetailPerOrder and related report endpoints resolve department scope where departmentId is used.
- **Admin/Settings:** Frontend: `SettingsProtectedRoute` allows only SuperAdmin, Director, HeadOfDepartment, Supervisor. Backend: some endpoints require `[Authorize(Roles = "SuperAdmin,Admin")]`. Department-scoped list/effective endpoints (BusinessHours, EscalationRules, ApprovalWorkflows, AutomationRules, SlaProfiles, OrderTypes, BuildingTypes, SplitterTypes, InstallationTypes, OrderCategories, InstallationMethods) use `ResolveDepartmentScopeAsync`; other dept → 403.
- **Jobs/Automation:** BackgroundJobsController: `[Authorize]` only. Inventory report export job is enqueued with resolved departmentId from API. **Unknown** which roles/departments may view/trigger jobs; plan says “only allowed departments” — to be defined if needed.
- **Users/Auth:** GetUsers (filter by department) and GetUsersByDepartment resolve/ensure department access; other dept → 403. Evidence: `UsersController.cs`.

---

## 3) Actions legend (what was checked)

- **View:** GET/list/read.
- **Create:** POST/create.
- **Edit:** PUT/PATCH/update.
- **Delete:** DELETE.
- **Approve/Submit:** Order approval, workflow submit, etc. (only Orders explicitly checked).
- **Export:** Export endpoints (not fully audited per module).
- **Trigger Job:** Triggering background jobs (e.g. P&L rebuild, email ingest).

---

## 4) Evidence (file paths, policy names, frontend guards)

| Item | Evidence |
|------|----------|
| Roles in JWT | `AuthService.cs`: GenerateJwtToken (roles from UserRole). GetCurrentUserAsync loads UserRoles. |
| Department membership resolution | `DepartmentAccessService.cs`: GetAccessAsync (SuperAdmin → Global; else DB DepartmentMemberships). ResolveDepartmentScopeAsync, EnsureAccessAsync. |
| Orders department enforcement | `OrdersController.cs`: ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId) before queries. |
| Inventory department enforcement | `InventoryController.cs`: ResolveDepartmentScopeAsync / ResolveLedgerContextAsync on materials, ledger, stock-summary, receive, export, schedule. |
| Department-scoped controllers (full list) | DepartmentsController (export), ServiceInstallersController, SkillsController, PayrollController, BillingRatecardController, BusinessHoursController, InstallationTypesController, OrderCategoriesController, InstallationMethodsController, EscalationRulesController, TasksController, PnlController, OrderTypesController, BuildingTypesController, SplitterTypesController, ApprovalWorkflowsController, SlaProfilesController, AutomationRulesController, AgentModeController, UsersController; SchedulerController already enforced. All use ResolveDepartmentScopeAsync or EnsureAccessAsync. |
| Report definitions | `ReportDefinitionsController.cs`: [Authorize], companyId from query. |
| Settings role restriction (frontend) | `SettingsProtectedRoute.tsx`: hasSettingsAccess = roles include SuperAdmin, Director, HeadOfDepartment, Supervisor. |
| Settings role restriction (backend) | BusinessHoursController, EscalationRulesController, ApprovalWorkflowsController, AutomationRulesController, SlaProfilesController: specific endpoints [Authorize(Roles = "SuperAdmin,Admin")]. |
| General route protection | `ProtectedRoute.tsx`: auth required; optional requiredPermission checks elevated roles (SuperAdmin, Director, HeadOfDepartment, Supervisor). |
| Department in API requests | `frontend/src/api/client.ts`: buildHeaders sets X-Department-Id from getActiveDepartmentId(); ensureDepartmentParam adds departmentId to query. `DepartmentContext.tsx`: activeDepartment from getDepartments(); setDepartmentGetter. |
| Departments CRUD and access | `DepartmentService.cs`: EnsureAccessAsync(department.Id) on get/update/delete and material allocations. |
| Background jobs | `BackgroundJobsController.cs`: [Authorize]; no department or role policy. |

---

## 5) Gaps and “Unknown” summary

| Gap / Unknown | What is needed to resolve |
|--------------|----------------------------|
| ~~Inventory department enforcement~~ | **Resolved.** Department scope enforced on all inventory materials/ledger/export/schedule endpoints. |
| Jobs: which departments can view/trigger | Define policy (e.g. Admin/SuperAdmin only, or specific roles/departments); add authorization checks to BackgroundJobsController if needed. |
| Reports: department scoping | PnlController and report endpoints that accept departmentId now resolve scope. Remaining: document any report execution that does not take departmentId. |
| Member (no department membership) | DepartmentAccessService returns None or Global per implementation; no membership may require department selection (403). Document intended behavior if needed. |
| Users/Auth: per-role matrix | GetUsers and GetUsersByDepartment enforce department access. Document which roles can View/Create/Edit/Delete users beyond "authenticated" if needed. |
| Export actions per module | Department-scoped export endpoints (Inventory, ServiceInstallers, Payroll, BillingRatecard, Departments export) resolve department scope. |
| Assets/Warehouses/Pnl summary | Accept departmentId but do not use in filtering yet; when filtering is added, apply ResolveDepartmentScopeAsync. See **§6** for endpoint-level documentation. |

---

## 6) PARTIAL (document only) — Assets, Warehouses, Pnl Summary

These endpoints accept `departmentId` (query or request context) and **validate access** when it is provided (`ResolveDepartmentScopeAsync`; 403 if unauthorized). They do **not** pass department scope to the underlying service, so **data is not filtered by department** (company-wide or unfiltered). Documented here to prevent accidental silent RBAC regressions. **No runtime behavior change.**

| Endpoint | Signature | Why departmentId exists | Current behavior | Phase 2 requirement |
|----------|-----------|-------------------------|-------------------|---------------------|
| **AssetsController.GetMaintenanceRecords** | `GET /api/assets/maintenance?assetId=&completed=&departmentId=` | API consistency; future department-scoped maintenance | `ResolveDepartmentScopeAsync` when departmentId/context set (403 if unauthorized). Service: `GetMaintenanceRecordsAsync(companyId, assetId, completed)` — **not filtered by department**. | If filtering by department is introduced, MUST use ResolveDepartmentScopeAsync and pass resolved scope to service; enforce membership. |
| **AssetsController.GetUpcomingMaintenance** | `GET /api/assets/maintenance/upcoming?daysAhead=&departmentId=` | Same | Same pattern; service: `GetUpcomingMaintenanceAsync(companyId, daysAhead)` — **not filtered by department**. | Same. |
| **AssetsController.GetDepreciationEntries** | `GET /api/assets/depreciation?period=&assetId=&isPosted=&departmentId=` | Same | Same pattern; service: `GetDepreciationEntriesAsync(companyId, period, assetId, isPosted)` — **not filtered by department**. | Same. |
| **WarehousesController.GetAll** | `GET /api/warehouses?companyId=&isActive=&departmentId=` | API consistency; future department-scoped warehouses | `ResolveDepartmentScopeAsync` when set (403 if unauthorized). Service: `GetAllAsync(effectiveCompanyId, isActive)` — **not filtered by department**. | If filtering by department is introduced, MUST use ResolveDepartmentScopeAsync and enforce membership. |
| **PnlController.GetPnlSummary** | `GET /api/pnl/summary?periodId=&startDate=&endDate=&departmentId=` | Validated for RBAC; PnlFact uses CostCentreId, not yet department | `ResolveDepartmentScopeAsync` when set (403 if unauthorized). Service: `GetPnlSummaryAsync(companyId, periodId, startDate, endDate)` — **not filtered by department**. | If P&L is ever filtered by department (e.g. CostCentre → Department), MUST use ResolveDepartmentScopeAsync and enforce membership. |

---

## 7) Summary

- **Enforced today:** Department scope is enforced across **all** department-scoped endpoints: Orders, Inventory (materials, ledger, stock-summary, receive, export, schedule), Scheduler, Departments (including export), ServiceInstallers (list + export), Skills (list, by-category, categories), Payroll (SI rate plans list + export), BillingRatecard (list + export), BusinessHours, InstallationTypes, OrderCategories, InstallationMethods, EscalationRules, Tasks, Pnl (detail per order), OrderTypes, BuildingTypes, SplitterTypes, ApprovalWorkflows (GetEffectiveWorkflow), SlaProfiles (GetProfiles, GetEffectiveProfile), AutomationRules (GetApplicableRules), AgentMode (CalculateKpis), Users (GetUsers filter, GetUsersByDepartment). Requesting a department the user does not belong to returns **403**.
- **Not enforced / Partial:** Background jobs view/trigger have no department or role restriction. AssetsController, WarehousesController, and PnlController.GetPnlSummary accept departmentId but do not use it in filtering yet.
- **Policies:** No named policies (e.g. "Orders", "Inventory", "Reports"); only `[Authorize]` and `[Authorize(Roles = "SuperAdmin,Admin")]` on selected endpoints.

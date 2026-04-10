# RBAC v2 Rollout Phase 4 – Full module coverage

**Date:** 2026-03-09  
**Status:** Phase 4 – Complete permission enforcement

---

## 1. Module coverage audit

### OrdersController
| Endpoint | Current protection | Department scope | Recommended permission |
|----------|--------------------|------------------|-------------------------|
| GET /api/orders | Policy "Orders" | Yes (query/header) | orders.view |
| GET /api/orders/paged | Policy "Orders" | Yes | orders.view |
| GET /api/orders/{id} | Policy "Orders" | Yes | orders.view |
| GET /api/orders/{id}/profitability, payout-breakdown, payout-snapshot, financial-alerts, onu-password, status-logs, reschedules, materials/* | Policy "Orders" | Yes | orders.view |
| POST /api/orders, PUT /api/orders/{id}, DELETE /api/orders/{id} | Policy "Orders" | Yes | orders.edit |
| POST status, notes, financial-alerts/*, materials/usage | Policy "Orders" | Yes | orders.edit |
| GET blocker-reasons | Policy "Orders" | Yes | orders.view |

### ReportsController
| Endpoint | Current protection | Department scope | Recommended permission |
|----------|--------------------|------------------|-------------------------|
| GET definitions, definitions/{key} | [Authorize] | No | reports.view |
| POST {reportKey}/run | [Authorize] | Yes | reports.view |
| GET stock-summary/export, orders-list/export, ledger/export, materials-list/export, scheduler-utilization/export | [Authorize] | Yes | reports.export |

### InventoryController
| Endpoint | Current protection | Department scope | Recommended permission |
|----------|--------------------|------------------|-------------------------|
| GET materials, materials/{id}, by-barcode, stock, movements, ledger, stock-summary, reports/* | Policy "Inventory" | Yes | inventory.view |
| GET reports/*/export, materials/export, materials/template | Policy "Inventory" | Yes | inventory.view (export = view for read; or inventory.edit for export) |
| POST materials, PUT/DELETE materials, POST movements, receive, transfer, allocate, issue, return, materials/import, reports/export/schedule | Policy "Inventory" | Yes | inventory.edit |

### BackgroundJobsController
| Endpoint | Current protection | Department scope | Recommended permission |
|----------|--------------------|------------------|-------------------------|
| GET health, summary | Policy "Jobs" | No | jobs.view |

### Settings (multiple controllers)
Controllers using Policy "Settings" or under settings routes: GlobalSettingsController, IntegrationSettingsController, OrderTypesController, BusinessHoursController, etc. Use settings.view for GET, settings.edit for POST/PUT/DELETE where applicable. Phase 4 applies permission to the main entry points; individual settings controllers can be updated incrementally.

---

## 2. Permission mapping

- **Catalog additions:** reports.export, settings.edit, jobs.view, jobs.run (jobs.run for future trigger/run endpoint).
- **Existing:** orders.view, orders.edit, reports.view, inventory.view, inventory.edit, settings.view.

---

## 3. Backend enforcement (implemented)

- **OrdersController:** All GETs use `[RequirePermission(PermissionCatalog.OrdersView)]`; all POST/PUT/DELETE use `[RequirePermission(PermissionCatalog.OrdersEdit)]`. Policy "Orders" retained.
- **ReportsController:** GET definitions and run use `ReportsView`; all export endpoints use `ReportsExport`.
- **InventoryController:** All GETs (materials, stock, movements, ledger, reports, export, template) use `InventoryView`; all POST/PUT/DELETE (materials CRUD, movements, receive, transfer, allocate, issue, return, import, reports/export/schedule) use `InventoryEdit`.
- **BackgroundJobsController:** GET health and summary use `JobsView`.
- **GlobalSettingsController / IntegrationSettingsController:** GETs use `SettingsView`; POST/PUT/DELETE and test endpoints use `SettingsEdit`.

Department scope is unchanged: department-scoped endpoints still use `ResolveDepartmentScopeOrFailAsync` or equivalent; permission attributes are in addition to existing policies.

## 4. Frontend alignment (implemented)

- **Sidebar:** Reports Hub uses `reports.view`; Background Jobs uses `jobs.view`; Orders, Inventory, Settings already use `orders.view`, `inventory.view`, `settings.view`.
- **Orders list:** Create Order and Import visible only when user has `orders.edit` (or SuperAdmin/Admin fallback when permissions not loaded).
- **Report runner:** Export button visible only when user has `reports.export`.
- **Inventory list:** Add Material visible only when user has `inventory.edit`.
- **Background jobs page:** Access gated by `jobs.view` (or SuperAdmin/Admin).
- **Settings:** `SettingsProtectedRoute` allows access when user has `settings.view` or legacy roles (SuperAdmin, Admin, Director, HeadOfDepartment, Supervisor).

## 5. Seed and tests

- **Seed:** Admin role receives all permissions whose names start with `orders.`, `reports.`, `inventory.`, `jobs.`, or `settings.` (in addition to existing admin.*, payout.*, rates.*, payroll.*).
- **TestUserPermissionProvider (Testing):** Admin role is granted the same prefixes so integration tests pass.
- **Tests:** `RbacPermissionEnforcementTests` include Phase 4: orders paged, reports definitions, reports export, inventory materials, background-jobs health, global-settings; SuperAdmin 200, Member 403, Admin 200 (with test provider). `PermissionCatalogTests` assert Phase 4 permissions are in the catalog.

## 6. Permissions per module (Phase 4)

| Module        | View permission  | Edit / other permission |
|---------------|------------------|--------------------------|
| Orders        | orders.view      | orders.edit              |
| Reports       | reports.view     | reports.export           |
| Inventory     | inventory.view   | inventory.edit           |
| Background jobs | jobs.view      | jobs.run (reserved)      |
| Settings      | settings.view    | settings.edit            |

## 7. Department-scoped modules

- **Orders:** Department scope (query/header); ResolveDepartmentScopeOrFailAsync used where applicable.
- **Reports:** Definitions are global; run and export are department-scoped (departmentId).
- **Inventory:** Department-scoped where applicable (materials, stock, movements, ledger).

## 8. How to add permission checks

1. **Backend:** Add `[RequirePermission(PermissionCatalog.ModuleAction)]` on the action; keep existing `[Authorize(Policy = "…")]` if present. For new permissions, add the constant to `PermissionCatalog` and include it in `AllOrdered` and `ByModuleDict`.
2. **Seed:** In `SeedRolePermissionsAsync`, ensure Admin (and any other default role) receives the new permission or prefix (e.g. `name.StartsWith("module.", StringComparison.OrdinalIgnoreCase)`).
3. **Frontend:** In `Sidebar.tsx`, set `permission: 'module.action'` on the nav item. For action buttons, use `user?.permissions?.includes('module.action')` (with SuperAdmin and optional Admin fallback when `permissions.length === 0`). For route guards, use `ProtectedRoute` with `requiredPermission` or a dedicated guard (e.g. `SettingsProtectedRoute`) that checks the permission or legacy roles.

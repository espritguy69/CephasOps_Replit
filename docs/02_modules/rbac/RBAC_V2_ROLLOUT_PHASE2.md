# RBAC v2 Rollout Phase 2 – Coverage Audit and Rollout

**Date:** 2026-03-09  
**Status:** Rollout Phase 2

---

## 1. Coverage audit (Phase 1)

### 1.1 Module → Controller mapping

| Module | Controller(s) | Current protection | Already RequirePermission |
|--------|----------------|--------------------|---------------------------|
| **Admin users** | AdminUsersController | [Authorize(Roles = "SuperAdmin,Admin")] | List, GetById: AdminUsersView; ResetPassword: AdminUsersResetPassword |
| **Admin users** | AdminUsersController | (same) | Create, Update, SetActive, SetRoles: **none** → need AdminUsersEdit |
| **Admin security** | AdminSecuritySessionsController | [Authorize(Roles = "SuperAdmin,Admin")] | GET: AdminSecurityView; Revoke: AdminSecuritySessionsRevoke |
| **Admin security** | LogsController | [Authorize]; security-activity & security-alerts: [Authorize(Roles = "SuperAdmin,Admin")] | **none** → AdminSecurityView |
| **Admin roles** | AdminRolesController | [Authorize(Roles = "SuperAdmin,Admin")] | **none** (optional: AdminRolesView/Edit) |
| **Admin general** | AdminController | [Authorize] | **none** (reindex, cache flush, health – keep role or add admin.view) |
| **Payout** | PayoutHealthController | [Authorize] | **none** → payout.health.view (read), payout.anomalies.review (ack/assign/resolve) |
| **Rates** | RatesController | [Authorize] | **none** → rates.view (read), rates.edit (mutations) |
| **Payroll** | PayrollController | [Authorize] | **none** → payroll.view (read), payroll.run (create run, finalize, mark-paid, create period) |
| **Orders** | OrdersController | [Authorize(Policy = "Orders")] | **none** (hybrid for later) |
| **Reports** | ReportsController, PnlController, ReportDefinitionsController | [Authorize] / Policy Reports | **none** (reports.view later) |
| **Inventory** | InventoryController | [Authorize(Policy = "Inventory")] | **none** (inventory.view later) |

### 1.2 Coverage table (priority order)

| Module | Endpoint / page | Current protection | Recommended permission | Priority |
|--------|------------------|--------------------|------------------------|----------|
| Admin users | POST/PUT/PATCH/PUT roles | Roles | admin.users.edit | 1 |
| Admin security | GET logs/security-activity, security-alerts | Roles | admin.security.view | 1 |
| Payout | GET dashboard, anomaly-summary, anomalies, etc. | [Authorize] | payout.health.view | 2 |
| Payout | POST anomalies acknowledge/assign/resolve/comment | [Authorize] | payout.anomalies.review | 2 |
| Rates | GET ratecards, gpon/* (read) | [Authorize] | rates.view | 3 |
| Rates | POST/PUT/DELETE ratecards, gpon partner/si rates | [Authorize] | rates.edit | 3 |
| Payroll | GET periods, runs, earnings | [Authorize] | payroll.view | 4 |
| Payroll | POST periods, POST runs, finalize, mark-paid | [Authorize] | payroll.run | 4 |
| Admin roles | GET/PUT roles, permissions | Roles | admin.roles.view, admin.roles.edit | 5 |
| Background jobs | GET health, trigger (if any) | Policy Jobs | admin.view (or keep policy) | 5 |

### 1.3 Frontend (sidebar / pages)

- Sidebar already uses permission strings (e.g. payout-health has no permission; Reports Hub has no permission; Payroll has payroll.view).
- Payout Health / Payout Anomalies: add permission payout.health.view.
- Admin/security pages: already admin.view; backend will enforce admin.security.view for logs.
- Rate management pages: align with rates.view / rates.edit where applicable.
- Payroll: already payroll.view; align run actions with payroll.run.

---

## 2. Permission mapping (Phase 2)

- **Existing in catalog:** admin.users.view, admin.users.edit, admin.users.reset-password, admin.security.view, admin.security.sessions.revoke, admin.roles.view, admin.roles.edit, payout.health.view, payout.repair.run, rates.view, rates.edit, payroll.view, payroll.run, orders.view, orders.edit, reports.view, inventory.view, admin.view, settings.view, etc.
- **Add:** payout.anomalies.review (anomaly acknowledge, assign, resolve, comment).
- **inventory.edit:** add only if we protect inventory mutations in this phase (optional; can defer).

---

## 3. Backend enforcement rollout (Phase 3)

- AdminUsersController: add [RequirePermission(AdminUsersEdit)] on Create, Update, SetActive, SetRoles.
- LogsController: add [RequirePermission(AdminSecurityView)] on GetSecurityActivity, GetSecurityAlerts.
- PayoutHealthController: add [RequirePermission(PayoutHealthView)] on GET dashboard, anomaly-summary, anomalies, anomaly-clusters, anomaly-reviews, GetAnomalyReview; add [RequirePermission(PayoutAnomaliesReview)] on POST acknowledge, assign, resolve, false-positive, comment.
- RatesController: add [RequirePermission(RatesView)] on GET ratecards, gpon read endpoints; add [RequirePermission(RatesEdit)] on POST/PUT/DELETE ratecards and gpon rates.
- PayrollController: add [RequirePermission(PayrollView)] on GET periods, runs, earnings, si-rate-plans; add [RequirePermission(PayrollRun)] on POST periods, POST runs, finalize, mark-paid.
- AdminRolesController: add [RequirePermission(AdminRolesView)] on GET list and GET permissions; add [RequirePermission(AdminRolesEdit)] on PUT permissions.

---

## 4. Frontend alignment (Phase 4)

- Sidebar: Payout Health / Payout Anomalies → permission payout.health.view.
- Security Activity page: backend already enforces admin.security.view; frontend can keep admin.view or use admin.security.view for consistency.
- Role Permissions page: admin.roles.view or admin.view.
- Payroll: show Run / Finalize / Mark paid only when user has payroll.run (if we have that in user.permissions).
- Rates: show edit/create/delete when user has rates.edit.

---

## 5. Safety and defaults (Phase 5)

- Seed: ensure Admin role has all new permissions (payout.health.view, payout.anomalies.review, rates.view, rates.edit, payroll.view, payroll.run, admin.roles.view, admin.roles.edit). Already seeded admin.*; add payout.*, rates.*, payroll.* to Admin in seed or via Role Permission matrix default.
- SuperAdmin: continues to bypass (handler unchanged).
- Do not remove [Authorize(Roles = "SuperAdmin,Admin")] from admin controllers; add permission on top.

---

## 6. Phase 3 – Department scope (2026-03-09)

- See [RBAC_V2_ROLLOUT_PHASE3_DEPARTMENT_SCOPE](RBAC_V2_ROLLOUT_PHASE3_DEPARTMENT_SCOPE.md). Permission + department scope model; reusable controller helpers (ResolveDepartmentScopeOrFailAsync); payroll si-rate-plans use helper; tests and docs updated.

---

## 7. Remaining backlog (post–Phase 2)

- Orders: add orders.view / orders.edit to OrdersController (hybrid with policy).
- Reports: add reports.view to ReportsController, PnlController, ReportDefinitionsController.
- Inventory: add inventory.view (and optionally inventory.edit) to InventoryController.
- Background jobs: add RequirePermission(admin.view) or keep Policy "Jobs" only.
- Frontend: finer-grained button visibility (e.g. “Run payroll” only when payroll.run).

---

## 8. Rollout note (Phase 2 complete)

- **Permission-enforced in Phase 2:** Admin users (admin.users.view, admin.users.edit, admin.users.reset-password), Admin security (admin.security.view, admin.security.sessions.revoke), Logs security-activity/alerts (admin.security.view), Admin roles (admin.roles.view, admin.roles.edit), Payout health dashboard and all anomaly GET/POST (payout.health.view, payout.anomalies.review), Rates – all ratecards and GPON partner/SI/custom rates GET and mutations (rates.view, rates.edit), Payroll – periods, runs, earnings, si-rate-plans and mutations (payroll.view, payroll.run).
- **Still hybrid (role or policy only):** Orders (Policy "Orders"), Inventory (Policy "Inventory"), Reports (Policy "Reports"), Background jobs (Policy "Jobs"), Settings (Policy "Settings"), general [Authorize] controllers.
- **How to add a permission check:** Add [RequirePermission(PermissionCatalog.SomePermission)] on the action; ensure permission is in PermissionCatalog and seeded for Admin (DatabaseSeeder gives Admin all admin.*, payout.*, rates.*, payroll.*); update frontend sidebar or buttons to use the same permission (and role fallback for payout/rates/payroll in Sidebar hasPermission).

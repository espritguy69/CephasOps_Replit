
# RBAC_MODULE.md
Role-Based Access Control (RBAC) for CephasOps – Full Architecture

---

## 1. Purpose

This module defines **who can see and do what** inside CephasOps across:

- Multiple companies:
  - Cephas Sdn. Bhd (ISP)
  - Cephas Trading & Services (ISP)
  - Kingsman Classic Services (Barbershop & Spa)
  - Menorah Travel & Tours (Travel)
- Multiple modules:
  - Orders, Scheduler, Inventory & RMA, Billing, P&L, Payroll, Settings, Email Parser, etc.
- Multiple apps:
  - Web admin (backoffice)
  - SI mobile/PWA app

RBAC must be:

- **Configurable** (no hard-coded roles in code).
- **Company-aware** (who can see which company).
- **Module-aware** (who can access which module).
- **Action-aware** (who can do what in that module).

> This is documentation only; it describes entities, rules, and flows. Current enforcement is below.

### Current implementation (single-company, department-scoped)

- **Roles:** JWT carries roles (e.g. SuperAdmin, Admin, Director, HeadOfDepartment, Supervisor, Member). Department membership is in DB (`DepartmentMembership`); resolved at runtime by `DepartmentAccessService`.
- **Department scope:** All department-scoped endpoints use `ResolveDepartmentScopeAsync(departmentId ?? requestContext.DepartmentId)` or `EnsureAccessAsync(departmentId)`. Requesting a department the user does not belong to returns **403**.
- **Coverage:** Orders, Inventory, Scheduler, Departments, Skills, Payroll, BillingRatecard, BusinessHours, ServiceInstallers, OrderTypes, BuildingTypes, SplitterTypes, ApprovalWorkflows, SlaProfiles, AutomationRules, AgentMode, Users, EscalationRules, Tasks, Pnl (list/export/detail). See **docs/RBAC_MATRIX_REPORT.md** for the full matrix and partial endpoints (Assets, Warehouses, Pnl summary).

---

## 2. RBAC Concepts

We use a classic multi-layer model:

- **User** → belongs to one or more **Companies**.
- **Role** → a named set of permissions (e.g. `CompanyAdmin`, `Scheduler`, `Warehouse`).
- **Permission** → a single capability (e.g. `orders.view`, `orders.assign`, `inventory.issue_to_si`).
- **RoleAssignment** → grants a Role to a User within a Company.

This allows:

- One user to be **Director** for all companies.
- Same user to be just **Viewer** for another company.
- Service Installer to be **SI** in one company and have **No access** to others.

---

## 3. Example Roles

These are **reference roles** you will probably configure in Settings → RBAC:

1. **SystemOwner**
   - Highest authority.
   - Can see all companies, all modules, manage roles & users.

2. **CompanyDirector**
   - Full access to one or more companies.
   - Can see:
     - All dashboards (Orders, P&L, Billing, Payroll, Inventory).
     - All reports.
   - Cannot change technical/internal settings unless explicitly granted.

3. **CompanyAdmin**
   - Day-to-day operational admin for one company.
   - Can:
     - Manage orders.
     - Manage SI profiles.
     - Manage schedule.
     - Manage inventory operations.
     - Manage billing submissions & track payments.
   - Limited access to sensitive global settings.

4. **Scheduler / Operations Controller**
   - Focused on:
     - Orders
     - Scheduler
     - Status changes (Pending → Assigned → On the Way → etc.)
   - Cannot change:
     - Billing records.
     - P&L or finance settings.
   - Read-only for SI rates and KPIs.

5. **Warehouse / Inventory Manager**
   - Access:
     - Inventory & RMA module.
     - Stock movements (partner → warehouse, warehouse → SI, returns, RMA).
   - Can:
     - Manage serialised/non-serialised material.
     - Update splitter usage (with approvals for standby ports).
   - Read-only:
     - Orders context for job material requirements.

6. **Finance / Billing Officer**
   - Access:
     - Billing & Finance
     - P&L views (company-level)
   - Can:
     - Generate invoices.
     - Record Submission IDs.
     - Handle rejections & re-uploads.
     - Record payments.
   - Read-only:
     - Orders statuses, Inventory cost snapshots, Payroll outcomes.

7. **Payroll / HR**
   - Access:
     - Payroll module
     - SI roster, employment status, SI rates.
   - Can:
     - Compute SI pay.
     - Generate payroll exports.
   - Read-only:
     - High-level P&L impact (without partner/customer details if needed).

8. **Service Installer (In-House)**
   - Access via SI Mobile App:
     - Assigned orders/tasks.
     - Own schedule & availability.
     - Material in-hand.
     - Scan serials (incoming/outgoing).
     - Capture photo proof (with time + GPS).
   - Cannot:
     - View global dashboards.
     - Change billing or rates.
     - See other SI’s pay.

9. **Service Installer (Subcon)**
   - Similar to in-house SI with more restricted access:
     - Only see:
       - Their own jobs.
       - Their own performance dashboard (completion, KPI).
     - Cannot:
       - See sensitive financial details (partner rates, P&L).
       - Adjust or view stock beyond their own issued material.

10. **ReadOnly / Auditor**
    - Can:
      - View reports and logs.
    - Cannot:
      - Modify any operational or financial data.

These roles are **configurable** and serve as standard presets.

---

## 4. Data Model (Conceptual)

### 4.1 User

- `Id`
- `Name`
- `Email`
- `Login`
- `IsActive`
- `DefaultCompanyId`
- `CreatedAt`

*(SI users may also carry references to SI profile records.)*

### 4.2 Role

- `Id`
- `Name` (e.g. `CompanyAdmin`, `Scheduler`, `WarehouseManager`)
- `Description`
- `IsSystemRole` (predefined roles locked by system)
- `IsCompanyScoped` (true/false)

### 4.3 Permission

- `Id`
- `Key` (string, e.g. `orders.view`, `orders.create`, `scheduler.assign`, `inventory.issue_to_si`)
- `Description`
- `Module` (Orders, Scheduler, Inventory, Billing, etc.)

### 4.4 RolePermission

- `Id`
- `RoleId`
- `PermissionId`

Defines which permissions a Role has.

### 4.5 UserRoleAssignment

- `Id`
- `UserId`
- `RoleId`
- `CompanyId`
- `IsActive`
- `ValidFrom`
- `ValidTo` (optional)
- `AssignedByUserId`

This is how we apply roles on a **per-company basis**.

---

## 5. Permission Taxonomy (Examples)

We model permissions at a granular level so roles can be composed.

### 5.1 Orders

- `orders.view`
- `orders.create_manual`
- `orders.edit`
- `orders.change_status`
- `orders.reschedule`
- `orders.view_history`
- `orders.delete` (rare / restricted)

### 5.2 Scheduler

- `scheduler.view_calendar`
- `scheduler.assign_si`
- `scheduler.update_status_from_calendar`
- `scheduler.manage_capacity`

### 5.3 Inventory & RMA

- `inventory.view`
- `inventory.manage_materials`
- `inventory.issue_to_si`
- `inventory.record_return_from_si`
- `inventory.manage_rma_requests`
- `inventory.update_splitter_usage`
- `inventory.use_standby_port` (requires approval attachment; permission ensures only certain roles can confirm)

### 5.4 Billing & Finance

- `billing.view`
- `billing.create_invoice`
- `billing.generate_invoice_pdf`
- `billing.record_submission_id`
- `billing.mark_rejected`
- `billing.correct_and_reupload`
- `billing.record_payment`

### 5.5 P&L

- `pnl.view_summary`
- `pnl.view_detailed_by_order`
- `pnl.manage_overheads`
- `pnl.recalculate`

### 5.6 Payroll

- `payroll.view`
- `payroll.manage_si_rates`
- `payroll.compute_payroll`
- `payroll.export_payroll`

### 5.7 Email Parser

- `emailparser.view_logs`
- `emailparser.manage_rules`
- `emailparser.retry_parse`
- `emailparser.mark_as_ignored`

### 5.8 Settings & Master Data

- `settings.view`
- `settings.manage_companies`
- `settings.manage_partners`
- `settings.manage_buildings`
- `settings.manage_materials`
- `settings.manage_cost_centres`
- `settings.manage_notifications`
- `settings.manage_kpi_rules`
- `settings.manage_rbac`

---

## 6. Enforcement Rules

Every incoming request must:

1. Determine user identity.
2. Determine active company context.
3. Check if user has a `UserRoleAssignment` in that company (or global role).
4. Resolve all permissions from roles.
5. Authorise based on required permission for endpoint.

Conceptually:

```text
IF user is SystemOwner:
    allow all
ELSE:
    companyId = activeCompany
    roles = UserRoleAssignments where CompanyId = companyId and IsActive
    permissions = union of RolePermission for those roles
    IF requiredPermission in permissions:
        allow
    ELSE:
        deny
```

---

## 7. UI Behaviour

### 7.1 Menu & Page Visibility

Frontend should hide/show menu items based on permissions:

- If user lacks `billing.view`:
  - Hide Billing menu.
- If user lacks `inventory.view`:
  - Hide Inventory menu.
- For SI mobile app:
  - Only show:
    - Next jobs
    - My materials
    - My performance

### 7.2 Field-Level Restrictions

Certain fields should be hidden or read-only depending on role:

- SI rates (sensitive) → only Payroll/Director.
- Partner rate cards → only Finance/Director/Admin.
- P&L detail view per partner → Directors / Finance.

---

## 8. Audit Logging

RBAC changes must be auditable:

- When roles are assigned/removed:
  - Log who changed what and when.
- When permissions in a role are changed:
  - Keep previous vs new snapshots (for compliance and debugging).

Example:

- If someone modifies Scheduler’s authority to change statuses, we must know:
  - Who added `orders.change_status` to `Scheduler` on a given date.

---

## 9. Multi-Company & Vertical Effects

### 9.1 Multi-Company Access

A user might:

- Have `CompanyAdmin` on **Cephas Sdn. Bhd only**.
- Have `ReadOnly` on **Cephas Trading**.
- Have no role on **Kingsman** or **Menorah**.

The UI must:

- Filter the **company switcher** to only those companies where the user has at least one active role, except for SystemOwner/Director who see all.

### 9.2 Vertical-Specific Modules

Certain roles make sense only in certain verticals:

- ISP:
  - Orders, Scheduler, Inventory & RMA, Assurance, Splitters.
- Barbershop:
  - POS, appointments, product inventory.
- Travel:
  - Tours, itineraries, bookings.

The multi-company + vertical design ensures:

- RBAC is reusable, but **modules allowed per company** are defined at Company + Vertical level.

---

## 10. SI Mobile App Security

For SI app:

- Use a reduced set of endpoints with focused permissions (e.g. `si.view_my_jobs`, `si.update_job_status`, `si.scan_materials`).
- SI tokens must **never** be able to call admin endpoints.
- In addition to role checks, always enforce that:
  - `ServiceInstallerId` in job matches the logged-in SI.
  - SI can only see their own KPIs and not others.

---

## 11. Notes for Cursor / Dev Implementation

- Implement RBAC as a **central service** usable by all modules.
- Keep permission keys consistent and documented.
- Avoid scattering ad-hoc checks; instead:
  - Decorate controllers/endpoints with required permission keys.
- Make RBAC configurable in Settings UI (later).
- Seed default roles and permissions for:
  - SystemOwner, CompanyDirector, CompanyAdmin, Scheduler, Warehouse, Finance, Payroll, SI, Subcon.

---

## 12. Summary

This RBAC module ensures:

- **Security** — Only the right people can see/update sensitive data (billing, SI rates, P&L).
- **Clarity** — Each role is clear (Admin, Scheduler, Warehouse, Finance, SI, etc.).
- **Scalability** — Easily extend to new companies, branches, verticals.
- **Future-Proofing** — No need to change core code when roles evolve; configuration-driven.

It ties together with all other modules to give you a safe, structured foundation for CephasOps across all your businesses.

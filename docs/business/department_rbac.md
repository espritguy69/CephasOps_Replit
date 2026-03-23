# Department Responsibilities & RBAC Model

**Related:** [Product overview](../overview/product_overview.md) | [Process flows](process_flows.md) | [02_modules/department/OVERVIEW](../02_modules/department/OVERVIEW.md) | [RBAC_MATRIX_REPORT](../RBAC_MATRIX_REPORT.md) | [02_modules/rbac/OVERVIEW](../02_modules/rbac/OVERVIEW.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. Department-based responsibilities (inferred)

| Department type | Responsibilities |
|-----------------|------------------|
| **Operations / GPON** | Order intake (parser + manual); order approval and enrichment; building/address setup; SI assignment; appointment setting; docket receipt and verification; docket upload to partner portal; readiness for invoice (BOQ, rate, materials). KPIs: assignment accuracy, docket verification, reschedule quality. |
| **Field / Installers (SI)** | Receive assigned jobs; update status (On the way, Met customer, Completed); report blockers or reschedules with evidence; submit completion package (splitter, port, ONU, photos, signature); correct dockets when rejected. KPIs: punctuality, completion accuracy. |
| **Inventory / Warehouse** | Receive stock; allocate to orders; issue/return; transfers; ledger and stock-by-location; low-stock alerts. Department-scoped. |
| **Finance / Billing** | Create invoice from ready orders; generate PDF; submit to MyInvois; track submission and rejections; record payment and match to order. KPI: billing accuracy. |
| **Finance / Payroll** | SI rate plans (job type, level, KPI); payroll periods and runs; earnings calculation; export for accounting/bank. |
| **Finance / P&L** | View revenue vs materials and SI cost; overhead entry; reports by partner, order type, period. |
| **Settings / Admin** | Departments, partners, buildings, order types/categories, installation methods, splitters, business hours, SLA, workflow definitions, document templates, KPI profiles, rate cards. |

---

## 2. RBAC model

- **Login:** JWT after credential check; refresh token supported. If user has MustChangePassword (e.g. after admin reset with “require password change on next login”), login/refresh return 403 and user must complete change-password flow before accessing the app.
- **Roles (from code):** SuperAdmin, Admin, Director, HeadOfDepartment, Supervisor, Member.
- **Department membership:** Stored in DB (DepartmentMembership); resolved at runtime. Users only access data for departments they belong to; otherwise 403.
- **SuperAdmin:** Can access without department restriction where implemented.
- **Policies:** Orders, Inventory, Reports (authenticated user); Jobs (SuperAdmin, Admin); Settings (SuperAdmin, Admin, Director, HeadOfDepartment, Supervisor); **User Management** (SuperAdmin, Admin only; `api/admin/users` and `/admin/users` in the app). User create/edit includes department membership assignment (multi-select and per-department role); actions are audited.
- **Department context:** Frontend sends X-Department-Id (or query departmentId); backend resolves via ResolveDepartmentScopeAsync; requesting another department → 403.
- **Overrides:** Only HOD/SuperAdmin can override certain protections (e.g. Blocker → Completed) with reason and evidence.

---

## 3. Single-company scope

- One company context; multiple departments within it.
- All department-scoped endpoints (Orders, Inventory, Scheduler, Departments, Skills, Payroll, BillingRatecard, BusinessHours, ServiceInstallers, OrderTypes, BuildingTypes, SplitterTypes, ApprovalWorkflows, SlaProfiles, AutomationRules, AgentMode, Users, EscalationRules, Tasks, Pnl, etc.) enforce department access; 403 when not allowed.

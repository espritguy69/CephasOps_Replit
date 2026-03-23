# DEPARTMENT_ENTITIES.md

## Department Module – Entities

The Department module models **internal functional units** within each company. It extends the standard `companyId` boundary with a finer-grained `departmentId` so ownership, KPIs, and access control can be enforced per team.

Departments are used for:

- Access control (dashboards, queues, approvals)
- Workflow ownership (which team handles which step)
- KPI & SLA tracking per department
- Material & billing handovers
- Translating global settings (KPI defaults, max concurrent jobs) into team-specific limits

Example departments: `OPS`, `INSTALLER`, `WAREHOUSE`, `FINANCE`, `BILLING`, `CUSTOMER_SERVICE`, `PROJECT`, `QA_QC`. Each company decides its own structure, but every record remains company-scoped.

---

## 1. Department

Represents a functional unit inside a company.

### 1.1 Department

- `Department`
  - `id` (UUID)
  - `companyId` (FK → Company)
  - `code` (uppercase string; e.g. `OPS`, `FIN`, `INST`)
  - `name` (e.g. `Operations`, `Finance`, `Warehouse`, `Installer Team`)
  - `description` (optional)
  - `email` (optional shared inbox for escalations/handoffs)
  - `parentDepartmentId` (nullable FK → Department for nested org charts)
  - `defaultLeadUserId` (nullable FK → User, used when auto-assigning tasks)
  - `maxConcurrentOrders` (nullable int; Scheduler/Workflow can block new work once the limit is hit)
  - `isActive` (bool)
  - `createdAt`, `createdByUserId`
  - `updatedAt`, `updatedByUserId`

#### Constraints

- Unique (`companyId`, `code`).
- `code` is uppercase A–Z, 1–10 characters.
- `parentDepartmentId`, if not null, must reference another Department inside the same `companyId`.

---

## 2. Department Members

Links Users to Departments and defines their departmental role. This augments global RBAC (roles/permissions) with departmental context.

### 2.1 DepartmentMember

- `DepartmentMember`
  - `id`
  - `companyId` (FK → Company; stored for quick filtering)
  - `departmentId` (FK → Department)
  - `userId` (FK → User)
  - `role` (string; e.g. `Manager`, `Lead`, `Staff`, `Viewer`)
  - `isManager` (bool)
  - `joinedAt`
  - `leftAt` (nullable)
  - `createdAt`, `createdByUserId`
  - `updatedAt`, `updatedByUserId`

Department membership does *not* replace global roles (like `SuperAdmin`). It **adds** another scope dimension so dashboards, alerts, and approvals can focus on “my department” views.

---

## 3. Department Workflow Rules

Defines which department owns which part of an order/job flow.

### 3.1 DepartmentWorkflowRule

- `DepartmentWorkflowRule`
  - `id`
  - `companyId` (FK → Company)
  - `departmentId` (FK → Department)
  - `orderTypeId` (FK → OrderType catalog)
  - `statusCode` (string; e.g. `ASSIGNED`, `ON_THE_WAY`, `READY_FOR_BILLING`)
  - `priority` (int; lower numbers evaluated first)
  - `isFallback` (bool; used when no specific rule matches)
  - `conditionsJson` (JSON; optional filters such as partner, building type, SLA flag)
  - `effectiveFrom` / `effectiveTo` (nullable DateTimes for phased rollouts)
  - `createdAt`, `createdByUserId`
  - `updatedAt`, `updatedByUserId`

**Usage:**

- Workflow Engine resolves `(companyId, orderTypeId, statusCode)` → owning `departmentId` whenever an order status changes.
- If multiple rules match, the lowest `priority` wins; if none match, the engine uses the fallback rule or the company default (`GlobalSettings` → `DefaultDepartmentForNewOrders`).
- Optional `conditionsJson` enables advanced matching (e.g. only for `Partner = TIME`, `BuildingType = FTTO`).

---

## 4. Department KPI / Limits (Optional)

Future-proofing for per-department SLA/KPI tracking. These can live in `DepartmentKpiConfig` tables or `GlobalSettingsService` JSON payloads:

- `DepartmentKpiDefaults` – structured JSON storing SLA targets by status transitions.
- `DepartmentMaxPerCompany` – numeric limit for how many departments a company can configure.
- `Department.maxConcurrentOrders` (entity field) – hard cap per department for active workload.

---

## 5. Links to Other Modules

These are **fields added to other entities**, not separate tables.

### 5.1 Orders

- `Order.currentDepartmentId` (FK → Department)
- `Order.previousDepartmentId` (nullable FK → Department)

Used to filter orders by department, show “Who owns this order now?”, and rebuild KPI timelines.

### 5.2 Material Movements

- `MaterialMovement.fromDepartmentId` (nullable)
- `MaterialMovement.toDepartmentId` (nullable)

Examples: Warehouse → Installer, Installer → Warehouse (returns), Installer → Scrap.

### 5.3 Billing & Payout

- `BillingRecord.departmentId` (optional; e.g. FINANCE)
- `InstallerPayoutRecord.departmentId` (e.g. FINANCE or OPS, depending on who handles payouts)

### 5.4 Background Jobs / Alerts

- Background jobs can route notifications using `departmentId` → `Department.email` / `defaultLeadUserId`.

---

## 6. Seed / Default Departments

For convenience, a default template per company can be seeded:

- `OPS` – Operations
- `INST` – Installer Team
- `WH` – Warehouse / Material
- `FIN` – Finance
- `BILL` – Billing
- `CS` – Customer Service
- `QA` – QA/QC

These can be edited/disabled at runtime, and Global Settings specify which department is the default when onboarding new orders or background jobs.


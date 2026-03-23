
## 3) `docs/07_frontend/ui/departments.md`

```md
# UI — Departments

This document defines the **Department UI** for CephasOps:

- Managing departments per company
- Assigning users to departments
- Viewing work by department
- Integrating departments into order boards and dashboards

---

## 1. Scope & Navigation

Primary screens:

1. **Department Management**  
   Path: `/settings/departments`
2. **Department Memberships**  
   Path: `/settings/departments/:id/members`
3. **Department Workflow Mapping**  
   Path: `/settings/departments/workflows`
4. **Operational Views by Department**  
   - “My Department Orders”
   - “Department Material Movements”
   - “Department KPIs”

All these screens are **scoped by active `companyId`**.

---

## 2. Department Management Screen

### 2.1 List View

Path: `/settings/departments`

Table columns:

- Code (badge)
- Name
- Description
- Members (count, clickable)
- Active (toggle)
- Actions (Edit, View Members)

Actions:

- **Add Department**
- **Edit Department**
- **Deactivate / Reactivate**

API:

```http
GET /api/companies/{companyId}/departments
POST /api/companies/{companyId}/departments
PUT /api/departments/{id}
2.2 Add/Edit Department Modal
Form fields:

Code (e.g. OPS, FIN)

Name

Description

Sort Order

Active

Validation:

Code required, uppercase, unique per company

Name required

3. Department Membership Screen
Path: /settings/departments/:id/members

3.1 Layout
Left side: Department details
Right side: Members table

Members table:

User Name

Email

Role (Manager / Staff / Viewer)

Primary? (checkbox)

Active (toggle)

Actions (Remove)

Actions:

Add Member (search user by name/email)

Change role

Toggle primary

API:

http
Copy code
GET /api/departments/{id}/members
POST /api/departments/{id}/members
PUT /api/department-memberships/{id}
DELETE /api/department-memberships/{id}
4. Department Workflow Mapping UI
Path: /settings/departments/workflows

This screen maps Order Types + Status Codes to Departments.

4.1 Filters
Order Type (dropdown)

Department (optional filter)

Status Code (search)

4.2 Table
Columns:

Order Type

Status Code

Status Label (friendly label)

Department (dropdown)

SLA Override (hours; optional)

Final Approval (checkbox)

Visible in Board (checkbox)

Inline editing is recommended:

Click department cell → dropdown of available departments

Click SLA cell → edit hours

API:

http
Copy code
GET /api/companies/{companyId}/department-workflow-rules
PUT /api/department-workflow-rules/{id}
POST /api/companies/{companyId}/department-workflow-rules   (for extra rules if needed)
Examples:

FTTH + ASSIGNED → OPS

FTTH + ON_THE_WAY → INSTALLER

FTTH + READY_FOR_BILLING → FINANCE

5. Operational Views by Department
5.1 My Department Orders
Path: /orders/department

Default filter:

currentDepartmentId = any department the current user belongs to.

Tabs or filters:

All

My Department

My Own (assigned) (later if you have assignee per user)

Columns:

Order ID

Customer

Service ID

Status

Current Department (badge)

SLA indicator

Last Update

API:

http
Copy code
GET /api/orders?currentDepartmentId=...
5.2 Order Details Panel
On the Order detail page:

Show Current Department and Previous Department:

text
Copy code
Current Department: INSTALLER
Previous Department: OPS
Optionally show “Department Trail”:

text
Copy code
OPS → INSTALLER → FINANCE
6. Material & Billing Department Views
6.1 Material Movements by Department
Path: /materials/department

Filters:

From Department

To Department

Date range

Use:

http
Copy code
GET /api/material-movements?fromDepartmentId=...&toDepartmentId=...
6.2 Billing / Payout by Department
Path: /billing/department

Filters:

Department

Status

Date Range

Shows:

Billing records owned by departmentId

Payout records owned by departmentId

7. KPIs & Dashboards (Per Department)
Department dashboards:

Cards:

Open Orders (current dept)

Overdue Orders (SLA breached)

Material variances (last 30 days)

Charts:

Time-to-handoff (from this dept to next)

Completion times for orders owned by this dept

API patterns:

http
Copy code
GET /api/kpi/department/{departmentId}/overview
GET /api/kpi/department/{departmentId}/time-series
8. Permissions
ROLE_SUPER_ADMIN

Can manage all companies’ departments, workflows, memberships.

ROLE_COMPANY_ADMIN

Can manage departments for their company only.

ROLE_DEPARTMENT_MANAGER

Can manage memberships inside their department.

Other roles

Read-only views, restricted to their own department(s).

9. Storybook Components
Create Storybook stories for:

<DepartmentList />

With OPS/FIN/WH/INST example

Empty state (no departments yet)

<DepartmentForm />

Add/Edit

Validation errors

<DepartmentMembersTable />

Manager/Staff/Viewer examples

<DepartmentWorkflowTable />

FTTH + status mapping to departments

SLA override example

<MyDepartmentOrders />

Mixed orders with dept badges and SLA markers

Mock data must match:

Department

DepartmentMembership

DepartmentWorkflowRule

Order with currentDepartmentId

10. Non-Goals
The Departments UI does not:

Replace global Role management (RBAC still separate)

Own Order status definitions (that belongs to Orders module)

Define parser behaviour (that’s in Email Parser module)

Its job is:

To give companies a clean way to model their internal teams, link them to workflows, and see work & KPIs by department.
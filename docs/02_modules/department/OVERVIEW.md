\# DEPARTMENT\_MODULE.md

\## Department Module – Functional Specification



The Department Module defines the business logic for modeling internal teams within each Company. It controls ownership of work, workflow transitions, access boundaries, and operational KPIs.



This module sits above the data model and below the UI layer.



---



\## 1. Purpose



Departments represent functional units inside a company such as:



\- OPS (Operations)

\- INSTALLER TEAM

\- WAREHOUSE

\- FINANCE

\- BILLING

\- CUSTOMER SERVICE

\- QA/QC



Each company in CephasOps can define a custom departmental structure.  

Departments allow:



\- Workflow ownership  

\- Access control  

\- SLA/KPI evaluation  

\- Material responsibility tracking  

\- Billing/payout ownership  



This module defines how departments work in the platform.



---



\## 2. Scope of the Module



\### Included:

\- Managing list of departments per company

\- Assigning users to departments

\- Controlling workflow ownership using `DepartmentWorkflowRule`

\- Linking orders to departments (current \& previous owner)

\- Linking material movements to departments

\- Linking billing and payout approval to departments

\- Department-level dashboards and KPIs

\- API endpoints to manage department configurations



\### Not included (belongs to other modules):

\- Role-based access control (RBAC module)

\- Status definitions (Orders module)

\- Entity definitions (05\_data\_model)

\- UI layout/design (07\_frontend)



---



\## 3. Key Responsibilities



\### 3.1 Department Management

\- Create / edit / disable departments

\- Validate department code uniqueness per company

\- Return all departments in a company

\- Provide department lists for dropdowns (for workflow mapping, assignments)



\### 3.2 Department Membership

\- Add/remove users from departments

\- Mark a primary department

\- Sync department membership into access control context

\- Inherit department roles (Manager / Staff / Viewer)



\### 3.3 Department Workflow Rules

Controls which department owns which part of the workflow.



Examples:

\- FTTH + ASSIGNED → OPS

\- FTTH + ON\_THE\_WAY → INSTALLER

\- FTTH + READY\_FOR\_BILLING → FINANCE



Module must:

\- Validate that each `(orderType, statusCode)` maps to exactly one department

\- Expose rule list for UI configuration

\- Compute “current department” during each order status transition



\### 3.4 Department in Orders

When order status changes:

1\. Lookup `(companyId, orderType, statusCode)` → departmentId  

2\. Write to:

&nbsp;  - `Order.currentDepartmentId`

&nbsp;  - `Order.previousDepartmentId`



\### 3.5 Department KPIs \& SLA Evaluation

Handles:



\- Time spent under each department

\- Time-to-handoff metrics (OPS → INSTALLER → FINANCE)

\- Department-level SLA burns



This module publishes events to the KPI engine.



\### 3.6 Departments in Materials

Material movements use:

\- `fromDepartmentId`

\- `toDepartmentId`



Warehouse issuing → installer  

Installer returning → warehouse  

Installer directly consuming → installer dept



The module enforces correct department transitions.



\### 3.7 Departments in Billing \& Payout

Finance department owns:

\- Billing validations  

\- Installer payout approvals  

\- Billing record review queues  



This module ensures billing queues filter by department.



---



\## 4. Integration With Other Modules



\### 4.1 Orders Module

When an order changes status:

\- Orders module triggers department lookup  

\- Department module returns owning department  

\- Orders module sets `currentDepartmentId`



\### 4.2 Materials Module

Material issuance/return calls Department module for:

\- Validating department transition  

\- Tracking responsibility



\### 4.3 Billing Module

Billing queues are filtered by department ownership.



\### 4.4 KPI Module

Department module provides:

\- Workflow rules

\- SLA overrides



KPI module computes:

\- Department SLA

\- Department load

\- SLA breach metrics



---



\## 5. Backend Services



\### 5.1 DepartmentService



Methods:



\- `GetDepartments(companyId)`

\- `CreateDepartment(companyId, payload)`

\- `UpdateDepartment(id, payload)`

\- `DeactivateDepartment(id)`

\- `GetDepartmentMembers(departmentId)`

\- `AddMember(departmentId, userId, role)`

\- `RemoveMember(membershipId)`



\### 5.2 DepartmentWorkflowService



Methods:



\- `GetRules(companyId)`

\- `AssignDepartmentToStatus(companyId, orderType, statusCode, departmentId)`

\- `GetDepartmentForStatus(companyId, orderType, statusCode)`

\- `GetNextDepartment(order, newStatus)`



\### 5.3 DepartmentSlaService



Methods:



\- `ComputeTimeInDepartment(orderId)`

\- `GetDepartmentKpis(departmentId)`

\- `EvaluateSlaBreach(orderId)`



---



\## 6. Events (Internal)



\### Published:

\- `DepartmentOwnershipChanged`

\- `DepartmentMembershipUpdated`

\- `DepartmentWorkflowRuleUpdated`

\- `DepartmentSLAEvaluationRequested`



\### Consumed:

\- `OrderStatusChanged`

\- `MaterialMovementCreated`

\- `BillingRecordCreated`

\- `UserRoleUpdated`



---



\## 7. API Endpoints

GET /api/companies/{id}/departments

POST /api/companies/{id}/departments

PUT /api/departments/{id}

DELETE /api/departments/{id}



GET /api/departments/{id}/members

POST /api/departments/{id}/members

PUT /api/department-memberships/{id}

DELETE /api/department-memberships/{id}



GET /api/companies/{id}/department-workflow-rules

PUT /api/department-workflow-rules/{id}

POST /api/companies/{id}/department-workflow-rules





---



\## 8. Dependencies



\### Required:

\- Company module

\- Orders module

\- Materials module

\- Billing module

\- KPI module

\- RBAC module (for security)

\- User module



\### Optional:

\- Notifications module (if departments own alerts)

\- Multi-branch module



---



\## 9. Non-Goals



This module does \*\*not\*\*:

\- Manage global RBAC

\- Define order status codes

\- Control parser logic

\- Control email intake



Its purpose is:



> \*\*To unify workflow ownership, enforce departmental responsibility, and allow KPI-driven operations.\*\*



---



\## 10. Summary



\- A Department is a core structural element inside each company.

\- Departmental logic touches Orders, Materials, Billing, KPI, and Access Control.

\- This module defines all operational rules regarding departmental ownership.

\- The data model lives in `/05\_data\_model`.

\- UI lives in `/07\_frontend/ui`.

**See also:** [Reference Types & Relationships](../../05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md) — full list of department-scoped types (building types, order types, order categories, installation methods, splitter types) and how they relate to departments and to each other.



🎉 Final Structure (Correct)

02\_modules/

&nbsp;   department\_module.md



05\_data\_model/

&nbsp;   department\_entities.md

&nbsp;   department\_relationships.md



07\_frontend/ui/

&nbsp;   departments.md




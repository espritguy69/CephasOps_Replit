Below is your full, clean, production-ready MULTI\_COMPANY\_ARCHITECTURE.md file.



📌 Place this file here:

cephasops/docs/02\_architecture/MULTI\_COMPANY\_ARCHITECTURE.md



This file is different from MULTI\_COMPANY\_MODULE.md because:



MULTI\_COMPANY\_MODULE.md = module specification (rules, behaviours)



MULTI\_COMPANY\_ARCHITECTURE.md = system-level architecture (how multi-company works at platform level)



This version focuses on architecture, separation layers, tenancy model, request flow, folder structure, and system-wide impact.



You can paste this directly into your repo.



MULTI\_COMPANY\_ARCHITECTURE.md



CephasOps – Multi-Company System Architecture

Version 2.0 (Final)

**⚠️ Current deployment:** CephasOps runs in **single-company mode** (one company, multiple departments). This document is **reference only** for possible future multi-company use.

---

This document defines the architecture-level design for running multiple companies inside one CephasOps platform with strict data isolation, role isolation, security boundaries, and company-aware workflows.



This is a high-level system architecture document, not a business module spec.

**Note:** Current CephasOps deployment runs in **single-company mode** (one company, multiple departments). This document describes the multi-company design for reference or future use.

1\. Architecture Goals



The multi-company architecture must deliver:



✔ Strong data isolation

✔ Strong permission isolation

✔ Shared platform with separate business domains

✔ Per-company custom logic (parser, workflow, billing, payroll)

✔ Separate tax \& e-Invoice settings per company

✔ Unified system logs but isolated visibility

✔ Director-level consolidated analytics without leakage

✔ Zero cross-company contamination



The goal: One platform, multiple businesses, no overlap.



2\. Multi-Tenancy Model



CephasOps uses a Hybrid Single-Database Multi-Tenant Architecture, with:



Shared tables



Company-scoped data using companyId



Strict RBAC enforcement



Company-specific configuration



Company-specific documents and invoices



Company-specific workflows



2.1 Why Hybrid Single DB?



Because it provides:



Lower infrastructure cost



Direct cross-company analytics



Fast development on shared modules



Easier onboarding of new companies



Isolation is enforced at the application layer, not by separate databases.



3\. Core Isolation Layer (Company Boundary)



Every domain entity includes:



companyId: GUID

createdByUserId: GUID

updatedByUserId: GUID





This boundary ensures:



✔ All queries are scoped

✔ All writes are scoped

✔ All workflows are scoped

✔ No global queries allowed unless Director role



This “company boundary” is the most important architectural rule.



4\. System Components Affected by Multi-Company Architecture

+-----------------------------+

|  Authentication \& RBAC      |

+-----------------------------+

|  Settings \& Master Data     |

+-----------------------------+

|  Parser + Email Engine      |

+-----------------------------+

|  Orders \& Workflow Engine   |

+-----------------------------+

|  Scheduler                  |

+-----------------------------+

|  Inventory \& RMA           |

+-----------------------------+

|  Billing + Tax + eInvoice  |

+-----------------------------+

|  Payroll                    |

+-----------------------------+

|  P\&L \& Analytics            |

+-----------------------------+

|  File Storage Layer         |

+-----------------------------+

Each component must honour the company boundary.



Company \& Dept Settings now explicitly manages the Department Layer:

\- It seeds each company's department catalog alongside ratecards, workflows, KPIs, and parser templates.
\- Every `departmentId` is created and audited inside the owning company's settings space—there are no global departments.
\- Downstream modules (Orders, Scheduler, Inventory, Billing, Background Jobs) always receive a `{companyId, departmentId}` tuple before they render or mutate data.



4.1 Department Layer (Within Each Company)



Each company can have multiple departments (e.g. `Operations`, `Scheduler`, `Warehouse`, `Billing`, `Finance`, `Travel`, `Barbershop`).



The Department layer provides:



\- A way to tag entities by `departmentId` where it makes sense:

&nbsp; - Orders (owning / primary department via `currentDepartmentId`, e.g. `ISP Ops`, `Kingsman`, `Menorah`)

&nbsp; - Schedules (which department’s board the job sits in)

&nbsp; - Inventory requests (which department requested materials)

&nbsp; - P\&L / cost centres (departmental breakdown within a company via `DepartmentMaterialCostCenter`)

&nbsp; - Material cost allocation (per department, per material template, with allocation percentages)

\- Visibility control on top of company:

&nbsp; - A user may see \*\*only their department’s\*\* orders and KPIs.

&nbsp; - Directors / Company Admins can see \*\*all departments\*\* within a company.

\- Configuration hooks:

&nbsp; - Per-department SLA / KPI thresholds

&nbsp; - Per-department dashboards and queues

&nbsp; - Per-department cost centre mappings for materials (`DepartmentMaterialCostCenter`)

&nbsp; - Department workflow rules (`DepartmentWorkflowRule`) that determine which department owns an order based on order type and status



Department NEVER crosses company boundary:

\- `departmentId` is always scoped by `companyId`.

\- There is no “global” department shared by multiple companies.

\- All department-related entities (Department, DepartmentMember, DepartmentWorkflowRule, DepartmentMaterialCostCenter) are company-scoped.

**Business Logic Scope:** every service (OrdersService, SchedulerService, BillingService, WorkflowEngine, PnlOrderDetailBuilder, etc.) now executes with a `{companyId, departmentId}` tuple. The companyId filters the data partition, while the departmentId determines the owning queue, KPI timers, visibility, approval logic, and cost centre allocation. If a workflow cannot resolve a valid department it falls back to the company's default rule and refuses to leak data across departments or companies.

**Cost Centre Allocation:** Material costs and revenue are allocated to departments and cost centres using `DepartmentMaterialCostCenter`, which maps `(CompanyId, DepartmentId, MaterialTemplateId)` to a `CostCenterCode` and optional `AllocationPercent`. When multiple departments share a material template, allocation percentages must sum to 100%. This enables accurate P&L reporting at the department and cost centre level.



5\. Authentication \& Authorization Layer

5.1 Users



One user account may belong to multiple companies:



User → CompanyMembership → Roles → Permissions



5.2 Active Company Context



Every session contains:



activeCompanyId

activeRole

permissions





Switching companies reloads:



Sidebar modules



Allowed actions



Data scope



Settings



Workflows



6\. Configuration Isolation



Each company maintains:



Parser templates



Ratecards



Workflow definitions



Document templates



Invoice number sequences



Payroll policies



P\&L allocation rules



Inventory categories



Warehouse structure



Nothing is shared unless explicitly defined.



7\. Multi-Company Request Flow (Backend)

Step 1 — Request In



Every API request includes:



JWT with userId



activeCompanyId from user session



Step 2 — RBAC Validation



System checks:



Is user allowed to access activeCompanyId?

Is user’s role valid for this endpoint?



Step 3 — Data Filtering



ORM/app layer enforces:



companyId = activeCompanyId



Step 4 — Operation Execution



Business logic runs using:



Company-specific settings



Company-specific rules



Company-specific templates



Step 5 — Response



Return result only for that company.



8\. Multi-Company UI Architecture (Frontend)



Frontend receives company context after login.



UI changes dynamically:



Modules shown



Menu items



Colours \& branding (optional)



Document templates



Parser rules



Available SIs



Inventory locations



Billing logic



Example:



If activeCompanyId = Kingsman

 → Hide ISP modules

 → Hide Inventory

 → Hide Scheduler

 → Show POS module (future)



9\. Module-by-Module Isolation Rules



Below is the architecture impact per module.



9.1 Email Parser



Parser templates are company-specific.



TIME parser applies only to Cephas companies.



Kingsman and Menorah do not use parser.



9.2 Orders Module



Company determines:



Order types



Allowed workflows



Service Installer pool



Building logic



Materials allowed



Orders cannot be viewed across companies.



9.3 Scheduler Module



SIs are company-scoped



Calendar is company-scoped



Leave, overtime, and capacity are company-scoped



Cross-company scheduling is forbidden.



9.4 Inventory \& RMA



Each company has:



Separate warehouses



Separate serialised inventory



Separate RMA flows



Separate GRN numbering



Optional:

Controlled stock movement Cephas ↔ Cephas Trading with approval.



9.5 Billing, Tax \& eInvoice



Each company has its own:



Invoice sequences



Ratecards



Tax settings



MyInvois API keys



Billing workflows



Example:



Cephas = SST registered, TIME partner, Telco billing



Kingsman = Retail POS



Menorah = Travel packages



Billing logic is company isolated.



9.6 Payroll Module



Each company has:



Own SIs



Salary structures



KPI rules



Payroll runs



Payment cycles



Payroll cannot mix companies.



9.7 P\&L Module



P\&L aggregates:



Revenue



Material usage



SI labour cost



Overheads



All by company boundary.



A director may view Consolidated Group P\&L.



10\. File Storage Architecture



Files stored by company:



files/{companyId}/{module}/{year}/{month}/{fileId}.pdf





Ensures:



No cross-company leakage



Easy audit



Easy removal if company leaves platform



11\. Logging \& Audit Layer



Each log entry includes:



companyId

entityId

userId

timestamp

action

before/after values





Directors can see aggregated logs.

Staff see only logs belonging to their company.



12\. Architecture Diagram (ASCII)



                     +-----------------------+

                     |   Authentication      |

                     +----------+------------+

                                |

                     +----------v------------+

                     |   Active Company      |

                     |   Context Resolver    |

                     +----------+------------+

                                |

            +------------------+-------------------+

            |                                      |

+-----------v-----------------+      +------------v-------------+

| Company \& Dept Settings     |      |    RBAC \& Permissions   |

+-------------+---------------+      +-------------+-----------+

              |                                     |

              +------------------+------------------+

                                 |

                       +---------v---------+

                       |  Department Layer |

                       | (Dept scopes \&    |

                       |  visibility)      |

                       +---------+---------+

                                 |

                        +--------v--------+

                        |  Business Logic |

                        | (Company + Dept |

                        |   Scoped)       |

                        +--------+--------+

                                 |

            +--------------------+-------------------------+

            |                    |                         |

+-----------v-----+     +--------v--------+      +---------v--------------+

| Orders / Parser |     | Inventory \& RMA |      | Billing / Payroll / P\&L|

+-----------------+     +-----------------+      +------------------------+

            |                      |                         |

            +----------+-----------+-------------------------+

                       |

                +------v-------+

                | File Storage |

                +--------------+



13\. Cross-Company Dashboards (Directors Only)



Directors can access:



Group P\&L



Combined Revenue



Combined Payroll Cost



Combined Inventory Usage



Cross-company KPIs



But no operational cross-access.



14\. Company Onboarding Flow

Create new company

    ↓

Create company settings

    ↓

Upload company templates

    ↓

Create ratecards

    ↓

Assign users to company

    ↓

Assign roles

    ↓

Ready for operations



15\. Folder Structure Recommendation

cephasops/

 ├── docs/

 │    ├── 02\_architecture/

 │    │      └── MULTI\_COMPANY\_ARCHITECTURE.md   <— THIS FILE

 │    ├── 05\_modules/

 │    │      └── MULTI\_COMPANY\_MODULE.md



16\. Summary



The Multi-Company Architecture guarantees:



Isolation of data



Isolation of operations



Isolation of tax, inventory, payroll, billing



Unified platform with modular company-specific behaviour



This is the foundation of CephasOps as a scalable multi-business, multi-vertical system.



✔ End of File


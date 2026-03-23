✅ 1. What “Departmental” Means in CephasOps



A Department represents an internal functional unit inside a company, such as:



Operations (OPS)



Installer Team



Material/Warehouse



Finance



Billing



Customer Service



QA/QC



Project Management



Technical / Network



HR (optional)



Each company can configure its own departmental structure.



This becomes extremely powerful when combined with:



Multi-company



Multi-branch



Role-based access



Workflows



Billing pipeline



🔥 2. Why Departmental Support Is Important

✔ Real-world reflection of Cephas operations



You already have OPS, Finance, Subcon, Material Control, etc.

Each needs different permissions, different views, different dashboards.



✔ Enables department-specific workflows



E.g.:



OPS handles assignment \& reschedules



Installer Team closes orders



Warehouse issues materials



Finance approves payouts



Billing generates invoices



✔ Better UI scoping



A Finance user should not see Installer details.

An Installer should not see financial data.



✔ Enables KPIs per department



Examples:



OPS → job creation delay



Installer → completion SLA



Warehouse → material variance accuracy



Finance → billing cycle duration



✔ Works perfectly with the Multi-Company model



Each company may have different departments.



🧩 3. Departmental Data Model (Full Spec)

Entity: Department

Field	Description

id	Unique

companyId	Belongs to a company

code	Short code (OPS, FIN, WH, etc.)

name	Full name

description	Optional

isActive	Enable/disable

createdAt	Audit

updatedAt	Audit



Index: (companyId, code) unique.



Entity: DepartmentRole



(Connects Department → User Role)



Field	Description

id	Unique

departmentId	Belongs to department

userId	User assignment

role	e.g. Manager, Staff, Approver

permissionsJson	Fine-grained permissions

Entity: DepartmentWorkflowRule



Defines what department is responsible for which step.



Field	Description

id	Unique

companyId	Company

orderType	FTTH, Modification, TTKT, etc.

statusCode	Assigned, OnTheWay, Completed

departmentId	Which department handles this step

isFinalApproval	e.g. Finance for payouts

🏗 4. Departmental Integration in the App

✔ Order Workflow



Every status transition is tagged with a department.



Example for FTTH:



Status	Department

Assigned	OPS

OnTheWay	Installer

Completed	Installer

Verified	OPS

ReadyForBilling	Finance

Billed	Finance

✔ Material Tracking



Materials assigned/returned flow through:



Warehouse Dept



Installer Dept



Finance Dept (verification)



So each material movement record includes:



fromDepartmentId



toDepartmentId



handledByUserId



Timestamps



✔ Billing \& Payout



Finance department handles:



Payout approval



Billing generation



Variance reports



Departments allow finance to ONLY see finance-relevant views.



✔ User Access Control



Users can belong to:



One or multiple departments



Each department defines what they can see and edit



Installer sees:



Assigned jobs



Map



Checklist



Material issued to them



OPS sees:



All orders



Assignment tools



SLA dashboards



Finance sees:



Billing



Payout



Reports



Warehouse sees:



Stock



Serial numbers



Material in/out



🎛 5. How It Looks in UI

Company Settings → Departments

\[Ops]        \[Finance]     \[Warehouse]

\[Installer]  \[QA/QC]       \[+ Add Dept]





Click department → configure:



Name



Code



Description



Members



Permissions



Workflow responsibilities



Order View Shows Department Ownership

Order #123456

Current Department: OPS

Responsible: John (OPS Manager)

Next Department: Installer



Filtering / Dashboard



Filter orders by department ownership



SLA per department



Cards showing department KPIs



Material variance by department



Billing readiness by department



📦 6. Add to Multi-Company Model



Departments become part of:



Company

&nbsp;├── Branches

&nbsp;├── Departments

&nbsp;    ├── Users

&nbsp;    ├── Workflows

&nbsp;    ├── KPI configurations

&nbsp;└── Rate Profiles





Every company may define different:



Departments



Workflow assignments



Rates per department



Material rules



KPI targets



🔒 7. Security Benefits



Department scoping gives:



Better access control



Cleaner UI (users see only what they need)



Less risk of data breach



Clear audit logs (department → user → action)



🧵 8. Should We Add Department Templates?



Yes — similar to rate profiles.



Templates include:



Default departments (OPS, Installer, Finance, Warehouse)



Preconfigured workflows



Preconfigured roles



A company can clone a template when onboarding.



🏁 FINAL ANSWER



YES — adding Departmental support is not only possible, but highly recommended.

It strengthens:



Access control



Workflow automation



Billing workflows



KPI dashboards



Multi-company architecture



Materials control



And aligns perfectly with CephasOps’s long-term structure.


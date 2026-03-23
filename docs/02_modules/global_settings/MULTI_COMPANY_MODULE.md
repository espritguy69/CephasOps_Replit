Below is your full, final, production-ready MULTI\_COMPANY\_MODULE.md.



📌 Place this file here:

cephasops/docs/05\_modules/MULTI\_COMPANY\_MODULE.md



This version consolidates everything from SYSTEM\_OVERVIEW, SETTINGS, STORYBOOK, and COMPANY SETUP into ONE single authoritative module.



MULTI\_COMPANY\_MODULE.md



CephasOps – Multi-Company Architecture \& Isolation Module

Version 2.0 (Final)



This module defines how CephasOps supports multiple companies operating inside one platform while maintaining strict data isolation, role isolation, and financial/legal separation.



This is NOT technical code.

It’s the business + architectural specification for Cursor AI to implement.



1\. Supported Companies



The system supports these four primary companies:



Cephas Sdn. Bhd – ISP Operations



Cephas Trading \& Services – ISP Operations + Inventory



Kingsman Classic Services – Barbershop \& Spa



Menorah Travel \& Tours – Travel Agent



Each is treated as a separate legal entity, with its own:



Users



Permissions



Inventory



Orders



Billing \& Tax



Payroll



P\&L



2\. Core Multi-Company Principles

2.1 Data Isolation



Each company’s data exists in a separate boundary:



Inventory stock cannot mix



Orders cannot be visible across companies



Payroll cannot mix



Billing \& invoicing cannot mix



P\&L is calculated per company



2.2 Shared Platform, Isolated Domains



The platform is shared, but business logic is isolated per company.



2.3 Cross-Company Access



Only Directors (or designated roles) may view consolidated P\&L and KPIs across all companies.



Operational staff are restricted to their company only.



2.4 Switching Companies



Users may belong to more than one company.

But must switch active company manually:



ActiveCompany = Cephas





Switching company changes:



Sidebar modules



Data scope



Permissions



Settings



Workflows



3\. Multi-Company Data Structure



Every primary entity must have:



companyId



Company-scoped ID sequences



Company-scoped numbering rules



Access controlled by RBAC



Core entities that MUST store companyId:



Module	Entities requiring companyId

Orders	Order, Appointment, Blockers, Dockets

Scheduler	SI Availability, Calendar

Payroll	PayrollRun, Earnings

Finance	Invoice, Payment, CreditNote

Inventory	Stock, Serial No, Movements, RMA

P\&L	PnlFact, PnlDetailPerOrder

Documents	GRN, Slip, RMA Ticket, SOA

Settings	Ratecards, Parser Templates, Cost Centres

4\. Multi-Company Separation in Each Module



Below is module-by-module isolation logic.



4.1 Orders Module (ISP)

Each company has:



Its own Order Types



Its own Ratecards



Its own Workflow configuration



Its own Schedulers \& Installers



Separate Excel parser templates



No mixing allowed:



Cephas orders cannot be seen by Cephas Trading users.



Kingsman \& Menorah DO NOT use ISP Orders unless customised later.



4.2 Scheduler Module



SI availability belongs to a specific company



Calendar events are company-bound



Rescheduling approvals belong to that company



Cross-company scheduling = forbidden



4.3 Inventory Module



Inventory must be strictly separated:



Separate warehouses



Separate item lists allowed



Separate serial tracking



Separate RMA loops



Allowed (optional future)



Controlled stock transfer between Cephas ↔ Cephas Trading



Must require director-level approval



4.4 Billing + Tax \& eInvoice



Each company has:



Its own invoice template



Own invoice numbering sequence



SST or tax profile



Separate MyInvois API Keys



Separate banking details



Separate partner billing (TIME, Celcom, Kingsman walk-ins, etc.)



Invoicing is never shared.



4.5 Payroll Module



Each company has:



Its own SI list



Own pay rates



Separate payroll runs



Separate banking lists



Separate approvals



SIs cannot work across companies unless manually dual-registered.



4.6 P\&L Module



P\&L MUST be computed:



Per company



Per vertical



Per partner



Per cost centre



Directors may view group-wide P\&L, but staff can only see their company.



4.7 Document Generation Module



All PDFs must include:



Company logo



Company registration



Company SST status



Address



Document numbering is per company:



Example:



INV-CEPHAS-2025-000481

INV-KINGSMAN-2025-000033



5\. Multi-Company Workflow Integration



Workflows are company-specific.



5.1 Parser → Order Lifecycle



Each company may have:



Different parser rules



Different building validation rules



Different MRA or RMA processes



Example:



TIME Activation Parser only applies to Cephas companies.

Kingsman has no parser pipeline.



5.2 Scheduler → SI App



SIs only see jobs in their company.



5.3 Inventory → Billing → Payroll → P\&L



All loops must operate within company boundaries:



No order may reference inventory from another company



No payroll roundup includes SIs from other companies



No P\&L mixing without director permission



6\. Multi-Company User \& Role Management



The RBAC system must enforce:



6.1 Users belong to one or more companies



Example user:



User: Simon

Companies:

&nbsp; - Cephas Sdn. Bhd (Admin, Director)

&nbsp; - Cephas Trading (Director)

&nbsp; - Kingsman Classic (Viewer)

&nbsp; - Menorah Travel (Viewer)



6.2 Role permissions are evaluated per active company



Example:



In Cephas — Simon is Admin



In Kingsman — Simon is Viewer



Switching company changes what he can do.



7\. Company-Level Settings \& Customisation



Each company has its own:



Parser templates



Order types



Ratecards



Material catalog



Approval workflows



Business rules



Document templates



Payroll rules



Billing rules



SST/eInvoice settings



Settings cannot leak between companies.



8\. Cross-Company Dashboards (Director Only)



For directors only:



Consolidated P\&L



Consolidated Revenue



Consolidated Material Cost



Consolidated SI cost



Partner comparison (if shared partners exist)



No operational mixing



Even directors cannot:



Assign SI cross-company



View operational logs between companies



Access inventory across companies



They can only view aggregated analytics.



9\. Security \& Compliance Rules

9.1 No cross-company data leakage



APIs must enforce companyId on every request.



9.2 Logs must be scoped by company



Order logs and SI logs remain isolated.



9.3 PI \& financial data must remain company-specific



Especially important for Kingsman \& Menorah (customer data privacy).



10\. Data Model Summary (company-scoped)



Every major table includes:



companyId: GUID

createdByUserId: GUID

updatedByUserId: GUID





Company separation is not optional.

It is a fundamental design constraint.



11\. Folder Structure (Docs + Source)



Recommended placement inside the repository:



cephasops/

&nbsp;├── docs/

&nbsp;│    ├── 01\_overview/

&nbsp;│    │     ├── SYSTEM\_OVERVIEW.md

&nbsp;│    │

&nbsp;│    ├── 02\_architecture/

&nbsp;│    │     ├── MULTI\_COMPANY\_MODULE.md   <— THIS FILE

&nbsp;│    │

&nbsp;│    ├── 03\_business/

&nbsp;│    │     ├── BUSINESS\_POLICIES.md

&nbsp;│    │     ├── USE\_CASES.md

&nbsp;│    │

&nbsp;│    ├── 04\_data/

&nbsp;│    │     ├── DATA\_MODEL\_SUMMARY.md

&nbsp;│    │

&nbsp;│    ├── 05\_modules/

&nbsp;│          ├── SETTINGS\_MODULE.md

&nbsp;│          ├── INVENTORY\_AND\_RMA\_MODULE.md

&nbsp;│          ├── BILLING\_TAX\_EINVOICE\_MODULE.md

&nbsp;│          ├── SERVICE\_INSTALLER\_APP\_MODULE.md

&nbsp;│          ├── DOCUMENT\_TEMPLATES\_MODULE.md

&nbsp;│          ├── WORKFLOW\_ENGINE.md

&nbsp;│          ├── ORDERS\_MODULE.md

&nbsp;│          ├── PAYROLL\_MODULE.md

&nbsp;│          ├── PNL\_MODULE.md

&nbsp;│          ├── SCHEDULER\_MODULE.md

&nbsp;│

&nbsp;└── src/

&nbsp;      ├── Domain/

&nbsp;      │     ├── Companies/

&nbsp;      │     └── MultiCompany/

&nbsp;      └── Api/

&nbsp;            ├── Controllers/

&nbsp;            │     └── MultiCompanyController.cs



12\. Summary



The Multi-Company module ensures that CephasOps can run:



ISP operations



Inventory + RMA



Billing



Payroll



P\&L



Barbershop



Travel



Across four companies, fully separated yet centrally managed.



This is the foundation for a unified yet compliant enterprise system.


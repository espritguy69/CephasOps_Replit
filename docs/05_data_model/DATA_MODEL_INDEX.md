\# CephasOps Data Model Index



This index links all entity and relationship specifications for the CephasOps platform.

Use this as the single entry point for Cursor and for humans reading the data model.



---



\## 1. Entities



All entity definitions live in `05\_data\_model/entities/`.



\- \*\*Orders\*\*

&nbsp; - `entities/orders\_entities.md`

\- \*\*Inventory \& RMA\*\*

&nbsp; - `entities/inventory\_entities.md`

\- \*\*Billing, Tax \& e-Invoice\*\*

&nbsp; - `entities/billing\_entities.md`

\- \*\*Scheduler\*\*

&nbsp; - `entities/scheduler\_entities.md`

\- \*\*Payroll\*\*

&nbsp; - `entities/payroll\_entities.md`

\- \*\*P\&L (Profit \& Loss)\*\*

&nbsp; - `entities/pnl\_entities.md`

\- \*\*Settings (Companies, Partners, Buildings, SIs, etc.)\*\*

&nbsp; - `entities/settings\_entities.md`

\- \*\*Users \& RBAC\*\*

&nbsp; - `entities/users\_rbac\_entities.md`

\- \*\*Email Parser \& Pipeline\*\*

&nbsp; - `entities/parser\_entities.md`

\- \*\*Document Templates \& Generated Documents\*\*

&nbsp; - `entities/document\_templates\_entities.md`

\- \*\*Global \& Company Settings\*\*

&nbsp; - `entities/global\_settings\_entities.md`

\- \*\*KPI Profiles\*\*

&nbsp; - `entities/kpi\_profile\_entities.md`

\- \*\*Logging \& Audit\*\*

&nbsp; - `entities/logging\_entities.md`

\- \*\*Background Jobs\*\*

&nbsp; - `entities/background\_jobs\_entities.md`

\- \*\*Splitters Management\*\*

&nbsp; - `entities/splitters\_entities.md`

\- \*\*Material Templates\*\*

&nbsp; - `entities/material\_templates\_entities.md`



---



\## 2. Relationships



All relationship definitions live in `05\_data\_model/relationships/`.



\- \*\*Orders\*\*

&nbsp; - `relationships/orders\_relationships.md`

\- \*\*Inventory \& RMA\*\*

&nbsp; - `relationships/inventory\_relationships.md`

\- \*\*Billing, Tax \& e-Invoice\*\*

&nbsp; - `relationships/billing\_relationships.md`

\- \*\*Scheduler\*\*

&nbsp; - `relationships/scheduler\_relationships.md`

\- \*\*Payroll\*\*

&nbsp; - `relationships/payroll\_relationships.md`

\- \*\*P\&L (Profit \& Loss)\*\*

&nbsp; - `relationships/pnl\_relationships.md`

\- \*\*Settings \& RBAC\*\*

&nbsp; - `relationships/settings\_rbac\_relationships.md`

\- \*\*Email Parser \& Pipeline\*\*

&nbsp; - `relationships/parser\_relationships.md`

\- \*\*Document Templates\*\*

&nbsp; - `relationships/document\_templates\_relationships.md`

\- \*\*Global \& Company Settings\*\*

&nbsp; - `relationships/global\_settings\_relationships.md`

\- \*\*KPI Profiles\*\*

&nbsp; - `relationships/kpi\_profile\_relationships.md`

\- \*\*Logging \& Audit\*\*

&nbsp; - `relationships/logging\_relationships.md`

\- \*\*Background Jobs\*\*

&nbsp; - `relationships/background\_jobs\_relationships.md`

\- \*\*Splitters Management\*\*

&nbsp; - `relationships/splitters\_relationships.md`

\- \*\*Material Templates\*\*

&nbsp; - `relationships/material\_templates\_relationships.md`



\### 2.1 Cross-Module Overview



\- \*\*Cross-module relationships (Orders ↔ Inventory ↔ Billing ↔ Payroll ↔ P\&L, Parser, Settings, etc.)\*\*

&nbsp; - `relationships/cross\_module\_relationships.md`



This file provides the high-level view of how all domains connect.



---



\## 3. How Cursor Should Use This Folder



1\. \*\*Read\*\* `DATA\_MODEL\_INDEX.md` first to discover all entity and relationship specs.

2\. \*\*Load\*\* all files under:

&nbsp;  - `05\_data\_model/entities/`

&nbsp;  - `05\_data\_model/relationships/`

3\. Treat these as the \*\*source of truth\*\* for:

&nbsp;  - Database schema

&nbsp;  - ORM models

&nbsp;  - Foreign key relationships

&nbsp;  - Cross-module navigation properties



---



\## 4. Related Documentation



For deeper architectural context, see:



\- Root-level \*\*`ARCHITECTURE\_BOOK.md`\*\* – full system architecture.

\- `01\_system/SYSTEM\_OVERVIEW.md` – high-level system description.

\- `02\_modules/` – per-module functional specs.

\- `08\_infrastructure/background\_jobs\_infrastructure.md` – background jobs \& scheduling infrastructure.




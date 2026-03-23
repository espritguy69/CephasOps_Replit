

\### `cross\_module\_relationships.md`



```md

\# Cross-Module Relationships  

CephasOps Data Model – Cross-Domain Map  

Version 1.0



This document shows how major domains connect:



\- Orders

\- Parser

\- Scheduler

\- SI App

\- Inventory \& RMA

\- Billing

\- Payroll

\- P\&L

\- Documents

\- Settings \& Companies



---



\## 1. Big Picture Mermaid Diagram



```mermaid

erDiagram

&nbsp;   Company ||--o{ Order : owns

&nbsp;   Company ||--o{ Invoice : owns

&nbsp;   Company ||--o{ PayrollPeriod : owns

&nbsp;   Company ||--o{ PnlFact : owns

&nbsp;   Company ||--o{ Material : owns

&nbsp;   Company ||--o{ File : owns



&nbsp;   EmailMessage ||--o{ ParseSession : has

&nbsp;   ParseSession ||--o{ ParsedOrderDraft : produces

&nbsp;   ParsedOrderDraft }o--|| Order : converts\_into



&nbsp;   Order ||--o{ OrderStatusLog : tracked\_by

&nbsp;   Order ||--o{ OrderMaterialUsage : uses

&nbsp;   Order ||--o{ OrderDocket : documented\_by

&nbsp;   Order ||--o{ PayrollItem : pays

&nbsp;   Order ||--o{ PnlOrderDetail : analysed\_as

&nbsp;   Order }o--o{ ScheduledSlot : scheduled\_in



&nbsp;   OrderMaterialUsage }o--|| StockMovement : backed\_by

&nbsp;   StockMovement }o--|| SerialisedItem : refers\_to



&nbsp;   Invoice ||--o{ InvoiceLine : has

&nbsp;   Invoice ||--o{ CreditNote : can\_be\_adjusted\_by

&nbsp;   Invoice ||--o{ PaymentAllocation : is\_paid\_by



&nbsp;   PayrollRun ||--o{ PayrollItem : consists\_of

&nbsp;   PayrollPeriod ||--o{ PayrollRun : contains



&nbsp;   DocumentTemplate ||--o{ GeneratedDocument : uses

&nbsp;   GeneratedDocument }o--|| File : stored\_as

2\. Parser → Orders

Flow:



EmailMessage ingested from EmailAccount.



ParseSession created to run correct ParserTemplate.



Parser produces one or more ParsedOrderDraft rows.



Admin (or automation) validates \& converts draft → Order.



Key links:



ParseSessions.emailMessageId → EmailMessages.id



ParsedOrderDrafts.parseSessionId → ParseSessions.id



ParsedOrderDrafts.createdOrderId → Orders.id



3\. Orders → Scheduler \& SI App

Scheduler:



Orders.id ↔ ScheduledSlots.orderId



ScheduledSlots.serviceInstallerId ↔ ServiceInstallers.id



SI App:



SiJobSessions.orderId ↔ Orders.id



SiJobEvents.jobSessionId ↔ SiJobSessions.id



SiPhotos.orderId ↔ Orders.id



SiDeviceScans.orderId ↔ Orders.id



Business linkage:



Scheduler decides who \& when.



SI App captures what actually happened.



4\. Orders → Inventory \& RMA

Job consumption:



Order.id → OrderMaterialUsage.orderId



OrderMaterialUsage.materialId → Materials



OrderMaterialUsage.serialisedItemId → SerialisedItems



OrderMaterialUsage.stockMovementId → StockMovements



Faulty equipment:



RmaItems.relatedOrderId → Orders



RmaItems.serialisedItemId → SerialisedItems



This allows:



SI bag reconciliation



Material COGS for P\&L



Fault tracking by job and vendor



5\. Orders → Billing

InvoiceLines.orderId connects orders to billing.



Simple-mode (1:1):



Invoices.linkedOrderId = Orders.id



Batch-mode:



Multiple orders included via InvoiceLines.orderId



Invoices.linkedOrderId = null or used only for a main/anchor job.



All revenue eventually flows into P\&L.



6\. Orders → Payroll

PayrollItems.orderId links each job to SI earnings.



SI-specific:



PayrollItems.serviceInstallerId → ServiceInstallers.id



Rules from SiRatePlanRules are applied based on:



Orders.orderTypeId



Orders.kpiCategory



kpiResult captured via Workflow / SI App



7\. Billing + Payroll + Inventory → P\&L

P\&L needs:



From Billing:



Invoices \& InvoiceLines → revenue



From Inventory:



OrderMaterialUsage (via StockMovements) → material cost



From Payroll:



PayrollItems → labour cost



From Settings / Finance:



OverheadEntries → indirect cost



Then:



PnlOrderDetails generated per order



PnlFacts aggregated by:



company



partner



vertical



cost centre



period



orderType



8\. Documents \& Templates

DocumentTemplates define:



Invoice PDF format



Docket layout



RMA forms, etc.



GeneratedDocuments:



Link template + entity + file.



Key bridges:



GeneratedDocuments.documentType + entityId → determine whether:



invoice



order docket



credit note



Files are always pointed to via:



GeneratedDocuments.fileId → Files.id



9\. Settings \& Multi-Company Boundaries

Shared constraints:



Every domain table uses companyId (except top-level shared lists like GlobalSettings).



UserCompanies \& UserRoles limit who can see/do what inside each company.



Critical invariants:



No entity is ever shared across companies.



Cross-company reporting is done at query/report level only, never by mixing companyIds in a single row.



End of Cross-Module Relationships


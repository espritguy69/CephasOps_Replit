It contains:



Entities



Relationships



Normalisation rules



Multi-company scoping



Module → table mapping



High-level ERD (ASCII)



Notes for Cursor AI



No code, only documentation.



DATA\_MODEL\_SUMMARY.md



CephasOps – Data Model Summary (Master Reference)

Version 1.0



This document summarises the entities, relationships, and data boundaries across all CephasOps modules.



It enables Cursor AI (and human developers) to generate consistent database schemas that follow the system rules.



1\. Core Principles



The data model is built on five fundamental rules:



1.1 Multi-Company Isolation



Every entity that belongs to a specific business unit MUST include:



companyId





Companies include:



Cephas Sdn. Bhd



Cephas Trading \& Services



Kingsman Classic Services



Menorah Travel and Tours



Only director roles can query cross-company.



1.2 Stable Identifiers



Use:



UUID (GUID) for all primary keys



No auto-increment IDs exposed to SI App



Serial numbers and device IDs stored as-is inside serialised item entities



1.3 Immutable Financial Records



Once invoices or payroll runs are finalized:



They become read-only



Amendments only allowed through credit notes or adjustments



1.4 Workflow-Driven Orders



Order state transitions are not manually editable — they must be executed by the Workflow Engine.



1.5 Derived Data Tables



Some tables are not “source of truth”:



PNLFact



PNLDetailPerOrder



PayrollSummary



These are derived and can be re-calculated at any time.



2\. High-Level Domain Map

COMPANY

&nbsp;├── Users, Roles, Permissions

&nbsp;├── Orders

&nbsp;│     ├── Schedule \& SI Assignment

&nbsp;│     ├── Dockets

&nbsp;│     └── Materials Usage

&nbsp;├── Inventory

&nbsp;│     ├── Materials

&nbsp;│     ├── Stock

&nbsp;│     ├── Serialised Items

&nbsp;│     └── RMA

&nbsp;├── Billing \& Invoices

&nbsp;│     └── Payments

&nbsp;├── Payroll

&nbsp;└── P\&L



3\. Entity List by Module



Below is the module → entity mapping.



3.1 Companies \& RBAC

Company



id



name



vertical (ISP, Barbershop, Travel)



tax settings



branding data (logo, footer text)



createdAt



User



id



name



email



phone



isActive



createdAt



UserCompany



id



userId



companyId



Role



id



name (Admin, Scheduler, Director, SI, Warehouse, Finance)



UserRole



id



userId



roleId



companyId



Permission



id



key



description



RolePermission



id



roleId



permissionId



3.2 Orders Module

Order



id



companyId



partnerId



orderTypeId (Activation, Modification, Relocation, Assurance)



serviceId / ticketNo / TTKT / AWO



customer details



buildingId



appointmentDateTime



assignedSiId



status



creationSource (Parser / Manual / API)



createdAt



OrderStatusLog



id



orderId



oldStatus



newStatus



triggeredByUserId



triggeredBySIId



metadata (GPS, notes)



createdAt



OrderReschedule



id



orderId



requestedByUserId



reason



oldDate



newDate



status (Pending, Approved, Rejected)



createdAt



OrderDocket



id



orderId



fileId



uploadedBy



uploadedAt



OrderMaterialUsage



id



orderId



materialId



serialisedItemId (nullable)



quantity



createdAt



3.3 Scheduler Module

Schedule



id



orderId



siId



scheduleDate



fromTime



toTime



SIAvailability



id



siId



date



isAvailable



notes



SILeave



id



siId



date



reason



3.4 Inventory Module

Material



id



name



code



category



unit



defaultCost



createdAt



StockLocation



id



companyId



name (Warehouse, SI Bag, RMA Bin)



type



StockLevel



id



companyId



materialId



locationId



quantity



StockMovement



id



companyId



fromLocationId



toLocationId



materialId



serialisedItemId (nullable)



quantity



orderId (nullable)



createdByUserId



createdAt



SerialisedItem



id



serialNo



materialId



companyId



currentLocationId



status (InStock, AssignedToSI, InstalledAtCustomer, Faulty, RMA)



orderId (last used)



createdAt



RMA



id



serialisedItemId



orderId



fileId (MRA PDF)



reason



status



createdAt



3.5 Billing, Tax \& Invoices

Partner



id



companyId



name



billingRules (JSON)



taxSettings



createdAt



Invoice



id



companyId



partnerId



invoiceNumber



orderId (nullable for batch)



issueDate



dueDate



total



taxAmount



netAmount



status (Draft, Submitted, Accepted, Rejected, Paid)



submissionId (for TIME portal)



einvoiceId



createdAt



InvoiceLine



id



invoiceId



description



amount



costCentreId



Payment



id



invoiceId



amount



paidDate



referenceNo



createdAt



3.6 Payroll Module

SIRateCard



id



companyId



partnerId



orderTypeId



siLevel (Junior, Senior, Subcon)



baseRate



onTimeBonus



kpiPenaltyRules (JSON)



PayrollPeriod



id



companyId



month



year



status (Open, Calculated, Locked, Paid)



PayrollItem



id



payrollPeriodId



siId



orderId



basePay



bonuses



penalties



netPay



3.7 P\&L Module

PnlFact



(Aggregated)



id



companyId



partnerId



verticalId



costCentreId



period (YYYY-MM)



orderTypeId



revenue



directMaterialCost



directLabourCost



indirectCost



grossProfit



netProfit



jobsCount



createdAt



PnlDetailPerOrder



id



orderId



companyId



period



revenue



materialCost



labourCost



overheadAllocated



profitForOrder



OverheadEntry



id



companyId



costCentreId



period



amount



description



source



createdByUserId



createdAt



3.8 Email Pipeline \& Parser

EmailMessage



id



companyId



subject



from



to



cc



bodyText



bodyHtml



receivedAt



attachmentMetadata (JSON)



EmailAttachment



id



emailId



fileId



fileType



uploadedAt



ParsedOrder



id



emailId



parsedJson (full structured payload)



confidenceLevel



status (PendingReview, Approved, Rejected)



createdAt



3.9 Files \& Storage

File



id



fileName



originalName



mimeType



size



companyId



ownerUserId



createdAt



Used for:



Dockets



Photos



MRA / RMA



Invoices (PDF)



4\. Cross-Module Relationships (ERD)



ASCII diagram:



COMPANY

&nbsp;├── USERS ── USERROLE ── ROLE

&nbsp;│

&nbsp;├── PARTNER ─────────────┐

&nbsp;│                        │

&nbsp;├── ORDER ───────────────┼─ INVOICE ─── PAYMENT

&nbsp;│    │                    │

&nbsp;│    ├─ ORDERSTATUSLOG    │

&nbsp;│    ├─ ORDERMATERIAL     │

&nbsp;│    └─ SCHEDULE ─ SI     │

&nbsp;│

&nbsp;├── INVENTORY

&nbsp;│    ├─ MATERIAL

&nbsp;│    ├─ STOCKLEVEL

&nbsp;│    ├─ STOCKMOVEMENT

&nbsp;│    ├─ SERIALISEDITEM

&nbsp;│    └─ RMA

&nbsp;│

&nbsp;├── PAYROLL

&nbsp;│    ├─ SIRATECARD

&nbsp;│    ├─ PAYROLLPERIOD

&nbsp;│    └─ PAYROLLITEM

&nbsp;│

&nbsp;└── P\&L

&nbsp;     ├─ PnlFact

&nbsp;     ├─ PnlDetailPerOrder

&nbsp;     └─ OverheadEntry



5\. Multi-Company Boundaries



Entities requiring companyId:



Orders + related logs



Scheduler



All inventory tables



Invoices, invoice lines, payments



Payroll



P\&L



Files



Entities that are global:



Roles



Permissions



Building types (optional)



Entities that are semi-global:



Materials (can be global but stock is per company)



OrderType (global)



Partner (company-scoped)



6\. Notes for Cursor AI



When generating a schema:



DO NOT AUTO-GENERATE foreign keys without checking the storybook.

Always include:



companyId for company-scoped tables



createdAt timestamp



Soft delete not allowed except for files



Orders:



Status transitions MUST NOT be mutated directly



Must call workflow engine



Inventory:



Serialised items must always have a status



Material usage must create stock movements



Billing:



Invoice → eInvoice → Payment chain must enforce immutability



Payroll:



Derived from Orders + SI behaviour



P\&L:



PnlFact \& PnlDetailPerOrder are derived tables



Must be safe to recalc



7\. Future Data Extensions (Reserved)



Kingsman POS



Menorah Travel itineraries



Equipment images, barcodes



WhatsApp notifications logs



All future extensions should respect:



companyId



clean relations



non-mutable financials


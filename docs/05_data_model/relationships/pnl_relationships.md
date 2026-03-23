\### `pnl\_relationships.md`



```md

\# P\&L – Relationships  

CephasOps Data Model – P\&L Relationships  

Version 1.0



This document explains how P\&L entities relate to:



\- Orders

\- Invoices

\- Inventory

\- Payroll

\- Overheads



---



\## 1. Core Bridges



P\&L is \*\*derived data\*\*. Core inputs:



\- Revenue:

&nbsp; - Invoices, InvoiceLines

\- Direct Material Cost:

&nbsp; - OrderMaterialUsage → StockMovements

\- Direct Labour Cost:

&nbsp; - PayrollItems

\- Overheads:

&nbsp; - OverheadEntries



Outputs:



\- PnlFacts (aggregated)

\- PnlOrderDetails (per Order)



---



\## 2. ERD (P\&L Scope)



```mermaid

erDiagram

&nbsp;   Order ||--o{ PnlOrderDetail : analysed\_as

&nbsp;   Company ||--o{ PnlFact : summarises

&nbsp;   Company ||--o{ OverheadEntry : allocates

&nbsp;   Order ||--o{ InvoiceLine : billed\_by

&nbsp;   Order ||--o{ OrderMaterialUsage : consumes

&nbsp;   Order ||--o{ PayrollItem : pays

3\. PnlOrderDetails Links

PnlOrderDetails.orderId → Orders.id



PnlOrderDetails.partnerId → Partners.id



PnlOrderDetails.orderTypeId → OrderTypes.id



For a given order:



revenueAmount = sum of related InvoiceLines (if invoiced).



materialCost = sum of related OrderMaterialUsage.totalCost.



labourCost = sum of related PayrollItems.netAmount.



overheadAllocated = derived based on chosen allocation strategy.



4\. PnlFacts Aggregation

PnlFacts.companyId → Companies.id



Aggregated from PnlOrderDetails \& OverheadEntries by:



period



optional partnerId



optional orderTypeId



optional costCentreId



optional vertical



Composition:



revenueAmount = sum of PnlOrderDetails.revenueAmount



directMaterialCost = sum of PnlOrderDetails.materialCost



directLabourCost = sum of PnlOrderDetails.labourCost



indirectCost = sum of OverheadEntries.amount assigned to this slice



5\. OverheadEntries Link

OverheadEntries.companyId → Companies



Optional link:



costCentreId → CostCentres



vertical = string label (ISP / Barbershop / Travel)



Allocation:



Simple mode:



Assign entire OverheadEntry to one PnlFact bucket.



Advanced mode:



Allocate proportionally across partners / order types / cost centres.



6\. Multi-Company Considerations

Every P\&L table is company-scoped.



Cross-company views (group P\&L) are computed at query/report level, not by mixing companyIds.



End of P\&L Relationships


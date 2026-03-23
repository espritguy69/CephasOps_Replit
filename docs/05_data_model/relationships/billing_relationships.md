\### `billing\_relationships.md`



```md

\# Billing \& Invoicing – Relationships  

CephasOps Data Model – Billing Relationships  

Version 1.0



This document describes relationships in the \*\*Billing \& Tax \& e-Invoice\*\* domain.



---



\## 1. High-Level Overview



Core relationships:



\- Company 1—\* Invoices

\- Invoice 1—\* InvoiceLines

\- Invoice 0–\* Payments (through allocations if needed)

\- Invoice 0–\* CreditNotes

\- Partner 1—\* Invoices / Payments / Ratecards

\- Order \*–0..1 Invoice (direct) or via InvoiceLine



---



\## 2. ERD (Billing Scope)



```mermaid

erDiagram

&nbsp;   Company ||--o{ Invoice : issues

&nbsp;   Invoice ||--o{ InvoiceLine : has

&nbsp;   Invoice ||--o{ CreditNote : can\_be\_adjusted\_by

&nbsp;   Company ||--o{ Payment : records

&nbsp;   Company ||--o{ BillingRatecard : configures

&nbsp;   Partner ||--o{ Invoice : billed\_to

&nbsp;   Partner ||--o{ Payment : pays

&nbsp;   Partner ||--o{ BillingRatecard : has

&nbsp;   Invoice ||--o{ PaymentAllocation : receives

&nbsp;   Payment ||--o{ PaymentAllocation : allocates

PaymentAllocation may be an optional table, depending on implementation.



3\. Invoice \& InvoiceLine

Invoices.id → InvoiceLines.invoiceId (1–\*)



InvoiceLines.orderId → Orders.id (0–1)



Job linking patterns:



Single-order invoice:



Invoices.linkedOrderId = Orders.id



plus one or more InvoiceLines also referencing that orderId



Batch invoice:



Invoices.linkedOrderId = null



multiple InvoiceLines.orderId across many jobs



4\. Payments \& Allocation

Payment.partnerId → Partners



Optional:



PaymentAllocations.paymentId → Payments



PaymentAllocations.invoiceId → Invoices



Balance:



Invoice.balanceOutstanding = totalAmount - sum(allocations - creditNotes).



5\. Credit Notes

CreditNotes.originalInvoiceId → Invoices



CreditNoteLines.creditNoteId → CreditNotes



Applied amounts reduce invoice outstanding.



Relationships:



1 Invoice → 0..\* CreditNotes



1 CreditNote → 1..\* CreditNoteLines



6\. BillingRatecard Links

BillingRatecards.partnerId → Partners



BillingRatecards.orderTypeId → OrderTypes



Usage:



When Order becomes invoice-eligible:



Determine partner



Determine orderType



Optional buildingType or additional conditions



Pick matching BillingRatecard



Use amount + taxRate to create InvoiceLines



7\. Partner Account \& Company

PartnerAccounts.partnerId → Partners



PartnerAccounts.companyId → Companies



Provides defaults for:



Billing address



Payment terms



Finance contact emails



8\. P\&L Integration

Invoices.totalAmount, InvoiceLines.lineSubtotal, lineTotal, taxAmount

feed into revenue side of P\&L.



Connections described further in pnl\_relationships.md.



End of Billing Relationships


\# Billing \& Invoicing Entities  

CephasOps – Billing Domain Data Model  

Version 1.0



This file defines entities for \*\*Billing, Tax \& e-Invoice\*\*:



\- Invoice

\- InvoiceLine

\- Payment

\- CreditNote

\- CreditNoteLine

\- BillingRatecard

\- PartnerAccount



All are \*\*company-scoped\*\* via `companyId`.



---



\## 1. Invoice



\### 1.1 Table: `Invoices`



| Field              | Type     | Required | Description                                                          |

|--------------------|----------|----------|----------------------------------------------------------------------|

| id                 | uuid     | yes      | Primary key.                                                         |

| companyId          | uuid     | yes      | FK → Companies.id.                                                   |

| invoiceNo          | string   | yes      | Human-readable invoice number (unique per company per year).         |

| invoiceType        | enum     | yes      | `Standard`, `Principal`, `Assurance`, `Relocation`, `KingsmanPOS`, `Travel`. |

| partnerId          | uuid     | yes      | FK → Partners.id.                                                    |

| customerName       | string   | yes      | Customer/partner display name (cached).                              |

| customerAddress    | text     | no       | Billing address snapshot.                                            |

| issueDate          | date     | yes      | Invoice date.                                                        |

| dueDate            | date     | no       | Optional due date.                                                   |

| currency           | string   | yes      | Typically `MYR`.                                                     |

| subtotalAmount     | decimal  | yes      | Sum of line net amounts before tax.                                 |

| taxAmount          | decimal  | yes      | Total tax (e.g. SST).                                                |

| totalAmount        | decimal  | yes      | Grand total.                                                         |

| balanceOutstanding | decimal  | yes      | Outstanding balance (derived from payments/credit notes).            |

| taxCode            | string   | no       | `SR`, `ZR`, etc.                                                     |

| taxRegistrationNo  | string   | no       | Company’s SST registration where applicable.                         |

| einvoiceStatus     | enum     | no       | `NotSubmitted`, `Submitted`, `Validated`, `Error`.                   |

| einvoiceUuid       | string   | no       | LHDN MyInvois UUID.                                                  |

| einvoiceQrUrl      | string   | no       | URL or payload used to generate QR code.                             |

| einvoiceErrorMsg   | string   | no       | Last error message from LHDN.                                        |

| status             | enum     | yes      | `Draft`, `Issued`, `PartiallyPaid`, `Paid`, `Cancelled`.             |

| locked             | boolean  | yes      | True after e-Invoice validation or when fully paid.                  |

| linkedOrderId      | uuid     | no       | For single-job invoices.                                             |

| linkedOrderCount   | int      | yes      | Number of orders included (for batch).                               |

| notesInternal      | text     | no       | Notes for finance team.                                              |

| notesExternal      | text     | no       | Notes to print on invoice PDF.                                       |

| createdByUserId    | uuid     | yes      | FK → Users.id.                                                       |

| createdAt          | datetime | yes      | Created timestamp.                                                   |

| updatedAt          | datetime | yes      | Last update.                                                         |



---



\## 2. InvoiceLine



\### 2.1 Table: `InvoiceLines`



| Field            | Type     | Required | Description                                                 |

|------------------|----------|----------|-------------------------------------------------------------|

| id               | uuid     | yes      | Primary key.                                                |

| companyId        | uuid     | yes      | FK → Companies.id.                                          |

| invoiceId        | uuid     | yes      | FK → Invoices.id.                                           |

| orderId          | uuid     | no       | FK → Orders.id (if line relates to a specific job).         |

| description      | string   | yes      | Line description (`FTTH Activation`, etc.).                 |

| quantity         | decimal  | yes      | Typically 1 per job.                                        |

| unitPrice        | decimal  | yes      | Rate from BillingRatecard.                                  |

| lineSubtotal     | decimal  | yes      | `quantity \* unitPrice`.                                     |

| taxRate          | decimal  | yes      | 0 or 0.06 etc.                                              |

| taxAmount        | decimal  | yes      | Line-level tax.                                             |

| lineTotal        | decimal  | yes      | `lineSubtotal + taxAmount`.                                 |

| revenueCategory  | string   | no       | For P\&L breakdown (e.g. `TIME\_FTTH\_ACT`, `KSM\_HAIRCUT`).    |

| costCentreId     | uuid     | no       | FK → CostCentres.id (if used).                              |

| createdAt        | datetime | yes      | Created timestamp.                                          |



---



\## 3. Payment



\### 3.1 Table: `Payments`



| Field           | Type     | Required | Description                                        |

|-----------------|----------|----------|----------------------------------------------------|

| id              | uuid     | yes      | Primary key.                                       |

| companyId       | uuid     | yes      | FK → Companies.id.                                 |

| partnerId       | uuid     | yes      | FK → Partners.id.                                  |

| invoiceId       | uuid     | no       | FK → Invoices.id (for direct payments).            |

| amount          | decimal  | yes      | Payment amount.                                    |

| currency        | string   | yes      | `MYR`.                                             |

| paymentDate     | date     | yes      | When payment received.                             |

| method          | string   | yes      | `BankTransfer`, `FPX`, `Cash`, etc.                |

| referenceNo     | string   | no       | Bank reference, cheque number, etc.               |

| notes           | text     | no       | Additional info.                                   |

| createdByUserId | uuid     | yes      | FK → Users.id.                                     |

| createdAt       | datetime | yes      | Created timestamp.                                 |



> For complex cases, a separate `PaymentAllocations` table can be used to split one payment across many invoices.



---



\## 4. CreditNote



\### 4.1 Table: `CreditNotes`



| Field              | Type     | Required | Description                                      |

|--------------------|----------|----------|--------------------------------------------------|

| id                 | uuid     | yes      | Primary key.                                     |

| companyId          | uuid     | yes      | FK → Companies.id.                               |

| creditNoteNo       | string   | yes      | Unique number per company per year.              |

| originalInvoiceId  | uuid     | yes      | FK → Invoices.id being reversed/adjusted.        |

| issueDate          | date     | yes      | Credit note date.                                |

| reason             | string   | yes      | `RateError`, `CancelledJob`, `Duplicate`, etc.   |

| subtotalAmount     | decimal  | yes      | Net amount.                                      |

| taxAmount          | decimal  | yes      | Tax reversed.                                    |

| totalAmount        | decimal  | yes      | Total credit.                                    |

| einvoiceStatus     | enum     | no       | Same statuses as invoice.                        |

| einvoiceUuid       | string   | no       | LHDN reference.                                  |

| status             | enum     | yes      | `Draft`, `Issued`, `Applied`.                    |

| createdByUserId    | uuid     | yes      | FK → Users.id.                                   |

| createdAt          | datetime | yes      | Created timestamp.                               |



\### 4.2 Table: `CreditNoteLines`



Similar to `InvoiceLines` but negative amounts applied to original invoice lines.



---



\## 5. BillingRatecard



Defines how much to charge per order type / building / partner.



\### 5.1 Table: `BillingRatecards`



| Field           | Type     | Required | Description                                           |

|-----------------|----------|----------|-------------------------------------------------------|

| id              | uuid     | yes      | Primary key.                                          |

| companyId       | uuid     | yes      | FK → Companies.id.                                    |

| partnerId       | uuid     | yes      | FK → Partners.id.                                     |

| orderTypeId     | uuid     | yes      | FK → OrderTypes.id.                                   |

| buildingType    | string   | no       | Optional building category (e.g. `HighRise`, `Landed`).|

| description     | string   | no       | Human-readable label.                                 |

| amount          | decimal  | yes      | Charge per job.                                       |

| taxRate         | decimal  | yes      | 0 or 0.06, etc.                                       |

| isActive        | boolean  | yes      | Active/inactive.                                      |

| effectiveFrom   | date     | no       | Start date of rate validity.                          |

| effectiveTo     | date     | no       | End date (nullable = still valid).                    |

| createdAt       | datetime | yes      | Created timestamp.                                    |



---



\## 6. PartnerAccount



Additional billing-related information per partner.



\### 6.1 Table: `PartnerAccounts`



| Field            | Type     | Required | Description                      |

|------------------|----------|----------|----------------------------------|

| id               | uuid     | yes      | Primary key.                     |

| companyId        | uuid     | yes      | FK → Companies.id.               |

| partnerId        | uuid     | yes      | FK → Partners.id.                |

| billingEmail     | string   | no       | Finance email.                   |

| billingAddress   | text     | no       | Snapshot address.                |

| paymentTermsDays | int      | no       | e.g. 30, 45.                     |

| notes            | text     | no       | Special instructions.            |



---



\## 7. Cross-Module Links



\- `InvoiceLines.orderId` → Orders  

\- `Invoice` → P\&L (Revenue)  

\- `Payment` → Ageing reports  

\- `BillingRatecard` used when Orders become invoice-eligible  



See `relationships/billing\_relationships.md` and `cross\_module\_relationships.md` for ERDs.



---



\# End of Billing Entities




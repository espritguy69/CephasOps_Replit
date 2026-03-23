Below is your full, final, production-ready BILLING\_TAX\_EINVOICE\_MODULE.md document.



📌 Place this file here:

cephasops/docs/05\_modules/BILLING\_TAX\_EINVOICE\_MODULE.md



This is the complete specification covering Billing, Tax Logic, SST, MyInvois (LHDN e-Invoice), Credit Notes, Payment Allocation, Ageing, and integration with Orders + P\&L.



BILLING\_TAX\_EINVOICE\_MODULE.md



CephasOps – Billing, Tax \& e-Invoicing Module

Version 2.0 (Final)



This module defines the end-to-end financial pipeline for all four companies in CephasOps:



Cephas Sdn. Bhd



Cephas Trading \& Services



Kingsman Classic Services



Menorah Travel \& Tours



It covers how invoices are generated, validated, taxed, e-invoiced, paid, and reported.



This is a pure specification — no coding — intended for Cursor AI to implement precisely.



1\. Purpose of the Billing System



The Billing module ensures:



✔ Accurate, consistent invoicing

✔ Automated invoice creation from Orders

✔ Multi-company isolation

✔ Partner-specific billing rules

✔ Compliant tax handling (SST / Zero-rated)

✔ Seamless MyInvois submission + QR validation

✔ Proper payment allocation

✔ Locking rules to protect accounting integrity

✔ Inputs to P\&L and Payroll



It replaces Excel-based invoices, manual uploads, and scattered finance workflows.



2\. What This Module Covers



Invoice Types



Invoice Generation Rules



Tax Rules (SST)



e-Invoice / MyInvois Integration



Credit Notes



Receipt \& Payments



Ageing \& Statement of Account



Numbering Rules



File Storage Rules



Integration with Orders, Inventory, Payroll, P\&L



3\. Document Outputs of Billing Module



Billing module automatically produces:



Tax Invoice (PDF)



Credit Note (PDF)



Statement of Account



e-Invoice JSON Payload



Validation QR Code



Audit trail logs



All documents follow the company’s branding and numbering rules.



4\. Invoice Types Supported

4.1 Standard Invoice



For most jobs (ISP, Barbershop, Travel).



4.2 Principal Invoice (TIME, Telcos)



Generated in batch from multiple orders.



4.3 Assurance Invoice



Special rate based on TTKT/AWO and partner rules.



4.4 Modification / Relocation Invoice



Different rate depending on:



Old address



New address



Materials used



4.5 Kingsman POS Invoice



Future implementation for walk-in customers.



4.6 Travel Invoice



Used by Menorah (trip packages, booking fees, deposits).



5\. Billing Lifecycle Overview



The billing workflow follows this flow:



Order Completed 

&nbsp;    ↓

Dockets Uploaded \& Validated 

&nbsp;    ↓

Invoice Eligible 

&nbsp;    ↓

Invoice Created 

&nbsp;    ↓

e-Invoice (MyInvois) Submitted 

&nbsp;    ↓

Invoice Locked 

&nbsp;    ↓

Partner Pays 

&nbsp;    ↓

Payment Applied 

&nbsp;    ↓

P\&L Updated





No steps can be skipped.



6\. Invoice Generation Rules

6.1 Prerequisites for Invoice Creation



An order becomes InvoiceEligible when:



✔ Order status = Completed

✔ SI photos uploaded

✔ Serial numbers validated

✔ Docket uploaded

✔ No unresolved blocker

✔ Materials cost confirmed

✔ Partner billing rules satisfied



If any prerequisite fails → InvoiceBlocked.



6.2 Rate Card Logic



Rate card depends on:



Company



Partner



Order type



Building type



SI classification (junior/senior/subcon)



Special partner instructions (TIME/ Celcom/ Digi)



Ratecard is stored in Settings → BillingRatecards.



The invoice line amount =



RatecardAmount \* Quantity (always 1 per job unless batch)



6.3 Invoice Line Structure



Each invoice line includes:



Description



Order ID



Service ID



Building



Rate type



Amount



Tax %



Total



7\. Tax (SST) Logic



Each company has its own tax profile:



Company	Tax Status

Cephas Sdn. Bhd	SST-Registered (Services)

Cephas Trading	Non-SST (Zero-rated)

Kingsman Classic	Non-SST (retail)

Menorah Travel	Zero-rated (travel services)



Tax engine rules:



7.1 Tax Calculation



SST is applied only if company is SST-registered.



TaxCode = SR (Service Tax)



Rate = 6% (current Malaysia SST service rate)



taxAmount = invoiceAmount \* 6%



7.2 Zero-Rated or Exempt



Companies without SST must always show:



Tax: 0%

TaxCode: ZR



7.3 Tax Summary



Invoice footer must show:



SST No (if applicable)



Tax amount



Tax code



Total with tax



8\. e-Invoice (MyInvois / LHDN) Specification



CephasOps must integrate with:



Malaysia LHDN MyInvois API (e-Invoicing)



8.1 When e-Invoice is submitted



Immediately after invoice creation:



Generate invoice PDF



Generate JSON payload for LHDN



Submit to MyInvois



Receive Validation response



Store:



UUID



QR Code



IRBM Status



Timestamp



Lock invoice (immutable)



8.2 e-Invoice Required Fields



Mandatory MyInvois fields:



Supplier details (company profile)



Customer/Partner details



Invoice date



Line items



Tax breakdown



SST tags (if applicable)



Total amounts



QR validation hash



All fields must follow MyInvois official schema.



8.3 Submission Status Flow

Draft → Submitted → Validating → Validated → Locked

&nbsp;         ↑

&nbsp;       Error → Retry





If submission fails, system shows reason and allows retry.



9\. Credit Notes



Credit Notes must be issued for:



Wrong rate applied



Duplicate invoice



Cancelled order after invoicing



Partner disputes



Rules:



Must reference original invoice



Must produce e-Invoice for credit note



Cannot exceed original invoice value



Appears in Ageing \& P\&L



10\. Payments Management

10.1 Payment Recording



Payment object includes:



InvoiceId



Amount



Date



Method (bank transfer/FPX/cash)



Reference No



Note



Payments reduce outstanding balance.



10.2 Partial Payment



Allowed.



Balance must recalculate immediately.



10.3 Overpaid Payment



Creates credit balance (advance payment).



10.4 Payment Locks



When invoice is fully paid → Locked.



11\. Ageing \& Statement of Account

11.1 Ageing Buckets



Current



1–30 days



31–60 days



61–90 days



90 days



Generated per partner.



11.2 Statement of Account (SOA)



Contains:



Open invoices



Payments



Credit notes



Current outstanding



Ageing summary



SOA is produced monthly or on-demand.



12\. Numbering Rules



Each company uses its own sequence:



Invoices

INV-<COMPANY>-YYYY-#####  

e.g. INV-CEPHAS-2025-000154



Credit Notes

CN-<COMPANY>-YYYY-#####



Payment Receipt

RCPT-<COMPANY>-YYYY-#####





Sequences reset each year.



13\. File Storage Structure



All billing documents stored here:



files/{companyId}/billing/{year}/{month}/invoice-{invoiceId}.pdf

files/{companyId}/billing/{year}/{month}/creditnote-{id}.pdf

files/{companyId}/billing/{year}/{month}/receipt-{id}.pdf

files/{companyId}/billing/einvoice/{invoiceId}.json

files/{companyId}/billing/einvoice/{invoiceId}-qr.png





All validated files are immutable.



14\. Integration Logic With Other Modules

14.1 Orders → Billing



Order must reach InvoiceEligibility before invoicing.



14.2 Inventory → Billing



Materials used feed into P\&L, not invoice amount.



14.3 Payroll → Billing



SI performance affects payroll, not invoices.



14.4 P\&L → Billing



Invoices provide Revenue.

Billing does not compute profit; P\&L does.



14.5 Scheduler → Billing



Scheduler ensures appointment correctness but does not affect billing amount.



15\. Audit Logs



Billing actions must log:



Invoice creation



Partner uploaded date



e-Invoice submission



Payment received



Credit note issued



Corrections attempted



Numbering changes



All logs must be immutable.



16\. Multi-Company Behaviour



Each company has:



Separate invoice sequences



Separate tax rules



Separate MyInvois API keys



Separate customers/partners



Isolated invoices \& payments



Different billable partners



A user switching companies changes:



Invoice templates



Rates



Tax profiles



Approval flows



No mixing allowed.



17\. Triggers \& Automation

Automated:



✔ Auto-create invoice after completion \& validation

✔ Auto-generate e-Invoice payload

✔ Auto-submit to MyInvois

✔ Auto-lock invoice after validation

✔ Auto-notify finance for failed e-Invoice

✔ Auto-update P\&L



Optional:



✔ Auto-email invoice PDF to partners

✔ Auto-SOA every month

✔ Auto-ageing report every Monday



18\. Error Handling



Common errors:



Missing SST No



Partner address missing



Failed MyInvois submission



Invalid ratecard



Docket missing



Serial mismatches



Order not fully validated



System must show:



Error reason



Blocked invoices list



Suggested correction



19\. Summary



The Billing, Tax \& e-Invoice module:



Centralises all invoicing



Ensures tax compliance



Integrates with MyInvois



Protects financial integrity



Produces clear audit trails



Powers revenue for P\&L



Operates within multi-company boundaries



This module is essential for automation, compliance, and profitability across the Cephas group.


# USER\_FLOWS.md

\# USER\_FLOWS

CephasOps – Key ISP User Flows



This document sits between Storybook narratives and UI wireframes.



---



\## 1. TIME Activation → Completed → Paid



Actors: Partner, Admin, Scheduler, SI, Warehouse, Finance.



Steps:



1\. Parser creates Pending Order.

2\. Admin reviews order details.

3\. Scheduler assigns SI in calendar.

4\. SI executes job via SI PWA (OnTheWay → MetCustomer → Completed).

5\. SI submits docket; Admin receives \& uploads.

6\. Finance generates invoice, uploads to portal.

7\. Admin records Submission ID; invoice status tracked until Paid.



Each step mapped to:



\- Storybook: `STORYBOOK\_V1.md` – Story 1.

\- Pages: `/orders`, `/scheduler`, `/invoices`.

\- APIs: `API\_BLUEPRINT.md`.



---



\## 2. Assurance TTKT Flow



1\. Parser classifies Assurance email, creates Assurance order.

2\. Scheduler prioritises as high-urgency.

3\. SI performs troubleshooting; may replace ONU/router.

4\. Faulty router recorded; RMA request created if needed.

5\. If billable, docket/invoice flow same as activation; else recorded as no-charge for KPI.



---



\## 3. Docket KPI



\- Start: `OrderCompletedAt`.

\- Docket KPI: `DocketsReceivedAt` - `OrderCompletedAt` ≤ configured threshold (e.g., 30 minutes).

\- SI performance \& payroll may use this KPI.



---



\## 4. SI Performance \& Payroll



\- Monthly view:

&nbsp; - Jobs completed

&nbsp; - On-time arrival %

&nbsp; - Docket KPI %

\- Payroll module uses:

&nbsp; - Base rate per order type

&nbsp; - KPI multipliers.



UI mapping:



\- SI PWA `/profile`

\- Admin `/settings` → SI Rates

\- `/pnl` \& `/reports` for aggregated metrics.




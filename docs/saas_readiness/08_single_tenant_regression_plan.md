# Single-Tenant Regression Preservation Plan

**Date:** 2026-03-13

Defines how to verify that **each tenant still behaves like the original single-company CephasOps flow**. The goal is to ensure that converting to multi-tenant SaaS did not break the core operational behaviour that one company expects.

---

## 1. Scope

The following flows must behave identically for a single tenant as they did in the pre-SaaS single-company model (aside from intentional multi-tenant additions such as company context in JWT and tenant-scoped lists):

- Order lifecycle (draft → completed → invoiced → payment)
- Assignment and scheduling
- SI (service installer) execution
- Blockers and reschedules
- Docket handling
- Invoice generation and MyInvois
- Payroll impact
- Reporting visibility

---

## 2. Normal order lifecycle

| Step | What to verify | How to verify | Pass criterion |
|------|----------------|---------------|----------------|
| **Draft creation** | Parsed draft or manual draft creates order with correct company; order has CompanyId. | Create draft (parser or manual); load order; assert CompanyId = current tenant. | Order created; status and fields correct. |
| **Enrichment** | Admin can set Service ID, partner, customer, address, building, appointment for the order. | Enrich order; save; reload. | All fields persist; order remains in correct tenant. |
| **Assignment** | Order can be assigned to an SI; slot can be set; assignment is stored and visible. | Assign order to SI; set slot; view in scheduler and SI app. | Assignment and slot visible; SI sees job in SI app. |
| **Status progression** | Order can move: Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted (and side paths: Blocker, Reschedule). | Execute transitions via API or workflow; verify status and event store. | Each transition succeeds; event store has entry with correct CompanyId. |
| **Order completed** | SI can complete with splitter, port, ONU serial, photos, signature where required. | Complete from SI app or API with required data. | Order in OrderCompleted; splitter/port/serial stored; no cross-tenant data. |
| **Dockets** | Dockets received and verified; rejected path allows correction; accepted path allows upload. | Mark dockets received; verify; reject or accept; upload to partner (or simulate). | Docket workflow completes; order moves to DocketsUploaded or ReadyForInvoice as designed. |
| **Invoice** | Invoice can be generated from order (BOQ, rate card, materials); PDF/document generated. | Generate invoice for order; check PDF and line items. | Invoice and lines correct for that order and tenant. |
| **MyInvois** | Invoice can be submitted to MyInvois; status polled (or mocked). | Submit invoice; poll status. | Submission and status tied to tenant’s credentials and data. |
| **Payment** | Payment can be recorded and matched to invoice; order can move to Completed. | Record payment; mark order Completed. | Order in Completed; balance updated; no wrong-tenant payment. |

**Regression risk:** Any step that previously worked in single-company mode but now fails (e.g. 403, missing data, wrong company) or that writes to another tenant is a regression.

---

## 3. Assignment and scheduling

| Step | What to verify | How to verify | Pass criterion |
|------|----------------|---------------|----------------|
| **Slot list** | Scheduler shows slots for the tenant’s SIs and departments only. | Open scheduler; filter by department; assign slot. | Slots and SIs belong to current tenant. |
| **Calendar** | Calendar view shows only tenant’s assignments and availability. | GET calendar API or UI; run scheduler-utilization report. | No other tenant’s data. |
| **SI availability** | SI availability and leave requests are tenant-scoped. | Create leave request; view availability. | Only that tenant’s SIs and slots. |
| **Assignment persistence** | After assignment, order appears in SI app for that SI; unassign or reassign works. | Assign; check SI app; reassign; check again. | SI app list and detail correct. |

**Regression risk:** Scheduler or calendar returns empty or wrong data; assignment does not show in SI app; department scope broken.

---

## 4. SI execution

| Step | What to verify | How to verify | Pass criterion |
|------|----------------|---------------|----------------|
| **Job list** | SI sees only their assigned jobs for their company. | Login SI app as SI of tenant A; list jobs. | List matches assigned jobs for that SI in tenant A. |
| **Job detail** | Opening a job shows correct order, customer, address, materials, checklist. | Open job; verify all fields. | Data correct and complete. |
| **Transitions** | SI can perform allowed transitions (e.g. Start → OnTheWay → MetCustomer → OrderCompleted). | Execute each transition; provide required data (splitter, port, serial, photos where needed). | Transitions succeed; order status and event store updated. |
| **Photos** | SI can upload photos for the order/session. | Upload photo; verify stored and linked to order. | Photo stored with correct order and tenant. |
| **Checklist** | Checklist answers can be submitted; completion rules apply. | Submit checklist for status; complete. | Answers saved; workflow respects checklist. |
| **Earnings view** | SI can view own earnings (if applicable). | Open earnings page in SI app. | Data for that SI and tenant only. |

**Regression risk:** SI app shows wrong jobs, transitions fail, or data from another tenant appears.

---

## 5. Blockers and reschedules

| Step | What to verify | How to verify | Pass criterion |
|------|----------------|---------------|----------------|
| **Blocker** | Order can move to Blocker with reason and evidence; override to Completed requires HOD/SuperAdmin with reason. | Set Blocker; attempt override without permission (403); with permission, override with reason. | Blocker state and override rules work. |
| **Reschedule** | Reschedule pending approval path works; same-day reschedule requires evidence. | Request reschedule; approve or reject; same-day with evidence. | Reschedule workflow and evidence requirement enforced. |
| **Audit** | Blocker and reschedule create audit/event entries with correct CompanyId. | Check event store or audit for blocker/reschedule events. | Entries exist and have correct tenant. |

**Regression risk:** Blocker or reschedule flow broken; events written to wrong tenant.

---

## 6. Docket handling

| Step | What to verify | How to verify | Pass criterion |
|------|----------------|---------------|----------------|
| **Dockets received** | Admin can mark dockets received for the order. | Mark dockets received. | Status and history updated. |
| **Verify** | Admin can verify docket (accept or reject). | Verify; reject with reason; SI corrects; verify again. | Reject/accept and correction path work. |
| **Upload** | Verified dockets can be uploaded to partner (TIME or mock). | Upload; confirm in system. | Upload tied to order and tenant. |
| **Visibility** | Docket data and attachments are tenant-scoped. | View docket; download attachment. | Only that tenant’s dockets. |

**Regression risk:** Docket workflow or attachment access broken; wrong tenant’s docket visible.

---

## 7. Invoice generation

| Step | What to verify | How to verify | Pass criterion |
|------|----------------|---------------|----------------|
| **Rate card** | Invoice uses tenant’s rate card and billing rules. | Generate invoice; check lines and totals. | Correct rates and tax for tenant. |
| **Materials** | Invoice line items and materials match order and tenant’s catalog. | Generate; compare to order and material list. | No cross-tenant materials or rates. |
| **Document** | PDF or document generated from tenant’s template. | Generate; open PDF. | Template and data for tenant. |
| **MyInvois submit** | Submission uses tenant’s credentials and tax registration. | Submit; check submission history. | Submission record and status for tenant. |

**Regression risk:** Invoice or MyInvois uses wrong tenant’s data or credentials.

---

## 8. Payroll impact

| Step | What to verify | How to verify | Pass criterion |
|------|----------------|---------------|----------------|
| **Job completion** | Completed order is included in SI’s payroll for that tenant. | Complete order; run payroll for period; check payroll line for that SI. | Payroll line reflects job and rate for tenant. |
| **Rates** | SI rate and KPI adjustments are from tenant’s rate profile. | Compare payroll line to tenant’s rate card and KPI rules. | Correct rate and adjustments. |
| **P&L** | Order contributes to tenant’s P&L only. | Run P&L for tenant; include period with completed order. | P&L row for order; no other tenant’s orders. |

**Regression risk:** Payroll or P&L includes wrong tenant’s orders or rates.

---

## 9. Reporting visibility

| Step | What to verify | How to verify | Pass criterion |
|------|----------------|---------------|----------------|
| **Orders list report** | Report shows only tenant’s orders; filters work. | Run orders-list report with filters. | Rows and counts for tenant only. |
| **Stock / ledger** | Stock summary and ledger show only tenant’s inventory and movements. | Run stock-summary and ledger reports. | Data tenant-scoped. |
| **Scheduler utilization** | Utilization report shows only tenant’s slots and SIs. | Run scheduler-utilization. | Same. |
| **Export** | Exports (csv, xlsx, pdf) contain only tenant’s data. | Export each report type; parse and check. | Same. |
| **Dashboard** | Dashboard KPIs and recent activity are tenant-scoped. | Open dashboard; check counts and lists. | Same. |

**Regression risk:** Any report or dashboard shows empty data (scope too strict) or another tenant’s data (scope missing).

---

## 10. Execution approach

- **Per tenant:** Run the above for at least one “primary” tenant (e.g. Cephas) and one “new” tenant (e.g. second company) to ensure both behave like the original single-company flow.
- **Evidence:** Record order IDs, status progressions, invoice numbers, and one payroll/P&L snapshot per tenant.
- **Failure:** Any failure in sections 2–9 that did not exist in single-company mode is a **single-tenant regression** and should be fixed before declaring SaaS-ready, unless documented as known limitation with a workaround.

This plan aligns with [02_manual_uat_plan.md](02_manual_uat_plan.md) Stage 4 and Stage 5 and with [01_master_checklist.md](01_master_checklist.md) section 2.
